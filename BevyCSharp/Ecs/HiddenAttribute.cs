namespace Bevy;

/// <summary>Not shown at all.</summary>
/// <remarks>
/// For a field that is a component's own working state. It is still a field of the struct and
/// still saved; it is not somebody else's business.
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class HiddenAttribute : Attribute;
