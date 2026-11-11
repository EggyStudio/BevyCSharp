using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>Covers Bevy's tilemap chunks, a grid of tiles drawn from the layers of one image, made and changed from C#.</summary>
/// <remarks>
/// A chunk's tiles are a list of optional structs that reflection lists no field of, so they cross
/// the bridge as an array of their own. These read back what was written, every field of a tile and
/// an empty cell, and the refusals of a chunk given the wrong number of tiles and of a write past
/// its end.
/// </remarks>
[Collection("engine")]
public sealed class TilemapTests
{
    /// <summary>A chunk's tiles read back as they were made, and as they were written over afterwards.</summary>
    [SkippableFact]
    public void AChunksTilesReadBackAsTheyWereMadeAndWritten()
    {
        Needs.Renderer();

        var ran = false;
        using var app = new App(Config.OffscreenFor(64, 64, frames: 3));
        app.AddPlugin(new EnginePlugin());
        app.Startup(ctx =>
        {
            // A tileset of four layers, which the chunk's shader reads as an array.
            var chunk = new TilemapChunk { Width = 2, Height = 2, TileWidth = 8, TileHeight = 8, Tileset = Render.CreateTarget(4, 4, layers: 4) };
            var tinted = new TileData(3, new Color(0.5f, 0.25f, 1f, 0.75f), Visible: false, TileOrientation.MirrorHRotate90);
            var entity = ctx.Ecs.Spawn();

            Render2d.SetTilemap(ctx.Ecs, entity, chunk, [TileData.FromTilesetIndex(1), null, tinted, TileData.FromTilesetIndex(0)]);
            Assert.Equal(TileData.FromTilesetIndex(1), Render2d.TileAt(ctx.Ecs, entity, 0));
            Assert.Null(Render2d.TileAt(ctx.Ecs, entity, 1));
            Assert.Equal(tinted, Render2d.TileAt(ctx.Ecs, entity, chunk.IndexOf(0, 1)));

            Render2d.SetTiles(ctx.Ecs, entity, 1, [TileData.FromTilesetIndex(2), null]);
            Assert.Equal(TileData.FromTilesetIndex(2), Render2d.TileAt(ctx.Ecs, entity, 1));
            Assert.Null(Render2d.TileAt(ctx.Ecs, entity, 2));

            Assert.Throws<ArgumentException>(() => Render2d.SetTilemap(ctx.Ecs, entity, chunk, [null]));
            var past = Assert.Throws<BevyNativeException>(() => Render2d.SetTiles(ctx.Ecs, entity, 3, [null, null]));
            Assert.Equal(NativeStatus.BufferTooSmall, past.Status);
            ran = true;
        }, "Test.Tilemap");

        Assert.Equal(0, app.Run());
        Assert.True(ran);
    }

    /// <summary>A run that draws nothing refuses a chunk, rather than letting Bevy's hook ask for what is not there.</summary>
    [SkippableFact]
    public void AHeadlessRunRefusesAChunk()
    {
        Needs.Renderer();

        BevyNativeException? refused = null;
        using var harness = new EngineHarness(frames: 2);
        harness.OnContext(Stage.Startup, ctx =>
        {
            var chunk = new TilemapChunk { Width = 1, Height = 1, TileWidth = 8, TileHeight = 8, Tileset = Render.CreateTarget(4, 4) };
            refused = Assert.Throws<BevyNativeException>(() => Render2d.SetTilemap(ctx.Ecs, ctx.Ecs.Spawn(), chunk, [null]));
        });

        harness.Run();
        Assert.Equal(NativeStatus.Unsupported, refused?.Status);
    }

    /// <summary>A tile's place is its middle in the chunk's own space, the chunk centered on its entity, as Bevy places it.</summary>
    [Fact]
    public void ATilesPlaceIsItsMiddleInAChunkCenteredOnItsEntity()
    {
        var chunk = new TilemapChunk { Width = 64, Height = 64, TileWidth = 8, TileHeight = 8 };
        Assert.Equal(new Vec3(-252f, -252f, 0f), chunk.TileTransform(0, 0).Translation);
        Assert.Equal(new Vec3(252f, -252f, 0f), chunk.TileTransform(63, 0).Translation);
        Assert.Equal(64 * 4 + 3, chunk.IndexOf(3, 4));
    }
}
