using System.Diagnostics;
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
/// a terminal or the tool controlling the app rather than the app. With a cap, a thread of its own
/// reads how much memory the process holds four times a second, and past the cap says why on the
/// console and in the crash file and ends the process, so it is the app that stops and says so.
/// </para>
/// <para>
/// The cap is <see cref="Config.MemoryCap"/>, or <c>BCS_MEMORY_CAP_GB</c> in the environment where
/// the config names none, in gigabytes, and there is none by default, so a shipped game is never
/// stopped by it. The tools that start apps to look at them, <c>bcs open</c>, <c>bcs test</c> and
/// <c>bcs run</c>, <c>build/drive-feature-test.sh</c> and the test suite, give <see
/// cref="DefaultCap"/> where the environment names none.
/// </para>
/// <para>
/// What is counted is the memory the process holds of the machine, which takes in the bridge's
/// allocations and the GPU driver's with the managed heap, and not what a GPU holds of its own. On
/// Linux the resident pages, which the system's own last resort weighs a process by, on macOS the
/// physical footprint, its resident pages with those the system compressed, and on Windows the
/// private bytes committed, its commit charge (<see cref="ResidentBytes"/>). Once the console and
/// the crash file have said why, the process
/// ends at once with <see cref="ExitCode"/>, running nothing an exit runs. Bevy's threads are still
/// drawing then, and a GPU driver torn down under them by the handlers of an ordinary exit crashed
/// the process, whose core dump held the memory for as long again as it took to write.
/// </para>
/// <para>
/// A test host stopped this way reads to <c>dotnet test</c> as one that crashed, and its tally of
/// the tests that ran before still says they passed, so <c>build/test.py</c>, which <c>bcs test</c>
/// and CI run the suite through, says a host whose output holds this line was stopped at its cap,
/// runs the suite again in parts and counts every listed test without a result.
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
    private static long _peak;
    private static Thread? _watch;

    /// <summary>The cap in force, in bytes, or zero where there is none.</summary>
    public static long Cap => Interlocked.Read(ref _cap);

    /// <summary>
    /// The cap in gigabytes the tools that start apps to look at them give, eight, or a quarter of
    /// the machine's memory where that is less.
    /// </summary>
    public static double DefaultCap { get; } = Math.Min(8.0, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / 4.0 / Gigabyte);

    /// <summary>How many bytes of the machine's memory the process holds now.</summary>
    /// <remarks>
    /// <para>
    /// The resident set on Linux, the pages in the machine's memory, which run it out and by which
    /// it weighs a process when they do.
    /// </para>
    /// <para>
    /// On macOS, the physical footprint, which Activity Monitor shows as a process's memory: its
    /// resident pages and those the system's memory compressor holds for it compressed. Short of
    /// memory, macOS compresses pages a process has not touched lately, which stay the process's
    /// and leave its resident size, so a resident size read 189 megabytes more for a quarter of a
    /// gigabyte written a byte a page on a runner short of memory, where the footprint counts it
    /// all. The resident size is read where the system does not answer.
    /// </para>
    /// <para>
    /// On Windows, the private bytes the process has committed, its commit charge, rather than its
    /// working set. Windows trims a working set as it pleases, moving pages the process has not
    /// touched lately to its standby list while they are still the process's, so a working set can
    /// read far less than the process holds, and read as a quarter of a gigabyte written a byte a
    /// page grew by 64 megabytes. What runs a Windows machine out is its commit limit, which the
    /// private bytes count toward until they are freed, whatever is trimmed.
    /// </para>
    /// <para>Each reading is kept toward the peak the <c>memory</c> command says.</para>
    /// </remarks>
    public static long ResidentBytes()
    {
        var held = Held();
        var peak = Interlocked.Read(ref _peak);
        while (held > peak)
        {
            var was = Interlocked.CompareExchange(ref _peak, held, peak);
            if (was == peak) break;
            peak = was;
        }

        return held;
    }

    /// <summary>The figure <see cref="ResidentBytes"/> reads, by system.</summary>
    private static long Held()
    {
        if (OperatingSystem.IsMacOS() && Footprint() is > 0 and var footprint) return footprint;
        if (!OperatingSystem.IsWindows()) return Environment.WorkingSet;

        using var process = Process.GetCurrentProcess();
        return process.PrivateMemorySize64;
    }

    /// <summary>
    /// The physical footprint macOS answers for this process, or zero where it answers nothing.
    /// </summary>
    private static long Footprint()
    {
        try
        {
            return ProcPidRusage(Environment.ProcessId, RusageInfoV0, out var info) == 0 ? (long)info.PhysFootprint : 0;
        }
        catch (Exception error) when (error is DllNotFoundException or EntryPointNotFoundException)
        {
            return 0;
        }
    }

    /// <summary>The flavor of <c>proc_pid_rusage</c> whose answer is <see cref="RusageInfo"/>.</summary>
    private const int RusageInfoV0 = 0;

    /// <summary>
    /// <c>rusage_info_v0</c> of macOS's <c>sys/resource.h</c>, field for field, of which only the
    /// footprint is read.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct RusageInfo
    {
        public ulong Uuid0, Uuid1, UserTime, SystemTime, PackageIdleWakeups, InterruptWakeups, PageIns, WiredSize;
        public ulong ResidentSize, PhysFootprint, StartTime, ExitTime;
    }

    /// <summary>
    /// The most of the machine's memory the process has held since it began, the largest
    /// <see cref="ResidentBytes"/> read here, or what the system says where it says more.
    /// </summary>
    /// <remarks>
    /// The readings here come first because they are the same on every system, and the same the
    /// cap and the soak use, where the system's own peak, <see cref="Process.PeakWorkingSet64"/>,
    /// is 0 on macOS. With a cap the watch reads four times a second, so a peak between two
    /// readings of the command is caught. Without one, the readings are the ones taken, this call's
    /// among them.
    /// </remarks>
    internal static long PeakBytes()
    {
        var now = ResidentBytes();

        // The system's own peak of the same figure where it keeps one, which Windows does not for
        // the private bytes, so there the readings are all there is, as on macOS.
        if (OperatingSystem.IsWindows()) return Math.Max(now, Interlocked.Read(ref _peak));

        using var process = Process.GetCurrentProcess();
        return Math.Max(Math.Max(now, Interlocked.Read(ref _peak)), process.PeakWorkingSet64);
    }

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
            // Said on the console already, and a tool controlling the app reads the console.
        }

        // Said again, last, since Bevy's threads go on writing to the console while the crash file
        // is written, and build/test.py reads the end of a lost host's output for why it went.
        Console.Error.WriteLine(said);

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

    /// <summary>macOS's usage of a process, from libproc, by the name .NET's own runtime loads it by.</summary>
    [LibraryImport("libproc", EntryPoint = "proc_pid_rusage")]
    private static partial int ProcPidRusage(int pid, int flavor, out RusageInfo info);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool TerminateProcess(nint process, uint code);

    [LibraryImport("kernel32.dll")]
    private static partial nint GetCurrentProcess();
}
