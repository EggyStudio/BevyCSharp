namespace Bevy;

/// <summary>What a slider shows beside its bar.</summary>
/// <remarks>
/// The number, by default, small and on the right, because the bar is the control and what the
/// number is for is saying exactly where the bar was left. A box wide enough to type in takes half
/// the row from the thing being dragged, which is worth it only when the number is one somebody was
/// told to use rather than one they are looking for.
/// </remarks>
public enum SliderReadout
{
    /// <summary>The number, read only, after the bar.</summary>
    Number,

    /// <summary>A box that can be typed into, which takes the width a box needs.</summary>
    Box,

    /// <summary>Nothing. The bar is the whole of it.</summary>
    None,
}
