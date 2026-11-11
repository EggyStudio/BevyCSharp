namespace Bevy;

/// <summary>Where a field or a button sits among its neighbors. Lower is first.</summary>
/// <param name="order">The place.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class OrderAttribute(int order) : Attribute
{
    /// <summary>The place.</summary>
    public int Order { get; } = order;
}
