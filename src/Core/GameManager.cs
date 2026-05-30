using System.Collections.Generic;
using System.Linq;
using DawnOfBlade.Auth;
using DawnOfBlade.Characters;
using DawnOfBlade.Combat;
using DawnOfBlade.Data;
using DawnOfBlade.Dialogue;
using DawnOfBlade.Interaction;
using DawnOfBlade.Inventory;
using DawnOfBlade.Items;
using DawnOfBlade.Learning;
using DawnOfBlade.Quests;
using DawnOfBlade.Save;
using DawnOfBlade.Shops;
using DawnOfBlade.Skills;
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
    private const string Currency = "coins";

    public bool IsInitialized { get; private set; }

    private readonly DefinitionDatabase _definitions = new();
    private readonly Inventory.Inventory _inventory = new();
    private readonly QuestLog _questLog = new();
    private readonly SaveService _saveService = new();
    private readonly Equipment _equipment = new();
    private readonly System.Random _random = new();
    private readonly IRandomSource _combatRandom = new SystemRandomSource();

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

    public override void _Ready()
    {
        InitializeSystems();
        BuildPrototypeHud();

        if (_saveService.SaveExists)
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

    // ---- Gathering / crafting --------------------------------------------

    public void GatherResource(ResourceNode node)
    {
        _inventory.Add(node.ItemId);
        AddSkillExperience(node.SkillId, node.Experience);

        if (node.ItemId == GatherItemId)
        {
            AdvanceQuests(CollectObjectiveId);
        }

        RefreshStatus();
        ShowNotice($"You gathered {ItemName(node.ItemId)}.");
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
    }

    // ---- Dialogue ---------------------------------------------------------

    public void ShowNpcDialogue(string speakerName)
    {
        var npc = _definitions.NpcById.Values.FirstOrDefault();
        var rootId = npc?.DialogueRootId;

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
        AddCloseButton("Continue");
        RefreshStatus();
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
        var hit = resolver.Resolve(_playerProfile, playerGear, _attackStyle, hostile.Profile, EquipmentBonuses.Zero);

        var playerLine = hit.Landed
            ? (hit.Damage > 0 ? $"You hit {hit.Damage}." : "You hit, but it holds.")
            : "You miss.";

        if (hit.Landed)
        {
            hostile.Profile.ApplyDamage(hit.Damage);
        }

        if (hostile.Profile.IsDefeated)
        {
            AwardCombatExperience(hostile);
            var reward = 5 + hostile.Profile.MaxHitpoints;
            _inventory.Add(Currency, reward);
            ShowNotice($"{playerLine} You defeated the {hostile.DisplayName}! +{reward} coins. It recovers shortly.");

            var timer = GetTree().CreateTimer(4.0);
            timer.Timeout += hostile.ResetStats;

            RebuildPlayerProfile();
            RefreshStatus();
            return;
        }

        var enemyHit = resolver.Resolve(hostile.Profile, EquipmentBonuses.Zero, AttackStyle.Aggressive, _playerProfile, playerGear);
        if (enemyHit.Landed)
        {
            _playerProfile.ApplyDamage(enemyHit.Damage);
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
    }

    private void ApplyPlayerAppearance()
    {
        if (GetNodeOrNull<MeshInstance3D>("Player/Mesh") is { } mesh)
        {
            mesh.SetSurfaceOverrideMaterial(0, new StandardMaterial3D { AlbedoColor = new Color(_appearance.ShirtColor) });
        }
    }

    // ---- Save / Load ------------------------------------------------------

    private void SaveProgress()
    {
        var save = new SaveGame
        {
            PlayerName = Session.Username ?? "Player",
            Server = Session.Server ?? string.Empty,
            PlayerPosition = PlayerPositionArray(),
            Inventory = _inventory.Items.ToDictionary(pair => pair.Key, pair => pair.Value),
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

        ShowNotice(_saveService.Save(save) ? "Progress saved." : "Could not save progress.");
    }

    private void ApplySave(SaveGame save)
    {
        foreach (var pair in save.Inventory)
        {
            _inventory.Add(pair.Key, pair.Value);
        }

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
        if (GetNodeOrNull<Node3D>("Player") is { } player)
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
        if (_skills.TryGetValue(skillId, out var skill))
        {
            skill.AddExperience(amount);
        }
        else
        {
            var created = new SkillProgress(skillId);
            created.AddExperience(amount);
            _skills[skillId] = created;
        }

        if (skillId is "attack" or "strength" or "defense" or "hitpoints")
        {
            RebuildPlayerProfile();
        }
    }

    private string ItemName(string itemId) =>
        _definitions.ItemById.TryGetValue(itemId, out var item) ? item.DisplayName : itemId;

    private string SpeakerName(string speakerId) =>
        _definitions.NpcById.TryGetValue(speakerId, out var npc) ? npc.DisplayName : speakerId;

    // ---- HUD --------------------------------------------------------------

    private void BuildPrototypeHud()
    {
        var ui = new CanvasLayer { Name = "PrototypeHud" };
        AddChild(ui);

        _statusLabel = new Label { Position = new Vector2(16, 10) };
        ui.AddChild(_statusLabel);

        _questLabel = new Label { Position = new Vector2(16, 74) };
        ui.AddChild(_questLabel);

        AddHudButton(ui, "Craft Practice Chisel", new Vector2(16, 138), new Vector2(190, 32), CraftPracticeChisel);
        AddHudButton(ui, "Save", new Vector2(214, 138), new Vector2(80, 32), SaveProgress);
        AddHudButton(ui, "Load", new Vector2(302, 138), new Vector2(80, 32), () => GetTree().ReloadCurrentScene());

        _styleButton = AddHudButton(ui, $"Style: {_attackStyle}", new Vector2(16, 176), new Vector2(150, 32), CycleAttackStyle);
        AddHudButton(ui, "Randomize Look", new Vector2(174, 176), new Vector2(150, 32), RandomizeLook);

        _dialoguePanel = new PanelContainer
        {
            Visible = false,
            Position = new Vector2(16, 220),
            CustomMinimumSize = new Vector2(620, 160),
        };
        ui.AddChild(_dialoguePanel);

        _dialogueContent = new VBoxContainer();
        _dialogueContent.AddThemeConstantOverride("separation", 6);
        _dialoguePanel.AddChild(_dialogueContent);
    }

    private static Button AddHudButton(CanvasLayer ui, string text, Vector2 position, Vector2 size, System.Action onPressed)
    {
        var button = new Button { Text = text, Position = position, Size = size };
        button.Pressed += onPressed;
        ui.AddChild(button);
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
        AddCloseButton("Close");
        _dialoguePanel!.Visible = true;
    }

    private void ReplaceDialogueContent()
    {
        if (_dialoguePanel is null)
        {
            return;
        }

        if (_dialogueContent is not null)
        {
            _dialoguePanel.RemoveChild(_dialogueContent);
            _dialogueContent.QueueFree();
        }

        _dialogueContent = new VBoxContainer();
        _dialogueContent.AddThemeConstantOverride("separation", 6);
        _dialoguePanel.AddChild(_dialogueContent);
    }

    private void RefreshStatus()
    {
        if (_statusLabel is null)
        {
            return;
        }

        var who = Session.IsSignedIn ? $"{Session.Username} ({Session.Server})" : "Adventurer";
        var weapon = _equipment.ItemInSlot(EquipmentSlot.Weapon) is { } weaponId ? ItemName(weaponId) : "Unarmed";

        _statusLabel.Text =
            $"{who}   ·   Combat Lv {_playerProfile.CombatLevel}   ·   HP {_playerProfile.CurrentHitpoints}/{_playerProfile.MaxHitpoints}   ·   Coins {_inventory.Count(Currency)}\n" +
            $"Atk L{SkillLevel("attack")}  Str L{SkillLevel("strength")}  Def L{SkillLevel("defense")}  HP L{SkillLevel("hitpoints")}   |   Weapon: {weapon}\n" +
            $"Foraging L{SkillLevel("foraging")}  Crafting L{SkillLevel("crafting")}  Language L{SkillLevel("language")}   |   {ItemName(GatherItemId)} {_inventory.Count(GatherItemId)}";

        if (_questLabel is null || _mainQuest is null)
        {
            return;
        }

        var objectives = _mainQuest.Definition.Objectives
            .Select(o => $"{o.Description} ({_mainQuest.GetProgress(o.Id)}/{o.RequiredCount})");
        var state = _mainQuest.IsComplete ? "  [COMPLETE]" : string.Empty;
        _questLabel.Text = $"Quest: {_mainQuest.Definition.Title}{state}\n - " + string.Join("\n - ", objectives);
    }
}
