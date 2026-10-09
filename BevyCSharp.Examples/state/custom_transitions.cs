// Bevy's custom_transitions example, examples/state/custom_transitions.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using AppState = BevyCSharp.Examples.States.StatesExample.AppState;

namespace BevyCSharp.Examples.States;

// Transitions to the same state, the game of the states example started again with R. Bevy's adds
// schedules of its own run on such a transition, OnReenter and OnReexit, which take the game down
// and build it again. Here the state's leaving and entering do that, since Bevy's NextState::set,
// which setting a state calls, runs them on a value set again as on any other move.
internal static class CustomTransitions
{
    public static void Build(App app)
    {
        app.AddState(AppState.Menu);
        app.Startup(_ => Render2d.SpawnCamera2d(), "custom_transitions.Setup");

        app.AddStateSystem(AppState.Menu, entering: true, new SystemDescriptor(world => StatesExample.SetupMenu(new BehaviorContext(world)), "custom_transitions.SetupMenu"));
        app.On(Stage.Update, StatesExample.Menu, "custom_transitions.Menu", BehaviorConditions.InState(AppState.Menu));
        app.AddStateSystem(AppState.Menu, entering: false, new SystemDescriptor(world => StatesExample.CleanupMenu(new BehaviorContext(world)), "custom_transitions.CleanupMenu"));

        // Bevy's OnReenter and OnReexit, run as the game is entered or left, a restart among them.
        app.AddStateSystem(AppState.InGame, entering: true, new SystemDescriptor(world =>
        {
            StatesExample.SetupGame(new BehaviorContext(world));
            Console.WriteLine("Setup game");
        }, "custom_transitions.SetupGame"));
        app.AddStateSystem(AppState.InGame, entering: false, new SystemDescriptor(world =>
        {
            world.Resource<EcsWorld>().Despawn(StatesExample.Logo);
            Console.WriteLine("Teardown game");
        }, "custom_transitions.TeardownGame"));

        app.On(Stage.Update, StatesExample.Movement, "custom_transitions.Movement", BehaviorConditions.InState(AppState.InGame));
        app.On(Stage.Update, StatesExample.ChangeColor, "custom_transitions.ChangeColor", BehaviorConditions.InState(AppState.InGame));

        // Bevy's trigger_game_restart, the state set again to the value it holds.
        app.On(Stage.Update, ctx =>
        {
            if (ctx.Input.KeyPressed(Key.R)) ctx.SetState(AppState.InGame);
        }, "custom_transitions.TriggerGameRestart", BehaviorConditions.InState(AppState.InGame));

        // Bevy's log_transitions, from its dev tools, the restart among them as a move to itself.
        app.Update(ctx =>
        {
            foreach (var t in ctx.Read<StateTransitionEvent<AppState>>())
                Console.WriteLine($"AppState transition: {(t.Exited is { } from ? $"Some({from})" : "None")} => {(t.Entered is { } to ? $"Some({to})" : "None")}");
        }, "custom_transitions.LogTransitions");
    }
}
