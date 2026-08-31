using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>Something that happened, rather than something that is.</summary>
public readonly record struct Collided(Entity A, Entity B);

/// <summary>A second message type, to check the queues stay apart.</summary>
public readonly record struct Scored(int Points);

/// <summary>A behavior that reports what it read, to exercise the route through a context.</summary>
[Behavior]
public partial struct ScoreKeeper
{
    /// <summary>Total points read out of the bus.</summary>
    public static int Total;

    [OnUpdate]
    public static void Tick(BehaviorContext ctx)
    {
        foreach (var scored in ctx.Read<Scored>()) Total += scored.Points;
    }
}

/// <summary>
/// Covers the message bus: broadcast between systems that know nothing about each other.
/// </summary>
[Collection("engine")]
public sealed class MessageTests
{
    [Fact]
    public void EveryReaderSeesEveryMessageExactlyOnce()
    {
        using var harness = new EngineHarness(frames: 5);
        var first = 0;
        var second = 0;
        var sent = 0;

        harness.OnContext(Stage.Update, ctx =>
        {
            ctx.Send(new Scored(1));
            sent++;
        });

        // Two readers in different stages, neither of which knows about the other or about the
        // sender. Both totals must match what was sent.
        harness.OnContext(Stage.PostUpdate, ctx =>
        {
            foreach (var scored in ctx.Read<Scored>()) first += scored.Points;
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            foreach (var scored in ctx.Read<Scored>()) second += scored.Points;
        });

        harness.Run();

        // One frame of latency, so the last frame's message is never read.
        Assert.Equal(sent - 1, first);
        Assert.Equal(sent - 1, second);
    }

    [Fact]
    public void AMessageIsNotReadableInTheFrameItWasSent()
    {
        // The cost of swapping once a frame instead of giving each reader a cursor: the whole
        // frame agrees on one set, and the sender's own frame is not part of it.
        using var harness = new EngineHarness(frames: 3);
        var sameFrame = -1;
        var nextFrame = -1;
        var frame = 0;

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame == 1)
            {
                ctx.Send(new Scored(7));
                sameFrame = ctx.Read<Scored>().Length;
            }
            else if (frame == 2)
            {
                nextFrame = ctx.Read<Scored>().Length;
            }
        });

        harness.Run();

        Assert.Equal(0, sameFrame);
        Assert.Equal(1, nextFrame);
    }

    [Fact]
    public void MessageTypesDoNotMixWithEachOther()
    {
        using var harness = new EngineHarness(frames: 3);
        var collisions = 0;
        var scores = 0;
        var sent = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (sent) return;

            ctx.Send(new Collided(Entity.None, Entity.None));
            ctx.Send(new Scored(3));
            ctx.Send(new Scored(4));
            sent = true;
        });

        harness.OnContext(Stage.Last, ctx =>
        {
            collisions += ctx.Read<Collided>().Length;
            foreach (var scored in ctx.Read<Scored>()) scores += scored.Points;
        });

        harness.Run();

        Assert.Equal(1, collisions);
        Assert.Equal(7, scores);
    }

    [Fact]
    public void MessagesDoNotSurviveTwoSwaps()
    {
        // A message is readable for exactly one frame. Without that a reader running every frame
        // would count the same thing twice.
        using var harness = new EngineHarness(frames: 5);
        var reads = 0;
        var sent = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (sent) return;
            ctx.Send(new Scored(1));
            sent = true;
        });

        harness.OnContext(Stage.Last, ctx => reads += ctx.Read<Scored>().Length);
        harness.Run();

        Assert.Equal(1, reads);
    }

    [Fact]
    public void SendingFromAStartupSystemReachesTheFirstFrame()
    {
        using var harness = new EngineHarness(frames: 3);
        var read = 0;

        harness.OnContext(Stage.Startup, ctx => ctx.Send(new Scored(5)));
        harness.OnContext(Stage.Last, ctx =>
        {
            foreach (var scored in ctx.Read<Scored>()) read += scored.Points;
        });

        harness.Run();

        Assert.Equal(5, read);
    }

    [Fact]
    public void ABehaviorCanReadThroughItsContext()
    {
        ScoreKeeper.Total = 0;

        using var harness = new EngineHarness(frames: 4, discoverBehaviors: true);
        var sent = false;

        harness.OnContext(Stage.First, ctx =>
        {
            if (sent) return;
            ctx.Send(new Scored(11));
            sent = true;
        });

        harness.Run();

        Assert.Equal(11, ScoreKeeper.Total);
    }

    [Fact]
    public void SendingIsSafeFromSeveralThreadsAtOnce()
    {
        // Sends come from parallel behavior methods, so the queue has to tolerate it. Reading
        // stays on the main thread, where the swap happens.
        using var harness = new EngineHarness(frames: 3);
        var read = 0;
        var sent = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (sent) return;
            sent = true;

            Parallel.For(0, 500, i => ctx.Send(new Scored(1)));
        });

        harness.OnContext(Stage.Last, ctx => read += ctx.Read<Scored>().Length);
        harness.Run();

        Assert.Equal(500, read);
    }

    [Fact]
    public void EngineMessagesArriveOnTheSameBus()
    {
        // The point of draining Bevy's own messages onto this bus: a reader uses one API and does
        // not care which side sent what it is reading. A windowless run reports nothing, so what
        // is asserted here is that reading is safe and empty rather than broken.
        using var harness = new EngineHarness(frames: 4);
        var reads = 0;
        var resizes = 0;

        harness.OnContext(Stage.Update, ctx =>
        {
            reads++;
            resizes += ctx.Read<WindowResized>().Length;

            // The engine's messages read exactly like anything else, including being empty.
            Assert.True(ctx.Read<WindowFocusChanged>().IsEmpty);
            Assert.True(ctx.Read<CursorEntered>().IsEmpty);
        });

        harness.Run();

        Assert.True(reads > 1);
        Assert.Equal(0, resizes);
    }

    [Fact]
    public void AnEngineMessageAndAUserMessageShareAQueue()
    {
        // Same type, two senders. Nothing distinguishes them once they are on the bus, which is
        // what lets a test stand in for the window.
        using var harness = new EngineHarness(frames: 5);
        var widths = new List<float>();
        var sent = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!sent)
            {
                ctx.Send(new WindowResized(1280f, 720f));
                sent = true;
                return;
            }

            foreach (var resized in ctx.Read<WindowResized>()) widths.Add(resized.Width);
        });

        harness.Run();

        Assert.Equal([1280f], widths);
    }

    [Fact]
    public void DroppedFilesArriveAsMessages()
    {
        // A drop cannot be synthesised without a desktop, so what is checked here is that the
        // drain runs every frame without failing and reports nothing when nothing was dropped,
        // and that a reader written against the messages compiles and runs against the bus.
        using var harness = new EngineHarness(frames: 5);
        var paths = new List<string>();
        var cancellations = 0;

        harness.OnContext(Stage.Update, ctx =>
        {
            foreach (var dropped in ctx.Read<FileDropped>()) paths.Add(dropped.Path);
            foreach (var hovered in ctx.Read<FileHovered>()) paths.Add(hovered.Path);
            foreach (var _ in ctx.Read<FileHoverCancelled>()) cancellations++;
        });

        harness.Run();

        Assert.Empty(paths);
        Assert.Equal(0, cancellations);
    }

    [Fact]
    public void ADroppedFileCarriesItsPathThroughTheBus()
    {
        // The window is what sends these in a real run, so a test sends one itself to cover the
        // shape a reader sees.
        using var harness = new EngineHarness(frames: 5);
        var paths = new List<string>();
        var sent = false;

        harness.OnContext(Stage.Update, ctx =>
        {
            if (!sent)
            {
                ctx.Send(new FileDropped("/home/player/levels/arena.gltf"));
                sent = true;
                return;
            }

            foreach (var dropped in ctx.Read<FileDropped>()) paths.Add(dropped.Path);
        });

        harness.Run();

        Assert.Equal(["/home/player/levels/arena.gltf"], paths);
    }
}
