namespace Bevy;

/// <summary>Runs in the main update stage each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnUpdateAttribute : Attribute;
