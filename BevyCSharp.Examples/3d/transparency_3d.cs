using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates how to use transparency in 3D. Shows the effects of different blend modes. The
// alpha_mode of each material is set to show how they behave, and the alpha of every material
// fades in and out together.
//
// Bevy's cube on the left uses alpha to coverage, which the bridge has no mode for, so it blends.
internal static class Transparency3d
{
    private static readonly List<(AssetHandle Handle, MaterialSettings Settings)> Materials = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Materials.Clear();

            ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 6f, 6f), Make(new MaterialSettings { BaseColor = Scene.Srgb(0.3f, 0.5f, 0.3f) }), Transform.Identity);

            var sphere = Render.CreateMesh(MeshShape.Sphere, 0.5f);
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);

            // A sphere cut at half its alpha, and an unlit one cut at a tenth.
            ecs.Mesh(sphere, Make(new MaterialSettings { BaseColor = Scene.Srgb(0.2f, 0.7f, 0.1f, 0f), AlphaMode = AlphaMode.Mask, AlphaCutoff = 0.5f }), Transform.At(1f, 0.5f, -1.5f));
            ecs.Mesh(sphere, Make(new MaterialSettings { BaseColor = Scene.Srgb(0.2f, 0.7f, 0.1f, 0f), AlphaMode = AlphaMode.Mask, AlphaCutoff = 0.1f, Unlit = true }), Transform.At(-1f, 0.5f, -1.5f));

            // A color with an alpha below one blends, as Bevy makes a material from one.
            ecs.Mesh(cube, Make(new MaterialSettings { BaseColor = Scene.Srgb(0.5f, 0.5f, 1f, 0f), AlphaMode = AlphaMode.Blend }), Transform.At(0f, 0.5f, 0f));
            ecs.Mesh(cube, Make(new MaterialSettings { BaseColor = Scene.Srgb(0.5f, 1f, 0.5f, 0f), AlphaMode = AlphaMode.Blend }), Transform.At(-1.5f, 0.5f, 0f));

            ecs.Mesh(sphere, Make(new MaterialSettings { BaseColor = Scene.Srgb(0.7f, 0.2f, 0.1f) }), Transform.At(0f, 0.5f, -1.5f));

            ecs.PointLight(new Vec3(4f, 8f, 4f), shadows: true);
            ecs.Camera(Transform.LookingAt(new Vec3(-2f, 3f, 5f), Vec3.Zero, Vec3.UnitY));
        });

        // Every material's alpha, from nothing to whole and back.
        app.Update(ctx =>
        {
            var alpha = MathF.Sin(ctx.Time.Elapsed) / 2f + 0.5f;
            foreach (var (handle, settings) in Materials)
            {
                settings.BaseColor = settings.BaseColor with { A = alpha };
                Render.WriteMaterial(handle, settings);
            }
        }, "transparency_3d.FadeTransparency");
    }

    private static AssetHandle Make(MaterialSettings settings)
    {
        var handle = Render.CreateMaterial(settings);
        Materials.Add((handle, settings));
        return handle;
    }
}
