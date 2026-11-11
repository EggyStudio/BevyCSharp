using Bevy.Interop;

namespace Bevy;

/// <summary>
/// One tonal range's part of a color grade.
/// </summary>
/// <remarks>
/// The terms are the standard ASC CDL ones, so a grade written for a film pipeline carries across
/// unchanged. The picture leaves as <c>(in * Gain + Lift) ^ Gamma</c>, with saturation applied
/// around it. The defaults are the identity, so a section nobody touched changes nothing.
/// </remarks>
public sealed class GradingSection
{
    /// <summary>Below one drains color toward gray, above one spreads it out.</summary>
    public float Saturation { get; set; } = 1f;

    /// <summary>Below one pulls colors toward neutral gray, above one pushes them away.</summary>
    public float Contrast { get; set; } = 1f;

    /// <summary>The exponent, which mostly moves the top of the range.</summary>
    public float Gamma { get; set; } = 1f;

    /// <summary>The multiplier, which mostly moves the middle of the range.</summary>
    public float Gain { get; set; } = 1f;

    /// <summary>The offset, which mostly moves the bottom of the range.</summary>
    public float Lift { get; set; }

    /// <summary>The shape the bridge takes.</summary>
    internal NativeGradingSection ToNative() => new()
    {
        Saturation = Saturation,
        Contrast = Contrast,
        Gamma = Gamma,
        Gain = Gain,
        Lift = Lift,
    };
}
