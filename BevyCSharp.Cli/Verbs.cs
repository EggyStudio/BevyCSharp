using System.Text.Json;
using Bevy;

namespace BevyCSharp.Cli;

/// <summary>The verbs that talk to a running app.</summary>
internal static class Verbs
{
    /// <summary>What is running.</summary>
    /// <remarks>
    /// Reads the session files rather than connecting, so it answers even when an app has stopped
    /// running frames — which is the state worth being told about. A session whose process is gone
    /// is reported rather than hidden, because a file left behind is the usual reason a port refuses
    /// a connection.
    /// </remarks>
    public static int Status(Options options)
    {
        var sessions = CliSessionFile.All();

        if (sessions.Count == 0)
        {
            return Output.Refuse(
                options,
                "status",
                "NO_SESSION",
                "No app is serving. Start one with 'bcs open --editor', or run an app with "
                + "--serve. If one is open and this still says no, a sandboxed shell may not be "
                + "able to see it.");
        }

        var envelope = CliJson.Ok("status", writer =>
        {
            writer.WritePropertyName("sessions");
            writer.WriteStartArray();

            foreach (var session in sessions) Sessions.Describe(writer, session);

            writer.WriteEndArray();
        });

        return Output.Print(options, envelope, data =>
        {
            foreach (var session in data.GetProperty("sessions").EnumerateArray())
            {
                Console.WriteLine(
                    $"{Text(session, "state"),-12} {session.GetProperty("pid").GetInt32(),-8} "
                    + $"{Text(session, "name"),-22} port {session.GetProperty("port").GetInt32(),-6} "
                    + $"frame {session.GetProperty("frame").GetUInt64(),-8} {Text(session, "title")}");
            }
        });
    }

    /// <summary>Everything the connected app can be asked to do.</summary>
    public static int List(Options options)
    {
        if (Sessions.Pick(options, "list", out var refusal) is not { } session)
        {
            return Output.Print(options, refusal);
        }

        return Output.Print(
            options,
            Client.Send(session, "list", seconds: options.Timeout),
            data =>
            {
                foreach (var command in data.GetProperty("commands").EnumerateArray())
                {
                    var name = Text(command, "name");
                    var usage = Text(command, "usage");
                    var help = Text(command, "help");

                    Console.WriteLine(
                        $"{(usage.Length > 0 ? $"{name} {usage}" : name),-44} {help}");
                }
            });
    }

    /// <summary>Runs one command against the connected app.</summary>
    public static int Command(Options options, string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return Output.Refuse(
                options,
                "command",
                "BAD_ARGUMENT",
                "Which command? Run 'bcs list' to see what this app offers.");
        }

        if (Sessions.Pick(options, "command", out var refusal) is not { } session)
        {
            return Output.Print(options, refusal);
        }

        return Output.Print(
            options, Client.Send(session, "run", Line(arguments), options.Timeout));
    }

    /// <summary>Captures the window, and waits for the file to appear.</summary>
    /// <remarks>
    /// The capture is read back off the GPU over the frames after the request, so asking for it and
    /// looking for the file in the same breath finds nothing. This waits for the file rather than
    /// for a number of frames, and says so when it never arrives.
    /// </remarks>
    public static int Shot(Options options, string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return Output.Refuse(
                options, "shot", "BAD_ARGUMENT", "Where should the picture go? bcs shot <path>");
        }

        if (Sessions.Pick(options, "shot", out var refusal) is not { } session)
        {
            return Output.Print(options, refusal);
        }

        var path = Path.GetFullPath(arguments[0]);
        var was = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;

        var answer = Client.Send(session, "run", $"shot \"{path}\"", options.Timeout);

        using (var document = JsonDocument.Parse(answer))
        {
            if (!document.RootElement.GetProperty("success").GetBoolean())
            {
                return Output.Print(options, answer);
            }
        }

        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(options.Timeout, 5));

        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path) && File.GetLastWriteTimeUtc(path) > was)
            {
                // Settled rather than merely present: the file appears when the write starts.
                var size = new FileInfo(path).Length;
                Thread.Sleep(120);

                if (new FileInfo(path).Length != size) continue;

                return Output.Print(
                    options,
                    CliJson.Ok("shot", writer =>
                    {
                        writer.WriteString("path", path);
                        writer.WriteNumber("bytes", size);
                    }),
                    data => Console.WriteLine(
                        $"captured {data.GetProperty("path").GetString()} "
                        + $"({data.GetProperty("bytes").GetInt64()} bytes)"));
            }

            Thread.Sleep(60);
        }

        return Output.Refuse(
            options,
            "shot",
            "NO_CAPTURE",
            $"The app accepted the capture but no file appeared at {path}. A headless run cannot "
            + "capture, and a window that is not being drawn will not either.");
    }

    /// <summary>Asks the connected app to close.</summary>
    public static int Stop(Options options)
    {
        if (Sessions.Pick(options, "stop", out var refusal) is not { } session)
        {
            return Output.Print(options, refusal);
        }

        var answer = Client.Send(session, "run", "app.quit", options.Timeout);

        // Waited for rather than assumed, because "asked to close" and "closed" are different
        // answers to "can I start another one".
        var deadline = DateTime.UtcNow.AddSeconds(10);

        while (DateTime.UtcNow < deadline && session.Running) Thread.Sleep(100);

        if (session.Running)
        {
            // By the id in the session file, never by name: killing every process that looks like
            // an editor takes other people's unsaved work with it.
            try
            {
                System.Diagnostics.Process.GetProcessById(session.Pid).Kill();
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException
                                              or SystemException)
            {
                return Output.Print(options, answer);
            }
        }

        CliSessionFile.Remove(session.Pid);

        return Output.Print(
            options,
            CliJson.Ok("stop", writer =>
            {
                writer.WriteNumber("pid", session.Pid);
                writer.WriteString("name", session.Name);
            }),
            data => Console.WriteLine(
                $"stopped {data.GetProperty("name").GetString()} "
                + $"({data.GetProperty("pid").GetInt32()})"));
    }

    /// <summary>The words as one command line, quoting the ones that need it.</summary>
    /// <remarks>
    /// The app splits on whitespace and honours double quotes, which is the whole of the syntax, so
    /// a word that carries a space is quoted here to survive the trip as one word. Quotes and
    /// backslashes inside it are escaped rather than dropped, because the word may well be a
    /// fragment of C# and a string literal in it is the reason it was written.
    /// </remarks>
    private static string Line(string[] words) => string.Join(
        " ",
        words.Select(word => word.Length > 0 && !word.Any(char.IsWhiteSpace)
            ? word
            : $"\"{word.Replace("\\", "\\\\").Replace("\"", "\\\"")}\""));

    /// <summary>A string property, or an empty one.</summary>
    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}
