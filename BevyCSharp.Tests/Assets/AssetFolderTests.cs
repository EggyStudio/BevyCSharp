using Bevy;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// A folder of assets loaded at once, which Bevy's asset_loading loads, its handles given once the
/// whole of it has arrived.
/// </summary>
[Collection("engine")]
public sealed class AssetFolderTests
{
    [Fact]
    public void AFolderGivesTheHandlesOfWhatItHoldsOnceLoaded()
    {
        using var harness = new EngineHarness(frames: 300, fps: 240);

        var folder = AssetHandle.None;
        AssetHandle[] assets = [];
        var checker = AssetLoadState.NotLoaded;

        harness.OnContext(Stage.Startup, _ => folder = AssetServer.LoadFolder("textures"));
        harness.OnContext(Stage.Update, ctx =>
        {
            if (!AssetServer.TryReadFolder(folder, out assets)) return;

            // A file asked for by its path is the one the folder holds, there already rather than
            // loaded a second time.
            checker = AssetServer.StateOf(AssetServer.Load(AssetKind.Image, "textures/checker.png"));
            ctx.Exit();
        });

        harness.Run();

        // The two images the folder holds.
        Assert.Equal(2, assets.Length);
        Assert.Equal(AssetLoadState.Loaded, checker);
    }

    [Fact]
    public void AHandleThatIsNoFolderIsRefused()
    {
        using var harness = new EngineHarness(frames: 2);
        BevyNativeException? refused = null;

        harness.OnContext(Stage.Startup, ctx =>
            refused = Assert.Throws<BevyNativeException>(() =>
                AssetServer.TryReadFolder(AssetServer.Load(AssetKind.Image, "textures/checker.png"), out _)));

        harness.Run();

        Assert.Equal(NativeStatus.InvalidState, refused!.Status);
    }
}
