using Bevy;

namespace BevyCSharp.Examples.Animations;

// Plays an animation of a glTF model's morph targets, shapes its vertices blend between, over and
// over.
//
// Bevy's also prints the names of each mesh's morph targets as the mesh arrives, which no call
// here reads, so this is written in part.
internal static class MorphTargets
{
    private static Entity _model;
    private static bool _playing;

    public static void Build(App app)
    {
        (_model, _playing) = (Entity.None, false);
        app.Startup(ctx =>
        {
            Render.SetAmbientLight((1f, 1f, 1f), 150f);
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
            ctx.Ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationZ(MathF.PI / 2f), Vec3.One));
            ctx.Ecs.Camera(Transform.LookingAt(new Vec3(3f, 2.1f, 10.2f), Vec3.Zero, Vec3.UnitY));
        }, "morph_targets.Setup");

        app.SpawnGltf("models/animated/MorphStressTest.gltf", (_, root) => _model = root);
        app.Update(_ =>
        {
            if (_playing || _model == Entity.None || !Animation.TryClips(_model, out var clips) || clips.Count < 3) return;
            _playing = Animation.Play(_model, clips[2], new AnimationSettings { Repeat = true });
        }, "morph_targets.PlayAnimationWhenReady");
    }
}
