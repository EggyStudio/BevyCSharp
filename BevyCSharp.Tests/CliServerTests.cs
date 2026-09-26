using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a running app answering a socket, end to end.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here is stood in for. The app is an ordinary headless engine with the plugin on, the
/// requests go over loopback the way the command line sends them, and the answers come back from a
/// system inside the frame. The only thing worth asserting about this machinery is that it works
/// end to end, because every part of it exists to cross a boundary.
/// </para>
/// <para>
/// The engine collection, because two apps at once is not something the native side allows.
/// </para>
/// </remarks>
[Collection("engine")]
public sealed class CliServerTests : IDisposable
{
    private readonly string _was = CliSessionFile.Directory;
    private readonly string _mine = Path.Combine(
        Path.GetTempPath(), $"bcs-server-{Guid.NewGuid():N}");

    public CliServerTests() => CliSessionFile.Directory = _mine;

    public void Dispose()
    {
        CliSessionFile.Directory = _was;

        if (Directory.Exists(_mine)) Directory.Delete(_mine, recursive: true);
    }

    [Fact]
    public void AServingAppAnswersWhileItRuns()
    {
        using var app = Serving();

        var running = Start(app);
        var session = Ready();

        try
        {
            // What it is: the state a caller checks before sending anything else.
            var status = Ask(session, "status");

            Assert.True(status.GetProperty("success").GetBoolean());
            Assert.True(status.GetProperty("data").GetProperty("headless").GetBoolean());
            Assert.Equal(
                Environment.ProcessId, status.GetProperty("data").GetProperty("pid").GetInt32());

            // What it can do: the catalog, with the schema that makes it callable unseen.
            var list = Ask(session, "list");
            var commands = list.GetProperty("data").GetProperty("commands").EnumerateArray()
                .ToDictionary(command => command.GetProperty("name").GetString()!);

            Assert.Contains("app.status", commands.Keys);
            Assert.Contains("entity.list", commands.Keys);

            var parameter = Assert.Single(
                commands["log.tail"].GetProperty("parameters").EnumerateArray().ToArray());

            Assert.Equal("count", parameter.GetProperty("name").GetString());
            Assert.Equal("whole", parameter.GetProperty("kind").GetString());

            // And doing one, against the live world rather than a copy of it.
            var ran = Ask(session, "run", "entity.list");

            Assert.True(ran.GetProperty("success").GetBoolean());
            Assert.Equal(
                "entity.list", ran.GetProperty("data").GetProperty("command").GetString());

            // A command that asks to be held answers later, and says so by the frame it answers on.
            var before = ran.GetProperty("data").GetProperty("frame").GetUInt64();
            var waited = Ask(session, "run", "frames.wait 10");

            Assert.True(waited.GetProperty("success").GetBoolean());
            Assert.True(
                waited.GetProperty("data").GetProperty("frame").GetUInt64() >= before + 10,
                "frames.wait answered before the frames it was waiting for had passed");
        }
        finally
        {
            Stop(session, running);
        }

        // The file goes when the app does, so nothing advertises a port that is not there.
        Assert.Empty(CliSessionFile.All());
    }

    /// <summary>
    /// A misspelled command is a code, not a sentence.
    /// </summary>
    /// <remarks>
    /// The console answers anything it cannot do with a sentence, which is right for a person
    /// typing and useless to a caller, because "unknown command: nope" and "nothing at Spawn/Cube"
    /// are the same shape as an answer that worked. The name is checked before the line is run.
    /// </remarks>
    [Fact]
    public void AMisspelledCommandIsRefusedWithACode()
    {
        using var app = Serving();

        var running = Start(app);
        var session = Ready();

        try
        {
            var answer = Ask(session, "run", "nope");

            Assert.False(answer.GetProperty("success").GetBoolean());
            Assert.Equal(
                "UNKNOWN_COMMAND",
                answer.GetProperty("errors")[0].GetProperty("code").GetString());
        }
        finally
        {
            Stop(session, running);
        }
    }

    /// <summary>The port is open to the machine; the token decides who is answered.</summary>
    [Fact]
    public void AWrongTokenIsRefused()
    {
        using var app = Serving();

        var running = Start(app);
        var session = Ready();

        try
        {
            var answer = Send(
                session.Port,
                $$"""{"op":"status","token":"{{new string('0', 32)}}"}""");

            Assert.False(answer.GetProperty("success").GetBoolean());
            Assert.Equal(
                "BAD_TOKEN", answer.GetProperty("errors")[0].GetProperty("code").GetString());
        }
        finally
        {
            Stop(session, running);
        }
    }

    /// <summary>A headless engine with the server on, and a backstop so nothing can hang.</summary>
    private static App Serving()
    {
        var app = new App(new Config
        {
            Headless = true,
            HeadlessFps = 60,

            // Until something asks it to stop, as a session does.
            HeadlessFrames = 0,
            Serve = true,
            AssetRoot = EngineHarness.AssetDirectory,
        });

        app.AddPlugin(new EnginePlugin());
        app.AddPlugin(new CliPlugin());

        // Half a minute at the rate above. A test that fails before it can stop the app would
        // otherwise leave it running for as long as the suite does.
        app.AddSystem(Stage.First, new SystemDescriptor(
            static world =>
            {
                if (world.Resource<Time>().FrameCount > 1800) App.RequestExit();
            },
            "Test.Backstop"));

        return app;
    }

    /// <summary>Runs it off the test's own thread, the way a headless app may be run.</summary>
    private static Thread Start(App app)
    {
        var thread = new Thread(() => app.Run()) { IsBackground = true, Name = "test-app" };

        thread.Start();
        return thread;
    }

    /// <summary>Waits for the app to write that it is ready, as the tool does.</summary>
    private static CliSession Ready()
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (CliSessionFile.All().FirstOrDefault(session => session.State == "ready")
                is { } found)
            {
                return found;
            }

            Thread.Sleep(50);
        }

        throw new Xunit.Sdk.XunitException(
            "The app never wrote a ready session file, so nothing could have connected to it.");
    }

    /// <summary>Asks it to stop, and waits for the loop to end.</summary>
    private static void Stop(CliSession session, Thread running)
    {
        Ask(session, "run", "app.quit");

        Assert.True(running.Join(TimeSpan.FromSeconds(15)), "the app did not shut down");
    }

    /// <summary>One request, with this session's token on it.</summary>
    private static JsonElement Ask(CliSession session, string operation, string? line = null)
    {
        var request = line is null
            ? $$"""{"op":"{{operation}}","token":"{{session.Token}}"}"""
            : $$"""{"op":"{{operation}}","token":"{{session.Token}}","line":"{{line}}"}""";

        return Send(session.Port, request);
    }

    /// <summary>One line in, one line out, which is the whole of the protocol.</summary>
    private static JsonElement Send(int port, string request)
    {
        using var caller = new TcpClient();

        caller.SendTimeout = 15_000;
        caller.ReceiveTimeout = 15_000;
        caller.Connect("127.0.0.1", port);

        using var stream = caller.GetStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false))
        {
            AutoFlush = true,
            NewLine = "\n",
        };

        writer.WriteLine(request);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var answer = reader.ReadLine();

        Assert.NotNull(answer);

        // Cloned, because the document owns the buffer the element points into and this outlives it.
        using var document = JsonDocument.Parse(answer);
        return document.RootElement.Clone();
    }
}
