using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Writes into Bevy's log from C#, at Bevy's five levels, Bevy's <c>trace!</c> to <c>error!</c>.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's own systems write their lines to its log, which shows them by the level the app was made
/// with, info by default, and by <c>RUST_LOG</c> where it is set. A line from here is shown or left
/// out the same way, under the target <c>csharp</c>, so <c>RUST_LOG=csharp=debug</c> shows a game's
/// debug lines without Bevy's. An error line is one of the engine's errors as Bevy's own are, which a
/// test that fails on the engine's errors fails on too.
/// </para>
/// <para>
/// A line written each frame floods the log, and the <c>Once</c> forms are Bevy's <c>info_once!</c>
/// and its kin, written the first time their line of code runs and never again in the process,
/// whatever the line says, so a loop writes its first pass alone. <see cref="Once"/> runs any work so,
/// as Bevy's <c>once!</c> does.
/// </para>
/// <para>
/// This is apart from <see cref="ConsoleLog"/>, which keeps what C# writes to its own streams for a
/// console to show, and from the error stream an exception is written to.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// Log.Info("helpful information that is worth printing by default");
/// Log.WarnOnce("some warning we wish to call out only once");
/// </code>
/// </example>
public static class Log
{
    private static readonly ConcurrentDictionary<(string File, int Line), byte> Seen = new();

    /// <summary>Writes a line at the trace level, which Bevy leaves out unless asked for.</summary>
    public static void Trace(string message) => Write(0, message);

    /// <summary>Writes a line at the debug level, which Bevy leaves out unless asked for.</summary>
    public static void Debug(string message) => Write(1, message);

    /// <summary>Writes a line at the info level, the quietest Bevy shows by default.</summary>
    public static void Info(string message) => Write(2, message);

    /// <summary>Writes a line at the warn level, for something wrong that is not a failure.</summary>
    public static void Warn(string message) => Write(3, message);

    /// <summary>Writes a line at the error level, for something that failed.</summary>
    public static void Error(string message) => Write(4, message);

    /// <summary>Writes a line at the trace level the first time this line of code runs, Bevy's <c>trace_once!</c>.</summary>
    public static void TraceOnce(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (First(file, line)) Trace(message);
    }

    /// <summary>Writes a line at the debug level the first time this line of code runs, Bevy's <c>debug_once!</c>.</summary>
    public static void DebugOnce(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (First(file, line)) Debug(message);
    }

    /// <summary>Writes a line at the info level the first time this line of code runs, Bevy's <c>info_once!</c>.</summary>
    public static void InfoOnce(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (First(file, line)) Info(message);
    }

    /// <summary>Writes a line at the warn level the first time this line of code runs, Bevy's <c>warn_once!</c>.</summary>
    public static void WarnOnce(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (First(file, line)) Warn(message);
    }

    /// <summary>Writes a line at the error level the first time this line of code runs, Bevy's <c>error_once!</c>.</summary>
    public static void ErrorOnce(string message, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        if (First(file, line)) Error(message);
    }

    /// <summary>Runs <paramref name="work"/> the first time this line of code runs and never again, Bevy's <c>once!</c>.</summary>
    /// <remarks>For something costly a system does each frame and needs once, such as working out a table it then keeps.</remarks>
    public static void Once(Action work, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        ArgumentNullException.ThrowIfNull(work);
        if (First(file, line)) work();
    }

    // Whether this is the first time the line of code at file and line has asked, counting this one.
    private static bool First(string file, int line) => Seen.TryAdd((file, line), 0);

    private static void Write(int level, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Native.Check(Native.bcs_log_write(level, message), "writing to Bevy's log");
    }
}
