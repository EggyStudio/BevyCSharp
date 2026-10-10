// Bevy's game_menu example, examples/showcase/game_menu.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Games;

// Shows how to make a game's menus, a splash screen, then a main menu with a settings menu for the
// display quality and the volume, then a game that waits five seconds and returns to the menu. The
// buttons are Bevy's widgets, activated as they are pressed, and each setting a radio group. Each
// screen is despawned as the state that made it ends.
internal static class GameMenu
{
    internal enum GameState { Splash, Menu, Game }

    internal enum MenuState { Main, Settings, SettingsDisplay, SettingsSound, Disabled }

    private static readonly Color TextColor = Color.FromSrgb(0.9f, 0.9f, 0.9f);
    private static readonly Color Crimson = Color.FromSrgb8(220, 20, 60);

    internal static readonly Color NormalButton = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    internal static readonly Color HoveredButton = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    internal static readonly Color HoveredPressedButton = Color.FromSrgb(0.25f, 0.65f, 0.25f);
    internal static readonly Color PressedButton = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    // Bevy's DisplayQuality and Volume resources, and its SplashTimer and GameTimer.
    internal static DisplayQuality Quality = DisplayQuality.Medium;
    internal static int Volume = 7;
    private static GameTimer _splashTimer, _gameTimer;

    public static void Build(App app)
    {
        (Quality, Volume) = (DisplayQuality.Medium, 7);
        app.AddState(GameState.Splash);
        app.AddState(MenuState.Disabled);
        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();

            // Bevy's menu_plugin's observers, which style the buttons and the radio buttons as the
            // pointer comes and goes, take a setting's choice, and move through the menus.
            var ecs = ctx.Ecs;
            ecs.Observe<Pointer<Over>>(on => Style(on.Ecs, on.Entity, hovered: true, pressed: false));
            ecs.Observe<Pointer<Out>>(on => Style(on.Ecs, on.Entity, hovered: false, pressed: false));
            ecs.Observe<Pointer<Press>>(on =>
            {
                if (on.Ecs.Get<RadioButtonRef>(on.Entity) is not null) on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = PressedButton;
            });
            ecs.Observe<Pointer<Release>>(on =>
            {
                if (on.Ecs.Get<RadioButtonRef>(on.Entity) is not null) Style(on.Ecs, on.Entity, hovered: true, pressed: false);
            });
            ecs.Observe<ValueChange<Entity>>(OnValueChangeSettingRadio);
            ecs.Observe<Activate>(OnButtonActivateUpdateStates);
        }, "game_menu.Setup");

        // The splash, a logo for a second.
        app.AddStateSystem(GameState.Splash, entering: true, new SystemDescriptor(world => SplashSetup(new BehaviorContext(world)), "game_menu.SplashSetup"));
        app.On(Stage.Update, ctx =>
        {
            if (_splashTimer.Tick(ctx.Time.Delta).Finished) ctx.SetState(GameState.Menu);
        }, "game_menu.Countdown", BehaviorConditions.InState(GameState.Splash));

        // The game, its settings shown for five seconds.
        app.AddStateSystem(GameState.Game, entering: true, new SystemDescriptor(world => GameSetup(new BehaviorContext(world)), "game_menu.GameSetup"));
        app.On(Stage.Update, ctx =>
        {
            if (_gameTimer.Tick(ctx.Time.Delta).Finished) ctx.SetState(GameState.Menu);
        }, "game_menu.Game", BehaviorConditions.InState(GameState.Game));

        // The menus, a state of their own that the game's leaves at Disabled.
        app.AddStateSystem(GameState.Menu, entering: true, new SystemDescriptor(world => new BehaviorContext(world).SetState(MenuState.Main), "game_menu.MenuSetup"));
        app.AddStateSystem(MenuState.Main, entering: true, new SystemDescriptor(world => MainMenuSetup(new BehaviorContext(world)), "game_menu.MainMenuSetup"));
        app.AddStateSystem(MenuState.Settings, entering: true, new SystemDescriptor(world => SettingsMenuSetup(new BehaviorContext(world)), "game_menu.SettingsMenuSetup"));
        app.AddStateSystem(MenuState.SettingsDisplay, entering: true, new SystemDescriptor(world => DisplaySettingsMenuSetup(new BehaviorContext(world)), "game_menu.DisplaySettingsMenuSetup"));
        app.AddStateSystem(MenuState.SettingsSound, entering: true, new SystemDescriptor(world => SoundSettingsMenuSetup(new BehaviorContext(world)), "game_menu.SoundSettingsMenuSetup"));
    }

    private static void SplashSetup(BehaviorContext ctx)
    {
        _splashTimer = GameTimer.FromSeconds(1f, TimerMode.Once);
        var screen = Screen(ctx.Ecs, GameState.Splash);
        ctx.Ecs.Add(screen, new OnSplashScreen());
        var icon = Ui.SpawnNode(new UiSettings { Width = Length.Px(200f) });
        Ui.SetImage(icon, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        ctx.Ecs.SetParent(icon, screen);
    }

    private static void GameSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _gameTimer = GameTimer.FromSeconds(5f, TimerMode.Once);
        var screen = Screen(ecs, GameState.Game);
        ecs.Add(screen, new OnGameScreen());
        var box = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center, Color = (0f, 0f, 0f, 1f) });
        ecs.SetParent(box, screen);
        ecs.SetParent(Ui.SpawnText("Will be back to the menu shortly...", new UiSettings { Margin = Sides.All(Length.Px(50f)), Color = TextColor }, 67f), box);

        var settings = Ui.SpawnText(string.Empty, new UiSettings { Margin = Sides.All(Length.Px(50f)) }, 50f);
        ecs.SetParent(settings, box);
        var style = new UiTextSettings { FontSize = 50f };
        Ui.SpawnTextSpan(settings, $"quality: {Quality}", style, (0f, 0f, 1f, 1f));
        Ui.SpawnTextSpan(settings, " - ", style, TextColor);
        Ui.SpawnTextSpan(settings, $"volume: Volume({Volume})", style, (0f, 1f, 0f, 1f));
    }

    private static void MainMenuSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var screen = Screen(ecs, MenuState.Main);
        ecs.Add(screen, new OnMainMenuScreen());
        var column = Column(ecs, screen);
        ecs.SetParent(Ui.SpawnText("Bevy Game Menu UI", new UiSettings { Margin = Sides.All(Length.Px(50f)), Color = TextColor }, 67f), column);

        foreach (var (action, icon, label) in new[]
        {
            (MenuAction.Play, "right", "New Game"),
            (MenuAction.Settings, "wrench", "Settings"),
            (MenuAction.Quit, "exitRight", "Quit"),
        })
        {
            var button = SpawnButton(ecs, column, 300f, label);
            ecs.Add(button, new MenuButtonAction { Action = action });

            // The icon sits at the button's left, out of the flow that centers the label.
            var image = Ui.SpawnNode(new UiSettings { Width = Length.Px(30f), Absolute = true, Left = Length.Px(10f) });
            Ui.SetImage(image, AssetServer.Load(AssetKind.Image, $"textures/Game Icons/{icon}.png"));
            ecs.SetParent(image, button);
        }
    }

    private static void SettingsMenuSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var screen = Screen(ecs, MenuState.Settings);
        ecs.Add(screen, new OnSettingsMenuScreen());
        var column = Column(ecs, screen);
        foreach (var (action, label) in new[] { (MenuAction.SettingsDisplay, "Display"), (MenuAction.SettingsSound, "Sound"), (MenuAction.BackToMainMenu, "Back") })
            ecs.Add(SpawnButton(ecs, column, 200f, label), new MenuButtonAction { Action = action });
    }

    private static void DisplaySettingsMenuSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var screen = Screen(ecs, MenuState.SettingsDisplay);
        ecs.Add(screen, new OnDisplaySettingsMenuScreen());
        var column = Column(ecs, screen);
        var row = Row(ecs, column, "Display Quality");
        foreach (var quality in Enum.GetValues<DisplayQuality>())
        {
            var button = SpawnRadioButton(ecs, row, 150f, quality.ToString(), quality == Quality);
            ecs.Add(button, new QualitySetting { Value = quality });
        }

        ecs.Add(SpawnButton(ecs, column, 200f, "Back"), new MenuButtonAction { Action = MenuAction.BackToSettings });
    }

    private static void SoundSettingsMenuSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var screen = Screen(ecs, MenuState.SettingsSound);
        ecs.Add(screen, new OnSoundSettingsMenuScreen());
        var column = Column(ecs, screen);
        var row = Row(ecs, column, "Volume");
        for (var volume = 0; volume < 10; volume++)
        {
            var button = SpawnRadioButton(ecs, row, 30f, null, volume == Volume);
            ecs.Add(button, new VolumeSetting { Value = volume });
        }

        ecs.Add(SpawnButton(ecs, column, 200f, "Back"), new MenuButtonAction { Action = MenuAction.BackToSettings });
    }

    // A screen filling the window, its content centered, despawned when the state ends.
    private static Entity Screen<TState>(EcsWorld ecs, TState state) where TState : struct, Enum
    {
        var screen = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
        ecs.DespawnOnExit(screen, state);
        return screen;
    }

    private static Entity Column(EcsWorld ecs, Entity screen)
    {
        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center, Color = Crimson });
        ecs.SetParent(column, screen);
        return column;
    }

    // A row with a label in front of the radio buttons for a setting's values, a radio group of
    // Bevy's widgets.
    private static Entity Row(EcsWorld ecs, Entity column, string label)
    {
        var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Color = Crimson });
        ecs.Insert<RadioGroupRef>(row);
        ecs.SetParent(row, column);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = TextColor }, 33f), row);
        return row;
    }

    // A button of Bevy's widgets, activated as it is pressed rather than released.
    private static Entity SpawnButton(EcsWorld ecs, Entity parent, float width, string label)
    {
        var button = Node(ecs, parent, width, label, NormalButton);
        ecs.Insert<ButtonRef>(button);
        ecs.Insert<ActivateOnPressRef>(button);
        return button;
    }

    // A radio button of Bevy's widgets, checked where its value is the setting's.
    private static Entity SpawnRadioButton(EcsWorld ecs, Entity parent, float width, string? label, bool chosen)
    {
        var button = Node(ecs, parent, width, label, chosen ? PressedButton : NormalButton);
        ecs.Insert<RadioButtonRef>(button);
        if (chosen) ecs.Insert<CheckedRef>(button);
        return button;
    }

    private static Entity Node(EcsWorld ecs, Entity parent, float width, string? label, Color color)
    {
        var entity = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(width),
            Height = Length.Px(65f),
            Margin = Sides.All(Length.Px(20f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = color,
        });
        ecs.SetParent(entity, parent);
        if (label is not null) ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = TextColor }, 33f), entity);
        return entity;
    }

    // Bevy's on_button_over_style and on_button_out_style, and the color a radio button takes as
    // it is let go, a checked one green and a button or an unchecked one gray, lighter where the
    // pointer is over it.
    private static void Style(EcsWorld ecs, Entity entity, bool hovered, bool pressed)
    {
        var radio = ecs.Get<RadioButtonRef>(entity) is not null;
        if (!radio && ecs.Get<ButtonRef>(entity) is null) return;

        var chosen = radio && ecs.Get<CheckedRef>(entity) is not null;
        ecs.Wrap<BackgroundColorRef>(entity).Value = (chosen, hovered || pressed) switch
        {
            (true, true) => HoveredPressedButton,
            (true, false) => PressedButton,
            (false, true) => HoveredButton,
            _ => NormalButton,
        };
    }

    // Bevy's on_value_change_setting_radio, for either setting while its screen is up, the radio
    // button chosen checked in place of the one that was and the setting taking its value.
    private static void OnValueChangeSettingRadio(On<ValueChange<Entity>> on)
    {
        var ecs = on.Ecs;
        var chosen = on.Event.Value;
        if (ecs.Get<CheckedRef>(chosen) is not null) return;

        var menu = App.TryState<MenuState>(out var now) ? now : MenuState.Disabled;
        if (menu == MenuState.SettingsDisplay && ecs.TryGet<QualitySetting>(chosen, out var quality) && quality.Value != Quality)
            Quality = quality.Value;
        else if (menu == MenuState.SettingsSound && ecs.TryGet<VolumeSetting>(chosen, out var volume) && volume.Value != Volume)
            Volume = volume.Value;
        else
            return;

        foreach (var previous in ecs.EntitiesWith<QualitySetting>().Concat(ecs.EntitiesWith<VolumeSetting>()))
        {
            if (ecs.Get<CheckedRef>(previous) is not { } was) continue;
            ecs.Wrap<BackgroundColorRef>(previous).Value = NormalButton;
            was.Remove();
        }

        ecs.Insert<CheckedRef>(chosen);
    }

    // Bevy's on_button_activate_update_states, the menus and the game moved on by the button
    // activated.
    private static void OnButtonActivateUpdateStates(On<Activate> on)
    {
        if (!on.Ecs.TryGet<MenuButtonAction>(on.Entity, out var button)) return;

        var ctx = on.Context;
        switch (button.Action)
        {
            case MenuAction.Quit:
                ctx.Exit();
                break;
            case MenuAction.Play:
                ctx.SetState(GameState.Game);
                ctx.SetState(MenuState.Disabled);
                break;
            case MenuAction.Settings or MenuAction.BackToSettings:
                ctx.SetState(MenuState.Settings);
                break;
            case MenuAction.SettingsDisplay:
                ctx.SetState(MenuState.SettingsDisplay);
                break;
            case MenuAction.SettingsSound:
                ctx.SetState(MenuState.SettingsSound);
                break;
            case MenuAction.BackToMainMenu:
                ctx.SetState(MenuState.Main);
                break;
        }
    }
}

/// <summary>A quality the display can be set to.</summary>
public enum DisplayQuality { Low, Medium, High }

/// <summary>What a menu button does.</summary>
public enum MenuAction { Play, Settings, SettingsDisplay, SettingsSound, BackToMainMenu, BackToSettings, Quit }

/// <summary>A button that moves through the menus, starts the game or quits.</summary>
[Behavior]
public partial struct MenuButtonAction
{
    /// <summary>What it does.</summary>
    public MenuAction Action;
}

/// <summary>A radio button choosing the display quality, Bevy's <c>Setting</c> of a <c>DisplayQuality</c>.</summary>
[Behavior]
public partial struct QualitySetting
{
    /// <summary>The quality it chooses.</summary>
    public DisplayQuality Value;
}

/// <summary>A radio button choosing the volume, Bevy's <c>Setting</c> of a <c>Volume</c>.</summary>
[Behavior]
public partial struct VolumeSetting
{
    /// <summary>The volume it chooses, from zero to nine.</summary>
    public int Value;
}

/// <summary>The splash screen.</summary>
[Behavior]
public partial struct OnSplashScreen;

/// <summary>The game's screen.</summary>
[Behavior]
public partial struct OnGameScreen;

/// <summary>The main menu.</summary>
[Behavior]
public partial struct OnMainMenuScreen;

/// <summary>The settings menu.</summary>
[Behavior]
public partial struct OnSettingsMenuScreen;

/// <summary>The display settings.</summary>
[Behavior]
public partial struct OnDisplaySettingsMenuScreen;

/// <summary>The sound settings.</summary>
[Behavior]
public partial struct OnSoundSettingsMenuScreen;
