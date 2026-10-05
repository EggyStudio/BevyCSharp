using Bevy;

namespace BevyCSharp.Examples.Animations;

// Plays an animation on a skinned glTF model of a fox, its third clip, a run, over and over once
// the model has arrived.
internal static class AnimatedMesh
{
    private static Entity _fox;
    private static bool _playing;

    public static void Build(App app)
    {
        (_fox, _playing) = (Entity.None, false);
        app.Startup(ctx => FoxScene.Setup(ctx.Ecs), "animated_mesh.SetupCameraAndEnvironment");
        app.SpawnGltf("models/animated/Fox.glb", (_, root) => _fox = root);

        // Bevy plays the clip once its scene is ready, as this does once the clips are there.
        app.Update(_ =>
        {
            if (_playing || _fox == Entity.None || !Animation.TryClips(_fox, out var clips) || clips.Count < 3) return;
            _playing = Animation.Play(_fox, clips[2], new AnimationSettings { Repeat = true });
        }, "animated_mesh.PlayAnimationWhenReady");
    }
}

/// <summary>The ground, light and camera Bevy's fox examples share.</summary>
internal static class FoxScene
{
    public static void Setup(EcsWorld ecs)
    {
        Render.SetAmbientLight((1f, 1f, 1f), 2000f);
        ecs.Camera(Transform.LookingAt(new Vec3(100f, 100f, 150f), new Vec3(0f, 20f, 0f), Vec3.UnitY));
        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 500_000f, 500_000f), Scene.Material(Scene.Srgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

        // Bevy's EulerRot::ZYX of nothing, one radian and minus a quarter turn, its cascades
        // reaching four hundred units.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
        ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
        Render.SetShadowCascades(sun, maximum: 400f, firstBound: 200f);
    }
}
