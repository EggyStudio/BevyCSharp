namespace Bevy;

/// <summary>How loudly a line was said.</summary>
/// <remarks>
/// Four kinds and no more. What a person does with a console is scan it for the lines that matter,
/// and every extra kind is another decision at the moment of writing and another thing to filter.
/// </remarks>
public enum LogLevel
{
    /// <summary>Something happened.</summary>
    Info,

    /// <summary>Something happened that probably should not have.</summary>
    Warning,

    /// <summary>Something did not happen that should have.</summary>
    Error,

    /// <summary>What somebody typed, and what answered them.</summary>
    Echo,
}
