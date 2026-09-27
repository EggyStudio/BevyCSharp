using System.Diagnostics;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Plays the game in a window of its own, as Godot does, and builds it: the project is built and run
/// as a process beside the editor, driven from the Play tab.
/// </summary>
/// <remarks>
/// <para>
/// A process rather than a mode of the editor's own world, because a game that runs in the editor's
/// process shares its state, its crashes and its frame, and one that misbehaves takes the editor
/// with it. Its own window is also the window the game ships in, sized and framed as it will be.
/// </para>
/// <para>
/// What it runs is a project file, the sample's by default, and a setting names any other. It is run
/// through <c>dotnet run</c>, which builds it first when it is out of date, so the first press takes
/// as long as a build and later ones as long as a start. What the game writes goes to the editor's
/// console, each line marked as the game's, so a message from it is read where the editor's own are.
/// </para>
/// <para>
/// It lives in a tab rather than on the scene's toolbar, so the viewport holds the scene and the
/// tools that act on it and nothing about running the project. The tab keeps the last lines the
/// project wrote, beside whether it is running, since those are read together.
/// </para>
/// <para>
/// This is the first step of a larger plan, in <c>.github/PLAY.md</c>: a player that loads the scene
/// being edited rather than the game's own, a live view of the running game's world, and a tab that
/// builds a game to ship.
/// </para>
/// </remarks>
public static class EditorPlay
{
    /// <summary>How many of the lines the project wrote the tab keeps.</summary>
    private const int Kept = 2000;

    private static readonly Queue<string> _lines = new();
    private static readonly Lock _gate = new();

    private static Process? _job;
    private static Process? _stopped;
    private static bool _stopsWithEditor;

    /// <summary>The project file that is run, or empty for the sample's, found from where the editor runs.</summary>
    public static string Project { get; set; } = string.Empty;

    /// <summary>What is running: the game, a build, or nothing.</summary>
    public static PlayJob Job { get; private set; }

    /// <summary>Whether the game is running.</summary>
    public static bool Running => Job == PlayJob.Playing && Busy;

    /// <summary>Whether the game or a build is running.</summary>
    public static bool Busy => _job is { HasExited: false };

    /// <summary>How many lines have been written since the editor started, so a view knows when to follow.</summary>
    public static int Written { get; private set; }

    /// <summary>The project that is played and built, as a path, or nothing when there is none to find.</summary>
    public static string? Resolved => Project.Length > 0 ? Project : Sample();

    /// <summary>The last lines the game or a build wrote, oldest first.</summary>
    public static string[] Lines()
    {
        lock (_gate) return [.. _lines];
    }

    /// <summary>Forgets the lines written so far.</summary>
    public static void Clear()
    {
        lock (_gate) _lines.Clear();
    }

    /// <summary>Starts the game, or stops it if it is running.</summary>
    public static void Toggle()
    {
        if (Running)
        {
            Stop();
            return;
        }

        // The window, which the sample opens only when asked, since it runs headless otherwise.
        Start(PlayJob.Playing, ["run", "--project", "{project}", "--", "--window"]);
    }

    /// <summary>Builds the project without running it, which says whether it compiles.</summary>
    public static void Build() => Start(PlayJob.Building, ["build", "{project}"]);

    /// <summary>Runs <c>dotnet</c> over the project, stopping whatever ran before it.</summary>
    /// <remarks>
    /// One at a time, because a build while the game runs fails to replace the files the game has
    /// open on Windows, and on every platform a build and a run of one project write the same
    /// output folder.
    /// </remarks>
    private static void Start(PlayJob job, string[] arguments)
    {
        Stop();

        var project = Resolved;

        if (project is null || !File.Exists(project))
        {
            Say($"[play] there is no project at {project ?? "the sample's place"}; name one above");
            return;
        }

        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments) start.ArgumentList.Add(argument == "{project}" ? project : argument);

        try
        {
            _job = Process.Start(start);
        }
        catch (Exception exception)
        {
            Say($"[play] could not start dotnet for {Path.GetFileName(project)}: {exception.Message}");
            return;
        }

        if (_job is null) return;

        Job = job;

        _job.OutputDataReceived += static (_, line) => Say(line.Data);
        _job.ErrorDataReceived += static (_, line) => Say(line.Data);
        _job.EnableRaisingEvents = true;
        _job.Exited += static (sender, _) =>
        {
            // Not for one stopped from here, whose code is only the signal that ended it.
            if (sender is Process ended && ended == _job && ended != _stopped)
            {
                Say($"[play] finished with exit code {ended.ExitCode}");
            }
        };
        _job.BeginOutputReadLine();
        _job.BeginErrorReadLine();

        // Stopped with the editor, since a game left running by an editor that closed is a window
        // nobody knows the origin of. Hooked once, because it reads whichever process is current.
        if (!_stopsWithEditor)
        {
            AppDomain.CurrentDomain.ProcessExit += static (_, _) => Stop();
            _stopsWithEditor = true;
        }

        var name = Path.GetFileNameWithoutExtension(project);
        Say(job == PlayJob.Playing ? $"[play] building and starting {name}" : $"[play] building {name}");
    }

    /// <summary>Stops the game or the build, with everything it started.</summary>
    public static void Stop()
    {
        if (_job is not { HasExited: false } game) return;

        try
        {
            // The whole tree, since dotnet run starts the game as a child of its own.
            _stopped = game;
            game.Kill(entireProcessTree: true);
            Say("[play] stopped");
        }
        catch (InvalidOperationException)
        {
            // It ended on its own between asking and stopping.
        }
    }

    /// <summary>A line the project wrote, kept for the tab and passed to the editor's console marked as the game's.</summary>
    /// <remarks>
    /// Without the color codes a terminal would read, which Bevy's log writes and the console would
    /// otherwise show as text.
    /// </remarks>
    private static void Say(string? line)
    {
        if (line is not { Length: > 0 }) return;

        var plain = new System.Text.StringBuilder(line.Length);

        for (var index = 0; index < line.Length; index++)
        {
            // An escape, a bracket, then digits and semicolons up to the letter that ends it.
            if (line[index] == '\u001b' && index + 1 < line.Length && line[index + 1] == '[')
            {
                index += 2;
                while (index < line.Length && !char.IsLetter(line[index])) index++;
                continue;
            }

            plain.Append(line[index]);
        }

        var text = plain.ToString();

        // Read on the process's own threads, so the queue is shared with the tab under a lock.
        lock (_gate)
        {
            _lines.Enqueue(text);
            while (_lines.Count > Kept) _lines.Dequeue();
            Written++;
        }

        Console.WriteLine(text.StartsWith("[play]", StringComparison.Ordinal) ? text : $"[game] {text}");
    }

    /// <summary>
    /// The sample's project file, found by walking up from where the editor runs to the checkout
    /// that holds the solution, or nothing when the editor was not run from one.
    /// </summary>
    private static string? Sample()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "BevyCSharp.Sample", "BevyCSharp.Sample.csproj");

            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }
}

/// <summary>What <see cref="EditorPlay"/> is running.</summary>
public enum PlayJob
{
    /// <summary>Nothing has run yet.</summary>
    None,

    /// <summary>The game, in its own window.</summary>
    Playing,

    /// <summary>A build of the project.</summary>
    Building,
}
