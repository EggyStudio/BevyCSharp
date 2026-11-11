namespace Bevy;

/// <summary>
/// A sentence about what a field is for.
/// </summary>
/// <remarks>
/// Shown while the pointer is over the row rather than in a box that appears after a wait. A
/// tooltip nobody sees until they hover for a second is documentation that is only found by
/// accident.
/// </remarks>
/// <param name="text">The sentence.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class TooltipAttribute(string text) : Attribute
{
    /// <summary>The sentence.</summary>
    public string Text { get; } = text;
}
