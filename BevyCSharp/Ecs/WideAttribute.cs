namespace Bevy;

/// <summary>
/// Drawn across the whole panel, with no name beside it.
/// </summary>
/// <remarks>
/// For a value the name column has nothing to add to: a sentence of text, a script, a long path.
/// Every other row keeps its name in its column, so the one that gives it up has to say so.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class WideAttribute : Attribute;
