using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The run's log and what a crash said, written beside the executable so a player can send them.
/// </summary>
/// <remarks>
/// <para>
/// A game on a player's machine has no console anybody reads, so what it said and why it ended
/// would be lost with the window. The first app of a process starts this, unless
/// <see cref="Config.Logs"/> is an empty string, and from then on every line the console's log
/// holds is written to <c>latest.log</c> in <see cref="Folder"/>, Bevy's own lines among them, the
/// last five runs' kept as <c>latest.1.log</c> to <c>latest.4.log</c>.
/// </para>
/// <para>
/// An exception nothing caught, a task's exception nobody looked at, or a panic inside Bevy on one
/// of its own threads is written to <c>crash-&lt;time&gt;.txt</c> there, the panic handed over by
/// the bridge's panic hook as it happens, since such a panic may end the process before anything
/// else is said. The file holds what happened, the last panic the bridge caught, the last
/// <see cref="Tail"/> lines of the log, and the system, .NET, the bridge's ABI, the graphics
/// adapter and the backend, the things a report from somebody else's machine is read for. A
/// panic in one of Bevy's tasks is carried to the thread waiting on it and thrown there as well, so
/// whatever a run says after its first crash goes in the same file.
/// </para>
/// <para>
/// The next run says where the last crash's file is as it starts, on the log and in
/// <see cref="LastCrash"/>, so a game can offer to open it.
/// </para>
/// </remarks>
public static unsafe class CrashLog
{
    /// <summary>How many of the log's last lines a crash file holds.</summary>
    public const int Tail = 200;

    /// <summary>How many runs' logs are kept, this one's among them.</summary>
    public const int Runs = 5;

    private static readonly object Gate = new();
    private static StreamWriter? _latest;
    private static string? _crash;
    private static string? _adapter;
    private static string? _backend;
    private static int _repeats;
    private static bool _teed;

    /// <summary>
    /// Where the logs are written, or nothing before the first app or where it asked for none.
    /// </summary>
    public static string? Folder { get; private set; }

    /// <summary>
    /// Whether the first app of the process starts the log, as it does unless a test suite, which
    /// is many apps in one process, says otherwise.
    /// </summary>
    internal static bool StartedByApps { get; set; } = true;

    /// <summary>
    /// The crash file the previous run left, found as this one started, or nothing.
    /// </summary>
    public static string? LastCrash { get; private set; }

    /// <summary>The crash file this run has written, or nothing while it has not crashed.</summary>
    public static string? Crash
    {
        get { lock (Gate) return _crash; }
    }

    /// <summary>
    /// Starts the run's log and the crash hooks in a folder, once for the process, the first app's
    /// folder holding for every app after it.
    /// </summary>
    internal static void Start(string folder, GraphicsBackend backend)
    {
        lock (Gate)
        {
            _backend = backend.ToString();
            if (Folder is not null) return;

            try
            {
                Directory.CreateDirectory(folder);
                Rotate(folder);
                _latest = new StreamWriter(new FileStream(Path.Combine(folder, "latest.log"), FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    AutoFlush = true,
                };
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                // A folder the game may not write, as one under Program Files is, leaves the run
                // with no log rather than refusing to start.
                Console.Error.WriteLine($"[BevyCSharp] The run's log is not written, since {folder} could not be: {error.Message}");
                return;
            }

            Folder = folder;
        }

        _teed = !ConsoleLog.Teeing;
        ConsoleLog.Start();
        ConsoleLog.Added += Written;
        AppDomain.CurrentDomain.UnhandledException += Unhandled;
        TaskScheduler.UnobservedTaskException += Unobserved;
        Native.Check(Native.bcs_log_collect(1), "collecting Bevy's log lines");
        Native.Check(Native.bcs_crash_writer(&Panicked), "giving the bridge the crash log's writer");

        var marker = Path.Combine(folder, "crashed.txt");
        if (File.Exists(marker))
        {
            var last = File.ReadAllText(marker).Trim();
            File.Delete(marker);

            if (File.Exists(last))
            {
                LastCrash = last;
                Console.WriteLine($"[BevyCSharp] The last run crashed, and what it said is in {last}");
            }
        }
    }

    /// <summary>Stops the run's log and the hooks, for a test that started them.</summary>
    internal static void Stop()
    {
        if (Folder is null) return;

        Native.Check(Native.bcs_crash_writer(null), "taking back the crash log's writer");
        Native.Check(Native.bcs_log_collect(0), "stopping the collection of Bevy's log lines");
        AppDomain.CurrentDomain.UnhandledException -= Unhandled;
        TaskScheduler.UnobservedTaskException -= Unobserved;
        ConsoleLog.Added -= Written;
        if (_teed) ConsoleLog.Stop();

        lock (Gate)
        {
            _latest?.Dispose();
            _latest = null;
            _crash = null;
            _adapter = null;
            _repeats = 0;
            Folder = null;
            LastCrash = null;
        }
    }

    /// <summary>Moves the earlier runs' logs one along, the oldest going.</summary>
    private static void Rotate(string folder)
    {
        string Run(int back) => Path.Combine(folder, back == 0 ? "latest.log" : $"latest.{back}.log");

        File.Delete(Run(Runs - 1));
        for (var back = Runs - 2; back >= 0; back--)
        {
            if (File.Exists(Run(back))) File.Move(Run(back), Run(back + 1));
        }
    }

    /// <summary>
    /// Takes the lines Bevy logged since the last take into the console's log, and so into the
    /// run's. Called by the app each frame, and before a crash is written.
    /// </summary>
    internal static void TakeBevys()
    {
        if (Folder is null) return;

        var level = stackalloc int[1];
        while (true)
        {
            string line;
            try
            {
                line = Native.ReadText((buffer, capacity) => Native.bcs_log_take_line(buffer, capacity, level), "taking Bevy's log lines");
            }
            catch (BevyNativeException)
            {
                return;
            }

            if (line.Length == 0) return;

            ConsoleLog.Write(*level switch { >= 4 => LogLevel.Error, 3 => LogLevel.Warning, _ => LogLevel.Info }, line);
        }
    }

    /// <summary>
    /// Keeps the adapter's description for a crash file, read where the renderer can say it.
    /// </summary>
    internal static void Describe(string? adapter)
    {
        lock (Gate) _adapter ??= adapter;
    }

    /// <summary>
    /// Whether the adapter has been described, which a run asks again each frame until it is.
    /// </summary>
    internal static bool Described
    {
        get { lock (Gate) return _adapter is not null; }
    }

    /// <summary>What the most recent panic the bridge caught said, or nothing.</summary>
    internal static string LastPanic() =>
        Native.ReadText((buffer, capacity) => Native.bcs_last_panic(buffer, capacity), "reading the bridge's last panic");

    /// <summary>
    /// Writes a crash to this run's crash file, made the first time, and says where on the log.
    /// </summary>
    /// <param name="title">What happened, in a few words.</param>
    /// <param name="what">What was thrown or panicked, whole.</param>
    /// <returns>The file, or nothing where the run writes no logs.</returns>
    public static string? Write(string title, string what)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentNullException.ThrowIfNull(what);
        if (Folder is not { } folder) return null;

        TakeBevys();

        var text = new StringBuilder();
        string path;
        bool first;
        lock (Gate)
        {
            first = _crash is null;
            path = _crash ??= Path.Combine(folder, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.txt");

            if (first)
            {
                text.AppendLine("BevyCSharp crash report");
                text.AppendLine();
                text.AppendLine($"When:      {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
                text.AppendLine($"Program:   {Program()}");
                text.AppendLine($"System:    {RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})");
                text.AppendLine($".NET:      {RuntimeInformation.FrameworkDescription}");
                text.AppendLine($"Bridge:    ABI {Native.ExpectedAbiVersion}, renderer {(App.HasRenderer ? "in" : "left out")}, editor features {(App.HasEditor ? "in" : "left out")}");
                text.AppendLine($"Adapter:   {_adapter ?? "not known"}");
                text.AppendLine($"Backend:   {_backend ?? "not known"} asked for");
            }
        }

        text.AppendLine();
        text.AppendLine($"== {title}");
        text.AppendLine();
        text.AppendLine(what.TrimEnd());

        // The panic the bridge caught last, where it is not the one written above, since a panic
        // inside a call from C# comes back as an exception whose message has its first lines alone.
        var panic = Quietly(LastPanic) ?? string.Empty;
        if (panic.Length > 0 && !what.Contains(panic, StringComparison.Ordinal))
        {
            text.AppendLine();
            text.AppendLine("== The last panic inside the bridge");
            text.AppendLine();
            text.AppendLine(panic.TrimEnd());
        }

        text.AppendLine();
        text.AppendLine($"== The last {Tail} lines of the log");
        text.AppendLine();
        var lines = ConsoleLog.All();
        foreach (var line in lines.Skip(Math.Max(0, lines.Length - Tail))) text.AppendLine(Line(line));

        try
        {
            File.AppendAllText(path, text.ToString());
            File.WriteAllText(Path.Combine(folder, "crashed.txt"), path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return null;
        }

        if (first) Console.Error.WriteLine($"[BevyCSharp] {title}. What happened is written to {path}");
        return path;
    }

    /// <summary>The program's name and version, as its assembly says them.</summary>
    private static string Program()
    {
        var entry = System.Reflection.Assembly.GetEntryAssembly()?.GetName();
        return entry is null ? "not known" : $"{entry.Name} {entry.Version}";
    }

    /// <summary>A log line as the files write it, with its level and frame.</summary>
    private static string Line(LogLine line)
    {
        var level = line.Level switch
        {
            LogLevel.Warning => "WARN ",
            LogLevel.Error => "ERROR",
            LogLevel.Echo => "ECHO ",
            _ => "INFO ",
        };

        return $"[{line.Frame,6}] {level} {line.Text}";
    }

    /// <summary>
    /// Writes a line the console's log took to the run's log, a repeat as a count once it ends.
    /// </summary>
    private static void Written(LogLine line, bool repeat)
    {
        lock (Gate)
        {
            if (_latest is null) return;

            if (repeat)
            {
                _repeats++;
                return;
            }

            try
            {
                if (_repeats > 0) _latest.WriteLine($"         said {_repeats} more times");
                _repeats = 0;
                _latest.WriteLine(Line(line));
            }
            catch (IOException)
            {
                // A disk that filled is no reason to stop the game that is logging.
            }
        }
    }

    private static void Unhandled(object? sender, UnhandledExceptionEventArgs args) =>
        Quietly(() => Write(
            args.IsTerminating ? "An exception nothing caught, which ended the program" : "An exception nothing caught",
            args.ExceptionObject.ToString() ?? "nothing said"));

    private static void Unobserved(object? sender, UnobservedTaskExceptionEventArgs args) =>
        Quietly(() => Write("A task's exception nobody looked at", args.Exception.ToString()));

    /// <summary>
    /// What the bridge's panic hook calls with a panic on one of Bevy's own threads.
    /// </summary>
    /// <remarks>
    /// Called on the thread that panicked, before Bevy unwinds it, and nothing may unwind from it
    /// back into the bridge, so whatever goes wrong while writing is dropped.
    /// </remarks>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Panicked(byte* text, int length)
    {
        try
        {
            Write("A panic inside Bevy, on one of its own threads", Encoding.UTF8.GetString(text, length));
        }
        catch
        {
            // Nothing may unwind into the bridge (N 2.10 of NORM.md), and there is nowhere left
            // to say this.
        }
    }

    /// <summary>
    /// Runs something whose failure has nowhere to go, as a crash being written does.
    /// </summary>
    private static T? Quietly<T>(Func<T> run)
    {
        try
        {
            return run();
        }
        catch
        {
            return default;
        }
    }
}
