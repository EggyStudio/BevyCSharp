// Bevy's shader_material_screenspace_texture example,
// examples/shader/shader_material_screenspace_texture.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Shading;

// A cube whose texture is read where on the screen each pixel is, so as the camera circles it the
// picture stays where it is on the screen and the cube's edges cut it out.
internal static class ShaderMaterialScreenspaceTexture
{
    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
            ecs.SpawnPointLight(new Vec3(4f, 8f, 4f));

            var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/custom_material_screenspace_texture.slang"))
                .SetTexture("texture", AssetServer.Load(AssetKind.Image, "models/FlightHelmet/FlightHelmet_Materials_LensesMat_OcclusionRoughMetal.png"));
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), material, Transform.At(0f, 0.5f, 0f));

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(4f, 2.5f, 4f), Vec3.Zero, Vec3.UnitY));
        }, "shader_material_screenspace_texture.Setup");

        // Around the middle at forty-five degrees a second, looking at it.
        app.Update(ctx =>
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(_camera).Translation;
            var turned = Quat.FromAxisAngle(Vec3.UnitY, MathF.PI / 4f * ctx.Time.Delta) * at;
            ctx.Ecs.Set(_camera, Transform.LookingAt(turned, Vec3.Zero, Vec3.UnitY));
        }, "shader_material_screenspace_texture.RotateCamera");
    }
}
