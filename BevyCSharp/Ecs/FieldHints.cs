namespace Bevy;

/// <summary>
/// One reason a field might not be shown.
/// </summary>
/// <remarks>
/// The condition as it was written, kept whole rather than resolved: which field decides, what it
/// has to say, and whether the sense is reversed. Whoever is drawing asks the world; nothing here
/// can, and nothing here should have to.
/// </remarks>
/// <param name="Field">The field of the same component that decides.</param>
/// <param name="Value">
/// What that field has to read as, or <see langword="null"/> for "is on". An enum is named by its
/// name; everything else is written as it stands.
/// </param>
/// <param name="Not">Whether the sense is reversed.</param>
public readonly record struct FieldCondition(string Field, string? Value = null, bool Not = false);

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
/// <param name="Readout">What sits beside a slider's bar.</param>
/// <param name="ReadOnly">Whether it is shown but not editable.</param>
/// <param name="Hidden">Whether it is shown at all.</param>
/// <param name="Space">Whether a blank row goes above it.</param>
/// <param name="Separator">Whether a line goes above it.</param>
/// <param name="Color">Whether three numbers are a color rather than a place.</param>
/// <param name="Wide">Whether it is drawn across the panel with no name beside it.</param>
/// <param name="Inline">Whether the parts of it are drawn beside each other.</param>
/// <param name="Foldout">The fold it sits in, with slashes between the levels.</param>
/// <param name="FoldoutOpen">Whether that fold starts open.</param>
/// <param name="Note">A sentence to draw above it, in the panel.</param>
/// <param name="NoteKind">How loudly that sentence is said.</param>
/// <param name="Conditions">What has to hold for it to be shown at all.</param>
/// <param name="Changed">The methods to call once it has been changed.</param>
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
    SliderReadout Readout = SliderReadout.Number,
    bool ReadOnly = false,
    bool Hidden = false,
    bool Space = false,
    bool Separator = false,
    bool Color = false,
    bool Wide = false,
    bool Inline = false,
    string? Foldout = null,
    bool FoldoutOpen = true,
    string? Note = null,
    NoteKind NoteKind = NoteKind.Info,
    IReadOnlyList<FieldCondition>? Conditions = null,
    IReadOnlyList<string>? Changed = null,
    int Order = 0,
    string? Asset = null,
    string? Extensions = null)
{
    /// <summary>What a field with no attributes on it asked for, which is nothing.</summary>
    public static readonly FieldHints None = new();

    /// <summary>Whether the field asked to be a slider.</summary>
    public bool HasRange => Minimum is not null && Maximum is not null;

    /// <summary>What has to hold for it to be shown at all.</summary>
    public IReadOnlyList<FieldCondition> Conditions { get; init; } = Conditions ?? [];

    /// <summary>The methods to call once it has been changed.</summary>
    public IReadOnlyList<string> Changed { get; init; } = Changed ?? [];
}

/// <summary>
/// Everything a method's attributes asked for.
/// </summary>
/// <param name="Label">What the button says, or nothing for the method's own name.</param>
/// <param name="Tooltip">A sentence about what pressing it does.</param>
/// <param name="Hidden">Whether it is offered at all.</param>
/// <param name="Line">Whether the button shares a line with its neighbours, and where in it.</param>
/// <param name="Weight">How much of that line it takes against the others on it.</param>
/// <param name="Space">Whether a blank row goes above it.</param>
/// <param name="Separator">Whether a line goes above it.</param>
/// <param name="Header">A heading to draw above it.</param>
/// <param name="Foldout">The fold it sits in, with slashes between the levels.</param>
/// <param name="FoldoutOpen">Whether that fold starts open.</param>
/// <param name="Note">A sentence to draw above it, in the panel.</param>
/// <param name="NoteKind">How loudly that sentence is said.</param>
/// <param name="Order">Where it sits among the other buttons.</param>
public sealed record MethodHints(
    string? Label = null,
    string? Tooltip = null,
    bool Hidden = false,
    ButtonLine Line = ButtonLine.Alone,
    double Weight = 1d,
    bool Space = false,
    bool Separator = false,
    string? Header = null,
    string? Foldout = null,
    bool FoldoutOpen = true,
    string? Note = null,
    NoteKind NoteKind = NoteKind.Info,
    int Order = 0)
{
    /// <summary>What a method with no attributes on it asked for.</summary>
    public static readonly MethodHints None = new();
}
