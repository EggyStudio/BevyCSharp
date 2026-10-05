// Bevy's mesh2d_alpha_mode example, examples/2d/mesh2d_alpha_mode.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Shows how a 2D mesh's transform and its material's alpha mode decide what is drawn over what,
// which needs the depth buffer to be used for opaque and transparent meshes alike.
internal static class Mesh2dAlphaMode
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();

        var icon = AssetServer.Load(AssetKind.Image, "branding/icon.png");
        var square = Render.CreateMesh(MeshShape.Rectangle, 256f, 256f);
        // CSS white, blue and green, the last half bright, as on the web.
        (float R, float G, float B, float A) white = (1f, 1f, 1f, 1f), blue = (0f, 0f, 1f, 1f), green = Scene.Srgb8(0, 128, 0);

        void Spawn((float R, float G, float B, float A) color, AlphaMode2d mode, float x, float z)
        {
            var entity = ctx.Ecs.Spawn();
            ctx.Ecs.Add(entity, Transform.At(x, 0f, z));
            Render2d.SetMesh(ctx.Ecs, entity, square);
            Render2d.SetMaterial(ctx.Ecs, entity, Render2d.CreateMaterial(new ColorMaterialSettings { Color = color, AlphaMode = mode, Texture = icon }));
        }

        // Opaque, so each square is drawn whole with its transparent parts black, the blue one on
        // top and the white and green ones behind it.
        Spawn(white, AlphaMode2d.Opaque, -400f, 0f);
        Spawn(blue, AlphaMode2d.Opaque, -300f, 1f);
        Spawn(green, AlphaMode2d.Opaque, -200f, -1f);

        // Masked and blended. The white icon has only its picture drawn, over the green square
        // and under the blue one.
        Spawn(white, AlphaMode2d.Mask, 200f, 0f);
        Spawn(blue with { A = 0.7f }, AlphaMode2d.Blend, 300f, 1f);
        Spawn(green with { A = 0.7f }, AlphaMode2d.Blend, 400f, -1f);
    }, "mesh2d_alpha_mode.Setup");
}
