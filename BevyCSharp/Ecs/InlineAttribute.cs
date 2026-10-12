namespace Bevy;

/// <summary>
/// Draws the parts of a value beside each other rather than one per row.
/// </summary>
/// <remarks>
/// Three numbers on one line suit a position, because it is one value, it is read left to right,
/// and three rows of it costs three times the height for no more information. Long numbers are cut
/// rather than allowed to wrap, which is the trade, and a value whose numbers matter to five digits
/// is one to leave stacked.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InlineAttribute : Attribute;
