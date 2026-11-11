namespace Bevy;

/// <summary>
/// How much one pixel of a drag on the handle is worth.
/// </summary>
/// <remarks>
/// The editor's own step is right for a position in meters and wrong for a count of bullets. This
/// says which scale the field is on.
/// </remarks>
/// <param name="amount">What a pixel is worth.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StepAttribute(double amount) : Attribute
{
    /// <summary>What a pixel is worth.</summary>
    public double Amount { get; } = amount;
}
