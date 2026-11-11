namespace Bevy;

/// <summary>
/// Draws a number as a slider between two ends.
/// </summary>
/// <remarks>
/// Suits a field with a sensible range. A bar you can throw to one end is faster than a box you
/// have to type into, and it says what the ends are without a word of documentation. A value
/// outside the range is still shown, with the slider at the end it is past.
/// </remarks>
/// <param name="minimum">The low end.</param>
/// <param name="maximum">The high end.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class RangeAttribute(double minimum, double maximum) : Attribute
{
    /// <summary>The low end.</summary>
    public double Minimum { get; } = minimum;

    /// <summary>The high end.</summary>
    public double Maximum { get; } = maximum;

    /// <summary>What sits beside the bar, if anything.</summary>
    public SliderReadout Readout { get; init; } = SliderReadout.Number;
}
