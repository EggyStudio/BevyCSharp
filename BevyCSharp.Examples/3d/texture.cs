// Bevy's texture example, examples/3d/texture.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// This example shows various ways to configure texture materials in 3D.
internal static class Texture
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var logo = AssetServer.Load(AssetKind.Image, "branding/bevy_logo_dark_big.png");

        // A quad four times as wide as it is high, and the logo on it plain, tinted red and blue.
        const float Width = 8f;
        var quad = Render.CreateMesh(MeshShape.Rectangle, Width, Width * 0.25f);
        var tilt = Quat.FromRotationX(-MathF.PI / 5f);

        foreach (var (z, tint) in new[] { (1.5f, (1f, 1f, 1f, 1f)), (0f, (1f, 0f, 0f, 0.5f)), (-1.5f, (0f, 0f, 1f, 0.5f)) })
        {
            var material = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = tint,
                BaseColorTexture = logo,
                AlphaMode = AlphaMode.Blend,
                Unlit = true,
            });
            ctx.Ecs.SpawnMesh(quad, material, new Transform(new Vec3(0f, 0f, z), tilt, Vec3.One));
        }

        ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(3f, 5f, 8f), Vec3.Zero, Vec3.UnitY));
    });
}
