// Bevy's 3d_rotation example, examples/transforms/3d_rotation.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Transforms;

// Illustrates how to rotate an object around an axis, a cube turning about Y at three tenths of a
// turn a second.
internal static class Rotation3d
{
    public static void Build(App app) =>
        app.Startup(ctx => ctx.Ecs.Add(CubeScene.Spawn(ctx.Ecs, Transform.Identity), new Rotatable { Speed = 0.3f }), "3d_rotation.Setup");
}

/// <summary>A thing turning about Y at its own speed.</summary>
[Behavior]
public partial struct Rotatable
{
    /// <summary>How fast it turns, in whole turns a second.</summary>
    public float Speed;

    /// <summary>
    /// Turned by its speed times a whole turn in radians times the frame's time, so the speed is
    /// how many turns it makes in a second at any frame rate.
    /// </summary>
    [OnUpdate]
    public void RotateCube(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationY(Speed * MathF.Tau * ctx.Time.Delta) * transform.Rotation;
}
