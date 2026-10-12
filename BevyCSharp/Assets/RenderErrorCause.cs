namespace Bevy;

/// <summary>An error <see cref="Render.CauseError"/> causes on the GPU, as Bevy's render_recovery example does.</summary>
public enum RenderErrorCause
{
    /// <summary>
    /// Running out of the GPU's memory, by asking for far more pictures than a device holds. It
    /// reaches the machine's own graphics, which may need a restart to recover.
    /// </summary>
    OutOfMemory = 1,

    /// <summary>A validation error, from a buffer too large to make, which nothing else is hurt by.</summary>
    Validation = 2,

    /// <summary>A lost device, by destroying it.</summary>
    DeviceLost = 3,

    /// <summary>
    /// A lost device, by a compute shader that never ends, which the system resets the GPU over. It
    /// reaches the machine's own graphics, which may need a restart to recover.
    /// </summary>
    EndlessLoop = 4,
}
