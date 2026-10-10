// Bevy's computed_states example, examples/state/computed_states.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.States;

// Advanced state patterns using computed states. The app's state holds values, a game paused or in
// turbo, and states worked out from it say whether the game is on, whether it is paused, whether
// it is in turbo, and which tutorial to show, each with systems of its own. Space pauses, T turns
// turbo on and off, Escape goes back to the menu, whose Tutorial checkbox turns the tutorial off.
internal static class ComputedStatesExample
{
    // Bevy's AppState, Menu or InGame { paused, turbo }, its values as bits of the one number a
    // state holds here: InGame, and with it Paused and Turbo where they are set.
    [Flags]
    internal enum AppState { Menu = 0, InGame = 1, Paused = 2, Turbo = 4 }

    internal enum TutorialState { Active, Inactive }

    // Bevy's InGame, a computed state that exists while the game is on.
    [ComputedFrom(typeof(AppState))]
    internal enum InGame { Present }

    // Bevy's TurboMode, which exists while the game is on in turbo.
    [ComputedFrom(typeof(AppState))]
    internal enum TurboMode { Present }

    [ComputedFrom(typeof(AppState))]
    internal enum IsPaused { NotPaused, Paused }

    // Which instructions the tutorial shows, while it is on and the game is.
    [ComputedFrom(typeof(TutorialState), typeof(InGame), typeof(IsPaused))]
    internal enum Tutorial { MovementInstructions, PauseInstructions }

    private const float Speed = 100f;
    private const float TurboSpeed = 300f;

    internal static readonly Color NormalButton = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    internal static readonly Color HoveredButton = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    internal static readonly Color PressedButton = Color.FromSrgb(0.35f, 0.75f, 0.35f);
    internal static readonly Color ActiveButton = Color.FromSrgb(0.15f, 0.85f, 0.15f);
    internal static readonly Color HoveredActiveButton = Color.FromSrgb(0.25f, 0.55f, 0.25f);
    internal static readonly Color PressedActiveButton = Color.FromSrgb(0.35f, 0.95f, 0.35f);

    private static Entity _menu, _logo;

    public static void Build(App app)
    {
        app.AddState(AppState.Menu);
        app.AddState(TutorialState.Active);
        app.AddComputedState<InGame, AppState>(state => state.HasFlag(AppState.InGame) ? InGame.Present : null);
        app.AddComputedState<IsPaused, AppState>(state => !state.HasFlag(AppState.InGame) ? null : state.HasFlag(AppState.Paused) ? IsPaused.Paused : IsPaused.NotPaused);
        app.AddComputedState<TurboMode, AppState>(state => state.HasFlag(AppState.InGame | AppState.Turbo) ? TurboMode.Present : null);
        app.AddComputedState<Tutorial, TutorialState, InGame, IsPaused>((tutorial, _, paused) =>
            tutorial != TutorialState.Active ? null : paused == IsPaused.Paused ? Tutorial.PauseInstructions : Tutorial.MovementInstructions);

        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();

            // Bevy's menu_activate and menu_tutorial_checked, the Play button starting the game and
            // the Tutorial checkbox turning the tutorial on and off, while the menu is up.
            ctx.Ecs.Observe<Activate>(on =>
            {
                if (StateRegistry.Current<AppState>() == AppState.Menu) on.Context.SetState(AppState.InGame);
            });
            ctx.Ecs.Observe<ValueChange<bool>>(on =>
            {
                if (StateRegistry.Current<AppState>() == AppState.Menu)
                    on.Context.SetState(on.Event.Value ? TutorialState.Active : TutorialState.Inactive);
            });
        }, "computed_states.Setup");
        OnEnter(app, AppState.Menu, SetupMenu);
        app.On(Stage.Update, MenuStyling, "computed_states.MenuStyling", BehaviorConditions.InState(AppState.Menu));
        app.AddStateSystem(AppState.Menu, entering: false, new SystemDescriptor(world => world.Resource<EcsWorld>().Despawn(_menu), "computed_states.CleanupMenu"));
        OnEnter(app, InGame.Present, SetupGame);

        foreach (var (system, name) in new (Action<BehaviorContext>, string)[] { (TogglePause, "TogglePause"), (ChangeColor, "ChangeColor"), (QuitToMenu, "QuitToMenu") })
            app.On(Stage.Update, system, $"computed_states.{name}", BehaviorConditions.InState(InGame.Present));
        app.On(Stage.Update, ToggleTurbo, "computed_states.ToggleTurbo", BehaviorConditions.InState(IsPaused.NotPaused));
        app.On(Stage.Update, Movement, "computed_states.Movement", BehaviorConditions.InState(IsPaused.NotPaused));

        OnEnter(app, IsPaused.Paused, SetupPausedScreen);
        OnEnter(app, TurboMode.Present, SetupTurboText);
        OnEnter(app, Tutorial.MovementInstructions, ecs => Instructions(ecs, Tutorial.MovementInstructions,
            "Move the bevy logo with the arrow keys", "Press T to enter TURBO MODE", "Press SPACE to pause", "Press ESCAPE to return to the menu"));
        OnEnter(app, Tutorial.PauseInstructions, ecs => Instructions(ecs, Tutorial.PauseInstructions,
            "Press SPACE to resume", "Press ESCAPE to return to the menu"));

        // Bevy's log_transitions, from its dev tools, for the two states set by hand.
        app.Update(ctx =>
        {
            foreach (var t in ctx.Read<StateTransitionEvent<AppState>>()) Console.WriteLine($"AppState transition: {Some(t.Exited, Named)} => {Some(t.Entered, Named)}");
            foreach (var t in ctx.Read<StateTransitionEvent<TutorialState>>()) Console.WriteLine($"TutorialState transition: {Some(t.Exited, s => s.ToString())} => {Some(t.Entered, s => s.ToString())}");
        }, "computed_states.LogTransitions");
    }

    private static void OnEnter<TState>(App app, TState state, Action<EcsWorld> setup) where TState : struct, Enum =>
        app.AddStateSystem(state, entering: true, new SystemDescriptor(world => setup(world.Resource<EcsWorld>()), $"computed_states.Enter{state}"));

    // Rust's debug form, Some(value) or None, and the app's state written as Bevy's variant is.
    private static string Some<T>(T? value, Func<T, string> name) where T : struct => value is { } v ? $"Some({name(v)})" : "None";

    private static string Named(AppState state) => state.HasFlag(AppState.InGame)
        ? $"InGame {{ paused: {state.HasFlag(AppState.Paused).ToString().ToLowerInvariant()}, turbo: {state.HasFlag(AppState.Turbo).ToString().ToLowerInvariant()} }}"
        : "Menu";

    // Bevy's toggle_pause and toggle_turbo, the one value's bit turned over and the other kept.
    private static void TogglePause(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Space)) ctx.SetState(ctx.State<AppState>() ^ AppState.Paused);
    }

    private static void ToggleTurbo(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(LogicalKey.Character("t"))) ctx.SetState(ctx.State<AppState>() ^ AppState.Turbo);
    }

    private static void QuitToMenu(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Escape)) ctx.SetState(AppState.Menu);
    }

    private static void SetupMenu(EcsWorld ecs)
    {
        _menu = Column(ecs, UiJustify.Center);

        // A widget of Bevy's, hovered as the pointer is over it and acting as it is pressed.
        Entity Widget(MenuButtonKind kind, Color color)
        {
            var widget = Ui.SpawnNode(new UiSettings { Width = Length.Px(200f), Height = Length.Px(65f), Justify = UiJustify.Center, Align = UiAlign.Center, Color = color });
            ecs.Insert<HoveredRef>(widget);
            ecs.Insert<ActivateOnPressRef>(widget);
            ecs.Add(widget, new MenuButton { Kind = kind });
            ecs.SetParent(widget, _menu);
            ecs.SetParent(Ui.SpawnText(kind.ToString(), new UiSettings { Color = Color.FromSrgb(0.9f, 0.9f, 0.9f) }, 33f), widget);
            return widget;
        }

        ecs.Insert<ButtonRef>(Widget(MenuButtonKind.Play, NormalButton));

        // The tutorial a checkbox, checked while it is on, which checks and unchecks itself.
        var active = StateRegistry.Current<TutorialState>() == TutorialState.Active;
        var tutorial = Widget(MenuButtonKind.Tutorial, active ? ActiveButton : NormalButton);
        ecs.Insert<CheckboxRef>(tutorial);
        if (active) ecs.Insert<CheckedRef>(tutorial);
        Ui.SelfUpdate(tutorial, UiWidgetKind.Checkbox);
    }

    // Bevy's menu_styling, each widget colored by whether it is pressed or hovered, the tutorial's
    // greener while the tutorial is on.
    private static void MenuStyling(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var tutorialOn = StateRegistry.Current<TutorialState>() == TutorialState.Active;
        foreach (var widget in ecs.EntitiesWith<MenuButton>())
        {
            var on = tutorialOn && ecs.GetOrDefault<MenuButton>(widget).Kind == MenuButtonKind.Tutorial;
            var pressed = ecs.Get<PressedRef>(widget) is not null;
            var hovered = ecs.Get<HoveredRef>(widget)?.Value == true;
            ecs.Wrap<BackgroundColorRef>(widget).Value = (pressed, hovered, on) switch
            {
                (true, _, true) => PressedActiveButton,
                (true, _, false) => PressedButton,
                (false, true, true) => HoveredActiveButton,
                (false, true, false) => HoveredButton,
                (false, false, true) => ActiveButton,
                _ => NormalButton,
            };
        }
    }

    private static void SetupGame(EcsWorld ecs)
    {
        _logo = ecs.Spawn();
        ecs.Add(_logo, Transform.Identity);
        Render2d.SetSprite(ecs, _logo, AssetServer.Load(AssetKind.Image, "branding/icon.png"));
        ecs.DespawnOnExit(_logo, InGame.Present);
    }

    // Bevy's movement, faster in turbo.
    private static void Movement(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var direction = new Vec3(
            (input.KeyDown(Key.ArrowRight) ? 1f : 0f) - (input.KeyDown(Key.ArrowLeft) ? 1f : 0f),
            (input.KeyDown(Key.ArrowUp) ? 1f : 0f) - (input.KeyDown(Key.ArrowDown) ? 1f : 0f),
            0f);
        if (direction == Vec3.Zero || !ctx.Ecs.IsAlive(_logo)) return;

        var speed = App.TryState<TurboMode>(out _) ? TurboSpeed : Speed;
        var transform = ctx.Ecs.GetOrDefault<Transform>(_logo);
        transform.Translation += direction.Normalized * speed * ctx.Time.Delta;
        ctx.Ecs.Set(_logo, transform);
    }

    private static void ChangeColor(BehaviorContext ctx)
    {
        if (!ctx.Ecs.IsAlive(_logo) || ctx.Ecs.Get<SpriteRef>(_logo) is not { } sprite) return;
        sprite.Color = sprite.Color with { B = MathF.Sin(ctx.Time.Elapsed * 0.5f) + 2f };
    }

    // A node filling the window, its children in a column ten apart, placed along it as asked.
    private static Entity Column(EcsWorld ecs, UiJustify justify) => Ui.SpawnNode(new UiSettings
    {
        Width = Length.Percent(100f),
        Height = Length.Percent(100f),
        Justify = justify,
        Align = UiAlign.Center,
        Direction = UiDirection.Column,
        RowGap = Length.Px(10f),
        Absolute = true,
    });

    private static void SetupPausedScreen(EcsWorld ecs)
    {
        Console.WriteLine("Printing Pause");
        var screen = Column(ecs, UiJustify.Center);
        ecs.DespawnOnExit(screen, IsPaused.Paused);
        var square = Ui.SpawnNode(new UiSettings { Width = Length.Px(400f), Height = Length.Px(400f), Justify = UiJustify.Center, Align = UiAlign.Center, Color = NormalButton });
        ecs.SetParent(square, screen);
        ecs.SetParent(Ui.SpawnText("Paused", new UiSettings { Color = Color.FromSrgb(0.9f, 0.9f, 0.9f) }, 33f), square);
    }

    private static void SetupTurboText(EcsWorld ecs)
    {
        var top = Column(ecs, UiJustify.Start);
        ecs.DespawnOnExit(top, TurboMode.Present);
        ecs.SetParent(Ui.SpawnText("TURBO MODE", new UiSettings { Color = Color.FromSrgb(0.9f, 0.3f, 0.1f) }, 33f), top);
    }

    private static void Instructions(EcsWorld ecs, Tutorial which, params string[] lines)
    {
        var bottom = Column(ecs, UiJustify.End);
        ecs.DespawnOnExit(bottom, which);
        foreach (var line in lines) ecs.SetParent(Ui.SpawnText(line, new UiSettings { Color = Color.FromSrgb(0.3f, 0.3f, 0.7f) }, 33f), bottom);
    }
}

/// <summary>Which button of the menu an entity is, Bevy's <c>MenuButton</c> of <c>computed_states</c>.</summary>
[Behavior]
public partial struct MenuButton
{
    /// <summary>Whether it starts the game or turns the tutorial on and off.</summary>
    public MenuButtonKind Kind;
}

/// <summary>The buttons of <c>computed_states</c>'s menu.</summary>
public enum MenuButtonKind
{
    /// <summary>Starts the game.</summary>
    Play,

    /// <summary>Turns the tutorial on and off.</summary>
    Tutorial,
}
