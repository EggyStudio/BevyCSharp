// Bevy's sub_states example, examples/state/sub_states.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using AppState = BevyCSharp.Examples.States.StatesExample.AppState;

namespace BevyCSharp.Examples.States;

// Using sub-states for a hierarchy of states, the game of the states example with a pause that
// exists only while the game does, Space pausing it and the pause screen going when it ends.
internal static class SubStates
{
    public static void Build(App app)
    {
        app.AddState(AppState.Menu);
        app.AddSubState(IsPaused.Running);
        app.Startup(_ => Render2d.SpawnCamera2d(), "sub_states.Setup");

        app.AddStateSystem(AppState.Menu, entering: true, new SystemDescriptor(world => StatesExample.SetupMenu(new BehaviorContext(world)), "sub_states.SetupMenu"));
        app.On(Stage.Update, StatesExample.Menu, "sub_states.Menu", BehaviorConditions.InState(AppState.Menu));
        app.AddStateSystem(AppState.Menu, entering: false, new SystemDescriptor(world => StatesExample.CleanupMenu(new BehaviorContext(world)), "sub_states.CleanupMenu"));
        app.AddStateSystem(AppState.InGame, entering: true, new SystemDescriptor(world => StatesExample.SetupGame(new BehaviorContext(world)), "sub_states.SetupGame"));
        app.AddStateSystem(IsPaused.Paused, entering: true, new SystemDescriptor(world => SetupPausedScreen(new BehaviorContext(world)), "sub_states.SetupPausedScreen"));

        app.On(Stage.Update, StatesExample.Movement, "sub_states.Movement", BehaviorConditions.InState(IsPaused.Running));
        app.On(Stage.Update, StatesExample.ChangeColor, "sub_states.ChangeColor", BehaviorConditions.InState(IsPaused.Running));
        app.On(Stage.Update, ctx =>
        {
            if (ctx.Input.KeyPressed(Key.Space))
                ctx.SetState(ctx.State<IsPaused>() == IsPaused.Running ? IsPaused.Paused : IsPaused.Running);
        }, "sub_states.TogglePause", BehaviorConditions.InState(AppState.InGame));

        // Bevy's log_transitions, from its dev tools, printing each move of the app's state.
        app.AddTransitionSystem(AppState.Menu, AppState.InGame, new SystemDescriptor(_ => Console.WriteLine("AppState transition: Some(Menu) => Some(InGame)"), "sub_states.LogTransitions"));
    }

    // A gray square saying Paused, despawned as the pause ends.
    private static void SetupPausedScreen(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var screen = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Direction = UiDirection.Column,
            RowGap = Length.Px(10f),
        });
        ecs.DespawnOnExit(screen, IsPaused.Paused);

        var normal = Color.FromSrgb(0.15f, 0.15f, 0.15f);
        var square = Ui.SpawnNode(new UiSettings { Width = Length.Px(400f), Height = Length.Px(400f), Justify = UiJustify.Center, Align = UiAlign.Center, Color = (normal.R, normal.G, normal.B, 1f) });
        ecs.SetParent(square, screen);

        var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);
        ecs.SetParent(Ui.SpawnText("Paused", new UiSettings { Color = (light.R, light.G, light.B, 1f) }, 33f), square);
    }
}

/// <summary>Whether the game of <c>sub_states</c> is paused, a state that exists only in the game.</summary>
[SubStateOf(typeof(StatesExample.AppState), StatesExample.AppState.InGame)]
internal enum IsPaused
{
    /// <summary>Playing.</summary>
    Running,

    /// <summary>Stopped, with the pause screen up.</summary>
    Paused,
}
