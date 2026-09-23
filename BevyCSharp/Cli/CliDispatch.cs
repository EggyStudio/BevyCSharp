namespace Bevy;

/// <summary>
/// Turns one request into one envelope.
/// </summary>
/// <remarks>
/// The whole of the protocol's vocabulary is here, and it is deliberately four words long. Anything
/// a caller wants to do is a console command, discovered with <c>list</c> and run with <c>run</c>,
/// so the transport never has to learn a verb and a project that adds a command has added one to
/// the command line as well.
/// </remarks>
internal static class CliDispatch
{
    /// <summary>
    /// Answers a request, or nothing when the answer is to be held for a later frame.
    /// </summary>
    /// <param name="request">What was asked.</param>
    /// <param name="world">The world this frame lent.</param>
    /// <returns>The envelope, or <see langword="null"/> when the request was held.</returns>
    public static string? Answer(CliRequest request, World world) => request.Operation switch
    {
        "ping" => CliJson.Ok("ping", writer => writer.WriteString("state", "ready"), request.Id),
        "status" => Status(request, world),
        "list" => List(request),
        "run" => Run(request, world),
        _ => CliJson.Fail(
            request.Operation,
            "UNKNOWN_OPERATION",
            $"'{request.Operation}' is not something this app answers. "
            + "It answers ping, status, list and run.",
            request.Id),
    };

    /// <summary>Where the app is right now.</summary>
    private static string Status(CliRequest request, World world)
    {
        var time = world.Resource<Time>();
        var config = world.Resource<Config>();

        return CliJson.Ok("status", writer =>
        {
            writer.WriteNumber("pid", Environment.ProcessId);
            writer.WriteString("title", config.Title);
            writer.WriteBoolean("headless", config.Headless);
            writer.WriteNumber("frame", time.FrameCount);
            writer.WriteNumber("fps", Math.Round(time.SmoothedFps, 1));
            writer.WriteNumber("elapsed", Math.Round(time.ElapsedSeconds, 3));
            writer.WriteBoolean("renderer", App.HasRenderer);
            writer.WriteBoolean("editor", App.HasEditor);
            writer.WriteNumber("commands", ConsoleCommands.All.Count);
        }, request.Id);
    }

    /// <summary>
    /// Everything this app can be asked to do, with what each takes.
    /// </summary>
    /// <remarks>
    /// The parameter list is the point. A caller that has never seen this app can read the catalog
    /// and compose a call from it, which is what makes the command line usable without reading the
    /// source of whatever is running.
    /// </remarks>
    private static string List(CliRequest request) => CliJson.Ok("list", writer =>
    {
        writer.WritePropertyName("commands");
        writer.WriteStartArray();

        foreach (var command in ConsoleCommands.All)
        {
            writer.WriteStartObject();
            writer.WriteString("name", command.Name);
            writer.WriteString("help", command.Help);
            writer.WriteString("usage", command.Usage);

            writer.WritePropertyName("parameters");
            writer.WriteStartArray();

            foreach (var parameter in command.Parameters)
            {
                writer.WriteStartObject();
                writer.WriteString("name", parameter.Name);
                writer.WriteString("kind", parameter.Kind);
                writer.WriteBoolean("line", parameter.TakesLine);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }, request.Id);

    /// <summary>
    /// Runs one command line against the live world.
    /// </summary>
    /// <remarks>
    /// The name is looked up before the line is run, so a caller that misspelled a command is told
    /// so with a code rather than with a sentence it would have to read. Everything past that is
    /// the command's own business. What it answers is the payload, and whether that counts as a
    /// failure is what it said through <see cref="ConsoleHost.Fail"/>.
    /// </remarks>
    private static string? Run(CliRequest request, World world)
    {
        var line = (request.Line ?? string.Empty).Trim();

        if (line.Length == 0)
        {
            return CliJson.Fail(
                "command", "BAD_ARGUMENT", "No command was given to run.", request.Id);
        }

        var cut = line.IndexOf(' ');
        var name = cut < 0 ? line : line[..cut];

        if (ConsoleCommands.Find(name) is null)
        {
            return CliJson.Fail(
                "command",
                "UNKNOWN_COMMAND",
                $"'{name}' is not a command this app registers. Run 'bcs list' to see what is.",
                request.Id);
        }

        string? answer;

        using (ConsoleHost.Lend(world)) answer = ConsoleCommands.Run(line);

        var frame = world.Resource<Time>().FrameCount;

        if (ConsoleHost.Failure is { } failure)
        {
            return CliJson.Envelope(
                "command",
                success: false,
                data: writer => Payload(writer, name, answer, frame),
                errors: [failure],
                id: request.Id);
        }

        var envelope = CliJson.Ok(
            "command", writer => Payload(writer, name, answer, frame), request.Id);

        if (ConsoleHost.Held is not { } release) return envelope;

        // Answered now, handed back later. The command has already done whatever it does; what is
        // being waited for is the frames after it, which is what makes "act, settle, then look"
        // a single call rather than a poll. The envelope is built again on the way out, so the
        // frame it reports is the frame the caller is hearing about rather than the one it asked on.
        request.Holding = name;
        request.Held = answer;
        request.Release = release;
        return null;
    }

    /// <summary>The envelope for a held request, built on the frame that releases it.</summary>
    public static string Release(CliRequest request, ulong frame) => CliJson.Ok(
        "command",
        writer => Payload(writer, request.Holding ?? string.Empty, request.Held, frame),
        request.Id);

    /// <summary>What a run answers with: the command, what it said, and when.</summary>
    private static void Payload(
        System.Text.Json.Utf8JsonWriter writer, string name, string? answer, ulong frame)
    {
        writer.WriteString("command", name);

        if (answer is null) writer.WriteNull("result");
        else writer.WriteString("result", answer);

        writer.WriteNumber("frame", frame);
    }
}
