namespace Bevy;

/// <summary>Runs once after the main loop exits.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnCleanupAttribute : Attribute;
