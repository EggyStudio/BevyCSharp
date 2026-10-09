// Bevy's translation example, examples/transforms/translation.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Illustrates how to move an object along an axis, a cube sliding along its own X and turning back
// once it is five units from where it started.
internal static class Translation
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        // Where the cube starts, which it measures its distance from.
        var spawn = Vec3.Zero;
        ctx.Ecs.Add(CubeScene.Spawn(ctx.Ecs, new Transform(spawn)), new Movable { Spawn = spawn, MaxDistance = 5f, Speed = 2f });
    }, "translation.Setup");
}

/// <summary>A thing sliding back and forth along its own X, never farther than its distance from where it started.</summary>
[Behavior]
public partial struct Movable
{
    /// <summary>Where it started.</summary>
    public Vec3 Spawn;

    /// <summary>How far from where it started it goes before it turns back.</summary>
    public float MaxDistance;

    /// <summary>How fast it moves, negative while it comes back.</summary>
    public float Speed;

    /// <summary>
    /// Moved along its own X, which is the world's while it is not turned, and turned back once it
    /// is too far from where it started.
    /// </summary>
    [OnUpdate]
    public void MoveCube(BehaviorContext ctx, ref Transform transform)
    {
        if ((Spawn - transform.Translation).Length > MaxDistance) Speed = -Speed;
        transform.Translation += transform.Rotation * Vec3.UnitX * Speed * ctx.Time.Delta;
    }
}
