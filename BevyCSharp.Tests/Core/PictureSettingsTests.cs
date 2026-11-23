using Bevy;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the render page, each setting's native choice and the choices the panel offers, a start
/// reading each from the settings file by its text, and a game's picture set over them, whose exact
/// size holds the scale unused. The picture drawn on a shown window is the window storm's
/// (<see cref="WindowStormTests"/>), since a process makes one window.
/// </summary>
[Collection("engine")]
public sealed class PictureSettingsTests : IDisposable
{
    private readonly Func<Config, string?> _pathFor = SettingsPath.For;
    private readonly TestFolder _folder = new("bcs-picture-settings-");

    public void Dispose()
    {
        SettingsPath.For = _pathFor;
        WindowSettingsTests.Forget(_folder);
        PictureSettings.Reset();
        _folder.Dispose();
    }

    private static Setting Find(string name) => Registry.Process.FindSetting(name)!;

    [Fact]
    public void EverySettingIsTheRegistrysOnTheRenderPageAndNativeWhereNothingSetsIt()
    {
        PictureSettings.Reset();
        Assert.Equal(["render.scale", "render.filter", "render.aspect", "render.fit", "render.fps_limit"], PictureSettings.All.Select(s => s.Name));
        // The panel lists them in this order and not by name.
        Assert.Equal(new int?[] { 0, 1, 2, 3, 4 }, PictureSettings.All.Select(s => s.Order));
        foreach (var setting in PictureSettings.All)
        {
            Assert.Same(setting, Find(setting.Name));
            Assert.True(setting.Saved && setting.InPanel && !setting.AtNextStart, setting.Name);
            Assert.Equal(setting.DefaultText, setting.Text);
        }
        Assert.Equal(["100%", "Linear", "native", "Fit", "off"], PictureSettings.All.Select(s => s.DefaultText));
        Assert.Equal((new Picture(), 0), (PictureSettings.Current, PictureSettings.FrameLimit));

        Assert.Equal(["50%", "67%", "75%", "100%", "150%", "200%"], Find("render.scale").Choices);
        Assert.Equal(["Linear", "Nearest", "Sharp"], Find("render.filter").Choices);
        Assert.Equal(["native", "16:9", "16:10", "21:9", "4:3"], Find("render.aspect").Choices);
        Assert.Equal(["Fit", "Stretch"], Find("render.fit").Choices);
        Assert.Equal(["off", "30", "60", "120", "144", "240"], Find("render.fps_limit").Choices);

        using var harness = new EngineHarness(frames: 3);
        var seen = new List<string>();
        harness.OnContext(Stage.Update, ctx =>
        {
            if (seen.Count > 0) return;
            foreach (var line in new[] { "render.scale 50%", "render.filter sharp", "render.aspect 4:3", "render.fit stretch", "render.fps_limit 60" })
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
    public void AStartReadsTheRenderPageFromTheFileByEachValuesText()
    {
        Needs.Renderer();
        var path = _folder.File(SettingsPath.FileName);
        File.WriteAllText(path, string.Join('\n',
            "render.scale 67", "render.aspect 4:3", "render.fit STRETCH", "render.filter sharp", "render.fps_limit 144",
            "render.scale 300%", "render.aspect 4", "render.fps_limit fast", ""));
        SettingsPath.For = _ => path;

        WindowSettings.Open(Config.Windowed("Start", 640, 400), new WindowPlace(false, 0, 0, 640, 400, false));

        // Each refused line leaves the one before it standing, as the read of the whole file
        // leaves it as the app starts.
        Assert.Equal(new Picture { Scale = 0.67f, Aspect = (4f, 3f), Fit = PictureFit.Stretch, Filter = PictureFilter.Sharp }, PictureSettings.Current);
        Assert.Equal(144, PictureSettings.FrameLimit);
        Assert.Equal(["67%", "Sharp", "4:3", "Stretch", "144"], PictureSettings.All.Select(s => s.Text));
    }

    [Fact]
    public void AGamesPictureSetsThePageAndItsExactSizeHoldsTheScaleUnused()
    {
        PictureSettings.Reset();
        Render.Picture = new Picture { Scale = 0.5f, Filter = PictureFilter.Nearest, Aspect = (21f, 9f) };
        Assert.Equal(["50%", "Nearest", "21:9", "Fit", "off"], PictureSettings.All.Select(s => s.Text));
        Assert.Null(Find("render.scale").Locked);

        Render.Picture = Render.Picture with { Size = (640, 360) };
        Assert.Equal("The game fixes the render size at 640x360, so the scale waits until it lets the size go.", Find("render.scale").Locked);
        Assert.Equal(("50%", (640u, 360u)), (Find("render.scale").Text, Render.Picture.Size));
        Render.Picture = Render.Picture with { Size = null };
        Assert.Null(Find("render.scale").Locked);

        // A game's picture is not a player's, so the file keeps the player's choice.
        Assert.Equal("100%", Find("render.scale").SavedText);

        Assert.Throws<ArgumentOutOfRangeException>(() => Render.Picture = default);
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.Picture = new Picture { Scale = 2.5f });
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.Picture = new Picture { Size = (0, 360) });
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.Picture = new Picture { Aspect = (0f, 1f) });
        Assert.Throws<ArgumentOutOfRangeException>(() => Render.Picture = new Picture { Fit = (PictureFit)7 });
        Assert.Equal(new Picture { Scale = 0.5f, Filter = PictureFilter.Nearest, Aspect = (21f, 9f) }, Render.Picture);
    }
}
