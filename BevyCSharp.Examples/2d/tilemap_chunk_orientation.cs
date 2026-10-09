// Bevy's tilemap_chunk_orientation example, examples/2d/tilemap_chunk_orientation.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Shows a tilemap chunk rendered with a single draw call, including different orientations of
// tiles (rotated, mirrored) and using different tileset indices, colors, alpha and visibility to
// show all tile features. Each column is one of the eight orientations, each row one tint, the rows
// alternate between the tileset's two arrows, and the top row is hidden.
internal static class TilemapChunkOrientation
{
    private static readonly TileOrientation[] Orientations =
    [
        TileOrientation.Default,
        TileOrientation.Rotate90,
        TileOrientation.Rotate180,
        TileOrientation.Rotate270,
        TileOrientation.MirrorH,
        TileOrientation.MirrorHRotate90,
        TileOrientation.MirrorHRotate180,
        TileOrientation.MirrorHRotate270,
    ];

    // A tint a row, the last four seen through.
    private static readonly Color[] Colors =
    [
        Color.White,
        new(1f, 0f, 0f),
        new(0f, 1f, 0f),
        new(0f, 0f, 1f),
        new(1f, 0f, 0f, 0.25f),
        new(0f, 1f, 0f, 0.25f),
        new(0f, 0f, 1f, 0.25f),
        new(1f, 1f, 1f, 0.5f),
    ];

    public static void Build(App app) => app.Startup(Setup, "tilemap_chunk_orientation.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var chunk = new TilemapChunk
        {
            Width = 8,
            Height = 8,
            TileWidth = 64,
            TileHeight = 64,
            // Two arrows stacked in the one image, a layer each.
            Tileset = AssetServer.LoadImage("textures/arrow.png", new TextureSettings { Layers = 2 }),
            AlphaMode = AlphaMode2d.Blend,
        };

        var tiles = new TileData?[chunk.Count];
        for (var i = 0; i < tiles.Length; i++)
        {
            var (row, column) = (i / 8, i % 8);
            tiles[i] = new TileData((ushort)(row % 2), Colors[row], Visible: row != 7, Orientations[column]);
        }

        Render2d.SetTilemap(ctx.Ecs, ctx.Ecs.Spawn(), chunk, tiles);

        // Bevy's ClearColor resource, a pale blue behind the arrows.
        var camera = Render2d.SpawnCamera2d();
        ctx.Ecs.Wrap<CameraRef>(camera).ClearColor = new ClearColorConfig.Custom(Color.FromSrgb(0.5f, 0.5f, 0.9f));
    }
}
