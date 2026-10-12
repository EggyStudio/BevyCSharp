using System.Diagnostics;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Where sessions are written down, and how they are read back.
/// </summary>
/// <remarks>
/// One directory per user rather than one per project, because the question a tool asks is "what is
/// running", and answering it should not require knowing where to look first.
/// </remarks>
public static class CliSessionFile
{
    /// <summary>
    /// The directory the files live in, created on first use.
    /// </summary>
    /// <remarks>
    /// One per user rather than one per project, and overridable with <c>BCS_SESSIONS</c> so that a
    /// test, or a second checkout being driven at the same time, can have a directory of its own
    /// instead of writing into the one a person's own apps are using.
    /// </remarks>
    public static string Directory { get; set; } =
        Environment.GetEnvironmentVariable("BCS_SESSIONS") is { Length: > 0 } named
            ? named
            : Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData,
                    Environment.SpecialFolderOption.DoNotVerify),
                "BevyCSharp",
                "sessions");

    /// <summary>Where the file for a given process is.</summary>
    public static string PathFor(int pid) => Path.Combine(Directory, $"{pid}.json");

    /// <summary>Writes one, replacing whatever was there.</summary>
    /// <remarks>
    /// Written whole and moved into place, so a reader never catches a half-written file. The mode
    /// is set on the temporary file before the move, so it is never briefly readable by others.
    /// </remarks>
    public static void Write(CliSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        System.IO.Directory.CreateDirectory(Directory);

        var final = PathFor(session.Pid);
        var temporary = final + ".tmp";

        using (var stream = File.Create(temporary))
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("pid", session.Pid);
            writer.WriteNumber("port", session.Port);
            writer.WriteString("token", session.Token);
            writer.WriteString("project", session.Project);
            writer.WriteString("name", session.Name);
            writer.WriteString("title", session.Title);
            writer.WriteString("started", session.Started.ToString("O"));
            writer.WriteNumber("abi", session.Abi);
            writer.WriteBoolean("renderer", session.Renderer);
            writer.WriteBoolean("editor", session.Editor);
            writer.WriteString("state", session.State);
            writer.WriteNumber("frame", session.Frame);
            writer.WriteString("heartbeat", session.Heartbeat.ToString("O"));
            writer.WriteEndObject();
        }

        Restrict(temporary);
        File.Move(temporary, final, overwrite: true);
    }

    /// <summary>Reads one, or nothing when the file is missing or not one of these.</summary>
    /// <remarks>
    /// A file being written as it is read, or left behind by an older version, answers nothing
    /// rather than throwing. Discovery runs on every command, and a tool that cannot list sessions
    /// because one of them is malformed is a tool that cannot be used to fix it.
    /// </remarks>
    public static CliSession? Read(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllBytes(path));
            var root = document.RootElement;

            return new CliSession(
                Pid: root.GetProperty("pid").GetInt32(),
                Port: root.GetProperty("port").GetInt32(),
                Token: Text(root, "token"),
                Project: Text(root, "project"),
                Name: Text(root, "name"),
                Title: Text(root, "title"),
                Started: Moment(root, "started"),
                Abi: root.TryGetProperty("abi", out var abi) ? abi.GetInt32() : 0,
                Renderer: Flag(root, "renderer"),
                Editor: Flag(root, "editor"),
                State: Text(root, "state"),
                Frame: root.TryGetProperty("frame", out var frame) ? frame.GetUInt64() : 0,
                Heartbeat: Moment(root, "heartbeat"));
        }
        catch (Exception error) when (error is IOException
                                          or JsonException
                                          or KeyNotFoundException
                                          or InvalidOperationException
                                          or FormatException
                                          or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Every session written down, newest first, whether or not it is still alive.</summary>
    public static IReadOnlyList<CliSession> All()
    {
        if (!System.IO.Directory.Exists(Directory)) return [];

        var found = new List<CliSession>();

        // Listed twice and the two joined, since a listing taken while a session moves its
        // heartbeat into place can leave that session out on some filesystems, btrfs among them,
        // and a command would then say nothing is serving about one in a hundred times. The move
        // is over in less time than a listing takes, so the second one sees it.
        var paths = new SortedSet<string>(System.IO.Directory.GetFiles(Directory, "*.json"), StringComparer.Ordinal);
        paths.UnionWith(System.IO.Directory.GetFiles(Directory, "*.json"));

        foreach (var path in paths)
        {
            if (Read(path) is { } session) found.Add(session);
        }

        found.Sort((left, right) => right.Started.CompareTo(left.Started));
        return found;
    }

    /// <summary>Takes one back out, on the way down.</summary>
    public static void Remove(int pid)
    {
        try
        {
            File.Delete(PathFor(pid));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Nothing useful left to do. The app is closing, and a file it could not delete is a
            // file the next `bcs status` reports as gone anyway.
        }
    }

    /// <summary>Deletes the files of sessions whose processes are no longer there.</summary>
    /// <returns>How many were swept up.</returns>
    public static int Prune()
    {
        var swept = 0;

        foreach (var session in All())
        {
            if (session.Running) continue;

            Remove(session.Pid);
            swept++;
        }

        return swept;
    }

    /// <summary>Makes a file readable by its owner and nobody else, where that means anything.</summary>
    private static void Restrict(string path)
    {
        if (OperatingSystem.IsWindows()) return;

        try
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // A filesystem that has no opinion about modes. The token still applies.
        }
    }

    /// <summary>A string property, or an empty one.</summary>
    private static string Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) ? value.GetString() ?? string.Empty : string.Empty;

    /// <summary>A boolean property, or false.</summary>
    private static bool Flag(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>A moment, or the beginning of time, which reads as stale.</summary>
    private static DateTimeOffset Moment(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value)
        && DateTimeOffset.TryParse(value.GetString(), out var moment)
            ? moment
            : DateTimeOffset.MinValue;
}
