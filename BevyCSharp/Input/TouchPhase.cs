namespace Bevy;

/// <summary>Where a touch is in its life.</summary>
public enum TouchPhase
{
    /// <summary>Still down, and not new this frame.</summary>
    Held = 0,

    /// <summary>Went down this frame.</summary>
    Started = 1,

    /// <summary>Came up this frame. Reported once, then gone.</summary>
    Ended = 2,
}
