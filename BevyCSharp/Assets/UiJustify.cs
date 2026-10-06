namespace Bevy;

public enum UiJustify
{
    /// <summary>Whatever the layout would do unasked.</summary>
    Default = 0,

    /// <summary>Packed against the start of the axis.</summary>
    Start = 1,

    /// <summary>Packed against the end of the axis.</summary>
    End = 2,

    /// <summary>The start of the axis, or its end when the direction is reversed.</summary>
    FlexStart = 3,

    /// <summary>The end of the axis, or its start when the direction is reversed.</summary>
    FlexEnd = 4,

    /// <summary>Packed around the middle.</summary>
    Center = 5,

    /// <summary>Stretched to fill the axis.</summary>
    Stretch = 6,

    /// <summary>Spread out, with the leftover space between the children.</summary>
    SpaceBetween = 7,

    /// <summary>Spread out, with equal space between and around the children.</summary>
    SpaceEvenly = 8,

    /// <summary>Spread out, with half-size space at the two ends.</summary>
    SpaceAround = 9,
}
