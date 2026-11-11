namespace Bevy;

/// <summary>
/// One node's own answer to how its parent aligns things.
/// </summary>
/// <remarks>
/// The same choices as <see cref="UiAlign"/>, set on the child instead of the parent, plus
/// <see cref="Auto"/> for leaving the parent to decide. What the odd item out uses.
/// </remarks>
public enum UiAlignSelf
{
    /// <summary>Whatever the parent's alignment says.</summary>
    Auto = 0,

    /// <summary>Against the start of the cross axis.</summary>
    Start = 1,

    /// <summary>Against the end of the cross axis.</summary>
    End = 2,

    /// <summary>The start of the cross axis, or its end when the direction is reversed.</summary>
    FlexStart = 3,

    /// <summary>The end of the cross axis, or its start when the direction is reversed.</summary>
    FlexEnd = 4,

    /// <summary>Centered across the axis.</summary>
    Center = 5,

    /// <summary>Lined up on the baseline of the text inside it.</summary>
    Baseline = 6,

    /// <summary>Stretched to fill the cross axis.</summary>
    Stretch = 7,
}
