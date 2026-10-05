using Bevy;

namespace BevyCSharp.Examples.TwoD;

// Shows how to draw a flat mesh, made from a rectangle, in a 2D scene.
internal static class Mesh2dExample
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        Render2d.SpawnCamera2d();

        // A rectangle of one unit on each side, scaled to 128, in Bevy's basic purple.
        var square = ctx.Ecs.Spawn();
        ctx.Ecs.Add(square, new Transform(Vec3.Zero, Quat.Identity, new Vec3(128f)));
        Render2d.SetMesh(ctx.Ecs, square, Render.CreateMesh(MeshShape.Rectangle));
        Render2d.SetMaterial(ctx.Ecs, square, Render2d.CreateMaterial(new ColorMaterialSettings { Color = Scene.Srgb(0.5f, 0f, 0.5f) }));
    }, "mesh2d.Setup");
}
