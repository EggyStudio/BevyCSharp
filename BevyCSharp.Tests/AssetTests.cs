using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers asset loading and the handle table behind it.
/// </summary>
/// <remarks>
/// These run headless. Assets are file loading and storage with no GPU involvement, which is why
/// the headless profile carries them: the same bridge serves both builds and the table can be
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
            var handle = AssetServer.Load(AssetKind.Mesh, "models/nothing-here.gltf");

            Assert.True(handle.IsValid);
            Assert.True(AssetServer.IsAlive(handle));
            Assert.NotEqual(AssetLoadState.Unknown, handle.State);
        });

        harness.Run();
    }

    [Fact]
    public void AMissingFileEndsUpFailedRatherThanStuck()
    {
        // Nothing here loads a real asset, so this is the state transition that can be observed
        // without shipping a fixture: queued, attempted, given up on.
        using var harness = new EngineHarness(frames: 30);
        var handle = AssetHandle.None;
        var reachedFailed = false;

        harness.OnContext(Stage.Startup, _ =>
            handle = AssetServer.Load(AssetKind.Image, "textures/does-not-exist.png"));

        harness.OnContext(Stage.Last, _ =>
        {
            if (handle.State == AssetLoadState.Failed) reachedFailed = true;
        });

        harness.Run();

        Assert.True(reachedFailed, $"expected the load to fail, ended at {handle.State}");
    }

    [Fact]
    public void ReleasingFreesTheHandleButNotOthers()
    {
        using var harness = new EngineHarness(frames: 2);

        harness.OnContext(Stage.Startup, _ =>
        {
            var before = AssetServer.LiveHandleCount;

            var first = AssetServer.Load(AssetKind.Mesh, "a.gltf");
            var second = AssetServer.Load(AssetKind.Mesh, "b.gltf");
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
            var stale = AssetServer.Load(AssetKind.Mesh, "first.gltf");
            AssetServer.Release(stale);

            var reused = AssetServer.Load(AssetKind.Mesh, "second.gltf");

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
            var first = AssetServer.Load(AssetKind.Mesh, "shared.gltf");
            var second = AssetServer.Load(AssetKind.Mesh, "shared.gltf");

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
}
