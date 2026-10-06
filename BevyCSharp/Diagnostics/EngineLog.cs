using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Where every error the engine logs and goes on from is said, on the error stream as before and to
/// whoever listens, each laid to the app that logged it.
/// </summary>
/// <remarks>
/// <para>
/// The errors of this engine are an exception a system or a behavior threw, which the app logs and
/// runs on from; an exception a callback from the bridge caught, an observer's, a component hook's,
/// a state rule's, a queued command's, or a carried file's that could not be read; a material file
/// that could not be read again, a resource that would not be disposed and a project file that would
/// not be read; and whatever Bevy logs at its error level, which the bridge keeps for the managed
/// side to take. A panic the bridge's guard catches comes back as an exception from the call that
/// made it, which is a system's where a system made the call, and reaches here as that.
/// </para>
/// <para>
/// A warning is said on the error stream as it always was, and is not an error here. Two are left so,
/// a system scoped to a state nobody added and an override of a scene that found no node, each a
/// likely mistake that the engine runs on from as it should.
/// </para>
/// <para>
/// An error is laid to the app it is given, or else to the app that is running, so a listener that
/// knows which app is whose knows whose an error is, whichever thread logged it. Bevy's own errors
/// are kept by the bridge and taken as a run ends and as the app is disposed, which lays them to
/// the app that logged them, and they are not written again, since Bevy has said them already.
/// </para>
/// </remarks>
internal static unsafe class EngineLog
{
    /// <summary>
    /// Every error, with the app it is laid to or none, what kind it is, what it says, and the
    /// exception where there is one.
    /// </summary>
    internal static event Action<App?, string, string, Exception?>? ErrorLogged;

    /// <summary>Writes an error to the error stream and tells whoever listens.</summary>
    /// <param name="app">The app it is laid to, or none for the app that is running.</param>
    /// <param name="kind">What it comes from, such as <c>system</c> or <c>observer</c>.</param>
    /// <param name="text">The line written, as the error stream has always had it.</param>
    /// <param name="exception">What was thrown, where something was.</param>
    internal static void Error(App? app, string kind, string text, Exception? exception = null)
    {
        Console.Error.WriteLine(text);
        Tell(app ?? App.Running, kind, text, exception);
    }

    /// <summary>Takes every error line Bevy logged and the bridge kept, and tells whoever listens.</summary>
    /// <param name="app">The app that logged them.</param>
    internal static void TakeBevys(App app)
    {
        // Each line is taken whole or not at all, so a line longer than the first try's buffer is
        // asked for again with room for it, and an empty answer is the end.
        while (true)
        {
            string line;
            try
            {
                line = Native.ReadText((buffer, capacity) => Native.bcs_log_take_error(buffer, capacity), "taking Bevy's errors");
            }
            catch (BevyNativeException)
            {
                // A bridge that cannot answer has nothing to hand over, which is no reason to throw
                // from the end of a run.
                return;
            }

            if (line.Length == 0) return;
            Tell(app, "bevy", line, null);
        }
    }

    private static void Tell(App? app, string kind, string text, Exception? exception)
    {
        // A listener that throws must not take down the code that logged, which is often a catch
        // already, or a callback the bridge made.
        try
        {
            ErrorLogged?.Invoke(app, kind, text, exception);
        }
        catch (Exception)
        {
        }
    }
}
