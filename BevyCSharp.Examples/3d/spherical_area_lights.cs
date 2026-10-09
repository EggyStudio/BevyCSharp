// Bevy's spherical_area_lights example, examples/3d/spherical_area_lights.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates how lighting is affected by different radius of point lights.
internal static class SphericalAreaLights
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render.SetAmbientLight((1f, 1f, 1f), 60f);

        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0.2f, 1.5f, 2.5f), Vec3.Zero, Vec3.UnitY));

        ecs.SpawnMesh(
            Render.CreateMesh(MeshShape.Plane, 100f, 100f),
            Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.2f, 0.2f, 0.2f), Roughness = 0.08f }),
            Transform.Identity);

        // Six bulbs, each a little larger than the last, with a light as wide as it in it.
        const int Count = 6;
        var mesh = Render.CreateMesh(MeshShape.Sphere, 1f);
        var blue = Color.FromSrgb(0.2f, 0.2f, 1f);

        for (var i = 0; i < Count; i++)
        {
            var percent = i / (float)Count;
            var radius = percent * 0.4f;

            var bulb = ecs.SpawnMesh(
                mesh,
                Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.5f, 0.5f, 1f), Unlit = true }),
                new Transform(new Vec3(-2f + percent * 4f, 0.3f, 0f), Quat.Identity, new Vec3(radius)));

            var light = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Point,
                Intensity = 1_000_000f,
                Radius = radius,
                Color = (blue.R, blue.G, blue.B),
                Shadows = false,
            });
            ecs.SetParent(light, bulb);
        }
    });
}
