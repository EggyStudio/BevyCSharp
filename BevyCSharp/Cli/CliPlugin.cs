using System.Reflection;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Serves this app to the command line, when it is asked to.
/// </summary>
/// <remarks>
/// <para>
/// Added to every app by <see cref="DefaultPlugins"/> and inert unless <see cref="Config.Serve"/>
/// is set or <c>BCS_SERVE</c> is in the environment, so an ordinary run opens no port and writes no
/// file. What it costs when it is on is one thread waiting on a socket and one system a frame that
/// usually finds an empty queue.
/// </para>
/// <para>
/// Nothing here is privileged, which is the point. The editor is a BevyCSharp app like any other,
/// so a game gets the same thing the editor does, and a tool written against one works against the
/// other.
/// </para>
/// </remarks>
public sealed class CliPlugin : IPlugin
{
    /// <summary>The environment variable that turns it on without touching the code.</summary>
    public const string Variable = "BCS_SERVE";

    /// <summary>How often the session file is touched, in frames.</summary>
    /// <remarks>
    /// About twice a second at sixty, which is ten times inside the patience a reader allows. Often
    /// enough that a busy frame does not read as a dead app, rare enough that it is not a write per
    /// frame.
    /// </remarks>
    private const ulong Beat = 30;

    private readonly CliQueue _queue = new();
    private CliServer? _server;

    /// <summary>True when the environment asks for a server regardless of the config.</summary>
    public static bool Asked =>
        Environment.GetEnvironmentVariable(Variable) is { Length: > 0 } value
        && value is not ("0" or "off" or "false" or "no");

    /// <inheritdoc/>
    public void Build(App app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Config.Serve && !Asked) return;

        _server = new CliServer(_queue);

        var session = Describe(app, "starting", frame: 0);
        CliSessionFile.Write(session);

        Console.WriteLine(
            $"[bcs] serving on 127.0.0.1:{_server.Port}. "
            + "Drive it with bcs status, bcs list, bcs command <name>.");

        // Announced once the first frame is in, because a caller waiting for "ready" is waiting to
        // be able to ask something, and nothing can be asked until there is a frame to answer in.
        app.AddSystem(Stage.Startup, new SystemDescriptor(
            world => CliSessionFile.Write(Describe(app, "ready", Frame(world))),
            "Cli.Ready"));

        // At the top of the frame, so a command sees the world as the frame's own systems will,
        // and anything it changed is in place before they run.
        app.AddSystem(Stage.First, new SystemDescriptor(world => Tick(app, world), "Cli.Serve"));

        app.AddSystem(Stage.Cleanup, new SystemDescriptor(_ => Close(), "Cli.Close"));

        // The loop can also end without reaching Cleanup, and a file left behind would advertise a
        // port nothing is listening on.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Close();
    }

    /// <summary>
    /// Answers what has been asked, and says the app is still here.
    /// </summary>
    /// <remarks>
    /// A heartbeat that cannot be written is swallowed, because the app is running whether or not
    /// its file says so, and an exception out of a system on a transient filesystem error would
    /// stop a session over bookkeeping. The first write, in <see cref="Build"/>, is not swallowed,
    /// because a directory that cannot be written to at all means nothing will ever find this app.
    /// </remarks>
    private void Tick(App app, World world)
    {
        _queue.Pump(world);

        var frame = Frame(world);
        if (frame % Beat != 0) return;

        try
        {
            CliSessionFile.Write(Describe(app, "ready", frame));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Tried again on the next beat.
        }
    }

    /// <summary>Stops serving and takes the session file back out.</summary>
    private void Close()
    {
        if (_server is null) return;

        _queue.Abandon();
        _server.Dispose();
        _server = null;

        CliSessionFile.Remove(Environment.ProcessId);
    }

    /// <summary>What this app tells the command line about itself.</summary>
    private CliSession Describe(App app, string state, ulong frame) => new(
        Pid: Environment.ProcessId,
        Port: _server?.Port ?? 0,
        Token: _server?.Token ?? string.Empty,
        Project: Environment.CurrentDirectory,
        Name: Assembly.GetEntryAssembly()?.GetName().Name ?? "app",
        Title: app.Config.Title,
        Started: Process.Started,
        Abi: Native.ExpectedAbiVersion,
        Renderer: App.HasRenderer,
        Editor: App.HasEditor,
        State: state,
        Frame: frame,
        Heartbeat: DateTimeOffset.UtcNow);

    /// <summary>The frame counter, or zero before the clock has been read.</summary>
    private static ulong Frame(World world) =>
        world.TryGetResource<Time>(out var time) ? time.FrameCount : 0;

    /// <summary>When this process started, asked once.</summary>
    private static class Process
    {
        /// <summary>The moment, fixed at first use so every file agrees on it.</summary>
        public static readonly DateTimeOffset Started = DateTimeOffset.UtcNow;
    }
}
