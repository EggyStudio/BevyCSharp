// Bevy's morph_targets example, examples/animation/morph_targets.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Plays an animation of a glTF model's morph targets, shapes its vertices blend between, over and
// over.
//
// Bevy's also prints the names of each mesh's morph targets as the mesh arrives, which no call
// here reads, so this is written in part.
internal static class MorphTargets
{
    private const string GltfPath = "models/animated/MorphStressTest.gltf";

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        ecs.Add(ecs.SpawnScene(AssetServer.LoadGltfScene(GltfPath)), new MorphAnimationToPlay { Clip = 2 });

        Render.SetAmbientLight((1f, 1f, 1f), 150f);
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f, Shadows = false });
        ecs.Add(sun, new Transform(Vec3.Zero, Quat.FromRotationZ(MathF.PI / 2f), Vec3.One));
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(3f, 2.1f, 10.2f), Vec3.Zero, Vec3.UnitY));
    }, "morph_targets.Setup");
}

/// <summary>
/// A model's clip, played over and over once the model has arrived, Bevy's <c>AnimationToPlay</c>
/// under another name since animated_mesh's shares the namespace.
/// </summary>
[Behavior]
public partial struct MorphAnimationToPlay
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
