namespace Bevy;

/// <summary>How an item sits across the cell it was placed in.</summary>
public enum CellAlign
{
    /// <summary>Whatever the layout would do, which is to fill the cell.</summary>
    Default = 0,

    /// <summary>Against the start of the cell.</summary>
    Start = 1,

    /// <summary>Against the end of it.</summary>
    End = 2,

    /// <summary>In the middle.</summary>
    Center = 3,

    /// <summary>Lined up with its neighbors' first line of text.</summary>
    Baseline = 4,

    /// <summary>Stretched to fill the cell.</summary>
    Stretch = 5,
}
