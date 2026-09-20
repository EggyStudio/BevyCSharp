using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Bevy;

namespace BevyCSharp.Cli;

/// <summary>Asks a running app something, over its loopback port.</summary>
internal static class Client
{
    /// <summary>
    /// Sends one request and reads the one answer.
    /// </summary>
    /// <remarks>
    /// A connection per invocation, because an invocation asks one thing. Keeping one open would
    /// save a millisecond on a loopback socket and cost every caller a lifetime to manage.
    /// </remarks>
    /// <param name="session">Which app to ask.</param>
    /// <param name="operation">What to ask: <c>ping</c>, <c>status</c>, <c>list</c> or <c>run</c>.</param>
    /// <param name="line">The command line, for <c>run</c>.</param>
    /// <param name="seconds">How long to wait before giving up.</param>
    /// <returns>The envelope, which is this tool's answer too.</returns>
    public static string Send(
        CliSession session, string operation, string? line = null, double seconds = 30)
    {
        ArgumentNullException.ThrowIfNull(session);

        try
        {
            using var caller = new TcpClient();

            var milliseconds = (int)Math.Clamp(seconds * 1000, 1000, int.MaxValue);

            caller.SendTimeout = milliseconds;
            caller.ReceiveTimeout = milliseconds;
            caller.Connect("127.0.0.1", session.Port);
            caller.NoDelay = true;

            using var stream = caller.GetStream();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false))
            {
                AutoFlush = true,
                NewLine = "\n",
            };

            writer.WriteLine(Request(session.Token, operation, line));

            using var reader = new StreamReader(stream, Encoding.UTF8);

            return reader.ReadLine() ?? CliJson.Fail(
                operation,
                "SESSION_UNREACHABLE",
                $"{session.Name} ({session.Pid}) accepted the connection and then said nothing. "
                + "It may have exited mid-request.");
        }
        catch (Exception error) when (error is SocketException or IOException)
        {
            return CliJson.Fail(
                operation,
                "SESSION_UNREACHABLE",
                $"Could not reach {session.Name} ({session.Pid}) on port {session.Port}: "
                + $"{error.Message}. It may have exited; 'bcs doctor' sweeps up stale sessions.");
        }
    }

    /// <summary>One request, as the line the app reads.</summary>
    private static string Request(string token, string operation, string? line)
    {
        var buffer = new MemoryStream(256);

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("op", operation);
            writer.WriteString("token", token);

            if (line is not null) writer.WriteString("line", line);

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
