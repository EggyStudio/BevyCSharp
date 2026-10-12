namespace Bevy;

/// <summary>Where a button sits when several of them share a line.</summary>
public enum ButtonLine
{
    /// <summary>On a line of its own.</summary>
    Alone,

    /// <summary>The first of a row of buttons.</summary>
    Start,

    /// <summary>One of the middle ones.</summary>
    Middle,

    /// <summary>The last one, after which the line is drawn.</summary>
    End,
}
