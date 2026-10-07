namespace Bevy;

/// <summary>One tile of a <see cref="TilemapChunk"/>, Bevy's <c>TileData</c>.</summary>
/// <param name="TilesetIndex">The layer of the tileset it is drawn from.</param>
/// <param name="Color">The tint its layer is multiplied by, white leaving it as it is.</param>
/// <param name="Visible">Whether it is drawn, a tile kept in its cell while it is hidden.</param>
/// <param name="Orientation">How it is turned and mirrored.</param>
public readonly record struct TileData(ushort TilesetIndex, Color Color, bool Visible, TileOrientation Orientation)
{
    /// <summary>A tile drawn from a layer of the tileset, white, shown and upright, Bevy's <c>from_tileset_index</c>.</summary>
    public static TileData FromTilesetIndex(ushort tilesetIndex) => new(tilesetIndex, Color.White, true, TileOrientation.Default);
}
