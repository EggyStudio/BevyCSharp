using System.Text.Json.Serialization;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A game's settings, as a record a settings screen changes one field of at a time.</summary>
public sealed record Settings(float Volume = 1f, bool Fullscreen = false, string Language = "en");

/// <summary>How the settings are read and written, without reflection.</summary>
[JsonSerializable(typeof(Settings))]
internal sealed partial class SettingsJson : JsonSerializerContext;

/// <summary>
/// Covers a value kept in the player's own directory between runs, and the <c>user://</c> root it
/// is kept under.
/// </summary>
/// <remarks>
/// No engine, but the directory is static and an app sets the game's name as it starts, so these
/// share the engine collection and point the directory somewhere of their own.
/// </remarks>
[Collection("engine")]
public sealed class PersistentTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "bcs-user-" + Guid.NewGuid().ToString("n"));

    public PersistentTests() => UserData.Root = _root;

    public void Dispose()
    {
        UserData.Root = EngineHarness.UserDirectory;
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void AValueIsTheDefaultUntilWrittenAndThenWhatWasWritten()
    {
        var settings = new Persistent<Settings>("settings", SettingsJson.Default.Settings, () => new Settings());

        Assert.Equal(new Settings(), settings.Value);
        Assert.Null(settings.Problem);
        Assert.False(File.Exists(settings.FullPath));
        Assert.Equal(Path.Combine(_root, "settings.json"), settings.FullPath);

        settings.Update(value => value with { Volume = 0.25f });
        Assert.True(settings.Changed);
        settings.Persist();
        Assert.False(settings.Changed);

        // Read again as the next run would.
        var again = new Persistent<Settings>("settings", SettingsJson.Default.Settings, () => new Settings());
        Assert.Equal(0.25f, again.Value.Volume);

        // A change not written is thrown away by a revert.
        again.Set(again.Value with { Language = "de" });
        again.Revert();
        Assert.Equal("en", again.Value.Language);

        // And a reset writes the default back.
        again.Reset();
        Assert.Equal(1f, new Persistent<Settings>("settings", SettingsJson.Default.Settings, () => new Settings()).Value.Volume);
    }

    [Fact]
    public void AFileThatCannotBeReadGivesTheDefaultAndIsLeftAlone()
    {
        Directory.CreateDirectory(_root);
        var file = Path.Combine(_root, "broken.json");
        File.WriteAllText(file, "{ not json");

        var settings = new Persistent<Settings>("broken", SettingsJson.Default.Settings, () => new Settings(Volume: 0.5f));

        Assert.Equal(0.5f, settings.Value.Volume);
        Assert.NotNull(settings.Problem);
        Assert.Equal("{ not json", File.ReadAllText(file));
    }

    [Fact]
    public void UserPathsResolveUnderTheDirectory()
    {
        Assert.Equal(Path.Combine(_root, "saves", "one.json"), UserData.Resolve("user://saves/one.json"));
        Assert.Equal(Path.Combine(_root, "saves", "one.scene.json"), SceneFile.Resolve("user://saves/one.scene.json"));

        // The platform's own when nothing is set, under the game's name.
        UserData.Root = null!;
        UserData.Name = "Some/Game";
        Assert.EndsWith("Some_Game", UserData.Root);
        UserData.Root = _root;
    }

    [Fact]
    public void BevyLoadsAFileTheGameWroteUnderUser()
    {
        // A picture the game wrote, as a screenshot or a painted decal would be.
        Directory.CreateDirectory(Path.Combine(_root, "shots"));
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "assets", "textures", "checker.png"),
            Path.Combine(_root, "shots", "checker.png"));

        using var harness = new EngineHarness(frames: 120, fps: 240);
        if (!App.HasRenderer) return;

        var image = AssetHandle.None;
        var state = AssetLoadState.Unknown;
        var failures = new List<AssetLoadFailed>();

        harness.OnContext(Stage.Startup, ctx => image = AssetServer.Load(AssetKind.Image, "user://shots/checker.png"));

        harness.OnContext(Stage.Update, ctx =>
        {
            foreach (var failure in ctx.Read<AssetLoadFailed>()) failures.Add(failure);

            state = image.State;
            if (state is AssetLoadState.Loaded or AssetLoadState.Failed) ctx.Exit();
        });

        harness.Run();

        Assert.Empty(failures);
        Assert.Equal(AssetLoadState.Loaded, state);
    }
}
