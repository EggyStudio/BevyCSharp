// Bevy's custom_transitions example, examples/state/custom_transitions.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;
using AppState = BevyCSharp.Examples.States.StatesExample.AppState;

namespace BevyCSharp.Examples.States;

// Transitions to the same state, the game of the states example started again by its Restart Game
// button. Bevy's adds schedules of its own run on such a transition, OnReenter and OnReexit, which
// take the game down and build it again. Here the state's leaving and entering do that, since
// Bevy's NextState::set, which setting a state calls, runs them on a value set again as on any
// other move.
internal static class CustomTransitions
{
    public static void Build(App app)
    {
        app.AddState(AppState.Menu);
        app.Startup(ctx =>
        {
            Render2d.SpawnCamera2d();
            StatesExample.ObserveActivate(ctx.Ecs);

            // Bevy's trigger_game_restart, the Restart Game button activated in the game setting
            // the state again to the value it holds.
            ctx.Ecs.Observe<Activate>(on =>
            {
                if (StateRegistry.Current<AppState>() == AppState.InGame) on.Context.SetState(AppState.InGame);
            });
        }, "custom_transitions.Setup");

        app.AddStateSystem(AppState.Menu, entering: true, new SystemDescriptor(world => StatesExample.SetupMenu(new BehaviorContext(world)), "custom_transitions.SetupMenu"));
        app.AddStateSystem(AppState.Menu, entering: false, new SystemDescriptor(world => StatesExample.CleanupMenu(new BehaviorContext(world)), "custom_transitions.CleanupMenu"));

        // Bevy's OnReenter and OnReexit, run as the game is entered or left, a restart among them.
        app.AddStateSystem(AppState.InGame, entering: true, new SystemDescriptor(world =>
        {
            StatesExample.SetupGame(new BehaviorContext(world));
            SetupInterface(world.Resource<EcsWorld>());
            Console.WriteLine("Setup game");
        }, "custom_transitions.SetupGame"));
        app.AddStateSystem(AppState.InGame, entering: false, new SystemDescriptor(world =>
        {
            world.Resource<EcsWorld>().Despawn(StatesExample.Logo);
            Console.WriteLine("Teardown game");
        }, "custom_transitions.TeardownGame"));

        app.On(Stage.Update, StatesExample.Movement, "custom_transitions.Movement", BehaviorConditions.InState(AppState.InGame));
        app.On(Stage.Update, StatesExample.ChangeColor, "custom_transitions.ChangeColor", BehaviorConditions.InState(AppState.InGame));

        // Bevy's log_transitions, from its dev tools, the restart among them as a move to itself.
        app.Update(ctx =>
        {
            foreach (var t in ctx.Read<StateTransitionEvent<AppState>>())
                Console.WriteLine($"AppState transition: {(t.Exited is { } from ? $"Some({from})" : "None")} => {(t.Entered is { } to ? $"Some({to})" : "None")}");
        }, "custom_transitions.LogTransitions");
    }

    // The interface of Bevy's setup_game, the hint and the Restart Game button, a button of Bevy's
    // widgets green while it is pressed. Spawned again on each restart over the last, as Bevy's
    // are, since its teardown takes the logo alone.
    private static void SetupInterface(EcsWorld ecs)
    {
        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f) });
        var hint = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(10f), Top = Length.Px(10f) });
        ecs.SetParent(hint, root);
        ecs.SetParent(Ui.SpawnText("Move with arrow keys.", new UiSettings()), hint);

        var button = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(10f),
            Bottom = Length.Px(10f),
            Padding = Sides.All(Length.Px(5f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = StatesExample.Normal,
        });
        ecs.Insert<ButtonRef>(button);
        ecs.Insert<HoveredRef>(button);
        ecs.Add(button, new HoverStyled());
        ecs.Observe<Add<PressedRef>>(button, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = StatesExample.Pressed);
        ecs.Observe<Remove<PressedRef>>(button, on =>
            on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = on.Ecs.Get<HoveredRef>(on.Entity)?.Value == true ? StatesExample.Hovered : StatesExample.Normal);
        ecs.SetParent(button, root);

        var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);
        ecs.SetParent(Ui.SpawnText("Restart Game", new UiSettings { Color = (light.R, light.G, light.B, 1f) }, 33f), button);
    }
}
