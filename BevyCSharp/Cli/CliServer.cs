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
/// and nothing else. A connection stays open for as many requests as the caller wants, because the
/// point of serving a running app at all is that the second question costs nothing: the app is
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
    private readonly List<Thread> _connections = [];
    private volatile bool _stopping;

    /// <summary>Starts listening, on a port chosen by the kernel.</summary>
    public CliServer(CliQueue queue)
    {
        _queue = queue;

        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();

        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();

        var accepting = new Thread(Accept)
        {
            IsBackground = true,
            Name = "bcs-cli-accept",
        };

        accepting.Start();
    }

    /// <summary>Where it is listening.</summary>
    public int Port { get; }

    /// <summary>What a request has to carry to be answered.</summary>
    public string Token { get; }

    /// <summary>How long a request may sit in the queue before the caller is told it timed out.</summary>
    /// <remarks>
    /// A backstop rather than a policy. A frame answers in milliseconds, so reaching this means the
    /// app stopped running frames, and a caller learning that in thirty seconds is better than one
    /// waiting on a socket that will never answer.
    /// </remarks>
    public static readonly TimeSpan Patience = TimeSpan.FromSeconds(30);

    /// <summary>Takes calls until the listener is closed.</summary>
    private void Accept()
    {
        while (!_stopping)
        {
            TcpClient caller;

            try
            {
                caller = _listener.AcceptTcpClient();
            }
            catch (Exception error) when (error is SocketException or ObjectDisposedException
                                              or InvalidOperationException)
            {
                return;
            }

            var thread = new Thread(() => Serve(caller))
            {
                IsBackground = true,
                Name = "bcs-cli-connection",
            };

            lock (_connections) _connections.Add(thread);

            thread.Start();
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
                // The caller hung up, which is an ordinary way for a request to end.
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

        try
        {
            using var document = JsonDocument.Parse(line);
            var root = document.RootElement;

            operation = root.TryGetProperty("op", out var op) ? op.GetString() ?? "run" : "run";
            command = root.TryGetProperty("line", out var body) ? body.GetString() : null;
            id = root.TryGetProperty("id", out var given) ? given.GetString() : null;
            token = root.TryGetProperty("token", out var carried) ? carried.GetString() : null;
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

        return request.Answer.Wait(Patience)
            ? request.Answer.Result
            : CliJson.Fail(
                operation,
                "TIMEOUT",
                $"The app did not answer within {Patience.TotalSeconds:0} seconds. It may be "
                + "stalled, or stopped running frames.",
                id);
    }

    /// <summary>Stops listening. Connections in flight end with their sockets.</summary>
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
    }
}
