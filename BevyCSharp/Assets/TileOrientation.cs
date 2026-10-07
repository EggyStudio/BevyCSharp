namespace Bevy;

/// <summary>
/// How a tile of a <see cref="TilemapChunk"/> is turned and mirrored, Bevy's <c>TileOrientation</c>.
/// </summary>
/// <remarks>
/// Each value is three bits, from the highest a mirror across the vertical axis, across the
/// horizontal one and across the diagonal, as Bevy numbers them, so the values are the turns a
/// quarter turn counterclockwise at a time, and then the same turns of the tile mirrored.
/// </remarks>
public enum TileOrientation : byte
{
    /// <summary>As the tileset draws it.</summary>
    Default = 0b000,

    /// <summary>Turned a quarter turn counterclockwise.</summary>
    Rotate90 = 0b011,

    /// <summary>Turned a half turn.</summary>
    Rotate180 = 0b110,

    /// <summary>Turned three quarters counterclockwise.</summary>
    Rotate270 = 0b101,

    /// <summary>Mirrored, its left and right swapped.</summary>
    MirrorH = 0b100,

    /// <summary>Mirrored, then turned a quarter turn counterclockwise.</summary>
    MirrorHRotate90 = 0b001,

    /// <summary>Mirrored, then turned a half turn.</summary>
    MirrorHRotate180 = 0b010,

    /// <summary>Mirrored, then turned three quarters counterclockwise.</summary>
    MirrorHRotate270 = 0b111,
}
