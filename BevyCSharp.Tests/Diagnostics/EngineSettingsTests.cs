using Bevy;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the engine's settings in the Diagnostics module's registry and the player's settings
/// file, each setting on the page of its first word, the clock's and the field's set through a
/// console line in a running app, and the file read as the app starts and written as a key changes.
/// </summary>
[Collection("engine")]
public sealed class EngineSettingsTests : IDisposable
{
    private readonly Func<Config, string?> _pathFor = SettingsPath.For;

    public void Dispose()
    {
        SettingsPath.For = _pathFor;
        foreach (var action in Enum.GetValues<KeyAction>()) Debug.Keys.KeyOf(action).Reset();
    }

    [Fact]
    public void EverySettingIsTheRegistrysOnThePageOfItsFirstWord()
    {
        Assert.NotEmpty(EngineSettings.All);
        foreach (var setting in EngineSettings.All)
        {
            Assert.Same(setting, Registry.Process.FindSetting(setting.Name));
            Assert.Contains(setting.Name.Split('.')[0], new[] { "time", "gi", "field", "scenes", "run" });
            Assert.False(setting.Saved, setting.Name);
            Assert.Equal(setting.Name.StartsWith("run.", StringComparison.Ordinal), setting.ReadOnly);
        }

        Assert.Equal(Answer.NotSettable, Registry.Process.Run("run.frames 5").Code);
        Assert.Equal(Answer.Refused, Registry.Process.Run("time.speed 2").Code);
    }

    [Fact]
    public void ALineSetsTheRunningAppsClockAndField()
    {
        using var harness = new EngineHarness(frames: 6);
        var frame = 0;
        var seen = new List<string>();

        harness.OnContext(Stage.Update, ctx =>
        {
            frame++;
            if (frame == 2)
            {
                foreach (var line in new[] { "time.speed 2", "time.paused on", "time.frame 0.05", "time.hold loads", "field.cell 0.5", "field.budget 3" })
                    seen.Add($"{line}: {ConsoleHost.Submit(line, ctx.World).Code ?? "done"}");
                seen.Add($"time.hold both: {ConsoleHost.Submit("time.hold both", ctx.World).Code}");
            }

            if (frame == 3)
            {
                var time = ctx.World.Resource<Time>();
                seen.Add($"speed {time.Speed}, paused {time.Paused}, frame {time.FrameSeconds}, held {time.Holds}");
                seen.Add($"field {SceneField.Asked.CellSize} {SceneField.Asked.Budget} {SceneField.Asked.Cascades}");
                ConsoleHost.Submit("time.paused off", ctx.World);
            }
        });
        harness.Run();

        Assert.Equal(
            [
                "time.speed 2: done", "time.paused on: done", "time.frame 0.05: done", "time.hold loads: done", "field.cell 0.5: done",
                "field.budget 3: done", $"time.hold both: {Answer.Refused}", "speed 2, paused True, frame 0.05, held Loads", "field 0.5 3 0",
            ],
            seen);
    }

    [Fact]
    public void TheFileIsReadAsTheAppStartsAndWrittenAsAKeyChanges()
    {
        using var folder = new TestFolder("bcs-settings-");
        var path = folder.File(SettingsPath.FileName);
        File.WriteAllText(path, "keys.overlay F7\n# a player's note\nfeature.other 3\n");
        SettingsPath.For = _ => path;

        using var harness = new EngineHarness(frames: 4);
        var frame = 0;
        string? overlay = null;
        harness.OnContext(Stage.Update, ctx =>
        {
            if (++frame != 2) return;
            overlay = Debug.Keys.KeyOf(KeyAction.Overlay).Value;
            ConsoleHost.Submit("keys.panel F8", ctx.World);
        });
        harness.Run();

        Assert.Equal("F7", overlay);
        // The saved settings that differ come first, in the registry's order, and the lines of no
        // setting this run knows after them as they stood.
        var lines = File.ReadAllLines(path);
        Assert.Equal(["keys.overlay F7", "keys.panel F8"], lines[..2].Order(StringComparer.Ordinal));
        Assert.Equal(["# a player's note", "feature.other 3"], lines[2..]);
    }

    [Fact]
    public void ARunWithNoWindowKeepsNoFile()
    {
        Assert.Null(SettingsPath.For(new Config { Headless = true }));
        Assert.Null(SettingsPath.For(new Config { Offscreen = true }));

        using var harness = new EngineHarness(frames: 3);
        var kept = true;
        harness.OnContext(Stage.Update, ctx => kept = ctx.World.TryGetResource<KeptSettings>(out _));
        harness.Run();

        Assert.False(kept);
    }
}
