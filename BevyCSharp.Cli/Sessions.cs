using Bevy;

namespace BevyCSharp.Cli;

/// <summary>Which running app a command is for.</summary>
internal static class Sessions
{
    /// <summary>The ones that are still there, whether or not they are answering.</summary>
    public static IReadOnlyList<CliSession> Live() =>
        [.. CliSessionFile.All().Where(session => session.Running)];

    /// <summary>
    /// Picks the one session a command is for, or explains why it cannot.
    /// </summary>
    /// <remarks>
    /// Two sessions and no way to tell them apart is an error rather than a guess. Picking one
    /// would be right half the time, and the other half is an agent driving the wrong app for as
    /// long as it takes to notice.
    /// </remarks>
    /// <param name="options">The filters the caller gave.</param>
    /// <param name="command">Which verb is asking, for the envelope.</param>
    /// <param name="refusal">The envelope to print when nothing was picked.</param>
    /// <returns>The session, or <see langword="null"/>.</returns>
    public static CliSession? Pick(Options options, string command, out string refusal)
    {
        refusal = string.Empty;

        var candidates = Live();

        if (options.Pid is { } pid)
        {
            candidates = [.. candidates.Where(session => session.Pid == pid)];
        }

        if (options.Name is { Length: > 0 } name)
        {
            candidates =
            [
                .. candidates.Where(session =>
                    session.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                    || session.Name.Contains(name, StringComparison.OrdinalIgnoreCase)),
            ];
        }

        if (options.Project is { Length: > 0 } project)
        {
            candidates =
            [
                .. candidates.Where(session =>
                    Same(session.Project, project)
                    || session.Project.StartsWith(project + Path.DirectorySeparatorChar,
                        StringComparison.Ordinal)),
            ];
        }

        // A stale one is only offered when it is the only one, so a session that is busy compiling
        // is still reachable while a livelier one is preferred.
        if (candidates.Any(session => !session.Stale))
        {
            candidates = [.. candidates.Where(session => !session.Stale)];
        }

        switch (candidates.Count)
        {
            case 1:
                return candidates[0];

            case 0:
                refusal = CliJson.Fail(
                    command,
                    "NO_SESSION",
                    Empty(options),
                    id: null);

                return null;

            default:
                refusal = CliJson.Envelope(
                    command,
                    success: false,
                    data: writer =>
                    {
                        writer.WritePropertyName("candidates");
                        writer.WriteStartArray();

                        foreach (var session in candidates) Describe(writer, session);

                        writer.WriteEndArray();
                    },
                    errors:
                    [
                        new CliError(
                            "AMBIGUOUS_SESSION",
                            $"{candidates.Count} apps are serving: "
                            + string.Join(", ", candidates.Select(one => $"{one.Name} ({one.Pid})"))
                            + ". Say which with --name <entry>, --session <pid> or --project <path>."),
                    ]);

                return null;
        }
    }

    /// <summary>Writes one session as the object every listing uses.</summary>
    public static void Describe(System.Text.Json.Utf8JsonWriter writer, CliSession session)
    {
        writer.WriteStartObject();
        writer.WriteNumber("pid", session.Pid);
        writer.WriteNumber("port", session.Port);
        writer.WriteString("name", session.Name);
        writer.WriteString("title", session.Title);
        writer.WriteString("project", session.Project);
        writer.WriteString("state", session.Report);
        writer.WriteNumber("frame", session.Frame);
        writer.WriteBoolean("renderer", session.Renderer);
        writer.WriteBoolean("editor", session.Editor);
        writer.WriteNumber("abi", session.Abi);
        writer.WriteString("started", session.Started.ToString("O"));
        writer.WriteEndObject();
    }

    /// <summary>What to say when nothing is running, given what was asked for.</summary>
    private static string Empty(Options options)
    {
        var filtered = options.Pid is not null
                       || options.Name is { Length: > 0 }
                       || options.Project is { Length: > 0 };

        return filtered
            ? "No serving app matches that. Run 'bcs status' to see what is running."
            : "No app is serving. Start one with 'bcs open --editor', or run an app with "
              + "--serve. If one is open and this still says no, a sandboxed shell may not be "
              + "able to see it.";
    }

    /// <summary>Whether two paths name the same directory, as this platform judges it.</summary>
    private static bool Same(string left, string right) => string.Equals(
        Path.TrimEndingDirectorySeparator(left),
        Path.TrimEndingDirectorySeparator(right),
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
}
