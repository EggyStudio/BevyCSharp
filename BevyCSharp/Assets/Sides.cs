namespace Bevy;

/// <summary>
/// A measurement taken on all four sides of a node.
/// </summary>
/// <remarks>
/// Padding, margin and border are each four lengths. One value is the common case, so a
/// <see cref="Length"/> converts to the same distance on every side and only a node whose sides
/// differ has to name them.
/// </remarks>
/// <param name="Left">The left side.</param>
/// <param name="Top">The top side.</param>
/// <param name="Right">The right side.</param>
/// <param name="Bottom">The bottom side.</param>
public readonly record struct Sides(Length Left, Length Top, Length Right, Length Bottom)
{
    /// <summary>Nothing on any side.</summary>
    public static Sides None => All(Length.Zero);

    /// <summary>The same length on every side.</summary>
    public static Sides All(Length value) => new(value, value, value, value);

    /// <summary>Left and right, with nothing at the top and bottom.</summary>
    public static Sides Horizontal(Length value) =>
        new(value, Length.Zero, value, Length.Zero);

    /// <summary>Top and bottom, with nothing at the left and right.</summary>
    public static Sides Vertical(Length value) =>
        new(Length.Zero, value, Length.Zero, value);

    /// <summary>Reads a single length as the same distance on every side.</summary>
    public static implicit operator Sides(Length value) => All(value);

    /// <inheritdoc/>
    public override string ToString() => $"({Left}, {Top}, {Right}, {Bottom})";
}
