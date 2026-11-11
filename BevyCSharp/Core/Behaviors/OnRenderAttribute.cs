namespace Bevy;

/// <summary>Runs in the render stage each frame, before <see cref="OnLastAttribute"/>.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class OnRenderAttribute : Attribute;
