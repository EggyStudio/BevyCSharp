using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers asset loading and the handle table behind it.
/// </summary>
/// <remarks>
/// These run headless. Assets are file loading and storage with no GPU involvement, which is why
/// the headless profile carries them, so the same bridge serves both builds and the table can be
/// tested in CI.
/// </remarks>
[Collection("engine")]
public sealed class AssetTests
{
    [Fact]
    public void LoadingReturnsAValidHandle()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, _ =>
        {
            var handle = AssetServer.Load(AssetKind.Image, "textures/checker.png");

            Assert.True(handle.IsValid);
            Assert.True(AssetServer.IsAlive(handle));
            Assert.NotEqual(AssetLoadState.Unknown, handle.State);
        });

        harness.Run();
    }

    [Fact]
    public void AZeroedHandleNamesNothing()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            // A component holding an asset starts out zeroed, so the table must never hand out a
            // key of zero, because a freshly added component would otherwise hold whatever was
            // loaded first, and nothing about it would look wrong.
            Assert.False(default(AssetHandle).IsValid);

            var first = AssetServer.Load(AssetKind.Image, "textures/checker.png");

            Assert.True(first.IsValid);
            Assert.NotEqual(default, first);
        });

        harness.Run();
    }

    [Fact]
    public void AHandleRemembersThePathItCameFrom()
    {
        using var harness = new EngineHarness(frames: 3);

        harness.OnContext(Stage.Startup, _ =>
        {
            var handle = AssetServer.Load(AssetKind.Image, "textures/checker.png");

            // The path is the one the handle was asked for, whether or not the file is there,
            // because a tool showing what a field points at has to be able to say so before the
            // load finishes, and has to say something truthful when it never does.
            Assert.Equal("textures/checker.png", AssetServer.PathOf(handle));
            Assert.Null(AssetServer.PathOf(AssetHandle.None));
        });

        harness.Run();
    }

    [Fact]
    [ExpectsError("bevy", "does-not-exist.png")]
    public void AMissingFileEndsUpFailedRatherThanStuck()
    {
        // Nothing here loads a real asset, so this is the state transition that can be observed
        // without shipping a fixture: queued, attempted, given up on.
        //
        // The attempt happens on an IO thread while the app spins frames as fast as it can, so
        // how many frames pass before the failure lands is a property of the machine. A fixed
        // budget is therefore a race, and one a slower runner loses. Run until the state settles
        // and stop on a wall clock instead, so a genuine hang still fails the test.
        using var harness = new EngineHarness(frames: 0, fps: 1000);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        var handle = AssetHandle.None;
        var lastSeen = AssetLoadState.Unknown;
        var reachedFailed = false;

        harness.OnContext(Stage.Startup, _ =>
            handle = AssetServer.Load(AssetKind.Image, "textures/does-not-exist.png"));

        harness.OnContext(Stage.Last, ctx =>
        {
            lastSeen = handle.State;
            if (lastSeen == AssetLoadState.Failed) reachedFailed = true;
            if (reachedFailed || DateTime.UtcNow > deadline) ctx.Exit();
        });

        harness.Run();

        Assert.True(reachedFailed, $"expected the load to fail, gave up at {lastSeen}");
    }

    [Fact]
    public void ReleasingFreesTheHandleButNotOthers()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            var before = AssetServer.LiveHandleCount;

            var first = AssetServer.Load(AssetKind.Image, "textures/checker.png");
            var second = AssetServer.Load(AssetKind.Image, "textures/cubemap.png");
            Assert.Equal(before + 2, AssetServer.LiveHandleCount);

            Assert.True(AssetServer.Release(first));
            Assert.Equal(before + 1, AssetServer.LiveHandleCount);

            Assert.False(AssetServer.IsAlive(first));
            Assert.True(AssetServer.IsAlive(second));

            // Releasing twice is a no-op rather than an error or a double free.
            Assert.False(AssetServer.Release(first));
        });

        harness.Run();
    }

    [Fact]
    public void AReleasedHandleDoesNotNameWhateverTookItsSlot()
    {
        // The reason a handle carries a generation as well as a slot index. Without one, the
        // stale handle below would silently start referring to a completely different asset.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            var stale = AssetServer.Load(AssetKind.Image, "textures/checker.png");
            AssetServer.Release(stale);

            var reused = AssetServer.Load(AssetKind.Image, "textures/cubemap.png");

            Assert.False(AssetServer.IsAlive(stale));
            Assert.True(AssetServer.IsAlive(reused));
            Assert.NotEqual(stale, reused);
            Assert.Equal(AssetLoadState.Unknown, stale.State);
        });

        harness.Run();
    }

    [Fact]
    public void LoadingTheSamePathTwiceGivesTwoHandlesToOneAsset()
    {
        // Bevy deduplicates by path, so the second load is a second reference rather than a
        // second read. Both are independently releasable.
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            var first = AssetServer.Load(AssetKind.Image, "textures/checker.png");
            var second = AssetServer.Load(AssetKind.Image, "textures/checker.png");

            Assert.NotEqual(first, second);
            Assert.True(AssetServer.Release(first));
            Assert.True(AssetServer.IsAlive(second));
            Assert.True(AssetServer.Release(second));
        });

        harness.Run();
    }

    [Fact]
    public void AnUnknownKindSaysWhichBuildWouldSupportIt()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            var ex = Assert.Throws<BevyNativeException>(
                () => AssetServer.Load("NotAnAssetType", "whatever"));

            Assert.Equal(NativeStatus.NoComponent, ex.Status);
            Assert.Contains("renderer", ex.Message);
        });

        harness.Run();
    }

    [Fact]
    public void AnInvalidHandleIsInertRatherThanDangerous()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            Assert.False(AssetHandle.None.IsValid);
            Assert.Equal(AssetLoadState.Unknown, AssetHandle.None.State);
            Assert.False(AssetServer.IsAlive(AssetHandle.None));
            Assert.False(AssetServer.Release(AssetHandle.None));
            Assert.False(AssetHandle.None.IsLoaded);
        });

        harness.Run();
    }

    [Fact]
    public void ARadianceImageDecodesInEveryBuild()
    {
        // One pixel of Radiance's RGBE, a shared exponent over three mantissas, so the file holds a
        // value of one in each channel. A row narrower than eight pixels is stored as it is, with
        // none of the format's run-length encoding, which keeps the file writable by hand.
        var path = Path.Combine(EngineHarness.AssetDirectory, $"light-{Guid.NewGuid():N}.hdr");
        File.WriteAllBytes(path, [.. "#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y 1 +X 1\n"u8, 128, 128, 128, 129]);

        // The load runs on an IO thread, so the frames it takes are the machine's; a wall clock
        // stops a hang instead, as the missing file's test does.
        using var harness = new EngineHarness(frames: 0, fps: 1000);
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        var handle = AssetHandle.None;
        var state = AssetLoadState.Unknown;

        // Read inside the loop, since a handle asked after the app has stopped names nothing.
        harness.OnContext(Stage.Startup, _ => handle = AssetServer.Load(AssetKind.Image, Path.GetFileName(path)));
        harness.OnContext(Stage.Last, ctx =>
        {
            state = handle.State;
            if (state is AssetLoadState.Loaded or AssetLoadState.Failed || DateTime.UtcNow > deadline) ctx.Exit();
        });

        try
        {
            harness.Run();
            Assert.Equal(AssetLoadState.Loaded, state);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
