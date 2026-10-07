using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render2d
{
    /// <summary>
    /// Makes an entity a tilemap chunk with its tiles, Bevy's <c>TilemapChunk</c> and
    /// <c>TilemapChunkTileData</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tiles are the whole chunk, row after row from the bottom row up, as many as
    /// <see cref="TilemapChunk.Count"/>, a null tile an empty cell. Bevy makes the chunk's mesh and
    /// material as the two are put on together, and refuses a chunk whose tiles are not all there.
    /// Called again, the chunk is made again, so its size or tileset changes that way, and its tiles
    /// alone through <see cref="SetTiles"/>.
    /// </para>
    /// <para>
    /// The chunk is drawn where its entity is, so the entity carries a transform, which this adds at
    /// the origin where there is none.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var chunk = new TilemapChunk
    /// {
    ///     Width = 64, Height = 64, TileWidth = 8, TileHeight = 8,
    ///     Tileset = AssetServer.LoadImage("textures/array_texture.png", new TextureSettings { Layers = 4 }),
    /// };
    /// Render2d.SetTilemap(ctx.Ecs, ctx.Ecs.Spawn(), chunk, tiles);
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">The tiles are not as many as the chunk holds.</exception>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, the tileset names no image, or this build or this run draws nothing, as
    /// a headless run does not.
    /// </exception>
    public static void SetTilemap(EcsWorld world, Entity entity, TilemapChunk chunk, ReadOnlySpan<TileData?> tiles)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(chunk);
        if (tiles.Length != chunk.Count)
            throw new ArgumentException($"A chunk of {chunk.Width} by {chunk.Height} holds {chunk.Count} tiles, and {tiles.Length} were given.", nameof(tiles));

        if (!world.Has<Transform>(entity)) world.Add(entity, Transform.Identity);

        var native = new NativeTilemapChunk
        {
            Width = chunk.Width,
            Height = chunk.Height,
            TileWidth = chunk.TileWidth,
            TileHeight = chunk.TileHeight,
            Tileset = chunk.Tileset.Key,
            AlphaMode = (int)chunk.AlphaMode,
            AlphaCutoff = chunk.AlphaCutoff,
        };

        var packed = Pack(tiles);
        fixed (NativeTile* first = packed)
        {
            var status = Native.bcs_tilemap_insert(entity.Bits, &native, first, packed.Length);
            if (status == NativeStatus.Unsupported)
            {
                // A build with the renderer refuses too, in a run that draws nothing.
                throw App.HasRenderer
                    ? new BevyNativeException(NativeStatus.Unsupported, "A tilemap chunk is drawn by a run that draws, and this one is headless. Start it offscreen or with a window.")
                    : Render.NoRenderer("Drawing a tilemap");
            }

            Native.Check(status, $"making {entity} a tilemap chunk");
        }
    }

    /// <summary>Writes tiles over a chunk's from <paramref name="start"/> on, which Bevy draws from the next frame.</summary>
    /// <remarks>
    /// One call however many, since each marks the chunk's tiles changed and Bevy writes the
    /// chunk's whole tile image again for the frame. A null tile empties its cell.
    /// <see cref="TilemapChunk.IndexOf"/> finds a tile's place.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is no chunk, the tiles run past its last one, or this build has no renderer.
    /// </exception>
    public static void SetTiles(EcsWorld world, Entity entity, int start, ReadOnlySpan<TileData?> tiles)
    {
        ArgumentNullException.ThrowIfNull(world);

        var packed = Pack(tiles);
        fixed (NativeTile* first = packed)
        {
            var status = Native.bcs_tilemap_write(entity.Bits, start, first, packed.Length);
            if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Drawing a tilemap");
            Native.Check(status, $"writing {tiles.Length} tiles of {entity} from {start}");
        }
    }

    /// <summary>A chunk's tile at <paramref name="index"/>, or null for an empty cell.</summary>
    /// <exception cref="BevyNativeException">
    /// The entity is no chunk, the index is past its last tile, or this build has no renderer.
    /// </exception>
    public static TileData? TileAt(EcsWorld world, Entity entity, int index)
    {
        ArgumentNullException.ThrowIfNull(world);

        NativeTile tile;
        var status = Native.bcs_tilemap_read(entity.Bits, index, &tile, 1);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Reading a tilemap");
        Native.Check(status, $"reading tile {index} of {entity}");
        return tile.ToTile();
    }

    private static NativeTile[] Pack(ReadOnlySpan<TileData?> tiles)
    {
        var packed = new NativeTile[tiles.Length];
        for (var i = 0; i < tiles.Length; i++) packed[i] = NativeTile.From(tiles[i]);
        return packed;
    }
}
