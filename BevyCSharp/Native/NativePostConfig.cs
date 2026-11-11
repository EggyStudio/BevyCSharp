using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What a camera does to the picture after the scene has been drawn.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativePostConfig
{
    /// <summary>0 none, 1 Reinhard, 2 Reinhard luminance, 3 ACES, 4 AgX, 5 boring, 6 Tony, 7 filmic.</summary>
    public int Tonemapping;

    /// <summary>Non-zero to dither before quantizing.</summary>
    public int Dither;

    /// <summary>Non-zero to draw into a high dynamic range target.</summary>
    public int Hdr;

    /// <summary>Samples per pixel, 1 for none, or 2, 4 or 8.</summary>
    public int Msaa;

    /// <summary>0 none, 1 FXAA, 2 SMAA.</summary>
    public int AntiAlias;

    /// <summary>0 low, 1 medium, 2 high, 3 ultra.</summary>
    public int AntiAliasQuality;

    /// <summary>Sharpening strength, 0 for none.</summary>
    public float Sharpen;

    /// <summary>Non-zero to scatter light from the brightest parts.</summary>
    public int Bloom;

    /// <summary>How much is scattered.</summary>
    public float BloomIntensity;

    /// <summary>Brightness a pixel reaches before it blooms.</summary>
    public float BloomThreshold;

    /// <summary>How gradually that threshold takes effect.</summary>
    public float BloomThresholdSoftness;

    /// <summary>0 energy conserving, 1 additive.</summary>
    public int BloomMode;
}
