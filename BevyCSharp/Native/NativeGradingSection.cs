using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>One tonal range's part of a color grade. Mirrors <c>BcsGradingSection</c>.</summary>
public struct NativeGradingSection
{
    /// <summary>Below one drains color toward gray, above one spreads it out.</summary>
    public float Saturation;

    /// <summary>Below one pulls toward neutral gray, above one pushes away.</summary>
    public float Contrast;

    /// <summary>The exponent, which mostly moves the top of the range.</summary>
    public float Gamma;

    /// <summary>The multiplier, which mostly moves the middle of the range.</summary>
    public float Gain;

    /// <summary>The offset, which mostly moves the bottom of the range.</summary>
    public float Lift;
}
