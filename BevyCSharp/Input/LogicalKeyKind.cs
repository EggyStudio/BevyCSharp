namespace Bevy;

/// <summary>What kind of key a <see cref="LogicalKey"/> is, as the variants of Bevy's <c>Key</c> divide them.</summary>
public enum LogicalKeyKind
{
    /// <summary>A key with a name of its own rather than a character, as Enter or ArrowLeft.</summary>
    Named,

    /// <summary>A key that types a character, as the one that types '?'.</summary>
    Character,

    /// <summary>A key that changes the character the next one types, as an accent does.</summary>
    Dead,
}
