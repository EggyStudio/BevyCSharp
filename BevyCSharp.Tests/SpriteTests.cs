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
    public void AFrameIsNamedByNumberOnceThereIsAnAtlas()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        var frames = AssetHandle.None;
        var walker = Entity.None;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var sheet = AssetServer.Load(AssetKind.Image, Texture);

            // Four frames across, two down, cut flush to the edges.
            frames = Render2d.CreateAtlas(8, 8, columns: 4, rows: 2);

            walker = ctx.Ecs.Spawn();
            Render2d.SetSprite(ctx.Ecs, walker, sheet, new SpriteSettings
            {
                Atlas = frames,
                Frame = 5,
                // Feet on the ground rather than the middle of the sprite there.
                Anchor = SpriteAnchor.BottomCenter,
            });

            // Stepping the animation is the same call with the next number.
            Render2d.SetSprite(ctx.Ecs, walker, sheet, new SpriteSettings
            {
                Atlas = frames,
                Frame = 6,
                Anchor = SpriteAnchor.BottomCenter,
            });
        });

        harness.Run();

        Assert.True(frames.IsValid);
        Assert.NotEqual(Entity.None, walker);
    }

    [Fact]
    public void APanelIsDrawnAtAnySizeFromOneSmallImage()
    {
        using var harness = new EngineHarness(frames: 3);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, ctx =>
        {
            var image = AssetServer.Load(AssetKind.Image, Texture);
            var panel = ctx.Ecs.Spawn();

            Render2d.SetSprite(ctx.Ecs, panel, image, new SpriteSettings
            {
                Size = (240f, 80f),
                Mode = SpriteImageMode.Sliced,
                SliceBorder = (3f, 3f, 3f, 3f),
                CornerScale = 1f,
            });

            var floor = ctx.Ecs.Spawn();
            Render2d.SetSprite(ctx.Ecs, floor, image, new SpriteSettings
            {
                Size = (400f, 32f),
                Mode = SpriteImageMode.Tiled,
                TileX = true,
                TileY = false,
                TileStretch = 0.5f,
            });

            Assert.True(ctx.Ecs.IsAlive(panel));
            Assert.True(ctx.Ecs.IsAlive(floor));
        });

        harness.Run();
    }

    [Fact]
    public void AnAtlasWithNoTilesIsRefused()
    {
        using var harness = new EngineHarness(frames: 2);
        if (!App.HasRenderer) return;

        harness.OnContext(Stage.Startup, _ =>
        {
            // A grid with no rows names no frames at all, which is a mistake rather than an
            // empty layout worth handing back.
            var empty = Assert.Throws<BevyNativeException>(
                () => Render2d.CreateAtlas(8, 8, columns: 4, rows: 0));
            Assert.Equal(NativeStatus.NullArgument, empty.Status);

            var flat = Assert.Throws<BevyNativeException>(
                () => Render2d.CreateAtlas(0, 8, columns: 4, rows: 2));
            Assert.Equal(NativeStatus.NullArgument, flat.Status);
        });

        harness.Run();
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
