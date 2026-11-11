namespace Bevy;

/// <summary>Shown, and not editable.</summary>
/// <remarks>
/// For something worked out rather than set. It is drawn like any other row so it can be read and
/// copied, and what it will not do is take an edit.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ReadOnlyAttribute : Attribute;
