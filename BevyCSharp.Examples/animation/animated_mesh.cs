// Bevy's animated_mesh example, examples/animation/animated_mesh.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Plays an animation on a skinned glTF model of a fox, its third clip, a run, over and over once
// the model has arrived.
internal static class AnimatedMesh
{
    private const string GltfPath = "models/animated/Fox.glb";

    public static void Build(App app)
    {
        // The fox's root, spawned at once, and the clip it plays, its scene arriving under it later.
        app.Startup(ctx => ctx.Ecs.Add(ctx.Ecs.SpawnScene(AssetServer.LoadGltfScene(GltfPath)), new AnimationToPlay { Clip = 2 }), "animated_mesh.SetupMeshAndAnimation");
        app.Startup(ctx => FoxScene.Setup(ctx.Ecs), "animated_mesh.SetupCameraAndEnvironment");
    }
}

/// <summary>A model's clip, played over and over once the model has arrived.</summary>
[Behavior]
public partial struct AnimationToPlay
{
    /// <summary>Which of the model's clips, by its place in the file.</summary>
    public int Clip;

    /// <summary>Whether it has been started.</summary>
    public bool Playing;

    /// <summary>
    /// The clip played on repeat once the model's clips are there, as Bevy plays it when the
    /// model's scene is ready.
    /// </summary>
    [OnUpdate]
    public void PlayAnimationWhenReady(BehaviorContext ctx)
    {
        if (Playing || !Animation.TryClips(ctx.Entity, out var clips) || clips.Count <= Clip) return;
        Playing = Animation.Play(ctx.Entity, clips[Clip], new AnimationSettings { Repeat = true });
    }
}

/// <summary>The ground, light and camera Bevy's fox examples share.</summary>
internal static class FoxScene
{
    public static void Setup(EcsWorld ecs)
    {
        Render.SetAmbientLight((1f, 1f, 1f), 2000f);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(100f, 100f, 150f), new Vec3(0f, 20f, 0f), Vec3.UnitY));
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 500_000f, 500_000f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);

        // Bevy's EulerRot::ZYX of nothing, one radian and minus a quarter turn, its cascades
        // reaching four hundred units.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = true });
        ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationY(1f) * Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
        Render.SetShadowCascades(sun, maximum: 400f, firstBound: 200f);
    }
}
