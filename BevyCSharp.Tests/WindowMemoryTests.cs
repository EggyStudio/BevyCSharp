using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers keeping where the window was left between runs: what is kept as the window moves and is
/// maximized, and what the next run opens with.
/// </summary>
/// <remarks>
/// No window, since a test run has none to move. The choice of what to keep is a function of what
/// was kept and what the window says, so it is tested as one, and the file it is kept in is read
/// as an app reads it before it opens. The player's directory is static, so these share the engine
/// collection and point it somewhere of their own.
/// </remarks>
[Collection("engine")]
public sealed class WindowMemoryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-window-" + Guid.NewGuid().ToString("n"));

    public WindowMemoryTests() => UserData.Root = _root;

    public void Dispose()
    {
        UserData.Root = EngineHarness.UserDirectory;
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void AMaximizedWindowKeepsTheSizeItHadBefore()
    {
        var kept = new WindowPlace(true, 100, 80, 1200, 700, false);

        // Moved and resized, it keeps where it is now.
        var moved = new WindowPlace(true, 300, 200, 1000, 600, false);
        Assert.Equal(moved, WindowMemory.Next(kept, moved));

        // Maximized, it keeps the window it goes back to, marked as maximized.
        var full = new WindowPlace(true, 0, 0, 2560, 1400, true);
        Assert.Equal(kept with { Maximized = true }, WindowMemory.Next(kept, full));

        // Put back, it is an ordinary window again.
        Assert.Equal(kept, WindowMemory.Next(kept with { Maximized = true }, kept));
    }

    [Fact]
    public void WithNoPositionReportedThePlaceKeptBeforeStays()
    {
        // As on Wayland, or before the window has been moved once.
        var kept = new WindowPlace(true, 100, 80, 1200, 700, false);
        var resized = new WindowPlace(false, 0, 0, 900, 500, false);

        Assert.Equal(new WindowPlace(true, 100, 80, 900, 500, false), WindowMemory.Next(kept, resized));

        // A window with no size yet says nothing worth keeping.
        Assert.Equal(kept, WindowMemory.Next(kept, default));
    }

    [Fact]
    public void AnAppAskingToRememberOpensWhereItWasLeft()
    {
        var config = new Config { Width = 800, Height = 600 };

        // Not asked, the config's size and the platform's place, whatever is kept.
        var kept = new Persistent<WindowPlace>("window", WindowJson.Default.WindowPlace, () => default);
        kept.Set(new WindowPlace(true, 40, 50, 1400, 900, true));
        kept.Persist();

        Assert.Equal(new WindowPlace(false, 0, 0, 800, 600, false), WindowMemory.Open(config));

        // Asked, the place kept.
        config.RememberWindow = true;
        Assert.Equal(new WindowPlace(true, 40, 50, 1400, 900, true), WindowMemory.Open(config));

        // Without a window there is nothing to place.
        config.Headless = true;
        Assert.Equal(new WindowPlace(false, 0, 0, 800, 600, false), WindowMemory.Open(config));

        // A first run, with nothing kept, opens as the config asks.
        File.Delete(kept.FullPath);
        config.Headless = false;
        Assert.Equal(new WindowPlace(false, 0, 0, 800, 600, false), WindowMemory.Open(config));

        // And leaves nothing behind for the next app.
        WindowMemory.Open(new Config());
    }
}
