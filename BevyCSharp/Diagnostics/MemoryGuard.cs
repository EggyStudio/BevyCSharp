using System.Globalization;
using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// Stops the process when it holds more of the machine's memory than its cap, before the machine
/// runs out.
/// </summary>
/// <remarks>
/// <para>
/// A scene, a technique or a leak can take memory faster than anything watching the frame notices,
/// and a machine that runs out ends whatever process the system chooses, which may be the editor,
/// a terminal or the tool driving the app rather than the app. With a cap, a thread of its own
/// reads how much memory the process holds four times a second, and past the cap says why on the
/// console and in the crash file and ends the process, so it is the app that stops and says so.
/// </para>
/// <para>
/// The cap is <see cref="Config.MemoryCap"/>, or <c>BCS_MEMORY_CAP_GB</c> in the environment where
/// the config names none, in gigabytes, and there is none by default, so a shipped game is never
/// stopped by it. The tools that start apps to look at them, <c>bcs open</c>, <c>bcs test</c> and
/// <c>bcs run</c>, the drive script and the test suite, give <see cref="DefaultCap"/> where the
/// environment names none.
/// </para>
/// <para>
/// What is counted is the resident memory, the pages of the machine's memory the process holds,
/// which takes in the bridge's allocations and the GPU driver's with the managed heap, and not
/// what a GPU holds of its own. Once the console and the crash file have said why, the process
/// ends at once with <see cref="ExitCode"/>, running nothing an exit runs. Bevy's threads are still
/// drawing then, and a GPU driver torn down under them by the handlers of an ordinary exit crashed
/// the process, whose core dump held the memory for as long again as it took to write.
/// </para>
/// </remarks>
public static partial class MemoryGuard
{
    /// <summary>The variable naming the cap in gigabytes, where the config names none.</summary>
    public const string Variable = "BCS_MEMORY_CAP_GB";

    /// <summary>What the process exits with when it is stopped at its cap.</summary>
    public const int ExitCode = 86;

    private const long Gigabyte = 1L << 30;

    private static readonly object Gate = new();
    private static long _cap;
    private static Thread? _watch;

    /// <summary>The cap in force, in bytes, or zero where there is none.</summary>
    public static long Cap => Interlocked.Read(ref _cap);

    /// <summary>
    /// The cap in gigabytes the tools that start apps to look at them give, eight, or a quarter of
    /// the machine's memory where that is less.
    /// </summary>
    public static double DefaultCap { get; } = Math.Min(8.0, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 4.0 / Gigabyte);

    /// <summary>How many bytes of the machine's memory the process holds now.</summary>
    public static long ResidentBytes() => Environment.WorkingSet;

    /// <summary>
    /// Holds the process to a cap from here on, starting the watch the first time, or with zero
    /// lifts the cap.
    /// </summary>
    /// <param name="gigabytes">The cap, in gigabytes of the machine's memory.</param>
    public static void Watch(double gigabytes)
    {
        Interlocked.Exchange(ref _cap, gigabytes > 0 ? (long)(gigabytes * Gigabyte) : 0);
        if (gigabytes <= 0) return;

        lock (Gate)
        {
            if (_watch is not null) return;

            _watch = new Thread(Run) { IsBackground = true, Name = "memory guard" };
            _watch.Start();
        }
    }

    /// <summary>
    /// Gives the environment the default cap where it names none, for a tool to call before it
    /// starts an app or makes one, so the app holds to it.
    /// </summary>
    /// <returns>The cap the environment names now, in gigabytes.</returns>
    public static double DefaultTheEnvironment()
    {
        if (Named() is { } named) return named;

        Environment.SetEnvironmentVariable(Variable, DefaultCap.ToString("0.##", CultureInfo.InvariantCulture));
        return DefaultCap;
    }

    /// <summary>
    /// The cap in gigabytes a config names, or the environment where it names none, or zero.
    /// </summary>
    internal static double CapFor(Config config) => config.MemoryCap > 0 ? config.MemoryCap : Named() ?? 0;

    /// <summary>
    /// What to say where the memory held is past a cap, or nothing where it is not.
    /// </summary>
    internal static string? Past(long held, long cap) =>
        cap > 0 && held > cap
            ? $"[BevyCSharp] The process holds {held / (double)Gigabyte:0.00} GB of the machine's memory, past its cap of "
              + $"{cap / (double)Gigabyte:0.00} GB, so it stops here rather than take what the machine has left. The cap is "
              + $"Config.MemoryCap or {Variable}."
            : null;

    /// <summary>The cap the environment names, or nothing where it names none that reads.</summary>
    private static double? Named() =>
        double.TryParse(Environment.GetEnvironmentVariable(Variable), NumberStyles.Float, CultureInfo.InvariantCulture, out var gigabytes)
        && gigabytes > 0
            ? gigabytes
            : null;

    private static void Run()
    {
        while (true)
        {
            Thread.Sleep(250);

            if (Past(ResidentBytes(), Cap) is { } said) Stop(said);
        }
    }

    /// <summary>Says why on the console and in the crash file, and ends the process.</summary>
    private static void Stop(string said)
    {
        Console.Error.WriteLine(said);

        try
        {
            CrashLog.Write("Stopped at its memory cap", said);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Said on the console already, and a tool driving the app reads the console.
        }

        try
        {
            if (OperatingSystem.IsWindows()) TerminateProcess(GetCurrentProcess(), ExitCode);
            else EndNow(ExitCode);
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException)
        {
            // A system without the call ends the usual way, which may crash on the way out.
        }

        Environment.Exit(ExitCode);
    }

    /// <summary>
    /// Ends the process with a code, running no handler, finalizer or destructor.
    /// </summary>
    [LibraryImport("libc", EntryPoint = "_exit")]
    private static partial void EndNow(int code);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TerminateProcess(nint process, uint code);

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentProcess();
}
