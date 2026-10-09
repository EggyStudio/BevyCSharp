// Bevy's fixed_timestep example, examples/ecs/fixed_timestep.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how to create systems that run every fixed timestep, rather than every tick, here twice a
// second while the frames run at sixty.
internal static class FixedTimestep
{
    private static float _lastFrame, _lastFixed;
    private static int _steps;

    // Bevy's Time::<Fixed>::from_seconds(0.5), as a rate.
    public static void Configure(Config config) => config.FixedHz = 2;

    public static void Build(App app)
    {
        (_lastFrame, _lastFixed, _steps) = (0f, 0f, 0);

        app.Update(ctx =>
        {
            Console.WriteLine(FormattableString.Invariant($"time since last frame_update: {ctx.Time.Elapsed - _lastFrame}"));
            _lastFrame = ctx.Time.Elapsed;
        }, "fixed_timestep.FrameUpdate");

        app.On(Stage.FixedUpdate, ctx =>
        {
            _steps++;
            Console.WriteLine(FormattableString.Invariant($"time since last fixed_update: {ctx.Time.Elapsed - _lastFixed}\n"));
            Console.WriteLine(FormattableString.Invariant($"fixed timestep: {ctx.Time.FixedDelta}\n"));

            // The time run past the steps taken so far, which Bevy calls the overstep and carries
            // into the next step.
            Console.WriteLine(FormattableString.Invariant($"time accrued toward next fixed_update: {ctx.Time.Elapsed - _steps * ctx.Time.FixedDelta}\n"));
            _lastFixed = ctx.Time.Elapsed;
        }, "fixed_timestep.FixedUpdate");
    }
}
