namespace Bevy;

/// <summary>
/// A grid of tiles drawn as one mesh from the layers of one image, Bevy's <c>TilemapChunk</c>.
/// </summary>
/// <remarks>
/// <para>
/// The tileset is an image loaded as an array of layers, a tile to a layer
/// (<see cref="TextureSettings.Layers"/>), and each tile names its layer
/// (<see cref="TileData.TilesetIndex"/>). The chunk is centered on its entity, and its tiles run
/// row after row from the bottom row up, <see cref="Width"/> across.
/// <see cref="Render2d.SetTilemap"/> makes an entity one, and <see cref="Render2d.SetTiles"/>
/// changes its tiles afterwards, which Bevy draws in one call however many there are.
/// </para>
/// </remarks>
public sealed class TilemapChunk
{
    /// <summary>How many tiles across.</summary>
    public uint Width { get; set; }

    /// <summary>How many tiles up.</summary>
    public uint Height { get; set; }

    /// <summary>How wide each tile is drawn, which is apart from how wide it is in the tileset.</summary>
    public uint TileWidth { get; set; }

    /// <summary>How tall each tile is drawn.</summary>
    public uint TileHeight { get; set; }

    /// <summary>The image the tiles are drawn from, loaded as an array of layers.</summary>
    public AssetHandle Tileset { get; set; } = AssetHandle.None;

    /// <summary>How alpha is read, opaque by default as Bevy's is.</summary>
    public AlphaMode2d AlphaMode { get; set; } = AlphaMode2d.Opaque;

    /// <summary>The alpha a masked pixel needs to be drawn.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>How many tiles it holds.</summary>
    public int Count => checked((int)(Width * Height));

    /// <summary>The place in the chunk's tiles of the tile <paramref name="x"/> across and <paramref name="y"/> up.</summary>
    public int IndexOf(uint x, uint y) => checked((int)(y * Width + x));

    /// <summary>
    /// Where the tile <paramref name="x"/> across and <paramref name="y"/> up sits, its middle, in the
    /// chunk's own space, Bevy's <c>calculate_tile_transform</c>.
    /// </summary>
    public Transform TileTransform(uint x, uint y) => Transform.At(
        x * TileWidth + TileWidth / 2f - TileWidth * Width / 2f,
        y * TileHeight + TileHeight / 2f - TileHeight * Height / 2f,
        0f);
}
