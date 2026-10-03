using System.Diagnostics;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Plays the game in a window of its own, as Godot does, and builds it. The project is built and
/// run as a process beside the editor, driven from the Play tab.
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
/// Export turns the project into a folder a player is given (<see cref="Export"/>), as a chain of
/// processes run one after another, each writing to the tab as the game does. The rest of the plan
/// is in <c>.github/PLAY.md</c>: a player that loads the scene being edited rather than the game's
/// own, and a live view of the running game's world.
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

    /// <summary>What is left of an export after the process running now, in order.</summary>
    private static readonly Queue<Step> _steps = new();

    /// <summary>One process of a chain, and what to do once it has ended well.</summary>
    /// <param name="File">What to run.</param>
    /// <param name="Arguments">What to run it with.</param>
    /// <param name="Says">The line the tab is given as it starts.</param>
    /// <param name="After">What follows it on this side, such as copying a file into place, or nothing.</param>
    private sealed record Step(string File, string[] Arguments, string Says, Action? After = null);

    /// <summary>The project file that is run, or empty for the sample's, found from where the editor runs.</summary>
    public static string Project { get; set; } = string.Empty;

    /// <summary>When the game was last started, which tells its session from any that ran before.</summary>
    public static DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.MaxValue;

    /// <summary>What is running: the game, a build, or nothing.</summary>
    public static PlayJob Job { get; private set; }

    /// <summary>Whether the game is running.</summary>
    public static bool Running => Job == PlayJob.Playing && Busy;

    /// <summary>Whether the game, a build or an export is running.</summary>
    public static bool Busy => _job is { HasExited: false } || Pending;

    /// <summary>Whether a chain has steps still to run, between one process ending and the next starting.</summary>
    private static bool Pending
    {
        get
        {
            lock (_gate) return _steps.Count > 0;
        }
    }

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

    /// <summary>Where the scene being played is written, a file of the editor's own.</summary>
    /// <remarks>
    /// Under the player's directory rather than over the scene's own file, so playing never
    /// changes what is saved, and nothing the editor writes to play lands in the project.
    /// </remarks>
    public static string PlayedScene => UserData.Resolve("user://play/scene.scene.json");

    /// <summary>Plays the scene being edited, or stops what is playing.</summary>
    /// <remarks>
    /// <para>
    /// Godot's "Run Current Scene" beside its "Run Project". The scene is written as it is now,
    /// unsaved changes and all, to <see cref="PlayedScene"/>, and <c>BevyCSharp.Player</c> plays it
    /// in a window of its own against the editor's asset folder, compiling the same scripts the
    /// editor runs. A scene with no camera of its own is seen from where the editor's camera is.
    /// </para>
    /// <para>
    /// The player is found in the checkout the editor runs from, as the sample is, and run through
    /// <c>dotnet run</c>, so the first press builds it.
    /// </para>
    /// </remarks>
    /// <returns>Why it could not start, or nothing once it has.</returns>
    public static string? PlayScene()
    {
        if (Running)
        {
            Stop();
            return null;
        }

        if (Player() is not { } player) return "the player is found in the checkout the editor runs from, and there is none";

        var world = EditorShell.Ecs;
        var written = EditorScene.Save(world, PlayedScene);

        string[] arguments = ["run", "--project", player, "--", "--scene", PlayedScene, "--assets", EditorPaths.Assets];

        // The editor's view, as seven numbers, so a scene with no camera opens on what was being
        // looked at.
        if (EditorSelection.Camera is { IsNone: false } camera && world.TryGet<Transform>(camera, out var view))
        {
            var numbers = new[] { view.Translation.X, view.Translation.Y, view.Translation.Z, view.Rotation.X, view.Rotation.Y, view.Rotation.Z, view.Rotation.W };
            arguments = [.. arguments, "--view", string.Join(',', numbers.Select(n => n.ToString(System.Globalization.CultureInfo.InvariantCulture)))];
        }

        Run(PlayJob.Playing, [new Step("dotnet", arguments, $"[play] playing the scene, {written} entities, from {PlayedScene}")]);
        return null;
    }

    /// <summary>The runtime identifiers an export can be made for, this machine's first.</summary>
    public static IReadOnlyList<string> Targets { get; } = [.. new[] { Host() }.Concat(
        ["linux-x64", "linux-arm64", "win-x64", "win-arm64", "osx-x64", "osx-arm64"]).Distinct()];

    /// <summary>The folder an export for <paramref name="rid"/> is written to, or nothing without a project.</summary>
    /// <remarks>
    /// Under the project's own <c>bin</c>, which every project already keeps out of version control,
    /// so an export is never committed by accident and is found beside the builds it came from.
    /// </remarks>
    public static string? ExportFolder(string rid) =>
        Resolved is { } project ? Path.Combine(Path.GetDirectoryName(project)!, "bin", "Export", rid) : null;

    /// <summary>
    /// Turns the project into a folder a player is given, for one runtime identifier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>dotnet publish</c> in Release, self-contained, so the player needs no .NET of their own,
    /// into <see cref="ExportFolder"/>. The publish carries the bridge and the assets the project's
    /// build copies beside it, which in this checkout is the bridge last built here, often the
    /// editor's. So the bridge a game ships is a render one built for exports
    /// (<c>build-native.sh --render --game</c>), kept under <c>build/game/&lt;rid&gt;/</c> apart from
    /// the one the projects here use, built before the publish when the Rust sources are newer
    /// than it and put over the one the publish copied after.
    /// </para>
    /// <para>
    /// With <paramref name="embed"/>, the project's <c>assets</c> folder is compiled into the game's
    /// assembly by the same publish (<c>BevyCSharpEmbedAssets</c>), and the asset folder is left out
    /// of what ships, apart from the scripts and shaders the game compiles from their files. <see cref="AssetFiles"/> reads the managed side's files from the
    /// assembly and hands Bevy a reader over the rest, so the bridge is the same shared one an export
    /// that does not embed carries, and embedding costs no build of its own.
    /// </para>
    /// <para>
    /// Not trimmed, and not compiled ahead of time, since the script host the editor shares with a
    /// game compiles at runtime and has to be left out of a shipped game first.
    /// </para>
    /// </remarks>
    /// <param name="rid">The runtime identifier, such as <c>linux-x64</c>.</param>
    /// <param name="embed">Whether to compile the assets into the game's assembly.</param>
    /// <returns>Why it could not start, or nothing once it has.</returns>
    public static string? Export(string rid, bool embed)
    {
        Stop();

        var project = Resolved;
        if (project is null || !File.Exists(project)) return $"there is no project at {project ?? "the sample's place"}";
        if (!Targets.Contains(rid)) return $"{rid} is not a target an export is made for: {string.Join(", ", Targets)}";

        var folder = ExportFolder(rid)!;
        var name = Path.GetFileNameWithoutExtension(project);
        var steps = new List<Step>();

        if (embed && !Directory.Exists(Path.Combine(Path.GetDirectoryName(project)!, "assets")))
            return $"{name} has no assets folder to embed";

        if (Script() is { } script && Triple(rid) is { } triple && !Current(GameBridge(rid)))
        {
            steps.Add(script.EndsWith(".ps1", StringComparison.Ordinal)
                ? new Step("pwsh", ["-NoProfile", "-File", script, "-Render", "-Game", "-Target", triple], $"[play] building a render bridge for {rid}")
                : new Step("bash", [script, "--render", "--game", "--target", triple], $"[play] building a render bridge for {rid}"));
        }

        steps.Add(new Step(
            "dotnet",
            [
                "publish", project, "-c", "Release", "-r", rid, "--self-contained", "-o", folder,

                // The asset folder compiled into the game's assembly, where both sides read it
                // (BevyCSharp.Embed.targets).
                .. embed ? new[] { "-p:BevyCSharpEmbedAssets=true" } : [],
            ],
            $"[play] publishing {name} for {rid}",
            () =>
            {
                // Ids in one index rather than a sidecar beside every file, so the folder a player
                // gets holds what the game loads.
                var assets = Path.Combine(folder, "assets");
                if (Directory.Exists(assets) && AssetIds.IndexForShipping(assets) is > 0 and var ids)
                    Say($"[play] {ids} asset ids written to {AssetIds.IndexName} in place of their sidecars");

                Game(folder, rid);
                if (embed) Embedded(folder);
                Say($"[play] exported {name} to {folder}, {Megabytes(folder)}");
            }));

        Run(PlayJob.Exporting, steps);
        return null;
    }

    /// <summary>Cuts the published assets down to the scripts and shaders, which the game's assembly does not carry.</summary>
    private static void Embedded(string folder)
    {
        var assets = Path.Combine(folder, "assets");
        if (!Directory.Exists(assets)) return;

        var kept = 0;
        foreach (var file in Directory.GetFiles(assets, "*", SearchOption.AllDirectories))
        {
            if (ReadHere(file))
            {
                kept++;
                continue;
            }

            File.Delete(file);
        }

        // Folders the files left behind, deepest first, so a parent is empty by the time it is asked,
        // and the asset folder itself when nothing is left in it.
        foreach (var directory in Directory.GetDirectories(assets, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length).Append(assets))
        {
            if (!Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
        }

        Say(kept == 0
            ? "[play] the game's assembly carries the assets, so the export has no asset folder"
            : $"[play] the game's assembly carries the assets, and {kept} scripts and shaders stay beside them");
    }

    /// <summary>The render bridge built for exports to <paramref name="rid"/>, or nothing outside a checkout.</summary>
    private static string? GameBridge(string rid) =>
        Script() is { } script ? Path.Combine(Path.GetDirectoryName(script)!, "game", rid) : null;

    /// <summary>
    /// Whether the bridge in a folder is newer than every Rust source, so building it again would
    /// make the same library.
    /// </summary>
    private static bool Current(string? folder)
    {
        if (folder is null || !Directory.Exists(folder)) return false;
        if (Directory.GetFiles(folder).FirstOrDefault() is not { } library) return false;

        var sources = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Script()!)!)!, "native", "bevy_csharp");
        if (!Directory.Exists(sources)) return true;

        var newest = Directory.GetFiles(sources, "*.rs", SearchOption.AllDirectories)
            .Append(Path.Combine(sources, "Cargo.toml"))
            .Where(File.Exists)
            .Max(File.GetLastWriteTimeUtc);

        return File.GetLastWriteTimeUtc(library) >= newest;
    }

    /// <summary>Puts the render bridge built for exports over the one the publish copied, where there is one.</summary>
    private static void Game(string folder, string rid)
    {
        if (GameBridge(rid) is not { } built || !Directory.Exists(built) || Directory.GetFiles(built).FirstOrDefault() is not { } library)
        {
            Say($"[play] no render bridge for {rid} was built here, so the export carries the one the project builds with");
            return;
        }

        File.Copy(library, Path.Combine(folder, Path.GetFileName(library)), overwrite: true);
        Say($"[play] the export carries the render bridge from {built}");
    }

    /// <summary>
    /// Whether a file in the assets is one embedding leaves on disk, which is a script or a shader
    /// the game compiles from its file as it runs, or a tool's file under a folder named with a dot.
    /// </summary>
    private static bool ReadHere(string path) =>
        path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".slang", StringComparison.OrdinalIgnoreCase)
        || path.Replace('\\', '/').Split('/').Any(part => part.Length > 1 && part[0] == '.');

    /// <summary>How large a folder is, in the units a person reads.</summary>
    private static string Megabytes(string folder)
    {
        var bytes = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length)
            : 0L;

        return $"{bytes / (1024.0 * 1024.0):0.#} MB";
    }

    /// <summary>This machine's runtime identifier, in the portable form an export takes.</summary>
    private static string Host()
    {
        var os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
            _ => "x64",
        };

        return $"{os}-{arch}";
    }

    /// <summary>The Rust target a runtime identifier is built with, as the build script maps them.</summary>
    private static string? Triple(string rid) => rid switch
    {
        "linux-x64" => "x86_64-unknown-linux-gnu",
        "linux-arm64" => "aarch64-unknown-linux-gnu",
        "win-x64" => "x86_64-pc-windows-msvc",
        "win-arm64" => "aarch64-pc-windows-msvc",
        "osx-x64" => "x86_64-apple-darwin",
        "osx-arm64" => "aarch64-apple-darwin",
        _ => null,
    };

    /// <summary>The checkout's native build script for this platform, or nothing outside a checkout.</summary>
    private static string? Script()
    {
        var name = OperatingSystem.IsWindows() ? "build-native.ps1" : "build-native.sh";

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "build", name);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

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

        var name = Path.GetFileNameWithoutExtension(project);
        var says = job == PlayJob.Playing ? $"[play] building and starting {name}" : $"[play] building {name}";

        Run(job, [new Step("dotnet", [.. arguments.Select(argument => argument == "{project}" ? project : argument)], says)]);
    }

    /// <summary>Runs a chain of processes, each once the one before it has ended well.</summary>
    private static void Run(PlayJob job, IEnumerable<Step> steps)
    {
        lock (_gate)
        {
            _steps.Clear();
            foreach (var step in steps) _steps.Enqueue(step);
        }

        Job = job;
        Next();
    }

    /// <summary>Starts the next step of the chain, if there is one.</summary>
    private static void Next()
    {
        Step? step;
        lock (_gate)
        {
            if (!_steps.TryDequeue(out step)) return;
        }

        var start = new ProcessStartInfo(step.File)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in step.Arguments) start.ArgumentList.Add(argument);

        // A game answers the bcs socket, so the Play tab can show its world while it runs. Set in
        // the environment rather than as an argument, since every app built on the library reads
        // it and a project's own Main need not know.
        if (Job == PlayJob.Playing)
        {
            start.Environment["BCS_SERVE"] = "1";
            StartedAt = DateTimeOffset.Now.AddSeconds(-1);
        }

        Process? process;
        try
        {
            process = Process.Start(start);
        }
        catch (Exception exception)
        {
            Say($"[play] could not start {step.File}: {exception.Message}");
            lock (_gate) _steps.Clear();
            return;
        }

        if (process is null) return;

        _job = process;
        Say(step.Says);

        process.OutputDataReceived += static (_, line) => Say(line.Data);
        process.ErrorDataReceived += static (_, line) => Say(line.Data);
        process.EnableRaisingEvents = true;
        process.Exited += (sender, _) =>
        {
            // Not for one stopped from here, whose code is only the signal that ended it.
            if (sender is not Process ended || ended != _job || ended == _stopped) return;

            if (ended.ExitCode != 0)
            {
                Say($"[play] finished with exit code {ended.ExitCode}");
                lock (_gate) _steps.Clear();
                return;
            }

            try
            {
                step.After?.Invoke();
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Say($"[play] {exception.Message}");
                lock (_gate) _steps.Clear();
                return;
            }

            if (!Pending)
            {
                if (step.After is null) Say("[play] finished with exit code 0");
                return;
            }

            Next();
        };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Stopped with the editor, since a game left running by an editor that closed is a window
        // nobody knows the origin of. Hooked once, because it reads whichever process is current.
        if (!_stopsWithEditor)
        {
            AppDomain.CurrentDomain.ProcessExit += static (_, _) => Stop();
            _stopsWithEditor = true;
        }
    }

    /// <summary>Stops the game or the build, with everything it started.</summary>
    public static void Stop()
    {
        lock (_gate) _steps.Clear();

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

    /// <summary>A line the project or its build wrote, kept for the tab and passed to the editor's console marked as whose it is.</summary>
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

        // Marked by what wrote it, so a compiler's line in the console is not taken for the game's.
        var mark = Job == PlayJob.Playing ? "[game]" : "[build]";
        Console.WriteLine(text.StartsWith("[play]", StringComparison.Ordinal) ? text : $"{mark} {text}");
    }

    /// <summary>
    /// The sample's project file, found by walking up from where the editor runs to the checkout
    /// that holds the solution, or nothing when the editor was not run from one.
    /// </summary>
    private static string? Sample() => Checkout("BevyCSharp.Sample", "BevyCSharp.Sample.csproj");

    /// <summary>The player's project file, found the way the sample's is.</summary>
    private static string? Player() => Checkout("BevyCSharp.Player", "BevyCSharp.Player.csproj");

    /// <summary>A file in the checkout the editor runs from, or nothing outside one.</summary>
    private static string? Checkout(string folder, string file)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, folder, file);

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

    /// <summary>An export, which is a publish and sometimes a bridge built before it.</summary>
    Exporting,
}
