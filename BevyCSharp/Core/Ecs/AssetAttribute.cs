namespace Bevy;

/// <summary>
/// Which sort of asset a field holds, so a tool can offer the right files.
/// </summary>
/// <remarks>
/// A handle is a handle whatever is behind it, so the field's type cannot say whether a mesh or a
/// sound belongs in it. This does, and a field without it is offered every file there is.
/// </remarks>
/// <param name="kind">One of the names on <see cref="AssetKind"/>.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class AssetAttribute(string kind) : Attribute
{
    /// <summary>The asset type, as <see cref="AssetKind"/> names it.</summary>
    public string Kind { get; } = kind;

    /// <summary>
    /// Which file extensions to offer, separated by spaces, or nothing for the usual ones.
    /// </summary>
    /// <remarks>
    /// For an asset type whose files are not named after it, such as a scene in a glTF file, a
    /// material in one, anything a game's own loader reads.
    /// </remarks>
    public string Extensions { get; init; } = string.Empty;
}
