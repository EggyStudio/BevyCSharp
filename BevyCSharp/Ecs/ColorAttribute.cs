namespace Bevy;

/// <summary>Three numbers that are a color rather than a place.</summary>
/// <remarks>
/// The rows are the same three boxes; what this adds is the patch of color beside them, which is
/// the only way to tell 0.8, 0.2, 0.1 from a shade of red at a glance.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ColorAttribute : Attribute;
