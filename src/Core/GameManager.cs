using Godot;
using DawnOfBlade.Inventory;
using DawnOfBlade.Learning;
using DawnOfBlade.Skills;

namespace DawnOfBlade.Core;

public partial class GameManager : Node
{
    public bool IsInitialized { get; private set; }

    private readonly Inventory.Inventory _inventory = new();
    private readonly SkillProgress _languageSkill = new("language");
    private readonly SkillProgress _gatheringSkill = new("gathering");
    private readonly SkillProgress _craftingSkill = new("crafting");
    private readonly LearningPrompt _firstPrompt = new(
        "What does 'blade' mean?",
        "Sword",
        new[] { "Sword", "Bread", "River" },
        "language",
        1
    );

    private Label? _statusLabel;
    private Label? _questLabel;
    private PanelContainer? _dialoguePanel;
    private VBoxContainer? _dialogueContent;
    private bool _hasSpokenToAri;
    private bool _craftedSunToken;

    public override void _Ready()
    {
        InitializeSystems();
        BuildPrototypeHud();
        RefreshStatus();
    }

    private void InitializeSystems()
    {
        // TODO: Register save, data loading, UI, and gameplay services as they are implemented.
        IsInitialized = true;
        GD.Print("Dawn of Blade core systems initialized.");
    }

    public void ShowNpcDialogue(string speakerName)
    {
        _hasSpokenToAri = true;
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        var content = _dialogueContent!;

        AddDialogueLabel($"{speakerName}: Dawn favors those who practice. Answer this and I'll give you a coin.");
        AddDialogueLabel(_firstPrompt.PromptText);

        foreach (var choice in _firstPrompt.Choices)
        {
            var button = new Button { Text = choice };
            button.Pressed += () => CallDeferred(MethodName.AnswerPrompt, choice);
            content.AddChild(button);
        }

        _dialoguePanel!.Visible = true;
    }

    private void AnswerPrompt(string answer)
    {
        var correct = answer == _firstPrompt.CorrectAnswer;
        if (correct)
        {
            _inventory.Add("sun_coin");
            _languageSkill.AddExperience(25);
        }

        EnsureDialoguePanel();
        ReplaceDialogueContent();
        var content = _dialogueContent!;

        AddDialogueLabel(correct
            ? "Ari: Correct. A steady mind makes a steady blade."
            : "Ari: Not quite. Blade means sword.");

        var closeButton = new Button { Text = "Continue" };
        closeButton.Pressed += () => _dialoguePanel!.Visible = false;
        content.AddChild(closeButton);
        RefreshStatus();
    }

    public void GatherSunShard()
    {
        _inventory.Add("sun_shard");
        _gatheringSkill.AddExperience(15);
        RefreshStatus();
        ShowNotice("You gathered a sun shard.");
    }

    private void CraftSunToken()
    {
        if (!_inventory.Remove("sun_shard", 2))
        {
            ShowNotice("You need 2 sun shards.");
            return;
        }

        _inventory.Add("sun_token");
        _craftingSkill.AddExperience(30);
        _craftedSunToken = true;
        RefreshStatus();
        ShowNotice("You crafted a sun token.");
    }

    private void BuildPrototypeHud()
    {
        var ui = new CanvasLayer { Name = "PrototypeHud" };
        AddChild(ui);

        _statusLabel = new Label
        {
            Text = "",
            Position = new Vector2(16, 16)
        };
        ui.AddChild(_statusLabel);

        _questLabel = new Label
        {
            Text = "",
            Position = new Vector2(16, 42)
        };
        ui.AddChild(_questLabel);

        var craftButton = new Button
        {
            Text = "Craft Sun Token",
            Position = new Vector2(16, 74),
            Size = new Vector2(150, 34)
        };
        craftButton.Pressed += CraftSunToken;
        ui.AddChild(craftButton);

        _dialoguePanel = new PanelContainer
        {
            Visible = false,
            Position = new Vector2(24, 360),
            CustomMinimumSize = new Vector2(520, 150)
        };
        ui.AddChild(_dialoguePanel);

        _dialogueContent = new VBoxContainer();
        _dialogueContent.AddThemeConstantOverride("separation", 8);
        _dialoguePanel.AddChild(_dialogueContent);
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
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _dialogueContent!.AddChild(label);
    }

    private void RefreshStatus()
    {
        if (_statusLabel is null)
        {
            return;
        }

        _statusLabel.Text = $"Coins: {_inventory.Count("sun_coin")}  Shards: {_inventory.Count("sun_shard")}  Tokens: {_inventory.Count("sun_token")}  Language L{_languageSkill.Level}  Gathering L{_gatheringSkill.Level}  Crafting L{_craftingSkill.Level}";

        if (_questLabel is not null)
        {
            var ariStep = _hasSpokenToAri ? "done" : "talk to Ari";
            var shardStep = _inventory.Count("sun_shard") >= 2 || _craftedSunToken ? "done" : "collect 2 shards";
            var craftStep = _craftedSunToken ? "done" : "craft token";
            _questLabel.Text = $"Quest: First Light - {ariStep}, {shardStep}, {craftStep}";
        }
    }

    private void ShowNotice(string text)
    {
        EnsureDialoguePanel();
        ReplaceDialogueContent();
        var content = _dialogueContent!;
        AddDialogueLabel(text);

        var closeButton = new Button { Text = "Close" };
        closeButton.Pressed += () => _dialoguePanel!.Visible = false;
        content.AddChild(closeButton);
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
        _dialogueContent.AddThemeConstantOverride("separation", 8);
        _dialoguePanel.AddChild(_dialogueContent);
    }
}
