namespace Bevy;

/// <summary>What an error the renderer reports does to the app, set with <see cref="Render.ErrorPolicy"/>.</summary>
/// <remarks>
/// Bevy's <c>RenderErrorPolicy</c>, with its own answer, closing the app, as the first. An error
/// left unaddressed under <see cref="Ignore"/> or <see cref="Recover"/> may come back every frame,
/// which can make the picture flash, so a game choosing either takes away what caused it, a
/// setting turned down or an entity hidden, and is tried with the error caused on purpose
/// (<see cref="Render.CauseError"/>).
/// </remarks>
public enum RenderErrorPolicy
{
    /// <summary>The app closes with an error, Bevy's own answer and the one unless set otherwise.</summary>
    Quit = 0,

    /// <summary>The error is logged and drawing carries on as if nothing happened.</summary>
    Ignore = 1,

    /// <summary>
    /// The app keeps running with drawing stopped, its systems still running, until the policy is
    /// set to another, which takes over from the next frame.
    /// </summary>
    StopRendering = 2,

    /// <summary>
    /// The renderer is made again, a device and all, with the backend the app started with, and
    /// drawing carries on with it. The answer to a lost device.
    /// </summary>
    /// <remarks>
    /// Bevy 0.20 makes it again without the lighting it had, so a mesh Bevy lights draws black after,
    /// where an unlit one draws as before, and a device lost while Bevy's GPU clustering waits on a
    /// readback ends the run in Bevy's own code, so a lost device is survived only some of the time.
    /// It keeps a game running long enough to save and say so, rather than carrying on as before.
    /// </remarks>
    Recover = 3,
}
