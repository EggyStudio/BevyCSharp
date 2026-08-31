using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the 2D path: a camera that measures in pixels, and sprites under it.
/// </summary>
/// <remarks>
/// What reaches the screen needs a window. What is checked here is that a sprite is an ordinary
/// entity, that the settings are accepted, and that a handle naming no image is refused rather
/// than drawing nothing.
/// </remarks>
[Collection("engine")]
public sealed class SpriteTests
{
    private const string Texture = "textures/checker.png";

    [Fact]
    public void ASpriteIsAnOrdinaryEntity()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        var camera = Entity.None;
        var sprite = Entity.None;
        var placed = false;

        harness.OnContext(Stage.Startup, ctx =>
        {
            camera = Render2d.SpawnCamera2d();

            sprite = ctx.Ecs.Spawn();
            Render2d.SetSprite(ctx.Ecs, sprite, AssetServer.Load(AssetKind.Image, Texture));

            // A sprite is placed by its Transform like anything else in the world, which is what
            // makes the 2D path the same ECS as the 3D one.
            ctx.Ecs.Add(sprite, Transform.At(120f, -80f, 0f));
        });

        harness.OnContext(Stage.Last, ctx =>
            placed = ctx.Ecs.GetRef<Transform>(sprite).Translation == new Vec3(120f, -80f, 0f));

        harness.Run();

        Assert.NotEqual(Entity.None, camera);
        Assert.NotEqual(Entity.None, sprite);
        Assert.True(placed);
    }

    [Fact]
    public void EverySpriteSettingIsAccepted()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var image = AssetServer.Load(AssetKind.Image, Texture);
            var entity = ctx.Ecs.Spawn();

            // One frame out of a sheet, tinted, mirrored and drawn at a fixed size.
            Render2d.SetSprite(ctx.Ecs, entity, image, new SpriteSettings
            {
                Color = (1f, 0.5f, 0.5f, 0.8f),
                Size = (64f, 64f),
                Rect = (0f, 0f, 1f, 1f),
                FlipX = true,
                FlipY = true,
            });

            // Replacing the sprite on an entity that has one is the same call.
            Render2d.SetSprite(ctx.Ecs, entity, image);
        });

        harness.Run();
    }

    [Fact]
    public void ASpriteNeedsAnImageThatExists()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var entity = ctx.Ecs.Spawn();

            var noImage = Assert.Throws<BevyNativeException>(
                () => Render2d.SetSprite(ctx.Ecs, entity, AssetHandle.None));
            Assert.Equal(NativeStatus.NoComponent, noImage.Status);

            var image = AssetServer.Load(AssetKind.Image, Texture);
            var gone = Assert.Throws<BevyNativeException>(
                () => Render2d.SetSprite(ctx.Ecs, Entity.None, image));
            Assert.Equal(NativeStatus.NoEntity, gone.Status);
        });

        harness.Run();
    }

    [Fact]
    public void TheTwoCamerasCoexist()
    {
        // A 2D camera ordered above a 3D one draws over the scene without clearing it, which is
        // what a 2D overlay on a 3D game needs.
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            var scene = Render.SpawnCamera3d();
            var overlay = Render2d.SpawnCamera2d(order: 1);

            Assert.NotEqual(Entity.None, scene);
            Assert.NotEqual(Entity.None, overlay);
            Assert.NotEqual(scene, overlay);
        });

        harness.Run();
    }
}
