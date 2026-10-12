using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>How a camera grades the picture. Mirrors <c>BcsGradingConfig</c>.</summary>
public struct NativeGradingConfig
{
    /// <summary>Stops of exposure applied before anything else.</summary>
    public float Exposure;

    /// <summary>White balance, toward blue below zero and toward orange above it.</summary>
    public float Temperature;

    /// <summary>White balance the other way, toward green and toward magenta.</summary>
    public float Tint;

    /// <summary>Hue rotation in degrees.</summary>
    public float Hue;

    /// <summary>Saturation applied to everything, after the three sections.</summary>
    public float PostSaturation;

    /// <summary>Where the midtones begin, as a luminance.</summary>
    public float MidtonesFrom;

    /// <summary>Where they end.</summary>
    public float MidtonesTo;

    /// <summary>The darkest range.</summary>
    public NativeGradingSection Shadows;

    /// <summary>The middle range.</summary>
    public NativeGradingSection Midtones;

    /// <summary>The brightest range.</summary>
    public NativeGradingSection Highlights;
}
