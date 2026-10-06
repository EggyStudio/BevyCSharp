namespace Bevy;

/// <summary>
/// How a node places its children across its axis.
/// </summary>
/// <remarks>
/// The cross axis, which for a <see cref="UiDirection.Row"/> is the vertical and for a
/// <see cref="UiDirection.Column"/> the horizontal.
/// </remarks>
public enum UiAlign
{
    /// <summary>Whatever the layout would do unasked.</summary>
    Default = 0,

    /// <summary>Against the start of the cross axis.</summary>
    Start = 1,

    /// <summary>Against the end of the cross axis.</summary>
    End = 2,

    /// <summary>The start of the cross axis, or its end when the direction is reversed.</summary>
    FlexStart = 3,

    /// <summary>The end of the cross axis, or its start when the direction is reversed.</summary>
    FlexEnd = 4,

    /// <summary>Centered across the axis, for a row of buttons.</summary>
    Center = 5,

    /// <summary>Lined up on the baselines of the text inside them.</summary>
    Baseline = 6,

    /// <summary>Stretched to fill the cross axis.</summary>
    Stretch = 7,
}
