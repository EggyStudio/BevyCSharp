namespace Bevy;

/// <summary>Runs after <see cref="OnUpdateAttribute"/>, before queued commands are applied.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnPostUpdateAttribute : Attribute;
