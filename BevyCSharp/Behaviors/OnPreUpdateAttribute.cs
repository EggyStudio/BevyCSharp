namespace Bevy;

/// <summary>Runs before <see cref="OnUpdateAttribute"/> each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnPreUpdateAttribute : Attribute;
