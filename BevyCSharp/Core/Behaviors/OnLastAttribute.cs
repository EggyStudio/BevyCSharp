namespace Bevy;

/// <summary>Runs at the very end of every frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnLastAttribute : Attribute;
