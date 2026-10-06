// Bevy's animated_material example, examples/3d/animated_material.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Shows how to animate material properties.
internal static class AnimatedMaterial
{
    // Turned by the golden angle from one cube to the next, so no two are near in hue.
    private const float GoldenAngle = 137.50777f;

    private static readonly List<(AssetHandle Handle, MaterialSettings Settings, float Hue)> Cubes = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Cubes.Clear();

            var camera = ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(3f, 1f, 3f), new Vec3(0f, -0.5f, 0f), Vec3.UnitY));
            Render.SetEnvironmentMap(
                camera,
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
                AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
                2000f);

            var cube = Render.CreateMesh(MeshShape.Cuboid, 0.5f, 0.5f, 0.5f);
            var hue = 0f;
            for (var x = -1; x < 2; x++)
            {
                for (var z = -1; z < 2; z++)
                {
                    var settings = new MaterialSettings { BaseColor = Color.FromHsl(hue, 1f, 0.5f) };
                    var material = Render.CreateMaterial(settings);
                    Cubes.Add((material, settings, hue));
                    ctx.Ecs.SpawnMesh(cube, material, Transform.At(x, 0f, z));
                    hue += GoldenAngle;
                }
            }
        });

        // Each cube's hue turned a hundred degrees a second.
        app.Update(ctx =>
        {
            for (var i = 0; i < Cubes.Count; i++)
            {
                var (handle, settings, hue) = Cubes[i];
                hue += ctx.Time.Delta * 100f;
                settings.BaseColor = Color.FromHsl(hue, 1f, 0.5f);
                Render.WriteMaterial(handle, settings);
                Cubes[i] = (handle, settings, hue);
            }
        }, "animated_material.AnimateMaterials");
    }
}
