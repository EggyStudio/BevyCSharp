// Bevy's game_menu example, examples/showcase/game_menu.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Games;

// Shows how to make a game's menus, a splash screen, then a main menu with a settings menu for the
// display quality and the volume, then a game that waits five seconds and returns to the menu. Each
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
        app.Startup(_ => Render2d.SpawnCamera2d(), "game_menu.Setup");

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

        // The menus, a state of their own that the game's leaves at Disabled. The buttons' own
        // behaviors answer their presses.
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
            var button = SpawnButton(ecs, row, 150f, quality.ToString(), selected: quality == Quality);
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
            var button = SpawnButton(ecs, row, 30f, null, selected: volume == Volume);
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

    // A row with a label in front of the buttons for a setting's values.
    private static Entity Row(EcsWorld ecs, Entity column, string label)
    {
        var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Color = Crimson });
        ecs.SetParent(row, column);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = TextColor }, 33f), row);
        return row;
    }

    // A button, marked as the setting chosen where it is, its color following its interaction.
    private static Entity SpawnButton(EcsWorld ecs, Entity parent, float width, string? label, bool selected = false)
    {
        var entity = Ui.SpawnNode(new UiSettings
        {
            Interactive = true,
            Width = Length.Px(width),
            Height = Length.Px(65f),
            Margin = Sides.All(Length.Px(20f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = selected ? PressedButton : NormalButton,
        });
        ecs.SetParent(entity, parent);
        ecs.Add(entity, new MenuButton());
        if (selected) ecs.Add(entity, new SelectedOption());
        if (label is not null) ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = TextColor }, 33f), entity);
        return entity;
    }

    // A setting's button pressed takes the choice from the one that had it, Bevy's setting_button
    // for either kind of setting, and answers whether the choice moved.
    internal static bool TakeSelection(BehaviorContext ctx, bool differs)
    {
        if (!differs || Ui.InteractionOf(ctx.Entity) != UiInteraction.Pressed) return false;

        foreach (var previous in ctx.Ecs.EntitiesWith<SelectedOption>())
        {
            ctx.Ecs.Wrap<BackgroundColorRef>(previous).Value = NormalButton;
            ctx.Cmd.Remove<SelectedOption>(previous);
        }

        ctx.Cmd.Add(ctx.Entity, new SelectedOption());
        return true;
    }
}

/// <summary>A quality the display can be set to.</summary>
public enum DisplayQuality { Low, Medium, High }

/// <summary>What a menu button does.</summary>
public enum MenuAction { Play, Settings, SettingsDisplay, SettingsSound, BackToMainMenu, BackToSettings, Quit }

/// <summary>
/// A button of the menus, Bevy's <c>Button</c>, which a node made interactive here does not carry
/// by that name, so the menus mark their own.
/// </summary>
[Behavior]
public partial struct MenuButton
{
    /// <summary>
    /// Colored by its interaction as it changes and by whether it is the setting chosen, while the
    /// menus are up, as Bevy's <c>button_system</c> colors it.
    /// </summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void ButtonSystem(BehaviorContext ctx)
    {
        if (ctx.State<GameMenu.GameState>() != GameMenu.GameState.Menu) return;

        var selected = ctx.Ecs.Has<SelectedOption>(ctx.Entity);
        ctx.Ecs.Wrap<BackgroundColorRef>(ctx.Entity).Value = (Ui.InteractionOf(ctx.Entity), selected) switch
        {
            (UiInteraction.Pressed, _) or (UiInteraction.None, true) => GameMenu.PressedButton,
            (UiInteraction.Hovered, true) => GameMenu.HoveredPressedButton,
            (UiInteraction.Hovered, false) => GameMenu.HoveredButton,
            _ => GameMenu.NormalButton,
        };
    }
}

/// <summary>A button that moves through the menus, starts the game or quits.</summary>
[Behavior]
public partial struct MenuButtonAction
{
    /// <summary>What it does.</summary>
    public MenuAction Action;

    /// <summary>Done when it is pressed, while the menus are up, as Bevy's <c>menu_action</c> does it.</summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void MenuActionSystem(BehaviorContext ctx)
    {
        if (ctx.State<GameMenu.GameState>() != GameMenu.GameState.Menu || Ui.InteractionOf(ctx.Entity) != UiInteraction.Pressed) return;

        switch (Action)
        {
            case MenuAction.Quit:
                ctx.Exit();
                break;
            case MenuAction.Play:
                ctx.SetState(GameMenu.GameState.Game);
                ctx.SetState(GameMenu.MenuState.Disabled);
                break;
            case MenuAction.Settings or MenuAction.BackToSettings:
                ctx.SetState(GameMenu.MenuState.Settings);
                break;
            case MenuAction.SettingsDisplay:
                ctx.SetState(GameMenu.MenuState.SettingsDisplay);
                break;
            case MenuAction.SettingsSound:
                ctx.SetState(GameMenu.MenuState.SettingsSound);
                break;
            case MenuAction.BackToMainMenu:
                ctx.SetState(GameMenu.MenuState.Main);
                break;
        }
    }
}

/// <summary>A button choosing the display quality, Bevy's <c>Setting</c> of a <c>DisplayQuality</c>.</summary>
[Behavior]
public partial struct QualitySetting
{
    /// <summary>The quality it chooses.</summary>
    public DisplayQuality Value;

    /// <summary>The quality chosen when it is pressed, in the display settings.</summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void SettingButton(BehaviorContext ctx)
    {
        if (ctx.State<GameMenu.MenuState>() != GameMenu.MenuState.SettingsDisplay) return;
        if (GameMenu.TakeSelection(ctx, GameMenu.Quality != Value)) GameMenu.Quality = Value;
    }
}

/// <summary>A button choosing the volume, Bevy's <c>Setting</c> of a <c>Volume</c>.</summary>
[Behavior]
public partial struct VolumeSetting
{
    /// <summary>The volume it chooses, from zero to nine.</summary>
    public int Value;

    /// <summary>The volume chosen when it is pressed, in the sound settings.</summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void SettingButton(BehaviorContext ctx)
    {
        if (ctx.State<GameMenu.MenuState>() != GameMenu.MenuState.SettingsSound) return;
        if (GameMenu.TakeSelection(ctx, GameMenu.Volume != Value)) GameMenu.Volume = Value;
    }
}

/// <summary>The setting's button chosen now.</summary>
[Behavior]
public partial struct SelectedOption;

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
