using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>Everything Bevy's <c>ColorMaterial</c> for a 2D mesh is made of.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeColorMaterial
{
    /// <summary>The color, linear RGBA.</summary>
    public fixed float Color[4];

    /// <summary>An image's key, or zero for none.</summary>
    public int Texture;

    /// <summary>Opaque at zero, masked at one, blended at two.</summary>
    public int AlphaMode;

    /// <summary>The alpha below which a masked pixel is not drawn.</summary>
    public float AlphaCutoff;

    /// <summary>The texture coordinates' transform, its two matrix columns and its translation.</summary>
    public fixed float Uv[6];
}
