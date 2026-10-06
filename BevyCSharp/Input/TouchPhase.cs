namespace Bevy;

/// <summary>Where a touch is in its life.</summary>
/// <remarks>
/// A finger in <see cref="Input.Touches"/> is held, started or ended, as the frame leaves it. A
/// <see cref="TouchInput"/> message, one for each change, is started, moved, ended or canceled, as
/// Bevy's <c>TouchPhase</c> is, and a wheel's <see cref="MouseWheel"/> says the same of a touchpad's
/// scroll.
/// </remarks>
public enum TouchPhase
{
    /// <summary>Still down, and not new this frame.</summary>
    Held = 0,

    /// <summary>Went down this frame.</summary>
    Started = 1,

    /// <summary>Came up this frame. Reported once, then gone.</summary>
    Ended = 2,

    /// <summary>Moved while down, as a message says of it.</summary>
    Moved = 3,

    /// <summary>Taken away by the platform before it came up, as a message says of it, which a call coming in does.</summary>
    Canceled = 4,
}
