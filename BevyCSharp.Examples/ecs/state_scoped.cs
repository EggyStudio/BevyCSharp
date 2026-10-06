// Bevy's state_scoped example, examples/ecs/state_scoped.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how to spawn entities that are automatically despawned either when entering or exiting
// specific game states. The state moves on each second, A to B to C(1) and back, and each says it
// holds and that it will be back, the one going as the state leaves and the other as it returns.
// C(1) is a state holding a value, so its texts go by a rule over the transition, which reads the
// value, rather than with one value.
internal static class StateScoped
{
    // Bevy's GameState, its C(u8) the value C(1) the example ever holds, an enum's member here.
    internal enum GameState { A, B, C1 }

    private static GameTimer _tickTock;

    public static void Build(App app)
    {
        app.AddState(GameState.A);
        app.Startup(_ =>
        {
            Render.SpawnCamera3d();
            _tickTock = GameTimer.FromSeconds(1f, TimerMode.Repeating);
        }, "state_scoped.SetupCamera");

        Edge(app, GameState.A, entering: true, "on_a_enter", ecs => ecs.DespawnOnExit(Say(ecs, "Game is in state 'A'", 0f, 0f), GameState.A));
        Edge(app, GameState.A, entering: false, "on_a_exit", ecs => ecs.DespawnOnEnter(Say(ecs, "Game state 'A' will be back in 1 second", 0f, 500f), GameState.A));
        Edge(app, GameState.B, entering: true, "on_b_enter", ecs => ecs.DespawnOnExit(Say(ecs, "Game is in state 'B'", 50f, 0f), GameState.B));
        Edge(app, GameState.B, entering: false, "on_b_exit", ecs => ecs.DespawnOnEnter(Say(ecs, "Game state 'B' will be back in 1 second", 50f, 500f), GameState.B));

        // Bevy's DespawnWhen, the rule reading the transition as a match on C(_) does.
        Edge(app, GameState.C1, entering: true, "on_c_1_enter", ecs =>
            ecs.DespawnWhen<GameState>(Say(ecs, "Game is in state 'C(1)'", 100f, 0f), transition => transition.Exited == GameState.C1));
        Edge(app, GameState.C1, entering: false, "on_c_1_exit", ecs =>
            ecs.DespawnWhen<GameState>(Say(ecs, "Game state 'C(1)' will be back in 1 second", 100f, 500f), transition => transition.Entered == GameState.C1));

        app.Update(Toggle, "state_scoped.Toggle");
    }

    // A system on entering or leaving a value, which says so as Bevy's info! does.
    private static void Edge(App app, GameState state, bool entering, string name, Action<EcsWorld> spawn) =>
        app.AddStateSystem(state, entering, new SystemDescriptor(world =>
        {
            Console.WriteLine(name);
            spawn(world.Resource<EcsWorld>());
        }, $"state_scoped.{name}"));

    private static Entity Say(EcsWorld ecs, string text, float top, float left) =>
        Ui.SpawnText(text, new UiSettings { Absolute = true, Top = Length.Px(top), Left = Length.Px(left), Color = Color.FromSrgb(0.5f, 0.5f, 1f) }, 33f);

    // Bevy's toggle, the state moved on each second.
    private static void Toggle(BehaviorContext ctx)
    {
        if (!_tickTock.Tick(ctx.Time.Delta).JustFinished) return;
        ctx.SetState(ctx.State<GameState>() switch
        {
            GameState.A => GameState.B,
            GameState.B => GameState.C1,
            _ => GameState.A,
        });
    }
}
