using System.Diagnostics;
using Bevy;

namespace BevyCSharp.Cli;

/// <summary>Starting an app that serves, and reading what it says.</summary>
internal static class Launch
{
    /// <summary>
    /// Starts an app with the server on, detached, and waits until it is answering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The piece that makes the loop startable without a person. It launches the app with its
    /// output redirected to a log beside the checkout, then watches the session directory until the
    /// app writes that it is ready, so when this returns the next command will be answered.
    /// </para>
    /// <para>
    /// Detached through the platform's own shell, which is the only portable way to hand a child a
    /// file for its output and then walk away from it. Nothing is piped back here, because a pipe
    /// nobody is reading stops the app as soon as it fills.
    /// </para>
    /// </remarks>
    public static int Open(Options options, string[] arguments)
    {
        if (Repo.Root is null)
        {
            return Output.Refuse(
                options,
                "open",
                "NO_CHECKOUT",
                "This is not inside a BevyCSharp checkout, so there is nothing to launch. Run an "
                + "app with --serve yourself, then use 'bcs status'.");
        }

        var project = Project(arguments);
        var extra = arguments.Where(argument => !argument.StartsWith("--project=", StringComparison.Ordinal)
                                                && argument is not ("--editor" or "--sample"))
            .ToList();

        // Always, because an app that is not serving is an app this tool cannot reach.
        if (!extra.Contains("--serve")) extra.Add("--serve");

        var already = Sessions.Live()
            .FirstOrDefault(session =>
                session.Name.Equals(project, StringComparison.OrdinalIgnoreCase) && !session.Stale);

        if (already is not null)
        {
            return Output.Print(
                options,
                CliJson.Ok("open", writer =>
                {
                    writer.WriteNumber("pid", already.Pid);
                    writer.WriteString("name", already.Name);
                    writer.WriteBoolean("started", false);
                }),
                data => Console.WriteLine(
                    $"{data.GetProperty("name").GetString()} is already serving "
                    + $"({data.GetProperty("pid").GetInt32()})"));
        }

        Directory.CreateDirectory(Repo.Logs);

        var log = Repo.LogFor(project);
        var started = DateTimeOffset.UtcNow;

        if (Start(project, extra, log) is { Length: > 0 } failure)
        {
            return Output.Refuse(options, "open", "LAUNCH_FAILED", failure);
        }

        var patience = TimeSpan.FromSeconds(Math.Max(options.Timeout, 90));
        var deadline = DateTime.UtcNow + patience;

        while (DateTime.UtcNow < deadline)
        {
            var session = Sessions.Live().FirstOrDefault(one =>
                one.Name.Equals(project, StringComparison.OrdinalIgnoreCase)
                && one.Started >= started.AddSeconds(-5)
                && one.State == "ready");

            if (session is not null)
            {
                return Output.Print(
                    options,
                    CliJson.Ok("open", writer =>
                    {
                        writer.WriteNumber("pid", session.Pid);
                        writer.WriteNumber("port", session.Port);
                        writer.WriteString("name", session.Name);
                        writer.WriteString("log", log);
                        writer.WriteBoolean("renderer", session.Renderer);
                        writer.WriteBoolean("started", true);
                    }),
                    data => Console.WriteLine(
                        $"{data.GetProperty("name").GetString()} is ready "
                        + $"({data.GetProperty("pid").GetInt32()}), log: "
                        + data.GetProperty("log").GetString()));
            }

            Thread.Sleep(150);
        }

        // The log is where the reason is. An app that will not start says so there and then exits,
        // and reporting only the timeout would send whoever asked looking in the wrong place.
        return Output.Print(
            options,
            CliJson.Envelope(
                "open",
                success: false,
                data: writer =>
                {
                    writer.WriteString("log", log);
                    writer.WriteString("tail", Tail(log, 15));
                },
                errors:
                [
                    new CliError(
                        "NOT_READY",
                        $"{project} did not start serving within {patience.TotalSeconds:0} "
                        + $"seconds. What it said is in {log}."),
                ]),
            data => Console.WriteLine(data.GetProperty("tail").GetString()));
    }

    /// <summary>Shows what a launched app has been saying.</summary>
    public static int Logs(Options options, string[] arguments)
    {
        var count = 40;
        var follow = false;

        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "-n" or "--lines" when index + 1 < arguments.Length
                                            && int.TryParse(arguments[index + 1], out var lines):
                    index++;
                    count = lines;
                    break;

                case "-f" or "--follow":
                    follow = true;
                    break;
            }
        }

        // The session says which log, and the name alone will do when nothing is running any more,
        // which is exactly when a log is worth reading.
        var name = Sessions.Pick(options, "logs", out _)?.Name ?? options.Name;

        if (name is null)
        {
            return Output.Refuse(
                options,
                "logs",
                "NO_SESSION",
                "Nothing is serving, so say which log to read: bcs logs --name BevyCSharp.Editor");
        }

        var log = Repo.LogFor(name);

        if (!File.Exists(log))
        {
            // Not every serving app was launched from here, and one that was not has no file. Its
            // own ring buffer holds the same lines.
            if (Sessions.Pick(options, "logs", out var refusal) is not { } session)
            {
                return Output.Print(options, refusal);
            }

            return Output.Print(
                options, Client.Send(session, "run", $"log.tail {count}", options.Timeout));
        }

        if (!follow)
        {
            return Output.Print(
                options,
                CliJson.Ok("logs", writer =>
                {
                    writer.WriteString("path", log);
                    writer.WriteString("lines", Tail(log, count));
                }),
                data => Console.WriteLine(data.GetProperty("lines").GetString()));
        }

        Console.WriteLine(Tail(log, count));

        using var reader = new StreamReader(
            new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));

        reader.ReadToEnd();

        while (true)
        {
            if (reader.ReadLine() is { } line) Console.WriteLine(line);
            else Thread.Sleep(200);
        }
    }

    /// <summary>Which project a launch is for.</summary>
    private static string Project(string[] arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument is "--sample") return "BevyCSharp.Sample";
            if (argument is "--editor") return "BevyCSharp.Editor";

            if (argument.StartsWith("--project=", StringComparison.Ordinal))
            {
                return argument["--project=".Length..];
            }
        }

        // The editor, because it is the one with something to look at and something to click.
        return "BevyCSharp.Editor";
    }

    /// <summary>
    /// Starts the app with its output in a file, and lets go of it.
    /// </summary>
    /// <returns>What went wrong, or an empty string.</returns>
    private static string Start(string project, IReadOnlyList<string> arguments, string log)
    {
        var binary = Repo.BinaryFor(project);
        var start = new ProcessStartInfo
        {
            WorkingDirectory = Repo.Root!,
            UseShellExecute = false,
        };

        if (binary is null)
        {
            return $"{project} has not been built yet. Run 'bcs build' first, or "
                   + $"'dotnet build {project}/{project}.csproj'.";
        }

        if (OperatingSystem.IsWindows())
        {
            start.FileName = "cmd.exe";
            start.ArgumentList.Add("/c");
            start.ArgumentList.Add(
                $"start \"{project}\" /b \"{binary}\" {string.Join(' ', arguments)} > \"{log}\" 2>&1");
        }
        else
        {
            // exec, so the shell is replaced by the app and the process the session file names is
            // the one that can be stopped.
            start.FileName = "/bin/sh";
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add(
                $"exec {Quote(binary)} {string.Join(' ', arguments.Select(Quote))} "
                + $"> {Quote(log)} 2>&1");
        }

        try
        {
            using var child = Process.Start(start);
            return child is null ? $"{project} did not start." : string.Empty;
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception
                                          or InvalidOperationException or IOException)
        {
            return $"{project} could not be started: {error.Message}";
        }
    }

    /// <summary>A word as one argument, whatever is in it.</summary>
    private static string Quote(string word) => $"'{word.Replace("'", "'\\''")}'";

    /// <summary>The last lines of a file, or a sentence saying there are none.</summary>
    private static string Tail(string path, int count)
    {
        try
        {
            using var stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            using var reader = new StreamReader(stream);

            var lines = new Queue<string>();

            while (reader.ReadLine() is { } line)
            {
                lines.Enqueue(line);
                if (lines.Count > count) lines.Dequeue();
            }

            return lines.Count == 0 ? "(nothing yet)" : string.Join("\n", lines);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return $"({path} could not be read: {error.Message})";
        }
    }
}
