namespace Bevy;

/// <summary>A line across the panel above a field.</summary>
/// <remarks>
/// For a break that is not worth a word. A heading says what comes next; a line only says that
/// what comes next is something else.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class SeparatorAttribute : Attribute;
