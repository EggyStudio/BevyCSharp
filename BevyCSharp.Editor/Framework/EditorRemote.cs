using System.Collections.Concurrent;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>One entity of the running game, as its <c>entity.list</c> line gives it.</summary>
/// <param name="Id">How the game's commands name it, <c>#</c> and its index.</param>
/// <param name="Name">What it is called.</param>
/// <param name="Components">What it carries, by name.</param>
internal sealed record RemoteEntity(string Id, string Name, string Components);

/// <summary>One field of the running game's picked entity, as its <c>entity.get</c> line gives it.</summary>
/// <param name="Field">The component and the field, joined by a dot, as <c>entity.set</c> takes them.</param>
/// <param name="Value">What it holds, as text.</param>
internal sealed record RemoteField(string Field, string Value);

/// <summary>
/// The world of the game the Play tab started, read while it runs, as Godot's remote scene tree
/// shows the running game beside the edited one.
/// </summary>
/// <remarks>
/// <para>
/// A game started from the Play tab is started with <c>BCS_SERVE</c> set, so it answers the
/// <c>bcs</c> socket, and this is a client of it like the tool is. It finds the game by the session
/// file it writes, the one that started after Play was pressed and is not the editor's own, and
/// asks <c>entity.list</c> a few times a second, and <c>entity.get</c> for the entity picked. A
/// field changed here is an <c>entity.set</c>, which lasts until the game stops and is not recorded
/// in the editor's history, since the edited scene is not what changed.
/// </para>
/// <para>
/// The asking is done on a thread of its own, because each question is a round trip to another
/// process and the interface draws sixty times a second. The panel draws whatever was answered
/// last, and what it asks for goes into a queue the thread empties.
/// </para>
/// </remarks>
internal static class EditorRemote
{
    /// <summary>How often the game is asked what it holds.</summary>
    private static readonly TimeSpan Every = TimeSpan.FromMilliseconds(400);

    private static readonly ConcurrentQueue<string> Asked = new();
    private static readonly Lock Gate = new();

    private static Thread? _thread;
    private static CliSession? _session;
    private static IReadOnlyList<RemoteEntity> _entities = [];
    private static IReadOnlyList<RemoteField> _fields = [];
    private static string? _picked;
    private static bool _paused;
    private static string? _problem;

    /// <summary>The game's session once it has answered, or nothing while it starts.</summary>
    internal static CliSession? Session
    {
        get
        {
            lock (Gate) return _session;
        }
    }

    /// <summary>The game's named entities, as last answered.</summary>
    internal static IReadOnlyList<RemoteEntity> Entities
    {
        get
        {
            lock (Gate) return _entities;
        }
    }

    /// <summary>The picked entity's fields, as last answered.</summary>
    internal static IReadOnlyList<RemoteField> Fields
    {
        get
        {
            lock (Gate) return _fields;
        }
    }

    /// <summary>Which of the game's entities is picked, by its id, or nothing.</summary>
    internal static string? Picked
    {
        get
        {
            lock (Gate) return _picked;
        }
    }

    /// <summary>Whether the game's clock was stopped from here.</summary>
    internal static bool Paused
    {
        get
        {
            lock (Gate) return _paused;
        }
    }

    /// <summary>Why the last question went unanswered, or nothing.</summary>
    internal static string? Problem
    {
        get
        {
            lock (Gate) return _problem;
        }
    }

    /// <summary>Starts asking, if a game is playing and nothing is asking yet.</summary>
    /// <remarks>Called each frame the Play tab is drawn. The thread ends by itself when the game does.</remarks>
    internal static void Watch()
    {
        if (!EditorPlay.Running || _thread is { IsAlive: true }) return;

        lock (Gate)
        {
            _session = null;
            _entities = [];
            _fields = [];
            _picked = null;
            _paused = false;
            _problem = null;
        }

        _thread = new Thread(Loop) { IsBackground = true, Name = "Remote world" };
        _thread.Start();
    }

    /// <summary>Picks one of the game's entities, whose fields are asked for from then on.</summary>
    internal static void Pick(string? id)
    {
        lock (Gate)
        {
            _picked = id;
            _fields = [];
        }
    }

    /// <summary>Sets a field of the picked entity in the running game.</summary>
    internal static void Set(string field, string value)
    {
        if (Picked is { } id) Asked.Enqueue($"entity.set {id} {field} {Quoted(value)}");
    }

    /// <summary>Stops the game's clock, or starts it again.</summary>
    internal static void TogglePause()
    {
        var pausing = !Paused;
        lock (Gate) _paused = pausing;
        Asked.Enqueue(pausing ? "app.pause on" : "app.pause off");
    }

    /// <summary>Runs a stopped game's clock for one frame.</summary>
    internal static void Step() => Asked.Enqueue("app.step 1");

    /// <summary>Asks until the game stops, finding it first.</summary>
    private static void Loop()
    {
        while (EditorPlay.Running)
        {
            if (Session is not { } session)
            {
                if (Find() is { } found)
                {
                    lock (Gate) _session = found;
                }

                Thread.Sleep(Every);
                continue;
            }

            while (Asked.TryDequeue(out var line)) Ask(session, line);

            if (Ask(session, "entity.list") is { } list)
            {
                lock (Gate) _entities = ReadEntities(list);
            }

            if (Picked is { } picked && Ask(session, $"entity.get {picked}") is { } fields)
            {
                lock (Gate)
                {
                    if (_picked == picked) _fields = ReadFields(fields);
                }
            }

            Thread.Sleep(Every);
        }
    }

    /// <summary>The game's session, the one started after Play that is not this editor.</summary>
    private static CliSession? Find()
    {
        var since = EditorPlay.StartedAt;
        var self = Environment.ProcessId;

        return CliSessionFile.All()
            .Where(session => session.Pid != self && session.Started >= since && session.State == "ready" && !session.Stale)
            .OrderByDescending(session => session.Started)
            .FirstOrDefault();
    }

    /// <summary>Runs one command in the game, keeping why it failed when it did.</summary>
    private static string? Ask(CliSession session, string line)
    {
        var answer = CliClient.Run(session, line, seconds: 2);

        lock (Gate) _problem = answer.Success ? null : answer.Error ?? $"{line} did not answer";

        return answer.Success ? answer.Result : null;
    }

    /// <summary>The named entities from an <c>entity.list</c> answer, a line each.</summary>
    /// <remarks>A line is <c>#12 Name [A, B]</c>, and the last line counts the unnamed, which are left out.</remarks>
    internal static IReadOnlyList<RemoteEntity> ReadEntities(string text)
    {
        var found = new List<RemoteEntity>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!line.StartsWith('#')) continue;

            var space = line.IndexOf(' ');
            var open = line.LastIndexOf(" [", StringComparison.Ordinal);
            if (space < 0 || open < space || !line.EndsWith(']')) continue;

            found.Add(new RemoteEntity(line[..space], line[(space + 1)..open], line[(open + 2)..^1]));
        }

        return found;
    }

    /// <summary>The fields from an <c>entity.get</c> answer, a line each after the first.</summary>
    /// <remarks>A line is <c>Component.Field = value</c>, and one without a value names a component with no fields.</remarks>
    internal static IReadOnlyList<RemoteField> ReadFields(string text)
    {
        var found = new List<RemoteField>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            var equals = line.IndexOf(" = ", StringComparison.Ordinal);
            if (equals > 0) found.Add(new RemoteField(line[..equals], line[(equals + 3)..]));
        }

        return found;
    }

    /// <summary>A value as one argument of a command line, quoted when it holds a space.</summary>
    private static string Quoted(string value) =>
        value.Contains(' ', StringComparison.Ordinal) ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"" : value;
}
