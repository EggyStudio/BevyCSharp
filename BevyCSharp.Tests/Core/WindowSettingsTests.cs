using Bevy;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the window's settings in the Diagnostics module's registry on the panel's window page,
/// each saved and only reading in a run whose window is not shown, and a start giving the window
/// the settings file's line of each before the window is made. The sets on a shown window, the
/// refusals and the file written by them are the window storm's (<see cref="WindowStormTests"/>),
/// since a process makes one window.
/// </summary>
[Collection("engine")]
public sealed class WindowSettingsTests : IDisposable
{
    private readonly Func<Config, string?> _pathFor = SettingsPath.For;
    private readonly TestFolder _folder = new("bcs-window-settings-");

    public void Dispose()
    {
        SettingsPath.For = _pathFor;
        Forget(_folder);
        _folder.Dispose();
    }

    [Fact]
    public void EverySettingIsTheRegistrysOnTheWindowPageSavedAndOnlyReadsWhereNoWindowIsShown()
    {
        Assert.Equal(["window.mode", "window.resolution", "window.resizable", "window.monitor", "window.vsync"], WindowSettings.All.Select(s => s.Name));
        // The panel lists them in this order and not by name.
        Assert.Equal(new int?[] { 0, 1, 2, 3, 4 }, WindowSettings.All.Select(s => s.Order));
        foreach (var setting in WindowSettings.All)
        {
            Assert.Same(setting, Registry.Process.FindSetting(setting.Name));
            Assert.True(setting.Saved, setting.Name);
            Assert.False(setting.AtNextStart, setting.Name);
        }
        Assert.Equal(["Windowed", "Borderless", "BorderlessFullscreen", "Fullscreen"], Registry.Process.FindSetting("window.mode")!.Choices);
        // Native where nothing is set, and offered alone where no window is shown to list sizes.
        Assert.Equal(("native", "native"), (Registry.Process.FindSetting("window.resolution")!.DefaultText, Registry.Process.FindSetting("window.resolution")!.Text));
        Assert.Equal(["native"], Registry.Process.FindSetting("window.resolution")!.Choices);

        using var harness = new EngineHarness(frames: 3);
        var seen = new List<string>();
        harness.OnContext(Stage.Update, ctx =>
        {
            if (seen.Count > 0) return;
            foreach (var line in new[] { "window.mode borderless", "window.resolution 900x500", "window.resizable off", "window.monitor 1", "window.vsync off" })
            {
                var answer = ConsoleHost.Submit(line, ctx.World);
                seen.Add($"{line}: {answer.Code}{(answer.Text.Contains("headless", StringComparison.Ordinal) ? " in a headless run" : "")}");
            }
        });
        harness.Run();

        Assert.Equal(5, seen.Count);
        Assert.All(seen, line => Assert.EndsWith($": {Answer.NotSettable} in a headless run", line));
    }

    [SkippableFact]
    public void AStartGivesTheWindowTheFilesLinesInOrderAndARunWithNoWindowItsConfig()
    {
        Needs.Renderer();
        var path = _folder.File(SettingsPath.FileName);
        File.WriteAllText(path, string.Join('\n',
            "window.mode Borderless", "window.resizable off", "window.resolution 1000x600", "# a player's note",
            "window.resolution 900x600", "window.monitor 1", "window.vsync off", "window.resolution 900by4", "keys.panel F8", ""));
        SettingsPath.For = _ => path;
        var place = new WindowPlace(false, 0, 0, 640, 400, false);

        var start = WindowSettings.Open(Config.Windowed("Start", 640, 400), place);
        var unshown = WindowSettings.Open(new Config { Offscreen = true, Width = 320, Height = 200, Vsync = true }, place with { Width = 320, Height = 200 });

        // The refused resolution that names no size leaves the one before it standing, as the
        // read of the whole file leaves it as the app starts.
        Assert.Equal((WindowSettings.Mode.Borderless, false, 900u, 600u, (int?)1, false), (start.Mode, start.Resizable, start.Width, start.Height, start.Monitor, start.Vsync));
        Assert.Equal((((uint, uint)?)(900u, 600u), WindowMode.Windowed), (start.Resolution, start.WindowMode));
        Assert.Equal((WindowSettings.Mode.Windowed, true, 320u, 200u, (int?)null, true), (unshown.Mode, unshown.Resizable, unshown.Width, unshown.Height, unshown.Monitor, unshown.Vsync));
        Assert.Null(unshown.Resolution);
        // A line of another page waits for the read of the whole file as the app starts.
        Assert.Equal("F5", Registry.Process.FindSetting("keys.panel")!.Text);
    }

    /// <summary>
    /// Gives the window's settings and the render page's back their defaults, as a start of a file
    /// holding them gives them, since a set of a setting stays in the process's registry and the
    /// settings file of the next test would keep it.
    /// </summary>
    internal static void Forget(TestFolder folder)
    {
        if (!App.HasRenderer) return;
        var path = folder.File("defaults.txt");
        File.WriteAllLines(path, WindowSettings.All.Concat(PictureSettings.All).Select(s => $"{s.Name} {CommandLine.Quote(s.DefaultText)}"));
        var was = SettingsPath.For;
        SettingsPath.For = _ => path;
        try
        {
            WindowSettings.Open(Config.Windowed("Defaults", 640, 400), new WindowPlace(false, 0, 0, 640, 400, false));
        }
        finally
        {
            SettingsPath.For = was;
        }
    }
}
