// Bevy's mesh2d_repeated_texture example, examples/2d/mesh2d_repeated_texture.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Shows how an image repeats across a 2D mesh when its coordinates are scaled past one, which
// takes an image whose sampler repeats rather than one that stretches its edge.
internal static class Mesh2dRepeatedTexture
{
    private const float RectangleOffset = 250f;
    private const float RectangleSide = 200f;
    private const float LabelOffset = RectangleSide / 2f + 25f;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var clamped = AssetServer.Load(AssetKind.Image, "textures/fantasy_ui_borders/panel-border-010.png");
        var repeated = AssetServer.LoadImage("textures/fantasy_ui_borders/panel-border-010-repeated.png",
            new TextureSettings { Wrap = TextureWrap.Repeat });
        var square = Render.CreateMesh(MeshShape.Rectangle, RectangleSide, RectangleSide);

        void Spawn(float x, AssetHandle image, Vec2 scale, string label)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(x, 0f, 0f));
            Render2d.SetMesh(ecs, entity, square);
            Render2d.SetMaterial(ecs, entity, Render2d.CreateMaterial(new ColorMaterialSettings { Texture = image, UvScale = scale }));

            var text = ecs.Spawn();
            ecs.Add(text, Transform.At(0f, LabelOffset, 0f));
            ecs.Insert<Text2dRef>(text).Value = label;
            ecs.SetParent(text, entity);
        }

        // The image once across, then twice across and three times down, first sampled to
        // repeat and then sampled as Bevy samples by default, stretching its last pixels.
        Spawn(0f, clamped, new Vec2(1f, 1f), "Control");
        Spawn(-RectangleOffset, repeated, new Vec2(2f, 3f), "Repeat On");
        Spawn(RectangleOffset, clamped, new Vec2(2f, 3f), "Repeat Off");
    }, "mesh2d_repeated_texture.Setup");
}
