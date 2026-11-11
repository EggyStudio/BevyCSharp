namespace Bevy;

/// <summary>What the number is measured in, shown after the box.</summary>
/// <param name="suffix">The unit, such as <c>m</c> or <c>deg</c>.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class UnitAttribute(string suffix) : Attribute
{
    /// <summary>The unit.</summary>
    public string Suffix { get; } = suffix;
}
