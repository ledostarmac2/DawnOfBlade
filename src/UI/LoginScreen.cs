using DawnOfBlade.Auth;
using Godot;

namespace DawnOfBlade.UI;

/// <summary>
/// Title/login screen. Presents a dark-fantasy themed sign-in form and a separate
/// account-creation view. On a successful sign-in it stores the session and loads
/// the gameplay scene. The UI is built in code so it themes consistently without a
/// shared Theme resource.
/// </summary>
public partial class LoginScreen : Control
{
	private const string MainScenePath = "res://scenes/Main.tscn";
	private const string LogoPath = "res://assets/branding/dawn_of_blade_logo.png";
	private const string BackgroundPath = "res://assets/branding/login_background.svg";
	private const string PrefsPath = "user://login.cfg";

	// Palette.
	private static readonly Color ColBg = new(0.035f, 0.035f, 0.045f);
	private static readonly Color ColPanel = new(0.08f, 0.08f, 0.10f, 0.97f);
	private static readonly Color ColField = new(0.02f, 0.02f, 0.03f, 0.85f);
	private static readonly Color ColGold = new(0.83f, 0.68f, 0.36f);
	private static readonly Color ColGoldDim = new(0.45f, 0.38f, 0.22f);
	private static readonly Color ColRed = new(0.50f, 0.10f, 0.12f);
	private static readonly Color ColRedHover = new(0.62f, 0.14f, 0.16f);
	private static readonly Color ColRedDown = new(0.40f, 0.07f, 0.09f);
	private static readonly Color ColText = new(0.90f, 0.88f, 0.84f);
	private static readonly Color ColMuted = new(0.62f, 0.60f, 0.56f);
	private static readonly Color ColError = new(0.93f, 0.45f, 0.45f);
	private static readonly Color ColOk = new(0.55f, 0.82f, 0.55f);

	private readonly AccountStore _accounts = new();

	private Control _loginView = null!;
	private Control _registerView = null!;

	private LineEdit _loginUser = null!;
	private LineEdit _loginPass = null!;
	private CheckBox _remember = null!;
	private Label _loginMsg = null!;

	private LineEdit _regUser = null!;
	private LineEdit _regEmail = null!;
	private LineEdit _regPass = null!;
	private LineEdit _regConfirm = null!;
	private Label _regMsg = null!;

	public override void _Ready()
	{
		BuildUi();
		LoadPrefs();
		ShowLogin();
		_loginUser.GrabFocus();
	}

	// ---- Layout -----------------------------------------------------------

	private void BuildUi()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);

		var bg = new ColorRect { Color = ColBg, MouseFilter = MouseFilterEnum.Ignore };
		bg.SetAnchorsPreset(LayoutPreset.FullRect);
		AddChild(bg);

		// Hand-authored dawn battle key art (three warriors vs three young dragons). Covers the
		// solid colour above; if the asset is ever missing the dark ColorRect remains the backdrop.
		if (GD.Load<Texture2D>(BackgroundPath) is { } keyArt)
		{
			var scene = new TextureRect
			{
				Texture = keyArt,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
				MouseFilter = MouseFilterEnum.Ignore,
			};
			scene.SetAnchorsPreset(LayoutPreset.FullRect);
			AddChild(scene);
		}

		// Ornate gold frame around the whole screen (border-only stylebox).
		var frame = new Panel { MouseFilter = MouseFilterEnum.Ignore };
		frame.SetAnchorsPreset(LayoutPreset.FullRect);
		frame.OffsetLeft = 14;
		frame.OffsetTop = 14;
		frame.OffsetRight = -14;
		frame.OffsetBottom = -14;
		var frameBox = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0) };
		frameBox.SetBorderWidthAll(2);
		frameBox.BorderColor = ColGoldDim;
		frameBox.SetCornerRadiusAll(6);
		frame.AddThemeStyleboxOverride("panel", frameBox);
		AddChild(frame);

		var scroll = new ScrollContainer();
		scroll.SetAnchorsPreset(LayoutPreset.FullRect);
		scroll.OffsetLeft = 18;
		scroll.OffsetTop = 12;
		scroll.OffsetRight = -18;
		scroll.OffsetBottom = -42;
		scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Auto;
		AddChild(scroll);

		var center = new CenterContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ExpandFill };
		scroll.AddChild(center);

		var column = new VBoxContainer { CustomMinimumSize = new Vector2(420, 0) };
		column.AddThemeConstantOverride("separation", 12);
		center.AddChild(column);

		var logo = new TextureRect
		{
			Texture = GD.Load<Texture2D>(LogoPath),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(112, 112),
		};
		column.AddChild(logo);

		column.AddChild(MakeTitle("DAWN OF BLADE"));
		column.AddChild(MakeLabel("— Enter the Realm —", ColGold, 14, HorizontalAlignment.Center));

		var panel = new PanelContainer { CustomMinimumSize = new Vector2(420, 0) };
		panel.AddThemeStyleboxOverride("panel", PanelBox());
		column.AddChild(panel);

		var margin = new MarginContainer();
		margin.AddThemeConstantOverride("margin_left", 26);
		margin.AddThemeConstantOverride("margin_right", 26);
		margin.AddThemeConstantOverride("margin_top", 22);
		margin.AddThemeConstantOverride("margin_bottom", 22);
		panel.AddChild(margin);

		_loginView = BuildLoginView();
		_registerView = BuildRegisterView();
		margin.AddChild(_loginView);
		margin.AddChild(_registerView);

		BuildFooter();
	}

	private Control BuildLoginView()
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 12);

		_loginUser = MakeField("Username", secret: false);
		v.AddChild(_loginUser);

		var passRow = new HBoxContainer();
		passRow.AddThemeConstantOverride("separation", 8);
		_loginPass = MakeField("Password", secret: true);
		_loginPass.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_loginPass.TextSubmitted += _ => OnLogin();
		passRow.AddChild(_loginPass);

		var eye = MakeSecondaryButton("Show");
		eye.ToggleMode = true;
		eye.CustomMinimumSize = new Vector2(66, 42);
		eye.Toggled += pressed =>
		{
			_loginPass.Secret = !pressed;
			eye.Text = pressed ? "Hide" : "Show";
		};
		passRow.AddChild(eye);
		v.AddChild(passRow);

		var optRow = new HBoxContainer();
		_remember = new CheckBox { Text = "Remember Me" };
		StyleCheck(_remember);
		optRow.AddChild(_remember);
		optRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		var forgot = MakeLink("Forgot Password?");
		forgot.Pressed += () => SetMsg(_loginMsg, "Password recovery isn't available in the prototype.", ColMuted);
		optRow.AddChild(forgot);
		v.AddChild(optRow);

		var enter = MakePrimaryButton("ENTER GAME");
		enter.Pressed += OnLogin;
		v.AddChild(enter);

		var btnRow = new HBoxContainer();
		btnRow.AddThemeConstantOverride("separation", 10);
		var create = MakeSecondaryButton("Create Account");
		create.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		create.Pressed += ShowRegister;
		btnRow.AddChild(create);
		var settings = MakeSecondaryButton("Settings");
		settings.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		settings.Pressed += () => SetMsg(_loginMsg, "Settings aren't available in the prototype yet.", ColMuted);
		btnRow.AddChild(settings);
		v.AddChild(btnRow);

		_loginMsg = MakeMessage();
		v.AddChild(_loginMsg);

		return v;
	}

	private Control BuildRegisterView()
	{
		var v = new VBoxContainer();
		v.AddThemeConstantOverride("separation", 12);

		v.AddChild(MakeLabel("CREATE ACCOUNT", ColGold, 20, HorizontalAlignment.Center));
		v.AddChild(MakeWrapped("Forge your legend. Your account is saved on this device only.", ColMuted, 12));

		_regUser = MakeField("Username (min 3 characters)", secret: false);
		v.AddChild(_regUser);
		_regEmail = MakeField("Email (optional)", secret: false);
		v.AddChild(_regEmail);
		_regPass = MakeField("Password (min 6 characters)", secret: true);
		v.AddChild(_regPass);
		_regConfirm = MakeField("Confirm Password", secret: true);
		_regConfirm.TextSubmitted += _ => OnRegister();
		v.AddChild(_regConfirm);

		var create = MakePrimaryButton("CREATE ACCOUNT");
		create.Pressed += OnRegister;
		v.AddChild(create);

		var back = MakeSecondaryButton("← Back to Login");
		back.Pressed += ShowLogin;
		v.AddChild(back);

		_regMsg = MakeMessage();
		v.AddChild(_regMsg);

		return v;
	}

	private void BuildFooter()
	{
		var version = MakeLabel("VERSION 1.0.0", ColMuted, 12, HorizontalAlignment.Right);
		version.SetAnchorsPreset(LayoutPreset.BottomRight);
		version.OffsetLeft = -200;
		version.OffsetTop = -36;
		version.OffsetRight = -28;
		version.OffsetBottom = -16;
		AddChild(version);

		var copyright = MakeLabel("© 2026 Dawn of Blade. All rights reserved.", ColMuted, 12, HorizontalAlignment.Left);
		copyright.SetAnchorsPreset(LayoutPreset.BottomLeft);
		copyright.OffsetLeft = 28;
		copyright.OffsetTop = -36;
		copyright.OffsetRight = 380;
		copyright.OffsetBottom = -16;
		AddChild(copyright);
	}

	// ---- Behaviour --------------------------------------------------------

	private void OnLogin()
	{
		var (ok, message) = _accounts.Validate(_loginUser.Text, _loginPass.Text);
		if (!ok)
		{
			SetMsg(_loginMsg, message, ColError);
			return;
		}

		SavePrefs();
		Session.Username = _loginUser.Text.Trim();

		if (GetTree().ChangeSceneToFile(MainScenePath) != Error.Ok)
		{
			SetMsg(_loginMsg, "Could not load the game scene.", ColError);
		}
	}

	private void OnRegister()
	{
		var (ok, message) = _accounts.Register(_regUser.Text, _regEmail.Text, _regPass.Text, _regConfirm.Text);
		if (!ok)
		{
			SetMsg(_regMsg, message, ColError);
			return;
		}

		var newUser = _regUser.Text.Trim();
		_regUser.Text = string.Empty;
		_regEmail.Text = string.Empty;
		_regPass.Text = string.Empty;
		_regConfirm.Text = string.Empty;

		ShowLogin();
		_loginUser.Text = newUser;
		SetMsg(_loginMsg, "Account created — sign in to continue.", ColOk);
		_loginPass.GrabFocus();
	}

	private void ShowLogin()
	{
		_loginView.Visible = true;
		_registerView.Visible = false;
	}

	private void ShowRegister()
	{
		_loginView.Visible = false;
		_registerView.Visible = true;
		SetMsg(_regMsg, string.Empty, ColMuted);
		_regUser.GrabFocus();
	}

	private void LoadPrefs()
	{
		var cfg = new ConfigFile();
		if (cfg.Load(PrefsPath) != Error.Ok)
		{
			return;
		}

		var remember = cfg.GetValue("login", "remember", false).AsBool();
		_remember.ButtonPressed = remember;
		if (remember)
		{
			_loginUser.Text = cfg.GetValue("login", "username", string.Empty).AsString();
		}

	}

	private void SavePrefs()
	{
		var cfg = new ConfigFile();
		cfg.SetValue("login", "remember", _remember.ButtonPressed);
		cfg.SetValue("login", "username", _remember.ButtonPressed ? _loginUser.Text.Trim() : string.Empty);
		cfg.Save(PrefsPath);
	}

	private static void SetMsg(Label label, string text, Color color)
	{
		label.Text = text;
		label.AddThemeColorOverride("font_color", color);
	}

	// ---- Styled control factories ----------------------------------------

	private LineEdit MakeField(string placeholder, bool secret)
	{
		var field = new LineEdit
		{
			PlaceholderText = placeholder,
			Secret = secret,
			CustomMinimumSize = new Vector2(0, 42),
		};
		field.AddThemeStyleboxOverride("normal", FieldBox(ColGoldDim));
		field.AddThemeStyleboxOverride("focus", FieldBox(ColGold));
		field.AddThemeColorOverride("font_color", ColText);
		field.AddThemeColorOverride("font_placeholder_color", ColMuted);
		field.AddThemeColorOverride("caret_color", ColGold);
		field.AddThemeFontSizeOverride("font_size", 16);
		return field;
	}

	private Button MakePrimaryButton(string text)
	{
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 48) };
		button.AddThemeStyleboxOverride("normal", ButtonBox(ColRed, ColGold));
		button.AddThemeStyleboxOverride("hover", ButtonBox(ColRedHover, ColGold));
		button.AddThemeStyleboxOverride("pressed", ButtonBox(ColRedDown, ColGold));
		button.AddThemeStyleboxOverride("focus", ButtonBox(ColRed, ColGold));
		button.AddThemeColorOverride("font_color", new Color(0.97f, 0.92f, 0.82f));
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 1f, 0.95f));
		button.AddThemeColorOverride("font_pressed_color", new Color(0.90f, 0.85f, 0.78f));
		button.AddThemeFontSizeOverride("font_size", 20);
		return button;
	}

	private Button MakeSecondaryButton(string text)
	{
		var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 40) };
		button.AddThemeStyleboxOverride("normal", ButtonBox(new Color(0.10f, 0.10f, 0.12f, 0.90f), ColGoldDim));
		button.AddThemeStyleboxOverride("hover", ButtonBox(new Color(0.15f, 0.13f, 0.10f, 0.95f), ColGold));
		button.AddThemeStyleboxOverride("pressed", ButtonBox(new Color(0.08f, 0.08f, 0.09f, 0.95f), ColGold));
		button.AddThemeStyleboxOverride("focus", ButtonBox(new Color(0.10f, 0.10f, 0.12f, 0.90f), ColGoldDim));
		button.AddThemeColorOverride("font_color", ColGold);
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.92f, 0.70f));
		button.AddThemeFontSizeOverride("font_size", 15);
		return button;
	}

	private Button MakeLink(string text)
	{
		var button = new Button { Text = text, Flat = true };
		var empty = new StyleBoxEmpty();
		button.AddThemeStyleboxOverride("normal", empty);
		button.AddThemeStyleboxOverride("hover", empty);
		button.AddThemeStyleboxOverride("pressed", empty);
		button.AddThemeStyleboxOverride("focus", empty);
		button.AddThemeColorOverride("font_color", ColGold);
		button.AddThemeColorOverride("font_hover_color", new Color(1f, 0.92f, 0.70f));
		button.AddThemeFontSizeOverride("font_size", 13);
		return button;
	}

	private void StyleCheck(CheckBox check)
	{
		check.AddThemeColorOverride("font_color", ColText);
		check.AddThemeColorOverride("font_hover_color", ColGold);
		check.AddThemeFontSizeOverride("font_size", 14);
	}

	private static Label MakeTitle(string text)
	{
		var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		label.AddThemeColorOverride("font_color", new Color(0.85f, 0.72f, 0.42f));
		label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.6f));
		label.AddThemeConstantOverride("shadow_offset_x", 2);
		label.AddThemeConstantOverride("shadow_offset_y", 2);
		label.AddThemeFontSizeOverride("font_size", 46);
		return label;
	}

	private static Label MakeLabel(string text, Color color, int size, HorizontalAlignment align)
	{
		var label = new Label { Text = text, HorizontalAlignment = align };
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeFontSizeOverride("font_size", size);
		return label;
	}

	private static Label MakeWrapped(string text, Color color, int size)
	{
		var label = MakeLabel(text, color, size, HorizontalAlignment.Center);
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		return label;
	}

	private static Label MakeMessage()
	{
		var label = MakeWrapped(string.Empty, ColMuted, 13);
		label.CustomMinimumSize = new Vector2(0, 18);
		return label;
	}

	// ---- StyleBox helpers -------------------------------------------------

	private static StyleBoxFlat PanelBox()
	{
		var box = new StyleBoxFlat { BgColor = ColPanel };
		box.SetBorderWidthAll(2);
		box.BorderColor = ColGold;
		box.SetCornerRadiusAll(6);
		box.ShadowColor = new Color(0, 0, 0, 0.5f);
		box.ShadowSize = 8;
		return box;
	}

	private static StyleBoxFlat FieldBox(Color border)
	{
		var box = new StyleBoxFlat { BgColor = ColField };
		box.SetBorderWidthAll(1);
		box.BorderColor = border;
		box.SetCornerRadiusAll(4);
		box.ContentMarginLeft = 12;
		box.ContentMarginRight = 12;
		box.ContentMarginTop = 8;
		box.ContentMarginBottom = 8;
		return box;
	}

	private static StyleBoxFlat ButtonBox(Color bg, Color border)
	{
		var box = new StyleBoxFlat { BgColor = bg };
		box.SetBorderWidthAll(1);
		box.BorderColor = border;
		box.SetCornerRadiusAll(4);
		box.ContentMarginLeft = 12;
		box.ContentMarginRight = 12;
		box.ContentMarginTop = 8;
		box.ContentMarginBottom = 8;
		return box;
	}
}
