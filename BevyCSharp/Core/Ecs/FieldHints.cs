namespace Bevy;

/// <summary>
/// Everything a field's attributes asked for, in one place.
/// </summary>
/// <remarks>
/// <para>
/// A record rather than a list of attributes to reflect over. The generator has already read the
/// attributes at compile time, and a tool needs the answers at runtime, not the questions. A drawer
/// asks whether there is a range and gets two numbers or nothing.
/// </para>
/// <para>
/// Anything that reads these is free to ignore what it does not understand, and a drawer written by
/// a game can put its own meaning on the same hints. This holds what the editor knows how to honor;
/// a field with none of them is drawn the plain way.
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
/// <param name="Order">Where it sits among its neighbors.</param>
/// <param name="Asset">Which sort of asset the field holds, when it holds one.</param>
/// <param name="Extensions">
/// Which files to offer for it, separated by spaces, or nothing for the ones that go with the
/// asset type.
/// </param>
/// <param name="FormerNames">The names the field had before, which an old file may still use.</param>
/// <param name="NotSaved">Whether no file is written with it (<see cref="NotSavedAttribute"/>).</param>
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
    string? Extensions = null,
    IReadOnlyList<string>? FormerNames = null,
    bool NotSaved = false)
{
    /// <summary>What a field with no attributes on it asked for, which is nothing.</summary>
    public static readonly FieldHints None = new();

    /// <summary>Whether the field asked to be a slider.</summary>
    public bool HasRange => Minimum is not null && Maximum is not null;

    /// <summary>What has to hold for it to be shown at all.</summary>
    public IReadOnlyList<FieldCondition> Conditions { get; init; } = Conditions ?? [];

    /// <summary>The methods to call once it has been changed.</summary>
    public IReadOnlyList<string> Changed { get; init; } = Changed ?? [];

    /// <summary>The names the field had before, which an old file may still use.</summary>
    public IReadOnlyList<string> FormerNames { get; init; } = FormerNames ?? [];
}
