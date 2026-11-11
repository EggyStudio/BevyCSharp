namespace Bevy;

/// <summary>What a note beside a field is saying.</summary>
public enum NoteKind
{
    /// <summary>A word over a group of fields.</summary>
    Heading,

    /// <summary>Something worth knowing.</summary>
    Info,

    /// <summary>Something that will probably go wrong.</summary>
    Warning,

    /// <summary>Something that is already wrong.</summary>
    Error,
}
