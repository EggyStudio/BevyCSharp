namespace Bevy;

/// <summary>What a pointer is doing, for <see cref="SyntheticInput"/>.</summary>
public enum PointerAction
{
    /// <summary>Moved somewhere, with no button changing.</summary>
    Move,

    /// <summary>A button went down.</summary>
    Press,

    /// <summary>A button came up.</summary>
    Release,
}
