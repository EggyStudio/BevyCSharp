using System.Net;
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
/// Nothing here is stood in for but a server that hangs up, which one test of how a quit is sent
/// needs. The app is an ordinary headless engine with the plugin on, the requests go over loopback
/// the way the command line sends them, and the answers come back from a system inside the frame. The only thing worth asserting about this machinery is that it works
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
    private readonly TestFolder _folder = new("bcs-server-");

    public CliServerTests() => CliSessionFile.Directory = _folder.Path;

    public void Dispose()
    {
        CliSessionFile.Directory = _was;

        _folder.Dispose();
    }

    [Fact]
    public void AServingAppAnswersWhileItRuns()
    {
        using var app = Serving();

        var running = Start(app);
        var session = Ready();

        try
        {
            // What it is, the state a caller checks before sending anything else.
            var status = Ask(session, "status");

            Assert.True(status.GetProperty("success").GetBoolean());
            Assert.True(status.GetProperty("data").GetProperty("headless").GetBoolean());
            Assert.Equal(
                Environment.ProcessId, status.GetProperty("data").GetProperty("pid").GetInt32());

            // What it can do, the catalog with the schema that makes it callable unseen.
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

            // A run that draws nothing has no pipelines to wait for, and says so at once.
            var compiled = Ask(session, "run", "pipelines.wait");
            Assert.True(compiled.GetProperty("success").GetBoolean());
            Assert.Contains("draws nothing", compiled.GetRawText());
        }
        finally
        {
            Stop(session, running);
        }

        // The file goes when the app does, so nothing advertises a port that is not there.
        Assert.Empty(CliSessionFile.All());
    }

    [Fact]
    public void TheLibrarysClientRunsACommandAndReadsItsAnswer()
    {
        using var app = Serving();

        var running = Start(app);
        var session = Ready();

        try
        {
            // What the editor asks a game it started, and how it reads what came back.
            var listed = CliClient.Run(session, "entity.list");
            Assert.True(listed.Success);
            Assert.NotNull(listed.Result);

            // The clock stopped from outside, as the Play tab's Pause does, and started again.
            Assert.Equal("paused", CliClient.Run(session, "app.pause on").Result);
            Assert.Equal("running", CliClient.Run(session, "app.pause off").Result);

            // A command that is not there fails with why.
            var missing = CliClient.Run(session, "no.such.command");
            Assert.False(missing.Success);
            Assert.False(string.IsNullOrEmpty(missing.Error));
        }
        finally
        {
            Stop(session, running);
        }
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

    /// <summary>
    /// A request is waited on as long as its caller says it will wait, rather than the half minute
    /// one that says nothing is given, so a command over many frames of a slow app is answered.
    /// </summary>
    /// <remarks>
    /// The queue is never drained, as an app that stopped running frames leaves it, so the answer
    /// is the one the wait ran out with, which names how long it was.
    /// </remarks>
    /// <summary>
    /// A shot naming a frame is taken in that frame, which a scene changing its color between two
    /// frames shows, and one naming a frame gone by is refused.
    /// </summary>
    /// <remarks>
    /// The red runs to frame 59 and the green from 60, so a picture asked for at 60 and taken a frame
    /// early is red. Each request is sent long before its frame, as a capture's is.
    /// </remarks>
    [SkippableFact]
    public void AShotNamingAFrameIsTakenInThatFrame()
    {
        Needs.Renderer();

        var folder = Path.Combine(_folder.Path, "shots");
        Directory.CreateDirectory(folder);

        var running = Start(() =>
        {
            var app = Serving(new Config
            {
                Offscreen = true,
                Width = 32,
                Height = 32,
                HeadlessFps = 60,
                FrameSeconds = 1.0 / 60.0,
            });
            app.AddSystem(Stage.Update, new SystemDescriptor(
                static world =>
                {
                    if (world.Resource<Time>().FrameCount is 1 or 60)
                        Render.SetClearColor(world.Resource<Time>().FrameCount < 60 ? (1f, 0f, 0f, 1f) : (0f, 1f, 0f, 1f));
                },
                "Test.Colors"));
            app.AddSystem(Stage.Startup, new SystemDescriptor(static _ => Render2d.SpawnCamera2d(), "Test.Camera"));
            return app;
        });
        var session = Ready();

        try
        {
            var red = Path.Combine(folder, "red.png");
            var green = Path.Combine(folder, "green.png");

            var early = Ask(session, "run", $"shot {red} 30");
            Assert.True(early.GetProperty("success").GetBoolean(), early.GetRawText());
            Assert.Equal(30ul, early.GetProperty("data").GetProperty("frame").GetUInt64());

            var late = Ask(session, "run", $"shot {green} 60");
            Assert.True(late.GetProperty("success").GetBoolean(), late.GetRawText());
            Assert.Equal(60ul, late.GetProperty("data").GetProperty("frame").GetUInt64());

            var passed = Ask(session, "run", $"shot {red} 1");
            Assert.Equal("FRAME_PASSED", passed.GetProperty("errors")[0].GetProperty("code").GetString());

            var (r, g, _) = FirstPixel(Written(red));
            Assert.True(r > 200 && g < 60, $"the picture at frame 30 is {(r, g)}, where the clear color was red");
            (r, g, _) = FirstPixel(Written(green));
            Assert.True(g > 200 && r < 60, $"the picture at frame 60 is {(r, g)}, where the clear color turned green");
        }
        finally
        {
            Stop(session, running);
        }
    }

    // A capture's file, once the picture has come back off the GPU and been written whole.
    private static string Written(string path)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 0)
            {
                var size = new FileInfo(path).Length;
                Thread.Sleep(120);
                if (new FileInfo(path).Length == size) return path;
            }

            Thread.Sleep(50);
        }

        throw new Xunit.Sdk.XunitException($"no picture was written to {path}");
    }

    // The first pixel of an eight-bit PNG, which every filter leaves as it is, since each guesses
    // zero for a byte with nothing before it or above it.
    private static (int R, int G, int B) FirstPixel(string path)
    {
        var bytes = File.ReadAllBytes(path);
        using var data = new MemoryStream();
        for (var at = 8; at + 8 <= bytes.Length;)
        {
            var length = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(at, 4));
            if (Encoding.ASCII.GetString(bytes, at + 4, 4) == "IDAT") data.Write(bytes, at + 8, length);
            at += 12 + length;
        }

        data.Position = 0;
        using var inflated = new MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(data, System.IO.Compression.CompressionMode.Decompress)) zlib.CopyTo(inflated);
        var pixels = inflated.ToArray();
        return (pixels[1], pixels[2], pixels[3]);
    }

    [Fact]
    public void ARequestIsWaitedOnAsLongAsItsCallerSays()
    {
        using var server = new CliServer(new CliQueue());

        var answer = Send(server.Port, $$"""{"op":"status","token":"{{server.Token}}","wait":1}""");

        var error = answer.GetProperty("errors")[0];
        Assert.Equal("TIMEOUT", error.GetProperty("code").GetString());
        Assert.StartsWith("The app did not answer within 1 second.", error.GetProperty("message").GetString());
    }

    /// <summary>
    /// A headless engine with the server on, or one drawing as <paramref name="config"/> says, and a
    /// backstop so nothing can hang.
    /// </summary>
    private static App Serving(Config? config = null)
    {
        config ??= new Config { Headless = true, HeadlessFps = 60 };

        // Until something asks it to stop, as a session does.
        config.HeadlessFrames = 0;
        config.Serve = true;
        config.AssetRoot = EngineHarness.AssetDirectory;
        var app = new App(config);

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

    /// <summary>
    /// Makes an app on a thread of its own and runs it there, for one that draws, which answers
    /// nothing when run on a thread other than the one it was made on.
    /// </summary>
    private static Thread Start(Func<App> make)
    {
        var thread = new Thread(() =>
        {
            using var app = make();
            app.Run();
        }) { IsBackground = true, Name = "test-app" };

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

    /// <summary>Asks it to stop, reads the answer, and waits for the loop to end.</summary>
    /// <remarks>
    /// The answer is completed in the frame that ends the app, and the server writes it before it
    /// closes the connection, so every app these tests stop reads <c>app.quit</c>'s answer back.
    /// </remarks>
    private static void Stop(CliSession session, Thread running)
    {
        var answer = Quit(session.Port, session.Token);

        Assert.True(running.Join(TimeSpan.FromSeconds(15)), "the app did not shut down");
        Assert.NotNull(answer);
        using var document = JsonDocument.Parse(answer);
        Assert.True(document.RootElement.GetProperty("success").GetBoolean(), answer);
    }

    /// <summary>Sends <c>app.quit</c>, and answers what came back, or null where the connection closed first.</summary>
    private static string? Quit(int port, string token)
    {
        try
        {
            using var caller = new TcpClient();
            caller.SendTimeout = 15_000;
            caller.ReceiveTimeout = 15_000;
            caller.Connect("127.0.0.1", port);

            using var stream = caller.GetStream();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };
            writer.WriteLine($$"""{"op":"run","token":"{{token}}","line":"app.quit"}""");

            using var reader = new StreamReader(stream, Encoding.UTF8);
            return reader.ReadLine();
        }
        catch (Exception error) when (error is IOException or SocketException)
        {
            return null;
        }
    }

    /// <summary>
    /// A quit whose connection closes before anything is answered is taken as sent, which is how an
    /// app shutting down at the end of the frame that ran it may leave it.
    /// </summary>
    [Fact]
    public void AQuitThatIsAnsweredWithNothingIsStillSent()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        // A server that reads the quit and hangs up, as one disposed in the meantime does.
        string? heard = null;
        var closing = new Thread(() =>
        {
            using var caller = listener.AcceptTcpClient();
            using var reader = new StreamReader(caller.GetStream(), Encoding.UTF8);
            heard = reader.ReadLine();
        });
        closing.Start();

        var answer = Quit(port, new string('0', 32));
        Assert.True(closing.Join(TimeSpan.FromSeconds(15)), "the stand-in server never heard the quit");

        Assert.Null(answer);
        Assert.Contains("app.quit", heard);
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

    /// <summary>
    /// A request answered as the server stops is written before the connection closes, as
    /// <c>app.quit</c>'s answer is completed in the frame that ends the app.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The answer is given the way an app gives its last ones on the way down, and the server is
    /// stopped straight after on the same thread, while the connection's thread may still be
    /// waking to write it.
    /// </para>
    /// <para>
    /// The race is narrow, so it is run many times over. A server that closed both ways at once
    /// lost the answer in some ten rounds of three hundred, the caller reading the end of the stream,
    /// and the chance of all three hundred passing by luck against it is some two in a hundred
    /// thousand. Each round is a millisecond or so.
    /// </para>
    /// </remarks>
    [Fact]
    public void ARequestAnsweredAsTheServerStopsIsStillWritten()
    {
        var lost = 0;
        for (var round = 0; round < 300; round++)
        {
            var queue = new CliQueue();
            var server = new CliServer(queue);
            using var caller = new TcpClient();
            caller.ReceiveTimeout = 15_000;
            caller.Connect(IPAddress.Loopback, server.Port);
            using var stream = caller.GetStream();
            using var reader = new StreamReader(stream, Encoding.UTF8);
            using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };

            writer.WriteLine($$"""{"op":"run","token":"{{server.Token}}","line":"app.quit"}""");
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (queue.Arrived == 0 && DateTime.UtcNow < deadline) Thread.Sleep(0);
            Assert.Equal(1, queue.Arrived);

            queue.Abandon();
            server.Dispose();

            string? answer;
            try
            {
                answer = reader.ReadLine();
            }
            catch (IOException)
            {
                answer = null;
            }

            if (answer is null) lost++;
            else Assert.Contains("SESSION_CLOSING", answer);
        }

        Assert.Equal(0, lost);
    }

    /// <summary>
    /// A server stopped with a caller still connected closes the connection and leaves no thread of
    /// its own alive, the caller's answered request showing the connection's thread had run.
    /// </summary>
    /// <remarks>
    /// No app, since the request is answered by the connection's thread itself, a wrong token
    /// needing no frame, and no wait on the clock, since stopping returns once the threads end.
    /// </remarks>
    [Fact]
    public void AStoppedServerWithACallerConnectedLeavesNoThreadAlive()
    {
        var server = new CliServer(new CliQueue());
        using var caller = new TcpClient();
        caller.Connect(IPAddress.Loopback, server.Port);
        using var stream = caller.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true, NewLine = "\n" };

        writer.WriteLine("{\"op\":\"run\",\"token\":\"wrong\",\"line\":\"help\"}");
        Assert.Contains("BAD_TOKEN", reader.ReadLine());

        // The thread taking calls and the one serving this caller, waiting on its next line.
        var threads = server.LiveThreads;
        Assert.Equal(2, threads.Count);

        server.Dispose();

        Assert.All(threads, thread => Assert.False(thread.IsAlive, $"{thread.Name} outlived the server"));

        // And the connection is closed rather than left for the caller to hang up.
        string? after;
        try
        {
            after = reader.ReadLine();
        }
        catch (IOException)
        {
            after = null;
        }

        Assert.Null(after);
    }
}
