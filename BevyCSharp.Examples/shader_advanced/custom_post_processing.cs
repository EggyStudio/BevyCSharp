// Bevy's custom_post_processing example, examples/shader_advanced/custom_post_processing.rs at
// v0.19.1, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// A pass of the example's own over what the camera drew, a chromatic aberration whose strength
// rises and falls, over a turning cube on white. Bevy writes the render graph node, its pipeline
// and its bind group for it, and here the shader is a program with a pass stage the camera runs.
internal static class CustomPostProcessing
{
    // The pass's values on the GPU, which the camera's settings are copied into each frame.
    internal static ShaderInstance Settings;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY), new CameraSettings { Clear = ClearMode.Custom, ClearColor = (1f, 1f, 1f, 1f) });
        Settings = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/post_processing.slang" }));
        Shaders.SetPasses(camera, new ShaderPass(Settings, AfterTonemapping: true));
        ecs.Add(camera, new PostProcessSettings { Intensity = 0.02f });

        var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));
        ecs.Add(cube, new PostProcessRotates());

        // Bevy's directional light at a tenth of its default, unturned, so shining along -Z.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 1_000f, Shadows = false });
        ecs.Add(sun, Transform.Identity);
    }, "custom_post_processing.Setup");
}

/// <summary>A camera's chromatic aberration, how far apart the pass pulls the colors.</summary>
[Behavior]
public partial struct PostProcessSettings
{
    /// <summary>How strong it is.</summary>
    public float Intensity;

    /// <summary>Risen and fallen with the time, by the sine of a sine, up to a little over a hundredth.</summary>
    [OnUpdate]
    public void UpdateSettings(BehaviorContext ctx) =>
        Intensity = (MathF.Sin(MathF.Sin(ctx.Time.Elapsed)) * 0.5f + 0.5f) * 0.015f;

    /// <summary>
    /// Copied into the pass's values once everything else has set it, as Bevy extracts the
    /// component into its render world each frame.
    /// </summary>
    [OnPostUpdate]
    public void Extract(BehaviorContext ctx) => CustomPostProcessing.Settings.Set("settings.intensity", Intensity);
}

/// <summary>
/// The cube, which turns about X and Z, Bevy's <c>Rotates</c> under another name since
/// shader_prepass's shares the namespace.
/// </summary>
[Behavior]
public partial struct PostProcessRotates
{
    /// <summary>Turned about the world's X and then its Z, as Bevy's <c>rotate_x</c> and <c>rotate_z</c> turn it.</summary>
    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationZ(0.15f * ctx.Time.Delta) * Quat.FromRotationX(0.55f * ctx.Time.Delta) * transform.Rotation;
}
