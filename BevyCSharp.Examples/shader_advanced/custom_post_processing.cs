using Bevy;

namespace BevyCSharp.Examples.Shading;

// A pass of the example's own over what the camera drew, a chromatic aberration whose strength
// rises and falls, over a turning cube on white. Bevy writes the render graph node, its pipeline
// and its bind group for it, and here the shader is a program with a pass stage the camera runs.
internal static class CustomPostProcessing
{
    private static Entity _cube;
    private static ShaderInstance _settings;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var camera = ecs.Camera(Transform.LookingAt(new Vec3(0f, 0f, 5f), Vec3.Zero, Vec3.UnitY), new CameraSettings { Clear = ClearMode.Custom, ClearColor = (1f, 1f, 1f, 1f) });
            _settings = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings { Pass = "shaders/post_processing.slang" })).Set("settings.intensity", 0.02f);
            Shaders.SetPasses(camera, new ShaderPass(_settings, AfterTonemapping: true));

            _cube = ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Scene.Material(Scene.Srgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));

            // Bevy's directional light at a tenth of its default, unturned, so shining along -Z.
            var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 1_000f, Shadows = false });
            ecs.Add(sun, Transform.Identity);
        }, "custom_post_processing.Setup");

        app.Update(ctx =>
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(_cube);
            // About the world's X and then its Z, as Bevy's rotate_x and rotate_z turn.
            var turn = Quat.FromRotationZ(0.15f * ctx.Time.Delta) * Quat.FromRotationX(0.55f * ctx.Time.Delta) * at.Rotation;
            ctx.Ecs.Set(_cube, at with { Rotation = turn });
        }, "custom_post_processing.Rotate");

        app.Update(ctx =>
        {
            var intensity = MathF.Sin(MathF.Sin(ctx.Time.Elapsed)) * 0.5f + 0.5f;
            _settings.Set("settings.intensity", intensity * 0.015f);
        }, "custom_post_processing.UpdateSettings");
    }
}
