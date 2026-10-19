using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Lets a running app be asked things from a terminal.
/// </summary>
/// <remarks>
/// <para>
/// One line of JSON in, exactly one line of JSON out, over a socket bound to the loopback interface
/// and nothing else. A connection stays open for as many requests as the caller makes, because the
/// point of serving a running app at all is that the second question costs nothing. The app is
/// already up, the assets are already loaded, and the answer comes back on the next frame.
/// </para>
/// <para>
/// The socket thread never touches the world. It parses, checks the token, queues, and waits. Every
/// answer is produced by <see cref="CliQueue.Pump"/> inside a system, which is the only place
/// anything in this engine may look at an entity.
/// </para>
/// <para>
/// Bound to <see cref="IPAddress.Loopback"/> on a port the kernel picks, so nothing reaches it from
/// off the machine and no two apps fight over a number. Where it landed is written to the session
/// file, which is how the command line finds it.
/// </para>
/// </remarks>
internal sealed class CliServer : IDisposable
{
    private readonly CliQueue _queue;
    private readonly TcpListener _listener;
    private readonly Thread _accepting;
    private readonly CancellationTokenSource _stop = new();
    private readonly Lock _gate = new();
    private readonly Dictionary<TcpClient, Thread> _open = [];
    private volatile bool _stopping;

    /// <summary>Starts listening, on a port chosen by the kernel.</summary>
    public CliServer(CliQueue queue)
    {
        _queue = queue;

        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();

        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

        _accepting = new Thread(Accept)
        {
            IsBackground = true,
            Name = "bcs-cli-accept",
        };

        _accepting.Start();
    }

    /// <summary>Where it is listening.</summary>
    public int Port { get; }

    /// <summary>What a request has to carry to be answered.</summary>
    public string Token { get; }

    /// <summary>
    /// How long a request may sit in the queue before the caller is told it timed out, where the
    /// request does not say how long its caller will wait.
    /// </summary>
    /// <remarks>
    /// A backstop rather than a policy. A frame answers in milliseconds, so reaching this means the
    /// app stopped running frames, and a caller learning that in thirty seconds is better than one
    /// waiting on a socket that will never answer.
    /// </remarks>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>The longest a caller can ask a request to be waited on, with <c>"wait"</c> in seconds.</summary>
    /// <remarks>
    /// A caller asks for longer than <see cref="Patience"/> when what it runs takes many frames of
    /// an app that draws slowly, as <c>frame.profile</c> over a stress test does, and the bound
    /// keeps a mistyped number from holding a connection open for good.
    /// </remarks>
    public static readonly TimeSpan LongestWait = TimeSpan.FromHours(1);

    /// <summary>How long the accepting thread waits for a call before it looks whether to stop.</summary>
    private const int PollMicroseconds = 250_000;

    /// <summary>Takes calls until the listener is closed.</summary>
    /// <remarks>
    /// A call is waited for by polling the listener, which pins nothing, and accepted only once one
    /// is waiting. A blocking accept keeps the buffer it writes the caller's address into pinned for
    /// as long as it waits, a new one after each call, so a young object sat pinned in the newest
    /// generation whenever a collection ran and held the space in front of it, which left the heap
    /// a full collection compacts megabytes larger from one reading to the next with nothing
    /// growing, as a soak of the stress program showed.
    /// </remarks>
    private void Accept()
    {
        while (!_stopping)
        {
            TcpClient caller;

            try
            {
                if (!_listener.Server.Poll(PollMicroseconds, SelectMode.SelectRead)) continue;
                caller = _listener.AcceptTcpClient();
            }
            catch (Exception error) when (error is SocketException or ObjectDisposedException
                                              or InvalidOperationException)
            {
                return;
            }

            // Kept while it is open, so stopping can close it, since its thread otherwise waits on
            // its read for as long as the caller keeps the connection, after the app is gone. It
            // leaves the set as it ends, so the set holds the connections open now and does not
            // grow by one per command.
            var serving = new Thread(() => Serve(caller))
            {
                IsBackground = true,
                Name = "bcs-cli-connection",
            };

            lock (_gate)
            {
                if (_stopping)
                {
                    caller.Dispose();
                    return;
                }

                _open[caller] = serving;
            }

            serving.Start();
        }
    }

    /// <summary>Answers one caller for as long as it keeps asking.</summary>
    private void Serve(TcpClient caller)
    {
        using (caller)
        {
            try
            {
                caller.NoDelay = true;

                using var stream = caller.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                using var writer = new StreamWriter(stream, new UTF8Encoding(false))
                {
                    AutoFlush = true,
                    NewLine = "\n",
                };

                while (!_stopping && reader.ReadLine() is { } line)
                {
                    if (line.Trim().Length == 0) continue;

                    writer.WriteLine(Handle(line));
                }
            }
            catch (Exception error) when (error is IOException or SocketException
                                              or ObjectDisposedException)
            {
                // The caller hung up, which is an ordinary way for a request to end, or the server
                // closed the connection as it stopped.
            }
            finally
            {
                lock (_gate) _open.Remove(caller);
            }
        }
    }

    /// <summary>
    /// Reads one request, queues it, and waits for the frame to answer it.
    /// </summary>
    /// <remarks>
    /// Everything that can be wrong with a request is answered with an envelope rather than by
    /// dropping the connection, so a caller that sent nonsense learns what was wrong with it and
    /// can send the next line.
    /// </remarks>
    private string Handle(string line)
    {
        string operation;
        string? command;
        string? id;
        string? token;
        var patience = Patience;

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            operation = root.TryGetProperty("op", out var op) ? op.GetString() ?? "run" : "run";
            command = root.TryGetProperty("line", out var body) ? body.GetString() : null;
            id = root.TryGetProperty("id", out var given) ? given.GetString() : null;
            token = root.TryGetProperty("token", out var carried) ? carried.GetString() : null;

            // As long as the caller is willing to wait, up to an hour, so a command that runs for
            // many frames of a slow app is answered rather than given up on at the backstop.
            if (root.TryGetProperty("wait", out var wait) && wait.TryGetDouble(out var seconds) && seconds > 0)
                patience = TimeSpan.FromSeconds(Math.Min(seconds, LongestWait.TotalSeconds));
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return CliJson.Fail(
                "request",
                "BAD_REQUEST",
                "That was not a JSON object. One request per line, "
                + "as {\"op\":\"run\",\"token\":\"...\",\"line\":\"help\"}.");
        }

        // Compared without short-circuiting on length, because the token is the only thing standing
        // between this port and anything else on the machine that went looking for it.
        if (token is null || !CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(Token)))
        {
            return CliJson.Fail(
                operation,
                "BAD_TOKEN",
                "The token did not match this session's. It is in the session file, which "
                + "'bcs status' reads.",
                id);
        }

        var request = new CliRequest(operation, command, id);
        _queue.Add(request);

        // Waited on until the server stops as well, since a request that arrived after the app
        // let go of its queue is never answered, and its connection would wait out its patience
        // after the app had gone.
        bool answered;
        try
        {
            answered = request.Answer.Wait(patience, _stop.Token);
        }
        catch (OperationCanceledException)
        {
            // The answer can be completed in the same instant the server stops, as app.quit's is in
            // the frame that ends the app, and the answer is the one to give.
            return request.Answer.IsCompletedSuccessfully
                ? request.Answer.Result
                : CliJson.Fail(operation, "SESSION_CLOSING", "The app is shutting down.", id);
        }

        return answered
            ? request.Answer.Result
            : CliJson.Fail(
                operation,
                "TIMEOUT",
                $"The app did not answer within {patience.TotalSeconds:0} {(patience.TotalSeconds == 1 ? "second" : "seconds")}. "
                + "It may be stalled, or stopped running frames.",
                id);
    }

    /// <summary>
    /// How long stopping waits for the connections' threads to write the answers they hold before
    /// it closes their sockets.
    /// </summary>
    /// <remarks>
    /// An answer is written in microseconds, so the bound is reached only on a platform where
    /// shutting a socket's receive side does not end a read already waiting on it, and there it is
    /// the delay an idle caller adds to the app's end.
    /// </remarks>
    public static readonly TimeSpan Linger = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Stops listening, lets each connection write the answer it holds, closes the connections
    /// still open, and waits for their threads to end.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A connection's thread waits on its read for as long as the caller keeps it open, so one left
    /// alone would outlive the app, which is how 3DEngine found the same leak on macOS. Every thread
    /// the server started has ended by the time this returns.
    /// </para>
    /// <para>
    /// A request answered in the app's last frame, as <c>app.quit</c>'s is, may not be written yet
    /// when this runs, so the connections are not closed outright. Each request still waiting is
    /// told the app is shutting down, and only the receive side of each socket is shut, which ends
    /// a read waiting for the next request on Linux and macOS and leaves the answer's way out
    /// open. The threads are given <see cref="Linger"/> to write and end, and a socket whose thread
    /// still reads after it, as one can on Windows, where the shutdown does not end a waiting read,
    /// is closed then.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_stopping) return;
        _stopping = true;

        try
        {
            _listener.Stop();
        }
        catch (SocketException)
        {
            // Already down; there is nothing to close.
        }

        KeyValuePair<TcpClient, Thread>[] open;
        lock (_gate) open = [.. _open];

        _stop.Cancel();

        foreach (var (caller, _) in open)
        {
            // A connection whose thread wrote its answer and ended since the copy was taken has let
            // its socket go, which leaves the client holding none.
            if (caller.Client is not { } socket) continue;

            try
            {
                socket.Shutdown(SocketShutdown.Receive);
            }
            catch (Exception error) when (error is SocketException or ObjectDisposedException)
            {
                // The caller hung up already, or the thread let the socket go in the meantime.
            }
        }

        var until = DateTime.UtcNow + Linger;
        foreach (var (caller, serving) in open)
        {
            var left = until - DateTime.UtcNow;
            if (left > TimeSpan.Zero && serving.Join(left)) continue;

            caller.Close();
        }

        _accepting.Join();
        foreach (var (_, serving) in open) serving.Join();
        _stop.Dispose();
    }

    /// <summary>The threads the server started that have not ended, for a test to see none are left.</summary>
    internal IReadOnlyList<Thread> LiveThreads
    {
        get
        {
            lock (_gate)
            {
                return [.. _open.Values.Prepend(_accepting).Where(thread => thread.IsAlive)];
            }
        }
    }
}
