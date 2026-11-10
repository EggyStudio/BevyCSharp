using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One tile of a tilemap chunk. Mirrors <c>BcsTile</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeTile
{
    /// <summary>The tint, linear RGBA.</summary>
    public float R, G, B, A;

    /// <summary>The layer of the tileset it is drawn from.</summary>
    public ushort TilesetIndex;

    /// <summary>Bevy's <c>TileOrientation</c> by its value.</summary>
    public byte Orientation;

    /// <summary>One where a tile is there at all, and two where it is drawn.</summary>
    public byte Flags;

    /// <summary>The tile as the bridge reads it, an empty cell with no bits set.</summary>
    public static NativeTile From(TileData? tile) => tile is not { } it
        ? default
        : new NativeTile
        {
            R = it.Color.R,
            G = it.Color.G,
            B = it.Color.B,
            A = it.Color.A,
            TilesetIndex = it.TilesetIndex,
            Orientation = (byte)it.Orientation,
            Flags = (byte)(1 | (it.Visible ? 2 : 0)),
        };

    /// <summary>The tile it describes, or null for an empty cell.</summary>
    public readonly TileData? ToTile() => (Flags & 1) == 0
        ? null
        : new TileData(TilesetIndex, new Color(R, G, B, A), (Flags & 2) != 0, (TileOrientation)Orientation);
}
