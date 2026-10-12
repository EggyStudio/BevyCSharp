namespace Bevy;

/// <summary>Runs once at startup, before the first frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnStartupAttribute : Attribute;
