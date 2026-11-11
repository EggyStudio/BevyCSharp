namespace Bevy;

/// <summary>
/// How far each corner of a node is rounded.
/// </summary>
/// <remarks>
/// Clockwise from the top left, which is the order every stylesheet states them in. A percentage is
/// read against the node's own size, so a radius of fifty percent on all four corners is an
/// ellipse and anything past that is clamped rather than refused.
/// </remarks>
/// <param name="TopLeft">How far the top left corner is rounded.</param>
/// <param name="TopRight">How far the top right corner is rounded.</param>
/// <param name="BottomRight">How far the bottom right corner is rounded.</param>
/// <param name="BottomLeft">How far the bottom left corner is rounded.</param>
public readonly record struct Corners(
    Length TopLeft,
    Length TopRight,
    Length BottomRight,
    Length BottomLeft)
{
    /// <summary>Square corners.</summary>
    public static Corners None => All(Length.Zero);

    /// <summary>The same radius on every corner.</summary>
    public static Corners All(Length value) => new(value, value, value, value);

    /// <summary>Rounded along the top edge only, like a tab.</summary>
    public static Corners Top(Length value) =>
        new(value, value, Length.Zero, Length.Zero);

    /// <summary>Rounded along the bottom edge only.</summary>
    public static Corners Bottom(Length value) =>
        new(Length.Zero, Length.Zero, value, value);

    /// <summary>Reads a single length as the same radius on every corner.</summary>
    public static implicit operator Corners(Length value) => All(value);

    /// <inheritdoc/>
    public override string ToString() =>
        $"({TopLeft}, {TopRight}, {BottomRight}, {BottomLeft})";
}
