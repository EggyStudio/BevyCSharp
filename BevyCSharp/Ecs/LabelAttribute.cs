namespace Bevy;

/// <summary>What a field is called on screen, when its own name is not the right words.</summary>
/// <param name="text">The words to show.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
public sealed class LabelAttribute(string text) : Attribute
{
    /// <summary>The words to show.</summary>
    public string Text { get; } = text;
}
