using Bevy;

namespace BevyCSharp.Examples.Assets;

// Shows how an image repeats across a cube when its coordinates are scaled past one: in the middle
// the image once, on the left twice across and three times down sampled to repeat, and on the right
// the same scaled image sampled as Bevy samples by default, stretching its last pixels.
internal static class RepeatedTexture
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var clamped = AssetServer.Load(AssetKind.Image, "textures/fantasy_ui_borders/panel-border-010.png");
        var repeated = AssetServer.LoadImage("textures/fantasy_ui_borders/panel-border-010-repeated.png", new TextureSettings { Wrap = TextureWrap.Repeat });

        ecs.Mesh(cube, Render.CreateMaterial(new MaterialSettings { BaseColorTexture = clamped }), Transform.Identity);
        ecs.Mesh(cube, Render.CreateMaterial(new MaterialSettings { BaseColorTexture = repeated, UvScale = (2f, 3f) }), Transform.At(-1.5f, 0f, 0f));
        ecs.Mesh(cube, Render.CreateMaterial(new MaterialSettings { BaseColorTexture = clamped, UvScale = (2f, 3f) }), Transform.At(1.5f, 0f, 0f));

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Shadows = true });
        ecs.Add(light, Transform.At(4f, 8f, 4f));
        ecs.Camera(Transform.LookingAt(new Vec3(0f, 1.5f, 4f), Vec3.Zero, Vec3.UnitY));
    }, "repeated_texture.Setup");
}
