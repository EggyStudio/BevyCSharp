namespace Bevy;

/// <summary>
/// Everything a field's attributes asked for, in one place.
/// </summary>
/// <remarks>
/// <para>
/// A record rather than a list of attributes to reflect over. The generator has already read the
/// attributes at compile time, and what a tool wants at runtime is the answers, not the questions:
/// a drawer asks whether there is a range and gets two numbers or nothing.
/// </para>
/// <para>
/// Anything that reads these is free to ignore what it does not understand, and a drawer written
/// by a game can put its own meaning on the same hints. What is here is what the editor knows how
/// to honour; a field with none of them is drawn the plain way.
/// </para>
/// </remarks>
/// <param name="Label">What to call the field, or nothing for its own name.</param>
/// <param name="Tooltip">A sentence about what it is for.</param>
/// <param name="Header">A heading to draw above it.</param>
/// <param name="Unit">What the number is measured in.</param>
/// <param name="Minimum">The low end of a range, when it has one.</param>
/// <param name="Maximum">The high end.</param>
/// <param name="Step">What one pixel of a drag is worth.</param>
/// <param name="ReadOnly">Whether it is shown but not editable.</param>
/// <param name="Hidden">Whether it is shown at all.</param>
/// <param name="Space">Whether a blank row goes above it.</param>
/// <param name="Colour">Whether three numbers are a colour rather than a place.</param>
/// <param name="ShowIf">The field that decides whether this one is shown.</param>
/// <param name="ShowIfNot">Whether that decision is reversed.</param>
/// <param name="Order">Where it sits among its neighbours.</param>
/// <param name="Asset">Which sort of asset the field holds, when it holds one.</param>
/// <param name="Extensions">
/// Which files to offer for it, separated by spaces, or nothing for the ones that go with the
/// asset type.
/// </param>
public sealed record FieldHints(
    string? Label = null,
    string? Tooltip = null,
    string? Header = null,
    string? Unit = null,
    double? Minimum = null,
    double? Maximum = null,
    double? Step = null,
    bool ReadOnly = false,
    bool Hidden = false,
    bool Space = false,
    bool Colour = false,
    string? ShowIf = null,
    bool ShowIfNot = false,
    int Order = 0,
    string? Asset = null,
    string? Extensions = null)
{
    /// <summary>What a field with no attributes on it asked for, which is nothing.</summary>
    public static readonly FieldHints None = new();

    /// <summary>Whether the field asked to be a slider.</summary>
    public bool HasRange => Minimum is not null && Maximum is not null;
}

/// <summary>
/// Everything a method's attributes asked for.
/// </summary>
/// <param name="Label">What the button says, or nothing for the method's own name.</param>
/// <param name="Tooltip">A sentence about what pressing it does.</param>
/// <param name="Hidden">Whether it is offered at all.</param>
/// <param name="Order">Where it sits among the other buttons.</param>
public sealed record MethodHints(
    string? Label = null,
    string? Tooltip = null,
    bool Hidden = false,
    int Order = 0)
{
    /// <summary>What a method with no attributes on it asked for.</summary>
    public static readonly MethodHints None = new();
}
