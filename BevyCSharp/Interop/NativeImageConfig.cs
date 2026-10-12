using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>How an image should be sampled, and how its bytes should be read.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeImageConfig
{
    /// <summary>0 clamp, 1 repeat, 2 mirror, for U.</summary>
    public int AddressU;

    /// <summary>0 clamp, 1 repeat, 2 mirror, for V.</summary>
    public int AddressV;

    /// <summary>0 nearest, 1 linear, when drawn larger than the texture.</summary>
    public int MagFilter;

    /// <summary>0 nearest, 1 linear, when drawn smaller.</summary>
    public int MinFilter;

    /// <summary>0 nearest, 1 linear, between mip levels.</summary>
    public int MipmapFilter;

    /// <summary>Maximum anisotropic samples; 1 disables it.</summary>
    public uint Anisotropy;

    /// <summary>Non-zero to read the file as sRGB.</summary>
    public int Srgb;

    /// <summary>How many layers a loaded file is cut into, top to bottom; 0 or 1 for one picture.</summary>
    public uint Layers;
}
