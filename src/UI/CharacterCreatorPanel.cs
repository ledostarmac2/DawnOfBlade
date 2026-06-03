using System;
using DawnOfBlade.Characters;
using Godot;

namespace DawnOfBlade.UI;

/// <summary>Full-screen first-entry appearance editor with a live low-poly character preview.</summary>
public partial class CharacterCreatorPanel : CanvasLayer
{
    private readonly Appearance _appearance;
    private readonly Action<Appearance> _onConfirmed;
    private HumanoidVisual? _preview;

    public CharacterCreatorPanel(Appearance initial, Action<Appearance> onConfirmed)
    {
        _appearance = initial.Clone();
        _onConfirmed = onConfirmed;
        Layer = 20;
    }

    public override void _Ready()
    {
        var backdrop = new ColorRect { Color = new Color(0.03f, 0.035f, 0.04f, 0.94f) };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(backdrop);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(860, 590) };
        panel.AddThemeStyleboxOverride("panel", PanelStyle());
        center.AddChild(panel);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 10);
        panel.AddChild(root);
        root.AddChild(Title("CHARACTER CREATOR", 24));

        var columns = new HBoxContainer();
        columns.AddThemeConstantOverride("separation", 14);
        root.AddChild(columns);

        var design = Column("DESIGN");
        design.CustomMinimumSize = new Vector2(245, 0);
        columns.AddChild(design);
        AddIndexSelector(design, "Head", () => _appearance.HeadStyle, value => _appearance.HeadStyle = value);
        AddIndexSelector(design, "Jaw", () => _appearance.JawStyle, value => _appearance.JawStyle = value);
        AddIndexSelector(design, "Torso", () => _appearance.TorsoStyle, value => _appearance.TorsoStyle = value);
        AddIndexSelector(design, "Arms", () => _appearance.ArmStyle, value => _appearance.ArmStyle = value);
        AddIndexSelector(design, "Hands", () => _appearance.HandStyle, value => _appearance.HandStyle = value);
        AddIndexSelector(design, "Legs", () => _appearance.LegStyle, value => _appearance.LegStyle = value);
        AddIndexSelector(design, "Feet", () => _appearance.FootStyle, value => _appearance.FootStyle = value);

        var previewColumn = new VBoxContainer { CustomMinimumSize = new Vector2(320, 0) };
        previewColumn.AddThemeConstantOverride("separation", 10);
        columns.AddChild(previewColumn);
        previewColumn.AddChild(BuildPreview());

        var presentation = new HBoxContainer();
        presentation.AddThemeConstantOverride("separation", 8);
        presentation.AddChild(Title("STYLE", 14));
        AddChoiceButton(presentation, "Masculine", "masculine");
        AddChoiceButton(presentation, "Feminine", "feminine");
        previewColumn.AddChild(presentation);

        var confirm = Button("CONFIRM APPEARANCE");
        confirm.CustomMinimumSize = new Vector2(0, 46);
        confirm.Pressed += () =>
        {
            _onConfirmed(_appearance.Clone());
            QueueFree();
        };
        previewColumn.AddChild(confirm);

        var colours = Column("COLOUR");
        colours.CustomMinimumSize = new Vector2(245, 0);
        columns.AddChild(colours);
        AddArraySelector(colours, "Hair", AppearanceOptions.HairColors, () => _appearance.HairColor, value => _appearance.HairColor = value);
        AddArraySelector(colours, "Torso", AppearanceOptions.ShirtColors, () => _appearance.ShirtColor, value => _appearance.ShirtColor = value);
        AddArraySelector(colours, "Legs", AppearanceOptions.LegColors, () => _appearance.LegColor, value => _appearance.LegColor = value);
        AddArraySelector(colours, "Feet", AppearanceOptions.FootColors, () => _appearance.FootColor, value => _appearance.FootColor = value);
        AddArraySelector(colours, "Skin", AppearanceOptions.SkinTones, () => _appearance.SkinTone, value => _appearance.SkinTone = value);
        AddArraySelector(colours, "Build", AppearanceOptions.BodyTypes, () => _appearance.BodyType, value => _appearance.BodyType = value);
        AddIndexSelector(colours, "Hair shape", () => _appearance.HairStyle, value => _appearance.HairStyle = value, AppearanceOptions.HairStyleCount);
    }

    private Control BuildPreview()
    {
        var container = new SubViewportContainer
        {
            CustomMinimumSize = new Vector2(320, 430),
            Stretch = true,
        };
        var viewport = new SubViewport { Size = new Vector2I(320, 430), TransparentBg = false };
        viewport.World3D = new World3D();
        container.AddChild(viewport);

        viewport.AddChild(new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-45, -25, 0),
            LightEnergy = 1.3f,
        });
        viewport.AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color("#272a27"),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color("#d2c09b"),
                AmbientLightEnergy = 0.7f,
            },
        });
        var camera = new Camera3D { Position = new Vector3(0, 1.35f, 5.0f) };
        camera.TreeEntered += () =>
        {
            camera.LookAt(new Vector3(0, 1.25f, 0));
            camera.Current = true;
        };
        viewport.AddChild(camera);

        _preview = new HumanoidVisual();
        viewport.AddChild(_preview);
        _preview.Apply(_appearance);
        return container;
    }

    private void AddChoiceButton(Container parent, string text, string value)
    {
        var button = Button(text);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.Pressed += () =>
        {
            _appearance.Presentation = value;
            RefreshPreview();
        };
        parent.AddChild(button);
    }

    private void AddIndexSelector(Container parent, string label, Func<int> read, Action<int> write, int count = AppearanceOptions.ShapeStyleCount)
    {
        AddSelector(parent, label, direction =>
        {
            write((read() + direction + count) % count);
            RefreshPreview();
        });
    }

    private void AddArraySelector(Container parent, string label, string[] values, Func<string> read, Action<string> write)
    {
        AddSelector(parent, label, direction =>
        {
            var index = Array.IndexOf(values, read());
            write(values[(index + direction + values.Length) % values.Length]);
            RefreshPreview();
        });
    }

    private static void AddSelector(Container parent, string label, Action<int> change)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        var left = Button("<");
        left.CustomMinimumSize = new Vector2(44, 36);
        left.Pressed += () => change(-1);
        row.AddChild(left);
        var caption = Title(label, 15);
        caption.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        caption.HorizontalAlignment = HorizontalAlignment.Center;
        row.AddChild(caption);
        var right = Button(">");
        right.CustomMinimumSize = new Vector2(44, 36);
        right.Pressed += () => change(1);
        row.AddChild(right);
        parent.AddChild(row);
    }

    private static VBoxContainer Column(string heading)
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        column.AddChild(Title(heading, 18));
        return column;
    }

    private static Label Title(string text, int size)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
        label.AddThemeColorOverride("font_color", new Color("#dbb35c"));
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static Button Button(string text)
    {
        var button = new Button { Text = text };
        button.AddThemeColorOverride("font_color", new Color("#e3c77f"));
        return button;
    }

    private static StyleBoxFlat PanelStyle()
    {
        var style = new StyleBoxFlat { BgColor = new Color("#343632") };
        style.SetBorderWidthAll(3);
        style.BorderColor = new Color("#8c7748");
        style.SetCornerRadiusAll(4);
        style.ContentMarginLeft = 18;
        style.ContentMarginRight = 18;
        style.ContentMarginTop = 14;
        style.ContentMarginBottom = 14;
        return style;
    }

    private void RefreshPreview() => _preview?.Apply(_appearance);
}
