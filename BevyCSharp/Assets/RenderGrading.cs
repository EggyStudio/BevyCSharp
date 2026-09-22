using Bevy.Interop;

namespace Bevy;

/// <summary>
/// One tonal range's part of a color grade.
/// </summary>
/// <remarks>
/// The terms are the standard ASC CDL ones, so a grade written for a film pipeline carries across
/// unchanged: the picture leaves as <c>(in * Gain + Lift) ^ Gamma</c>, with saturation applied
/// around it. The defaults are the identity, so a section nobody touched changes nothing.
/// </remarks>
public sealed class GradingSection
{
    /// <summary>Below one drains color towards grey, above one spreads it out.</summary>
    public float Saturation { get; set; } = 1f;

    /// <summary>Below one pulls colors towards neutral grey, above one pushes them away.</summary>
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

/// <summary>
/// How a camera grades the picture once the scene has been drawn and tonemapped.
/// </summary>
/// <remarks>
/// <para>
/// Three tonal ranges plus what applies to all of them, which is the shape a colorist works in.
/// Warming the shadows and cooling the highlights is two sections and one number saying where one
/// ends, rather than a curve per channel.
/// </para>
/// <para>
/// This is the knob a game gives an artist rather than a player. It runs after tonemapping, so it
/// is about the look of the finished picture; <see cref="PostSettings.Hdr"/> and the exposure the
/// camera meters at are about how the scene became a picture in the first place.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Render.SetColorGrading(camera, new GradingSettings
/// {
///     Temperature = -0.2f,                                  // cooler overall
///     Shadows = new GradingSection { Lift = 0.02f },        // lifted blacks
///     Highlights = new GradingSection { Saturation = 0.9f },
/// });
/// </code>
/// </example>
public sealed class GradingSettings
{
    /// <summary>Stops of exposure applied before anything else. Zero leaves it alone.</summary>
    public float Exposure { get; set; }

    /// <summary>White balance, towards blue below zero and towards orange above it.</summary>
    public float Temperature { get; set; }

    /// <summary>White balance the other way, towards green and towards magenta.</summary>
    public float Tint { get; set; }

    /// <summary>Hue rotation, in degrees.</summary>
    public float Hue { get; set; }

    /// <summary>Saturation applied to everything, after the three sections.</summary>
    public float Saturation { get; set; } = 1f;

    /// <summary>
    /// Which luminances count as midtones, as a pair from darkest to brightest.
    /// </summary>
    /// <remarks>
    /// What separates the three sections. Below the first number is shadow and above the second is
    /// highlight, so widening it gives the midtone section more of the picture to work on.
    /// </remarks>
    public (float From, float To) MidtonesRange { get; set; } = (0.2f, 0.7f);

    /// <summary>The darkest range.</summary>
    public GradingSection Shadows { get; set; } = new();

    /// <summary>The middle range.</summary>
    public GradingSection Midtones { get; set; } = new();

    /// <summary>The brightest range.</summary>
    public GradingSection Highlights { get; set; } = new();

    /// <summary>The shape the bridge takes.</summary>
    internal NativeGradingConfig ToNative() => new()
    {
        Exposure = Exposure,
        Temperature = Temperature,
        Tint = Tint,
        Hue = Hue,
        PostSaturation = Saturation,
        MidtonesFrom = MidtonesRange.From,
        MidtonesTo = MidtonesRange.To,
        Shadows = Shadows.ToNative(),
        Midtones = Midtones.ToNative(),
        Highlights = Highlights.ToNative(),
    };
}
