using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers a project's own settings in <c>project.json</c>, written and read back, and an app
/// taking its fixed step from them.
/// </summary>
/// <remarks>
/// In the engine collection, since one test runs an app, and the asset root it reads is static.
/// </remarks>
[Collection("engine")]
public sealed class ProjectSettingsTests : IDisposable
{
    private readonly TestFolder _folder = new("bcs-project-");

    public void Dispose() => _folder.Dispose();

    private string File(string text)
    {
        var path = Path.Combine(_folder.Path, ProjectSettings.FileName);
        System.IO.File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void SettingsWrittenAreReadBackAndDefaultsAreLeftOut()
    {
        var path = Path.Combine(_folder.Path, ProjectSettings.FileName);

        new ProjectSettings
        {
            StartupScene = "levels/first.scene.json",
            Title = "Courtyard",
            Icon = "icons/courtyard.png",
            FixedHz = 30,
            ExportTarget = "linux-x64",
            ExportAssets = "pack",
            Theme = "name\tShipped",
        }.Write(path);

        var read = ProjectSettings.ReadFrom(_folder.Path);
        Assert.Equal("levels/first.scene.json", read.StartupScene);
        Assert.Equal("Courtyard", read.Title);
        Assert.Equal("icons/courtyard.png", read.Icon);
        Assert.Equal(30d, read.FixedHz);
        Assert.Equal("linux-x64", read.ExportTarget);
        Assert.Equal("pack", read.ExportAssets);
        Assert.Equal("name\tShipped", read.Theme);

        new ProjectSettings().Write(path);
        Assert.DoesNotContain("fixedHz", System.IO.File.ReadAllText(path));
        Assert.Contains(ProjectSettings.Format, System.IO.File.ReadAllText(path));
    }

    [Fact]
    public void NoFileIsEveryDefaultAndAnotherFileIsTheDefaultsWithWhy()
    {
        var none = ProjectSettings.ReadFrom(_folder.Path);
        Assert.Null(none.StartupScene);
        Assert.Equal(0d, none.FixedHz);
        Assert.Null(none.Problem);

        File("""{ "format": "bevycsharp.scene.1", "fixedHz": 30 }""");
        var other = ProjectSettings.ReadFrom(_folder.Path);
        Assert.Equal(0d, other.FixedHz);
        Assert.Contains(ProjectSettings.FileName, other.Problem);

        File("not json at all");
        Assert.Contains(ProjectSettings.FileName, ProjectSettings.ReadFrom(_folder.Path).Problem);

        // The text alone throws, for a tool that asks.
        Assert.Throws<InvalidDataException>(() => ProjectSettings.Parse("not json at all"));
    }

    [Fact]
    public void ANewerFileIsReadAsFarAsItsFieldsMatch()
    {
        File("""{ "format": "bevycsharp.project.2", "startupScene": "intro.scene.json", "somethingNew": [1, 2] }""");

        Assert.Equal("intro.scene.json", ProjectSettings.ReadFrom(_folder.Path).StartupScene);
    }

    /// <summary>
    /// A window is titled after the project where the config leaves its title at the default, and
    /// after the config where it names one or the project has none.
    /// </summary>
    [Fact]
    public void AWindowTakesTheProjectsTitleWhereTheConfigLeavesIt()
    {
        var titled = new ProjectSettings { Title = "Courtyard" };

        Assert.Equal("Courtyard", App.TitleFor(new Config(), titled));
        Assert.Equal("Arena", App.TitleFor(new Config { Title = "Arena" }, titled));
        Assert.Equal(Config.DefaultTitle, App.TitleFor(new Config(), new ProjectSettings()));
    }

    [Fact]
    public void AnAppTakesItsFixedStepFromTheProjectWhereTheConfigLeavesIt()
    {
        File("""{ "format": "bevycsharp.project.1", "fixedHz": 25, "startupScene": "start.scene.json" }""");

        var fixedDelta = 0f;
        string? startup = null;

        using (var app = new App(new Config { Headless = true, HeadlessFrames = 3, AssetRoot = _folder.Path }))
        {
            startup = app.Project.StartupScene;
            app.AddSystem(Stage.Update, new SystemDescriptor(world => fixedDelta = world.Resource<Time>().FixedDelta, "Test.FixedDelta"));
            Assert.Equal(0, app.Run());
        }

        Assert.Equal("start.scene.json", startup);
        Assert.Equal(0.04f, fixedDelta, 5);
    }
}
