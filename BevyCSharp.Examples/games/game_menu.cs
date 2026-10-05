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
    private enum GameState { Splash, Menu, Game }

    private enum MenuState { Main, Settings, SettingsDisplay, SettingsSound, Disabled }

    private enum DisplayQuality { Low, Medium, High }

    private enum MenuButtonAction { Play, Settings, SettingsDisplay, SettingsSound, BackToMainMenu, BackToSettings, Quit }

    private static readonly (float R, float G, float B, float A) TextColor = Scene.Srgb(0.9f, 0.9f, 0.9f);
    private static readonly (float R, float G, float B, float A) Crimson = Scene.Srgb8(220, 20, 60);

    private static readonly Color NormalButton = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    private static readonly Color HoveredButton = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    private static readonly Color HoveredPressedButton = Color.FromSrgb(0.25f, 0.65f, 0.25f);
    private static readonly Color PressedButton = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    // What each button on screen does, what it last read, and whether it is the setting chosen,
    // which Bevy keeps as components and here sit beside the entity, keyed by it, since its action
    // and setting are of more than one type.
    private sealed class Button
    {
        public MenuButtonAction? Action;
        public DisplayQuality? Quality;
        public int? Volume;
        public UiInteraction Last;
        public bool Selected;
    }

    private static readonly Dictionary<Entity, Button> Buttons = [];
    private static DisplayQuality _displayQuality = DisplayQuality.Medium;
    private static int _volume = 7;
    private static float _timer;

    public static void Build(App app)
    {
        (_displayQuality, _volume) = (DisplayQuality.Medium, 7);
        Buttons.Clear();
        app.AddState(GameState.Splash);
        app.AddState(MenuState.Disabled);
        app.Startup(_ => Render2d.SpawnCamera2d(), "game_menu.Setup");

        // The splash, a logo for a second.
        app.AddStateSystem(GameState.Splash, entering: true, new SystemDescriptor(world => SplashSetup(new BehaviorContext(world)), "game_menu.SplashSetup"));
        app.On(Stage.Update, ctx => Countdown(ctx, 1f, GameState.Menu), "game_menu.Countdown", BehaviorConditions.InState(GameState.Splash));

        // The game, its settings shown for five seconds.
        app.AddStateSystem(GameState.Game, entering: true, new SystemDescriptor(world => GameSetup(new BehaviorContext(world)), "game_menu.GameSetup"));
        app.On(Stage.Update, ctx => Countdown(ctx, 5f, GameState.Menu), "game_menu.Game", BehaviorConditions.InState(GameState.Game));

        // The menus, a state of their own that the game's leaves at Disabled.
        app.AddStateSystem(GameState.Menu, entering: true, new SystemDescriptor(world => new BehaviorContext(world).SetState(MenuState.Main), "game_menu.MenuSetup"));
        app.AddStateSystem(MenuState.Main, entering: true, new SystemDescriptor(world => MainMenuSetup(new BehaviorContext(world)), "game_menu.MainMenuSetup"));
        app.AddStateSystem(MenuState.Settings, entering: true, new SystemDescriptor(world => SettingsMenuSetup(new BehaviorContext(world)), "game_menu.SettingsMenuSetup"));
        app.AddStateSystem(MenuState.SettingsDisplay, entering: true, new SystemDescriptor(world => DisplaySettingsMenuSetup(new BehaviorContext(world)), "game_menu.DisplaySettingsMenuSetup"));
        app.AddStateSystem(MenuState.SettingsSound, entering: true, new SystemDescriptor(world => SoundSettingsMenuSetup(new BehaviorContext(world)), "game_menu.SoundSettingsMenuSetup"));
        app.On(Stage.Update, ButtonSystem, "game_menu.ButtonSystem", BehaviorConditions.InState(GameState.Menu));
    }

    private static void Countdown(BehaviorContext ctx, float seconds, GameState next)
    {
        _timer += ctx.Time.Delta;
        if (_timer < seconds) return;
        _timer = 0f;
        ctx.SetState(next);
    }

    private static void SplashSetup(BehaviorContext ctx)
    {
        _timer = 0f;
        var screen = Screen(ctx.Ecs, GameState.Splash);
        var icon = Ui.SpawnNode(new UiSettings { Width = Length.Px(200f) });
        Ui.SetImage(icon, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        ctx.Ecs.SetParent(icon, screen);
    }

    private static void GameSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _timer = 0f;
        var screen = Screen(ecs, GameState.Game);
        var box = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center, Color = (0f, 0f, 0f, 1f) });
        ecs.SetParent(box, screen);
        ecs.SetParent(Ui.SpawnText("Will be back to the menu shortly...", new UiSettings { Margin = Sides.All(Length.Px(50f)), Color = TextColor }, 67f), box);

        var settings = Ui.SpawnText(string.Empty, new UiSettings { Margin = Sides.All(Length.Px(50f)) }, 50f);
        ecs.SetParent(settings, box);
        var style = new UiTextSettings { FontSize = 50f };
        Ui.SpawnTextSpan(settings, $"quality: {_displayQuality}", style, (0f, 0f, 1f, 1f));
        Ui.SpawnTextSpan(settings, " - ", style, TextColor);
        Ui.SpawnTextSpan(settings, $"volume: Volume({_volume})", style, (0f, 1f, 0f, 1f));
    }

    private static void MainMenuSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var column = Column(ecs, Screen(ecs, MenuState.Main));
        ecs.SetParent(Ui.SpawnText("Bevy Game Menu UI", new UiSettings { Margin = Sides.All(Length.Px(50f)), Color = TextColor }, 67f), column);

        foreach (var (action, icon, label) in new[]
        {
            (MenuButtonAction.Play, "right", "New Game"),
            (MenuButtonAction.Settings, "wrench", "Settings"),
            (MenuButtonAction.Quit, "exitRight", "Quit"),
        })
        {
            var button = SpawnButton(ecs, column, 300f, new Button { Action = action }, label);

            // The icon sits at the button's left, out of the flow that centers the label.
            var image = Ui.SpawnNode(new UiSettings { Width = Length.Px(30f), Absolute = true, Left = Length.Px(10f) });
            Ui.SetImage(image, AssetServer.Load(AssetKind.Image, $"textures/Game Icons/{icon}.png"));
            ecs.SetParent(image, button);
        }
    }

    private static void SettingsMenuSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var column = Column(ecs, Screen(ecs, MenuState.Settings));
        SpawnButton(ecs, column, 200f, new Button { Action = MenuButtonAction.SettingsDisplay }, "Display");
        SpawnButton(ecs, column, 200f, new Button { Action = MenuButtonAction.SettingsSound }, "Sound");
        SpawnButton(ecs, column, 200f, new Button { Action = MenuButtonAction.BackToMainMenu }, "Back");
    }

    private static void DisplaySettingsMenuSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var column = Column(ecs, Screen(ecs, MenuState.SettingsDisplay));
        var row = Row(ecs, column, "Display Quality");
        foreach (var quality in Enum.GetValues<DisplayQuality>())
            SpawnButton(ecs, row, 150f, new Button { Quality = quality, Selected = quality == _displayQuality }, quality.ToString());
        SpawnButton(ecs, column, 200f, new Button { Action = MenuButtonAction.BackToSettings }, "Back");
    }

    private static void SoundSettingsMenuSetup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var column = Column(ecs, Screen(ecs, MenuState.SettingsSound));
        var row = Row(ecs, column, "Volume");
        for (var volume = 0; volume < 10; volume++)
            SpawnButton(ecs, row, 30f, new Button { Volume = volume, Selected = volume == _volume }, null);
        SpawnButton(ecs, column, 200f, new Button { Action = MenuButtonAction.BackToSettings }, "Back");
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

    private static Entity SpawnButton(EcsWorld ecs, Entity parent, float width, Button button, string? label)
    {
        var color = button.Selected ? PressedButton : NormalButton;
        var entity = Ui.SpawnNode(new UiSettings
        {
            Interactive = true,
            Width = Length.Px(width),
            Height = Length.Px(65f),
            Margin = Sides.All(Length.Px(20f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = (color.R, color.G, color.B, color.A),
        });
        ecs.SetParent(entity, parent);
        if (label is not null) ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = TextColor }, 33f), entity);
        Buttons[entity] = button;
        return entity;
    }

    // Each button whose interaction changed is colored by it and by whether it is the setting
    // chosen, and a press does what the button is for, as Bevy's button_system, setting_button and
    // menu_action do between them.
    private static void ButtonSystem(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var gone in Buttons.Keys.Where(entity => !ecs.IsAlive(entity)).ToList()) Buttons.Remove(gone);

        foreach (var (entity, button) in Buttons.ToList())
        {
            var interaction = Ui.InteractionOf(entity);
            if (interaction == button.Last) continue;
            button.Last = interaction;

            if (interaction == UiInteraction.Pressed) Press(ctx, entity, button);
            ecs.Wrap<BackgroundColorRef>(entity).Value = (interaction, button.Selected) switch
            {
                (UiInteraction.Pressed, _) or (UiInteraction.None, true) => PressedButton,
                (UiInteraction.Hovered, true) => HoveredPressedButton,
                (UiInteraction.Hovered, false) => HoveredButton,
                _ => NormalButton,
            };
        }
    }

    private static void Press(BehaviorContext ctx, Entity entity, Button button)
    {
        // A setting's button takes the choice from the one that had it.
        if (button.Quality is { } quality && quality != _displayQuality || button.Volume is { } volume && volume != _volume)
        {
            foreach (var (other, chosen) in Buttons)
            {
                if (!chosen.Selected || other == entity) continue;
                chosen.Selected = false;
                ctx.Ecs.Wrap<BackgroundColorRef>(other).Value = NormalButton;
            }

            button.Selected = true;
            if (button.Quality is { } q) _displayQuality = q;
            if (button.Volume is { } v) _volume = v;
        }

        switch (button.Action)
        {
            case MenuButtonAction.Quit:
                ctx.Exit();
                break;
            case MenuButtonAction.Play:
                ctx.SetState(GameState.Game);
                ctx.SetState(MenuState.Disabled);
                break;
            case MenuButtonAction.Settings or MenuButtonAction.BackToSettings:
                ctx.SetState(MenuState.Settings);
                break;
            case MenuButtonAction.SettingsDisplay:
                ctx.SetState(MenuState.SettingsDisplay);
                break;
            case MenuButtonAction.SettingsSound:
                ctx.SetState(MenuState.SettingsSound);
                break;
            case MenuButtonAction.BackToMainMenu:
                ctx.SetState(MenuState.Main);
                break;
        }
    }
}
