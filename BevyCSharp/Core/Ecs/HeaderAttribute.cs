namespace Bevy;

/// <summary>A heading above a field, which groups the ones under it.</summary>
/// <param name="text">What the heading says.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class HeaderAttribute(string text) : Attribute
{
    /// <summary>What the heading says.</summary>
    public string Text { get; } = text;
}
