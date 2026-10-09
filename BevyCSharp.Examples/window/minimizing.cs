// Bevy's minimizing example, tests/window/minimizing.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Windowing;

// A window that minimizes itself on its sixtieth frame, over a scene drawn by a 3D camera and a
// 2D one above it.
internal static class Minimizing
{
    public static void Configure(Config config) => config.Title = "Minimizing";

    public static void Build(App app)
    {
        app.Startup(ctx => TestScene.Setup(ctx.Ecs), "minimizing.Setup");
        app.Update(ctx =>
        {
            if (ctx.Time.FrameCount == 60 && Window.Entity() != Entity.None) Window.Minimize();
        }, "minimizing.MinimizeAutomatically");
    }
}

/// <summary>The scene Bevy's two window tests draw, a cube on a plane and a blue square over it.</summary>
internal static class TestScene
{
    public static void Setup(EcsWorld ecs)
    {
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));
        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
        ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));

        // Drawn over the 3D picture, with nothing cleared under it.
        Render2d.SpawnCamera2d(order: 1);
        var square = ecs.Spawn();
        ecs.Add(square, Transform.Identity);
        Render2d.SetSprite(ecs, square, Render.CreateImage([255, 255, 255, 255], 1, 1), new SpriteSettings { Color = Color.FromSrgb(0.25f, 0.25f, 0.75f), Size = (50f, 50f) });
    }
}
