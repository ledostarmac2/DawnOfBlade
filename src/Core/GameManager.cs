using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DawnOfBlade.Auth;
using DawnOfBlade.Characters;
using DawnOfBlade.Combat;
using DawnOfBlade.Communication;
using DawnOfBlade.Data;
using DawnOfBlade.Dialogue;
using DawnOfBlade.Interaction;
using DawnOfBlade.Inventory;
using DawnOfBlade.Items;
using DawnOfBlade.Learning;
using DawnOfBlade.Player;
using DawnOfBlade.Quests;
using DawnOfBlade.Save;
using DawnOfBlade.Shops;
using DawnOfBlade.Skills;
using DawnOfBlade.UI;
using DawnOfBlade.World;
using DawnOfBlade.World.RiverValley;
using Godot;

namespace DawnOfBlade.Core;

/// <summary>
/// Root gameplay coordinator. Loads content definitions, applies the save file, and wires
/// world interactions (gathering, crafting, dialogue, language prompts, quests, shops, combat,
/// equipment, appearance) onto the underlying engine-independent C# systems.
/// </summary>
public partial class GameManager : Node3D
{
    private const string MainQuestId = "first_words";
    private const string CollectObjectiveId = "collect_sunleaf";
    private const string AnswerObjectiveId = "answer_prompt";
    private const string GatherItemId = "sunleaf";
    private const string CraftItemId = "practice_chisel";
    private const string BronzeBarItemId = "bronze_bar";
    private const string Currency = "coins";
    private static readonly Vector3 HomeSpawnPosition = Vector3.Zero;

    public bool IsInitialized { get; private set; }

    private readonly DefinitionDatabase _definitions = new();
    private readonly RiverValleyRegion _region = new();
    private readonly Inventory.Inventory _inventory = new();
    private readonly BankStorage _bank = new();
    private readonly QuestLog _questLog = new();
    private readonly SaveService _saveService = new(Session.Username);
    private readonly Equipment _equipment = new();
    private readonly System.Random _random = new();
    private readonly IRandomSource _combatRandom = new SystemRandomSource();

    // Codex built the in-process communication bus (src/Communication); this is the game-side
    // adoption. Gameplay publishes domain events here; subscribers drive HUD notices and telemetry.
    private readonly ICommunicationService _bus = new InProcessCommunicationService();
    private readonly List<System.IDisposable> _subscriptions = new();
    private readonly List<string> _pendingLevelUps = new();

    private readonly Dictionary<string, SkillProgress> _skills = new()
    {
        ["foraging"] = new SkillProgress("foraging"),
        ["crafting"] = new SkillProgress("crafting"),
        ["language"] = new SkillProgress("language"),
        ["attack"] = new SkillProgress("attack"),
        ["strength"] = new SkillProgress("strength"),
        ["defense"] = new SkillProgress("defense"),
        ["hitpoints"] = new SkillProgress("hitpoints"),
    };

    private readonly HashSet<string> _unlockedVocabulary = new();

    private Appearance _appearance = new();
    private CombatProfile _playerProfile = new(1, 1, 1, 10);
    private AttackStyle _attackStyle = AttackStyle.Aggressive;

    private QuestState? _mainQuest;
    private VocabularyEntry? _currentPrompt;
    private ShopStock? _openShop;
    private string _shopMessage = string.Empty;

    private Label? _statusLabel;
    private Label? _questLabel;
    private Button? _styleButton;
    private PanelContainer? _dialoguePanel;
    private VBoxContainer? _dialogueContent;
    private ScrollContainer? _dialogueScroll;
    private ProgressBar? _healthBar;
    private ProgressBar? _runEnergyBar;
    private Label? _healthLabel;
    private Label? _runEnergyLabel;
    private Label? _heartbeatLabel;
    private Label? _coordinateLabel;
    private MiniMapControl? _minimap;
    private Label? _chatLog;
    private PanelContainer? _inventoryPanel;
    private VBoxContainer? _inventoryContent;
    private FeedbackManager? _feedbackManager;
    private long _localTick;
    private bool _saveDirty;
    private bool _isFlushingSave;

    public override void _Ready()
    {
        var isFreshCharacter = !_saveService.SaveExists;
        InitializeSystems();
        WireCommunication();
        BuildPrototypeHud();
        _feedbackManager = GetNodeOrNull<FeedbackManager>("FeedbackManager");

        if (!isFreshCharacter)
        {
            ApplySave(_saveService.Load());
        }
        else
        {
            _inventory.Add(Currency, 50);
        }

        RebuildPlayerProfile();
        ApplyPlayerAppearance();
        RefreshStatus();
        SaveProgress(showNotice: false);

        var autosave = new Timer { WaitTime = 3.0, Autostart = true };
        autosave.Timeout += FlushSaveIfDirty;
        AddChild(autosave);

        var localHeartbeat = new Timer { WaitTime = 0.6, Autostart = true };
        localHeartbeat.Timeout += ProcessLocalTick;
        AddChild(localHeartbeat);

        if (isFreshCharacter)
        {
            ShowCharacterCreator();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest && IsInitialized)
        {
            FlushSave(force: true);
        }

        if (what == NotificationPredelete)
        {
            FlushSave(force: true);
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
    }

    public override void _ExitTree()
    {
        FlushSave(force: true);
    }

    private void InitializeSystems()
    {
        _definitions.Load();

        if (_definitions.QuestById.TryGetValue(MainQuestId, out var questDefinition))
        {
            _mainQuest = _questLog.Start(questDefinition);
        }

        IsInitialized = true;
        GD.Print($"Dawn of Blade core systems initialized for {Session.Username ?? "guest"}.");
    }

    // ---- Communication bus ------------------------------------------------

    /// <summary>
    /// Subscribes the game to the domain events it publishes. Level-ups surface as RuneScape-style
    /// notices; gather/defeat events are logged so future systems (quests, telemetry, a server
    /// adapter) can observe the same stream without GameManager knowing about them.
    /// </summary>
    private void WireCommunication()
    {
        _subscriptions.Add(_bus.Subscribe<SkillLeveledUp>((envelope, _) =>
        {
            var message = envelope.Message;
            _pendingLevelUps.Add($"Congratulations! Your {SkillDisplayName(message.SkillId)} level is now {message.Level}.");
            return ValueTask.CompletedTask;
        }));

        _subscriptions.Add(_bus.Subscribe<ResourceGathered>((envelope, _) =>
        {
            GD.Print($"[event] ResourceGathered {envelope.Message.ItemId} (+{envelope.Message.Experience} {envelope.Message.SkillId} xp)");
            return ValueTask.CompletedTask;
        }));

        _subscriptions.Add(_bus.Subscribe<EnemyDefeated>((envelope, _) =>
        {
            GD.Print($"[event] EnemyDefeated {envelope.Message.EnemyName} (+{envelope.Message.CoinReward} coins)");
            return ValueTask.CompletedTask;
        }));
    }

    // ---- Gathering / crafting --------------------------------------------

    public void GatherResource(ResourceNode node)
    {
        if (node.IsDepleted)
        {
            return;
        }

        _inventory.Add(node.ItemId);
        node.Deplete(_localTick);
        AddSkillExperience(node.SkillId, node.Experience);
        _ = _bus.PublishAsync(new ResourceGathered(node.ItemId, node.SkillId, node.Experience));

        if (node.ItemId == GatherItemId)
        {
            AdvanceQuests(CollectObjectiveId);
        }

        RefreshStatus();
        ShowNotice($"You gathered {ItemName(node.ItemId)}.");
        SaveProgress(showNotice: false);
    }

    private void CraftPracticeChisel()
    {
        if (!_inventory.Remove(GatherItemId, 2))
        {
            ShowNotice($"You need 2 {ItemName(GatherItemId)} to craft a {ItemName(CraftItemId)}.");
            return;
        }

        _inventory.Add(CraftItemId);
        AddSkillExperience("crafting", 30);
        RefreshStatus();
        ShowNotice($"You crafted a {ItemName(CraftItemId)}.");
        SaveProgress(showNotice: false);
    }

    private void SmeltBronzeBar()
    {
        if (_inventory.Count("copper_ore") < 1 || _inventory.Count("tin_ore") < 1)
        {
            ShowNotice("You need 1 Copper Ore and 1 Tin Ore to smelt a Bronze Bar.");
            return;
        }

        _inventory.Remove("copper_ore");
        _inventory.Remove("tin_ore");
        _inventory.Add(BronzeBarItemId);
        AddSkillExperience("smithing", 20);
        RefreshStatus();
        ShowNotice("You smelted a Bronze Bar.");
        SaveProgress(showNotice: false);
    }

    // ---- Dialogue ---------------------------------------------------------

    public void ShowNpcDialogue(string speakerKey)
    {
        var rootId = _definitions.NpcById.TryGetValue(speakerKey, out var npc)
            ? npc.DialogueRootId
            : _definitions.DialogueById.ContainsKey(speakerKey)
                ? speakerKey
                : _definitions.NpcById.Values.FirstOrDefault()?.DialogueRootId;

        if (rootId is not null && _definitions.DialogueById.TryGetValue(rootId, out var node))
        {
            ShowDialogueNode(node);
            return;
        }

        ShowVocabularyPrompt();
    }

    private void ShowDialogueNode(DialogueNode node)
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel($"{SpeakerName(node.SpeakerId)}: {node.Text}");

        if (node.Choices.Count > 0)
        {
            foreach (var choice in node.Choices)
            {
                var nextId = choice.NextNodeId;
                var button = new Button { Text = choice.Text };
                button.Pressed += () => OnDialogueChoice(nextId);
                _dialogueContent!.AddChild(button);
            }
        }
        else if (node.Id == "mira_lesson")
        {
            var practice = new Button { Text = "Practice a word" };
            practice.Pressed += ShowVocabularyPrompt;
            _dialogueContent!.AddChild(practice);
            AddCloseButton("Goodbye");
        }
        else
        {
            AddCloseButton("Continue");
        }

        _dialoguePanel!.Visible = true;
    }

    private void OnDialogueChoice(string? nextNodeId)
    {
        if (nextNodeId is not null && _definitions.DialogueById.TryGetValue(nextNodeId, out var node))
        {
            ShowDialogueNode(node);
            return;
        }

        _dialoguePanel!.Visible = false;
    }

    // ---- Language-learning prompts ---------------------------------------

    private void ShowVocabularyPrompt()
    {
        if (_definitions.Vocabulary.Count == 0)
        {
            ShowNotice("There are no lessons available yet.");
            return;
        }

        var entry = _definitions.Vocabulary[_random.Next(_definitions.Vocabulary.Count)];
        _currentPrompt = entry;

        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel($"Lesson: What does \"{entry.Term}\" mean?");

        foreach (var option in BuildPromptOptions(entry))
        {
            var button = new Button { Text = option };
            button.Pressed += () => CallDeferred(MethodName.AnswerVocabularyPrompt, option);
            _dialogueContent!.AddChild(button);
        }

        _dialoguePanel!.Visible = true;
    }

    private void AnswerVocabularyPrompt(string answer)
    {
        var entry = _currentPrompt;
        if (entry is null)
        {
            return;
        }

        var correct = answer == entry.Translation;
        if (correct)
        {
            AddSkillExperience("language", 25);
            _unlockedVocabulary.Add(entry.Term);
            AdvanceQuests(AnswerObjectiveId);
        }

        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel(correct
            ? $"Correct! \"{entry.Term}\" means \"{entry.Translation}\"."
            : $"Not quite. \"{entry.Term}\" means \"{entry.Translation}\".");
        FlushPendingLevelUps();
        AddCloseButton("Continue");
        RefreshStatus();
        SaveProgress(showNotice: false);
    }

    private List<string> BuildPromptOptions(VocabularyEntry entry)
    {
        var options = new List<string> { entry.Translation };

        foreach (var other in _definitions.Vocabulary)
        {
            if (options.Count >= 3)
            {
                break;
            }

            if (!options.Contains(other.Translation))
            {
                options.Add(other.Translation);
            }
        }

        for (var i = options.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }

        return options;
    }

    // ---- Quests -----------------------------------------------------------

    private void AdvanceQuests(string objectiveId, int amount = 1)
    {
        foreach (var completed in _questLog.Advance(objectiveId, amount))
        {
            GrantQuestRewards(completed);
        }
    }

    private void GrantQuestRewards(QuestState quest)
    {
        if (quest.RewardsGranted)
        {
            return;
        }

        foreach (var token in quest.Definition.Rewards)
        {
            if (!QuestReward.TryParse(token, out var reward))
            {
                continue;
            }

            switch (reward.Kind)
            {
                case QuestReward.KindXp:
                    AddSkillExperience(reward.Target, reward.Amount);
                    break;
                case QuestReward.KindItem:
                    _inventory.Add(reward.Target, reward.Amount);
                    break;
            }
        }

        quest.RewardsGranted = true;
        ShowNotice($"Quest complete: {quest.Definition.Title}! Rewards granted.");
        RefreshStatus();
        SaveProgress(showNotice: false);
    }

    // ---- Shops ------------------------------------------------------------

    public void OpenShop(string shopId)
    {
        if (!_definitions.ShopById.TryGetValue(shopId, out var definition))
        {
            ShowNotice("This shop is closed.");
            return;
        }

        _openShop = new ShopStock(definition);
        _shopMessage = "Buy or sell with your coins.";
        ShowShopView();
    }

    private void ShowShopView()
    {
        if (_openShop is null)
        {
            return;
        }

        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel($"{_openShop.DisplayName}   —   Coins: {_inventory.Count(Currency)}");
        AddDialogueLabel(_shopMessage);

        foreach (var itemId in _openShop.ItemIds)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 6);

            var label = new Label
            {
                Text = $"{ItemName(itemId)}  ({_openShop.QuantityOf(itemId)} @ {_openShop.PriceOf(itemId)}c)  you: {_inventory.Count(itemId)}",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            };
            row.AddChild(label);

            var id = itemId;
            var buy = new Button { Text = "Buy" };
            buy.Pressed += () => DoBuy(id);
            row.AddChild(buy);

            var sell = new Button { Text = "Sell" };
            sell.Pressed += () => DoSell(id);
            row.AddChild(sell);

            _dialogueContent!.AddChild(row);
        }

        AddCloseButton("Leave shop");
        _dialoguePanel!.Visible = true;
    }

    private void DoBuy(string itemId)
    {
        if (_openShop is null)
        {
            return;
        }

        var (ok, message) = ShopService.Buy(_openShop, itemId, _inventory);
        if (ok && TryAutoEquip(itemId, out var equippedMessage))
        {
            message += " " + equippedMessage;
        }

        _shopMessage = message;
        ShowShopView();
        RefreshStatus();
        SaveProgress(showNotice: false);
    }

    private void DoSell(string itemId)
    {
        if (_openShop is null)
        {
            return;
        }

        (_, _shopMessage) = ShopService.Sell(_openShop, itemId, _inventory);
        ShowShopView();
        RefreshStatus();
        SaveProgress(showNotice: false);
    }

    // ---- Equipment --------------------------------------------------------

    private bool TryAutoEquip(string itemId, out string message)
    {
        message = string.Empty;
        var definition = _definitions.FindEquipment(itemId);
        if (definition is null || !System.Enum.TryParse<EquipmentSlot>(definition.Slot, ignoreCase: true, out var slot))
        {
            return false;
        }

        if (SkillLevel("attack") < definition.RequiredAttack || SkillLevel("defense") < definition.RequiredDefense)
        {
            message = $"You need Attack {definition.RequiredAttack} / Defense {definition.RequiredDefense} to use that.";
            return true;
        }

        var newScore = definition.AttackBonus + definition.StrengthBonus + definition.DefenseBonus;
        if (_equipment.ItemInSlot(slot) is { } current && _definitions.FindEquipment(current) is { } currentDefinition)
        {
            var currentScore = currentDefinition.AttackBonus + currentDefinition.StrengthBonus + currentDefinition.DefenseBonus;
            if (newScore <= currentScore)
            {
                return false;
            }
        }

        _equipment.Equip(slot, itemId);
        ApplyPlayerEquipment();
        message = $"Equipped {ItemName(itemId)}.";
        return true;
    }

    // ---- Combat -----------------------------------------------------------

    public void AttackHostile(HostileActor hostile)
    {
        if (hostile.Profile.IsDefeated)
        {
            return;
        }

        var resolver = new CombatResolver(_combatRandom);
        var playerGear = PlayerGear();
        if (GetNodeOrNull<PlayerController>("Player") is { } attackingPlayer)
        {
            attackingPlayer.FaceTowards(hostile.GlobalPosition);
            attackingPlayer.PlayAttack(_equipment.ItemInSlot(EquipmentSlot.Weapon));
        }

        var hit = resolver.Resolve(_playerProfile, playerGear, _attackStyle, hostile.Profile, EquipmentBonuses.Zero);

        var playerLine = hit.Landed
            ? (hit.Damage > 0 ? $"You hit {hit.Damage}." : "You hit, but it holds.")
            : "You miss.";

        if (hit.Landed)
        {
            hostile.Profile.ApplyDamage(hit.Damage);
        }

        _feedbackManager?.ShowDamage(hostile, hit.Damage, hit.Landed);

        if (hostile.Profile.IsDefeated)
        {
            AwardCombatExperience(hostile);
            var reward = 5 + hostile.Profile.MaxHitpoints;
            _inventory.Add(Currency, reward);
            _ = _bus.PublishAsync(new EnemyDefeated(hostile.DisplayName, reward));
            DropGroundLoot(hostile.GlobalPosition, hostile.LootItemId, hostile.LootQuantity);
            ShowNotice($"{playerLine} You defeated the {hostile.DisplayName}! +{reward} coins. It recovers shortly.");

            var timer = GetTree().CreateTimer(4.0);
            timer.Timeout += hostile.ResetStats;

            RebuildPlayerProfile();
            RefreshStatus();
            SaveProgress(showNotice: false);
            return;
        }

        var enemyHit = resolver.Resolve(hostile.Profile, EquipmentBonuses.Zero, AttackStyle.Aggressive, _playerProfile, playerGear);
        if (enemyHit.Landed)
        {
            _playerProfile.ApplyDamage(enemyHit.Damage);
        }

        if (GetNodeOrNull<PlayerController>("Player") is { } player)
        {
            _feedbackManager?.ShowDamage(player, enemyHit.Damage, enemyHit.Landed);
        }

        var enemyLine = enemyHit.Landed ? $"It hits {enemyHit.Damage}." : "It misses.";

        if (_playerProfile.IsDefeated)
        {
            ShowNotice($"{playerLine} {enemyLine} You were knocked out, but recovered in the village.");
            RebuildPlayerProfile();
        }
        else
        {
            ShowNotice($"{playerLine} {enemyLine}   You {_playerProfile.CurrentHitpoints}/{_playerProfile.MaxHitpoints} · " +
                       $"{hostile.DisplayName} {hostile.Profile.CurrentHitpoints}/{hostile.Profile.MaxHitpoints}");
        }

        RefreshStatus();
        SaveProgress(showNotice: false);
    }

    private void AwardCombatExperience(HostileActor hostile)
    {
        var baseXp = hostile.Profile.MaxHitpoints * 4;
        var styleSkill = _attackStyle switch
        {
            AttackStyle.Accurate => "attack",
            AttackStyle.Aggressive => "strength",
            _ => "defense",
        };

        AddSkillExperience(styleSkill, baseXp);
        AddSkillExperience("hitpoints", baseXp / 2);
    }

    private EquipmentBonuses PlayerGear() => _equipment.TotalBonuses(id => _definitions.FindEquipment(id));

    private void RebuildPlayerProfile() =>
        _playerProfile = new CombatProfile(
            SkillLevel("attack"),
            SkillLevel("strength"),
            SkillLevel("defense"),
            9 + SkillLevel("hitpoints"));

    private void CycleAttackStyle()
    {
        _attackStyle = _attackStyle switch
        {
            AttackStyle.Accurate => AttackStyle.Aggressive,
            AttackStyle.Aggressive => AttackStyle.Defensive,
            _ => AttackStyle.Accurate,
        };

        if (_styleButton is not null)
        {
            _styleButton.Text = $"Style: {_attackStyle}";
        }

        SaveProgress(showNotice: false);
    }

    // ---- Appearance / customization --------------------------------------

    private void RandomizeLook()
    {
        _appearance = new Appearance
        {
            BodyType = AppearanceOptions.BodyTypes[_random.Next(AppearanceOptions.BodyTypes.Length)],
            HairStyle = _random.Next(AppearanceOptions.HairStyleCount),
            SkinTone = AppearanceOptions.SkinTones[_random.Next(AppearanceOptions.SkinTones.Length)],
            HairColor = AppearanceOptions.HairColors[_random.Next(AppearanceOptions.HairColors.Length)],
            ShirtColor = AppearanceOptions.ShirtColors[_random.Next(AppearanceOptions.ShirtColors.Length)],
            LegColor = AppearanceOptions.LegColors[_random.Next(AppearanceOptions.LegColors.Length)],
        };

        ApplyPlayerAppearance();
        ShowNotice($"You changed your look (shirt {_appearance.ShirtColor}, {_appearance.BodyType}).");
        SaveProgress(showNotice: false);
    }

    private void CycleBodyType() =>
        UpdateAppearance(appearance => appearance.BodyType = Next(AppearanceOptions.BodyTypes, appearance.BodyType), "Body type");

    private void CycleSkinTone() =>
        UpdateAppearance(appearance => appearance.SkinTone = Next(AppearanceOptions.SkinTones, appearance.SkinTone), "Skin tone");

    private void CycleHairColor() =>
        UpdateAppearance(appearance => appearance.HairColor = Next(AppearanceOptions.HairColors, appearance.HairColor), "Hair color");

    private void CycleShirtColor() =>
        UpdateAppearance(appearance => appearance.ShirtColor = Next(AppearanceOptions.ShirtColors, appearance.ShirtColor), "Shirt color");

    private void UpdateAppearance(System.Action<Appearance> update, string label)
    {
        update(_appearance);
        ApplyPlayerAppearance();
        SaveProgress(showNotice: false);
        ShowNotice($"{label} updated. Your appearance was saved.");
    }

    private static string Next(string[] values, string current)
    {
        var index = System.Array.IndexOf(values, current);
        return values[(index + 1 + values.Length) % values.Length];
    }

    private void ApplyPlayerAppearance()
    {
        if (GetNodeOrNull<HumanoidVisual>("Player/Humanoid") is { } humanoid)
        {
            humanoid.Apply(_appearance);
        }

        ApplyPlayerEquipment();
    }

    private void ApplyPlayerEquipment()
    {
        if (GetNodeOrNull<HumanoidVisual>("Player/Humanoid") is not { } humanoid)
        {
            return;
        }

        humanoid.ApplyEquipment(_equipment.ItemInSlot(EquipmentSlot.Weapon), _equipment.ItemInSlot(EquipmentSlot.Shield));
        humanoid.ApplyArmor("body", _equipment.ItemInSlot(EquipmentSlot.Body));
        humanoid.ApplyArmor("legs", _equipment.ItemInSlot(EquipmentSlot.Legs));
    }

    private void ShowCharacterCreator()
    {
        AddChild(new CharacterCreatorPanel(_appearance, appearance =>
        {
            _appearance = appearance;
            ApplyPlayerAppearance();
            SaveProgress(showNotice: false);
        }));
    }

    public void PickUpGroundLoot(GroundLootNode loot)
    {
        _inventory.Add(loot.ItemId, loot.Quantity);
        ShowNotice($"You picked up {loot.Quantity} x {ItemName(loot.ItemId)}.");
        loot.QueueFree();
        RefreshStatus();
        SaveProgress(showNotice: false);
    }

    private void DropGroundLoot(Vector3 position, string itemId, int quantity)
    {
        var loot = new GroundLootNode
        {
            DisplayName = $"{quantity} x {ItemName(itemId)}",
            ItemId = itemId,
            Quantity = quantity,
            ExpiresAtTick = _localTick + 200,
            Position = position,
        };
        loot.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.34f, BottomRadius = 0.34f, Height = 0.08f },
            Position = Vector3.Up * 0.08f,
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#d9ad42") },
        });
        loot.AddChild(new CollisionShape3D { Shape = new CylinderShape3D { Radius = 0.42f, Height = 0.18f } });
        AddChild(loot);
    }

    // ---- Save / Load ------------------------------------------------------

    private void SaveProgress(bool showNotice = true)
    {
        _saveDirty = true;
        if (showNotice)
        {
            FlushSave(force: true, showNotice: true);
        }
    }

    private void FlushSaveIfDirty() => FlushSave(force: false);

    private void FlushSave(bool force, bool showNotice = false)
    {
        if (_isFlushingSave || (!force && !_saveDirty) || !IsInitialized)
        {
            return;
        }

        _isFlushingSave = true;
        var saved = false;
        try
        {
            saved = _saveService.Save(CreateSaveGame());
        }
        finally
        {
            _isFlushingSave = false;
        }

        if (saved)
        {
            _saveDirty = false;
        }

        if (showNotice)
        {
            ShowNotice(saved ? "Progress saved." : "Could not save progress.");
        }
    }

    private SaveGame CreateSaveGame()
    {
        var save = new SaveGame
        {
            PlayerName = Session.Username ?? "Player",
            PlayerPosition = PlayerPositionArray(),
            Inventory = _inventory.Items.ToDictionary(pair => pair.Key, pair => pair.Value),
            Bank = _bank.Items.ToDictionary(pair => pair.Key, pair => pair.Value),
            SkillExperience = _skills.ToDictionary(pair => pair.Key, pair => pair.Value.Experience),
            UnlockedVocabularyIds = new HashSet<string>(_unlockedVocabulary),
            Equipment = _equipment.Worn.ToDictionary(pair => pair.Key.ToString(), pair => pair.Value),
            Appearance = _appearance.Clone(),
        };

        if (_mainQuest is not null)
        {
            if (_mainQuest.IsComplete)
            {
                save.CompletedQuestIds.Add(_mainQuest.Definition.Id);
            }

            save.QuestProgress[_mainQuest.Definition.Id] =
                _mainQuest.Progress.ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        return save;
    }

    private void ApplySave(SaveGame save)
    {
        foreach (var pair in save.Inventory)
        {
            _inventory.Add(pair.Key, pair.Value);
        }

        _bank.Load(save.Bank);

        foreach (var pair in save.SkillExperience)
        {
            _skills[pair.Key] = new SkillProgress(pair.Key, pair.Value);
        }

        foreach (var term in save.UnlockedVocabularyIds)
        {
            _unlockedVocabulary.Add(term);
        }

        foreach (var pair in save.Equipment)
        {
            if (System.Enum.TryParse<EquipmentSlot>(pair.Key, ignoreCase: true, out var slot))
            {
                _equipment.Equip(slot, pair.Value);
            }
        }

        ApplyPlayerEquipment();

        _appearance = save.Appearance ?? new Appearance();

        if (_definitions.QuestById.TryGetValue(MainQuestId, out var questDefinition))
        {
            save.QuestProgress.TryGetValue(MainQuestId, out var progress);
            _mainQuest = _questLog.Start(questDefinition, progress);
            if (_mainQuest.IsComplete)
            {
                _mainQuest.RewardsGranted = true;
            }
        }

        if (save.PlayerPosition.Length == 3 && GetNodeOrNull<Node3D>("Player") is { } player)
        {
            player.GlobalPosition = new Vector3(save.PlayerPosition[0], save.PlayerPosition[1], save.PlayerPosition[2]);
        }
    }

    private float[] PlayerPositionArray()
    {
        if (GetNodeOrNull<Node3D>("Player") is { } player && player.IsInsideTree())
        {
            var position = player.GlobalPosition;
            return new[] { position.X, position.Y, position.Z };
        }

        return new[] { 0.0f, 0.0f, 0.0f };
    }

    // ---- Skills helpers ---------------------------------------------------

    private int SkillLevel(string skillId) =>
        _skills.TryGetValue(skillId, out var skill) ? skill.Level : 1;

    private void AddSkillExperience(string skillId, int amount)
    {
        if (!_skills.TryGetValue(skillId, out var skill))
        {
            skill = new SkillProgress(skillId);
            _skills[skillId] = skill;
        }

        var levelBefore = skill.Level;
        skill.AddExperience(amount);
        var levelAfter = skill.Level;

        // Announce each level gained through the bus (handles multi-level jumps).
        for (var level = levelBefore + 1; level <= levelAfter; level++)
        {
            _ = _bus.PublishAsync(new SkillLeveledUp(skillId, level));
        }

        if (skillId is "attack" or "strength" or "defense" or "hitpoints")
        {
            RebuildPlayerProfile();
        }
    }

    private string ItemName(string itemId) =>
        _definitions.ItemById.TryGetValue(itemId, out var item) ? item.DisplayName : itemId;

    private string SkillDisplayName(string skillId) =>
        _definitions.SkillById.TryGetValue(skillId, out var skill) ? skill.DisplayName : skillId;

    private string SpeakerName(string speakerId) =>
        _definitions.NpcById.TryGetValue(speakerId, out var npc) ? npc.DisplayName : speakerId;

    // ---- HUD --------------------------------------------------------------

    private void BuildPrototypeHud()
    {
        var ui = new CanvasLayer { Name = "RealmHud" };
        AddChild(ui);

        var root = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        ui.AddChild(root);

        var statusPanel = MakeHudPanel();
        statusPanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        statusPanel.SetAnchor(Side.Right, 0.70f);
        statusPanel.OffsetLeft = 12;
        statusPanel.OffsetTop = 12;
        statusPanel.OffsetRight = -12;
        statusPanel.OffsetBottom = 184;
        root.AddChild(statusPanel);

        var statusColumn = MakeHudColumn(statusPanel, 8);
        _statusLabel = MakeHudLabel(string.Empty, 15);
        statusColumn.AddChild(_statusLabel);
        _questLabel = MakeHudLabel(string.Empty, 13);
        _questLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        statusColumn.AddChild(_questLabel);

        var vitals = new GridContainer { Columns = 2 };
        vitals.AddThemeConstantOverride("h_separation", 8);
        vitals.AddThemeConstantOverride("v_separation", 4);
        statusColumn.AddChild(vitals);
        _healthLabel = MakeHudLabel("Health", 12);
        _healthBar = MakeHudBar();
        _runEnergyLabel = MakeHudLabel("Run energy", 12);
        _runEnergyBar = MakeHudBar();
        vitals.AddChild(_healthLabel);
        vitals.AddChild(_healthBar);
        vitals.AddChild(_runEnergyLabel);
        vitals.AddChild(_runEnergyBar);

        var sidebar = MakeHudPanel();
        sidebar.SetAnchorsPreset(Control.LayoutPreset.TopRight);
        sidebar.SetAnchor(Side.Left, 0.72f);
        sidebar.SetAnchor(Side.Bottom, 1.0f);
        sidebar.OffsetLeft = 0;
        sidebar.OffsetRight = -12;
        sidebar.OffsetTop = 12;
        sidebar.OffsetBottom = -12;
        root.AddChild(sidebar);

        var sidebarColumn = MakeHudColumn(sidebar, 8);
        var minimap = MakeHudPanel();
        minimap.CustomMinimumSize = new Vector2(0, 194);
        sidebarColumn.AddChild(minimap);
        _minimap = new MiniMapControl();
        _minimap.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _minimap.SetTrackedPlayer(GetNodeOrNull<Node3D>("Player"));
        minimap.AddChild(_minimap);

        var locationRow = new HBoxContainer();
        sidebarColumn.AddChild(locationRow);
        _coordinateLabel = MakeHudLabel("Tile: 0, 0", 12);
        _coordinateLabel.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        locationRow.AddChild(_coordinateLabel);
        _heartbeatLabel = MakeHudLabel("Tick: 0", 12, HorizontalAlignment.Right);
        locationRow.AddChild(_heartbeatLabel);

        var tabs = new GridContainer { Columns = 3 };
        tabs.AddThemeConstantOverride("h_separation", 4);
        tabs.AddThemeConstantOverride("v_separation", 4);
        sidebarColumn.AddChild(tabs);
        AddHudButton(tabs, "Pack", ToggleInventory);
        AddHudButton(tabs, "Bank", ShowBankView);
        AddHudButton(tabs, "Skills", ShowSkillsView);
        AddHudButton(tabs, "Magic", ShowMagicView);
        AddHudButton(tabs, "Quests", ShowQuestsView);
        AddHudButton(tabs, "Social", ShowSocialView);
        AddHudButton(tabs, "Settings", ShowSettingsView);

        _inventoryPanel = MakeHudPanel();
        _inventoryPanel.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        sidebarColumn.AddChild(_inventoryPanel);
        var inventoryScroll = new ScrollContainer();
        inventoryScroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _inventoryPanel.AddChild(inventoryScroll);
        _inventoryContent = new VBoxContainer();
        _inventoryContent.AddThemeConstantOverride("separation", 4);
        inventoryScroll.AddChild(_inventoryContent);

        var quickActions = new GridContainer { Columns = 3 };
        quickActions.AddThemeConstantOverride("h_separation", 4);
        quickActions.AddThemeConstantOverride("v_separation", 4);
        sidebarColumn.AddChild(quickActions);
        AddHudButton(quickActions, "Craft", CraftPracticeChisel);
        AddHudButton(quickActions, "Smelt", SmeltBronzeBar);
        AddHudButton(quickActions, "Home", CastHomeTeleport);
        AddHudButton(quickActions, "Run", ToggleRun);
        _styleButton = AddHudButton(quickActions, $"Style: {_attackStyle}", CycleAttackStyle);
        AddHudButton(quickActions, "Customize", ShowCharacterCreator);
        AddHudButton(quickActions, "Save", () => SaveProgress());

        var chatPanel = MakeHudPanel();
        chatPanel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        chatPanel.SetAnchor(Side.Right, 0.70f);
        chatPanel.OffsetLeft = 12;
        chatPanel.OffsetTop = -138;
        chatPanel.OffsetRight = -12;
        chatPanel.OffsetBottom = -12;
        root.AddChild(chatPanel);
        var chatColumn = MakeHudColumn(chatPanel, 6);
        _chatLog = MakeHudLabel("Welcome to River Valley. Click the ground to travel and select nearby people or resources to interact.", 13);
        _chatLog.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _chatLog.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        chatColumn.AddChild(_chatLog);
        var chatInput = new LineEdit { PlaceholderText = "Press Enter to chat locally..." };
        chatInput.TextSubmitted += text =>
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                _chatLog.Text = $"{Session.Username ?? "Adventurer"}: {text.Trim()}";
                chatInput.Clear();
            }
        };
        chatColumn.AddChild(chatInput);

        _dialoguePanel = MakeHudPanel();
        _dialoguePanel.Visible = false;
        _dialoguePanel.SetAnchor(Side.Left, 0.08f);
        _dialoguePanel.SetAnchor(Side.Top, 0.18f);
        _dialoguePanel.SetAnchor(Side.Right, 0.68f);
        _dialoguePanel.SetAnchor(Side.Bottom, 0.78f);
        root.AddChild(_dialoguePanel);
        _dialogueScroll = new ScrollContainer();
        _dialogueScroll.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _dialoguePanel.AddChild(_dialogueScroll);
        _dialogueContent = new VBoxContainer();
        _dialogueContent.AddThemeConstantOverride("separation", 6);
        _dialogueScroll.AddChild(_dialogueContent);
    }

    private static PanelContainer MakeHudPanel()
    {
        var panel = new PanelContainer();
        var box = new StyleBoxFlat
        {
            BgColor = new Color(0.055f, 0.045f, 0.035f, 0.94f),
            BorderColor = new Color(0.57f, 0.43f, 0.22f),
        };
        box.SetBorderWidthAll(2);
        box.SetCornerRadiusAll(5);
        panel.AddThemeStyleboxOverride("panel", box);
        return panel;
    }

    private static VBoxContainer MakeHudColumn(Container parent, int separation)
    {
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        parent.AddChild(margin);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", separation);
        margin.AddChild(column);
        return column;
    }

    private static Label MakeHudLabel(string text, int size, HorizontalAlignment alignment = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, HorizontalAlignment = alignment };
        label.AddThemeColorOverride("font_color", new Color(0.94f, 0.86f, 0.68f));
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static ProgressBar MakeHudBar()
    {
        var bar = new ProgressBar
        {
            CustomMinimumSize = new Vector2(260, 16),
            MinValue = 0,
            MaxValue = 100,
            Value = 100,
            ShowPercentage = false,
        };
        return bar;
    }

    private static Button AddHudButton(Container parent, string text, System.Action onPressed)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 30) };
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.Pressed += onPressed;
        parent.AddChild(button);
        return button;
    }

    private void EnsureDialoguePanel()
    {
        if (_dialoguePanel is null || _dialogueContent is null)
        {
            BuildPrototypeHud();
        }
    }

    private void AddDialogueLabel(string text)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
        };
        _dialogueContent!.AddChild(label);
    }

    private void AddCloseButton(string text)
    {
        var button = new Button { Text = text };
        button.Pressed += () =>
        {
            _openShop = null;
            _dialoguePanel!.Visible = false;
        };
        _dialogueContent!.AddChild(button);
    }

    private void ShowNotice(string text)
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel(text);
        FlushPendingLevelUps();
        AddCloseButton("Close");
        _dialoguePanel!.Visible = true;
    }

    private void ToggleRun()
    {
        GetNodeOrNull<PlayerController>("Player")?.ToggleRun();
        RefreshVitals();
    }

    private void ToggleInventory()
    {
        if (_inventoryPanel is null)
        {
            return;
        }

        _inventoryPanel.Visible = !_inventoryPanel.Visible;
        RefreshInventoryPanel();
    }

    private void ShowBankView()
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel("River Valley Bank");
        AddDialogueLabel("Deposit or withdraw one item at a time.");
        foreach (var item in _inventory.Items.OrderBy(pair => ItemName(pair.Key)).ToArray())
        {
            var itemId = item.Key;
            AddDialogueAction($"Deposit {ItemName(itemId)} ({item.Value})", () =>
            {
                _bank.Deposit(_inventory, itemId);
                SaveProgress(showNotice: false);
                RefreshStatus();
                ShowBankView();
            });
        }

        foreach (var item in _bank.Items.OrderBy(pair => ItemName(pair.Key)).ToArray())
        {
            var itemId = item.Key;
            AddDialogueAction($"Withdraw {ItemName(itemId)} ({item.Value})", () =>
            {
                _bank.Withdraw(_inventory, itemId);
                SaveProgress(showNotice: false);
                RefreshStatus();
                ShowBankView();
            });
        }

        AddCloseButton("Close bank");
        _dialoguePanel!.Visible = true;
    }

    private void ShowSkillsView()
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel("Skills");
        foreach (var skill in _skills.OrderBy(pair => SkillDisplayName(pair.Key)))
        {
            AddDialogueLabel($"{SkillDisplayName(skill.Key)}: level {skill.Value.Level} ({skill.Value.Experience} xp)");
        }

        AddCloseButton("Close skills");
        _dialoguePanel!.Visible = true;
    }

    private void ShowMagicView()
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel("Spellbook");
        AddDialogueLabel("Utility");
        AddDialogueAction("Home Teleport - free", CastHomeTeleport);
        AddCloseButton("Close spellbook");
        _dialoguePanel!.Visible = true;
    }

    private void ShowQuestsView()
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel("Quest Journal");
        AddDialogueLabel(_questLabel?.Text ?? "No active quests.");
        AddCloseButton("Close journal");
        _dialoguePanel!.Visible = true;
    }

    private void ShowSocialView()
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel("Social");
        AddDialogueLabel("Party: Solo adventurer");
        AddDialogueLabel("Guild: Not affiliated");
        AddDialogueLabel("Nearby trade: Use local chat to coordinate exchanges.");
        AddDialogueLabel("The server transport will replace this local shell when multiplayer hosting lands.");
        AddCloseButton("Close social");
        _dialoguePanel!.Visible = true;
    }

    private void ShowSettingsView()
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        AddDialogueLabel("Realm Settings");
        AddDialogueLabel("Gameplay");
        AddDialogueAction("Toggle run mode", ToggleRun);
        AddDialogueAction("Cast Home Teleport", CastHomeTeleport);
        AddDialogueAction($"Cycle combat style ({_attackStyle})", () =>
        {
            CycleAttackStyle();
            ShowSettingsView();
        });
        AddDialogueLabel("Character");
        AddDialogueAction("Open character customization", ShowCharacterCreator);
        AddDialogueAction("Randomize appearance", () =>
        {
            RandomizeLook();
            ShowSettingsView();
        });
        AddDialogueLabel("Account");
        AddDialogueAction("Save progress now", () => SaveProgress());
        AddDialogueAction("Reload last saved progress", () => GetTree().ReloadCurrentScene());
        AddCloseButton("Close settings");
        _dialoguePanel!.Visible = true;
    }

    private void ProcessLocalTick()
    {
        _localTick++;
        GetNodeOrNull<PlayerController>("Player")?.ApplyLocalTick();
        foreach (var node in GetTree().GetNodesInGroup("resource_nodes"))
        {
            (node as ResourceNode)?.AdvanceTick(_localTick);
        }

        foreach (var node in GetTree().GetNodesInGroup("ground_loot"))
        {
            (node as GroundLootNode)?.AdvanceTick(_localTick);
        }

        RefreshVitals();
    }

    private void CastHomeTeleport()
    {
        if (GetNodeOrNull<PlayerController>("Player") is not { } player)
        {
            return;
        }

        player.TeleportTo(HomeSpawnPosition);
        RefreshVitals();
        SaveProgress(showNotice: false);
        ShowNotice("You cast Home Teleport and return to the Oakhaven spawn courtyard.");
    }

    private void AddDialogueAction(string text, System.Action action)
    {
        var button = new Button { Text = text };
        button.Pressed += action;
        _dialogueContent!.AddChild(button);
    }

    /// <summary>Appends any level-up lines queued by the SkillLeveledUp subscriber to the open panel.</summary>
    private void FlushPendingLevelUps()
    {
        if (_pendingLevelUps.Count == 0 || _dialogueContent is null)
        {
            return;
        }

        foreach (var line in _pendingLevelUps)
        {
            AddDialogueLabel(line);
        }

        _pendingLevelUps.Clear();
    }

    private void ReplaceDialogueContent()
    {
        if (_dialoguePanel is null)
        {
            return;
        }

        if (_dialogueContent is not null && _dialogueContent.GetParent() is { } parent)
        {
            parent.RemoveChild(_dialogueContent);
            _dialogueContent.QueueFree();
        }

        _dialogueContent = new VBoxContainer();
        _dialogueContent.AddThemeConstantOverride("separation", 6);
        _dialogueScroll!.AddChild(_dialogueContent);
    }

    private void RefreshStatus()
    {
        if (_statusLabel is null)
        {
            return;
        }

        var who = Session.IsSignedIn ? Session.Username : "Adventurer";
        var weapon = _equipment.ItemInSlot(EquipmentSlot.Weapon) is { } weaponId ? ItemName(weaponId) : "Unarmed";

        _statusLabel.Text =
            $"{who}   ·   Combat Lv {_playerProfile.CombatLevel}   ·   HP {_playerProfile.CurrentHitpoints}/{_playerProfile.MaxHitpoints}   ·   Coins {_inventory.Count(Currency)}\n" +
            $"Atk L{SkillLevel("attack")}  Str L{SkillLevel("strength")}  Def L{SkillLevel("defense")}  HP L{SkillLevel("hitpoints")}   |   Weapon: {weapon}\n" +
            $"Foraging L{SkillLevel("foraging")}  Crafting L{SkillLevel("crafting")}  Language L{SkillLevel("language")}   |   {ItemName(GatherItemId)} {_inventory.Count(GatherItemId)}";

        RefreshVitals();
        RefreshInventoryPanel();

        if (_questLabel is null || _mainQuest is null)
        {
            return;
        }

        var objectives = _mainQuest.Definition.Objectives
            .Select(o => $"{o.Description} ({_mainQuest.GetProgress(o.Id)}/{o.RequiredCount})");
        var state = _mainQuest.IsComplete ? "  [COMPLETE]" : string.Empty;
        _questLabel.Text = $"Quest: {_mainQuest.Definition.Title}{state}\n - " + string.Join("\n - ", objectives);
    }

    private void RefreshVitals()
    {
        if (_healthBar is not null)
        {
            _healthBar.MaxValue = _playerProfile.MaxHitpoints;
            _healthBar.Value = _playerProfile.CurrentHitpoints;
        }

        if (_healthLabel is not null)
        {
            _healthLabel.Text = $"Health: {_playerProfile.CurrentHitpoints}/{_playerProfile.MaxHitpoints}";
        }

        var player = GetNodeOrNull<PlayerController>("Player");
        if (_runEnergyBar is not null)
        {
            _runEnergyBar.Value = player?.RunEnergy ?? 100.0f;
        }

        if (_runEnergyLabel is not null)
        {
            var mode = player?.IsRunning == true ? "running" : "walking";
            _runEnergyLabel.Text = $"Run energy: {player?.RunEnergy ?? 100.0f:0} ({mode})";
        }

        if (_heartbeatLabel is not null)
        {
            _heartbeatLabel.Text = $"Local Tick: {_localTick}";
        }

        if (_coordinateLabel is not null && player is not null && player.IsInsideTree())
        {
            var tileSize = RiverValleyRegion.TileSizeMeters * OpenWorldBuilder.VisualWorldScale;
            var tileX = _region.RespawnTile.X + Mathf.RoundToInt(player.GlobalPosition.X / tileSize);
            var tileZ = _region.RespawnTile.Z + Mathf.RoundToInt(player.GlobalPosition.Z / tileSize);
            _coordinateLabel.Text = $"Tile: {tileX}, {tileZ}";
        }

        _minimap?.SetTrackedPlayer(player);
    }

    private void RefreshInventoryPanel()
    {
        if (_inventoryContent is null)
        {
            return;
        }

        foreach (var child in _inventoryContent.GetChildren())
        {
            child.QueueFree();
        }

        _inventoryContent.AddChild(new Label { Text = "Inventory" });
        if (_inventory.Items.Count == 0)
        {
            _inventoryContent.AddChild(new Label { Text = "  Empty" });
        }
        else
        {
            foreach (var item in _inventory.Items.OrderBy(pair => ItemName(pair.Key)))
            {
                _inventoryContent.AddChild(new Label { Text = $"  {ItemName(item.Key)} x{item.Value}" });
            }
        }

        _inventoryContent.AddChild(new HSeparator());
        _inventoryContent.AddChild(new Label { Text = "Equipment" });
        foreach (var slot in System.Enum.GetValues<EquipmentSlot>())
        {
            var itemName = _equipment.ItemInSlot(slot) is { } itemId ? ItemName(itemId) : "-";
            _inventoryContent.AddChild(new Label { Text = $"  {slot}: {itemName}" });
        }
    }
}
