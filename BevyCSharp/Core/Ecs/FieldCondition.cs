namespace Bevy;

/// <summary>
/// One reason a field might not be shown.
/// </summary>
/// <remarks>
/// The condition as it was written, kept whole rather than resolved, which field decides, what it
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
