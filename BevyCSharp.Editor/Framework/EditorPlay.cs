using System.Diagnostics;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Plays the game in a window of its own, as Godot does: the project is built and run as a process
/// beside the editor, and stopped from the same button.
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
/// This is the first step of a larger plan, in <c>.github/PLAY.md</c>: a player that loads the scene
/// being edited rather than the game's own, a live view of the running game's world, and a tab that
/// builds a game to ship.
/// </para>
/// </remarks>
public static class EditorPlay
{
    private static Process? _game;

    /// <summary>The project file that is run, or empty for the sample's, found from where the editor runs.</summary>
    public static string Project { get; set; } = string.Empty;

    /// <summary>Whether the game is running.</summary>
    public static bool Running => _game is { HasExited: false };

    /// <summary>Starts the game, or stops it if it is running.</summary>
    public static void Toggle()
    {
        if (Running)
        {
            Stop();
            return;
        }

        var project = Project.Length > 0 ? Project : Sample();

        if (project is null || !File.Exists(project))
        {
            Console.WriteLine($"[play] there is no project to run at {project ?? "the sample's place"}; set one in the settings");
            return;
        }

        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        start.ArgumentList.Add("run");
        start.ArgumentList.Add("--project");
        start.ArgumentList.Add(project);
        start.ArgumentList.Add("--");
        start.ArgumentList.Add("--window");

        try
        {
            _game = Process.Start(start);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[play] could not start {Path.GetFileName(project)}: {exception.Message}");
            return;
        }

        if (_game is null) return;

        _game.OutputDataReceived += static (_, line) => Say(line.Data);
        _game.ErrorDataReceived += static (_, line) => Say(line.Data);
        _game.BeginOutputReadLine();
        _game.BeginErrorReadLine();

        // Stopped with the editor, since a game left running by an editor that closed is a window
        // nobody knows the origin of.
        AppDomain.CurrentDomain.ProcessExit += static (_, _) => Stop();

        Console.WriteLine($"[play] building and starting {Path.GetFileNameWithoutExtension(project)}");
    }

    /// <summary>Stops the game, with everything it started.</summary>
    public static void Stop()
    {
        if (_game is not { HasExited: false } game) return;

        try
        {
            // The whole tree, since dotnet run starts the game as a child of its own.
            game.Kill(entireProcessTree: true);
            Console.WriteLine("[play] stopped");
        }
        catch (InvalidOperationException)
        {
            // It ended on its own between asking and stopping.
        }
    }

    /// <summary>A line the game wrote, into the editor's console, marked as the game's.</summary>
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

        Console.WriteLine($"[game] {plain}");
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
