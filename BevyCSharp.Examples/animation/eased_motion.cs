// Bevy's eased_motion example, examples/animation/eased_motion.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Shows motion along eased curves: an orange cube sliding back and forth with a cubic ease in and
// out, and turning a quarter turn with an elastic ease that overshoots and settles, over and over.
//
// Bevy builds an animation clip of the two curves, which plays them on the cube. A clip built in
// code is not bridged, so here the same curves are sampled at the clip's time each frame and the
// cube placed by them, which is written in part.
internal static class EasedMotion
{
    private static Entity _cube;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 2f, 2f, 2f), Render.CreateMaterial(Color.FromSrgb8(255, 165, 0)), Transform.At(-6f, 2f, 0f));

            var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 10_000_000f, Range = 100f, Shadows = true });
            ecs.Add(light, Transform.At(8f, 16f, 8f));
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 50f, 50f), Render.CreateMaterial(Color.FromSrgb8(192, 192, 192)), Transform.Identity);
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 6f, 12f), new Vec3(0f, 1.5f, 0f), Vec3.UnitY));
        }, "eased_motion.Setup");

        app.Update(ctx =>
        {
            // The clip lasts as long as its longer curve: the slide there and back over six
            // seconds, and the turn over four, held at its end until the clip starts again.
            var time = ctx.Time.Elapsed % 6f;
            var slide = time <= 3f ? time / 3f : (6f - time) / 3f;
            var x = -6f + 12f * EaseFunction.CubicInOut.Sample(slide);
            var turn = EaseFunction.ElasticInOut.Sample(MathF.Min(time, 4f) / 4f);
            ctx.Ecs.Set(_cube, new Transform(new Vec3(x, 2f, 0f), Quat.FromRotationY(MathF.PI / 2f * turn), Vec3.One));
        }, "eased_motion.Animate");
    }
}
