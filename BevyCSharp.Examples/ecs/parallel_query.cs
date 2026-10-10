// Bevy's parallel_query example, examples/ecs/parallel_query.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Illustrates queries run in parallel, a hundred and twenty-eight logos drifting and bouncing off
// the window's edges, each moved by a behavior the engine splits across threads once there are
// enough of them.
internal static class ParallelQueryExample
{
    // The window's size, read once a frame before the logos move, as Bevy's system reads its
    // window once, since a method run for each logo may run on a thread the world is not lent to.
    // Read by a system of the example's own rather than a static method of the behavior, which
    // would run in every example and fail in one with no window.
    internal static (uint Width, uint Height) WindowSize;

    public static void Build(App app)
    {
        app.On(Stage.PreUpdate, _ => WindowSize = Window.Size(), "parallel_query.MeasureWindow");
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Render2d.SpawnCamera2d();
            var texture = AssetServer.Load(AssetKind.Image, "branding/icon.png");
            var random = new Random(19878367);

            for (var z = 0; z < 128; z++)
            {
                var sprite = ecs.Spawn();
                ecs.Add(sprite, new Transform(new Vec3(0f, 0f, z), Quat.Identity, new Vec3(0.1f)));
                ecs.Add(sprite, new Drift { X = 20f * (random.NextSingle() - 0.5f), Y = 20f * (random.NextSingle() - 0.5f) });
                Render2d.SetSprite(ecs, sprite, texture);
            }
        }, "parallel_query.Spawn");
    }
}

/// <summary>
/// A logo's speed in pixels a frame, which it moves by and turns back on at the window's edge, as
/// Bevy's <c>parallel_query</c> moves its sprites.
/// </summary>
/// <remarks>
/// A behavior's per-entity method is what runs in parallel here, split across the thread pool above
/// a few thousand entities, where Bevy's example asks for it with <c>par_iter_mut</c> and batches
/// of 32. It moves by a fixed step a frame, not by time, as Bevy's does.
/// </remarks>
[Behavior]
public partial struct Drift
{
    /// <summary>Pixels a frame across.</summary>
    public float X;

    /// <summary>Pixels a frame up.</summary>
    public float Y;

    [OnUpdate]
    public void Move(BehaviorContext ctx, ref Transform transform)
    {
        transform.Translation += new Vec3(X, Y, 0f);

        var (width, height) = ParallelQueryExample.WindowSize;
        var (x, y) = (transform.Translation.X, transform.Translation.Y);
        if (!(-width / 2f < x && x < width / 2f && -height / 2f < y && y < height / 2f)) (X, Y) = (-X, -Y);
    }
}
