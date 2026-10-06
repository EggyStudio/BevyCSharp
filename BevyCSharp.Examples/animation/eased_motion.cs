// Bevy's eased_motion example, examples/animation/eased_motion.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Shows motion along eased curves, built into an animation clip in code. An orange cube slides back
// and forth with a cubic ease in and out, and turns a quarter turn with an elastic ease that
// overshoots and settles, over and over.
internal static class EasedMotion
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;

        // The clip, its target named as the cube is named, a slide taking three seconds each
        // way and a turn taking four.
        var clip = Animation.CreateClip();
        var target = AnimationTarget.FromNames("Cube");
        Animation.AddCurve(clip, target, AnimationCurve.Translation(new Vec3(-6f, 2f, 0f), new Vec3(6f, 2f, 0f), EaseFunction.CubicInOut, 3f).PingPong());

        // The repetition here is an illusion caused by the symmetry of the cube, which turns on the
        // forward journey and never turns back.
        Animation.AddCurve(clip, target, AnimationCurve.Rotation(Quat.Identity, Quat.FromRotationY(MathF.PI / 2f), EaseFunction.ElasticInOut, 4f));
        var (graph, node) = Animation.GraphFromClip(clip);

        // A cube together with what animates it, a player that plays the clip over and over and
        // the target it moves, itself.
        var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f), Render.CreateMaterial(Color.FromSrgb8(255, 165, 0)), Transform.At(-6f, 2f, 0f));
        ecs.SetName(cube, "Cube");
        Animation.PlayGraph(cube, graph, node, repeat: true);
        Animation.Animate(cube, target, player: cube);

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 10_000_000f, Range = 100f, Shadows = true });
        ecs.Add(light, Transform.At(8f, 16f, 8f));
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 50f, 50f), Render.CreateMaterial(Color.FromSrgb8(192, 192, 192)), Transform.Identity);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 6f, 12f), new Vec3(0f, 1.5f, 0f), Vec3.UnitY));
    }, "eased_motion.Setup");
}
