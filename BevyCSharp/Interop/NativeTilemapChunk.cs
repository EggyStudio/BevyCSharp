using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>A tilemap chunk's settings. Mirrors <c>BcsTilemapChunk</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct NativeTilemapChunk
{
    /// <summary>Its size in tiles.</summary>
    public uint Width, Height;

    /// <summary>The size each tile is drawn at.</summary>
    public uint TileWidth, TileHeight;

    /// <summary>The tileset's image key.</summary>
    public int Tileset;

    /// <summary>Zero opaque, one masked, two blended.</summary>
    public int AlphaMode;

    /// <summary>The alpha a masked pixel needs to be drawn.</summary>
    public float AlphaCutoff;
}
