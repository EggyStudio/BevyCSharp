using System.Text.Json.Serialization;
using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A game's settings, as a record a settings screen changes one field of at a time.</summary>
public sealed record Settings(float Volume = 1f, bool Fullscreen = false, string Language = "en");

/// <summary>
/// A game's settings as a record of properties with defaults of their own, the shape a settings
/// screen's <c>with</c> changes, which gains a field from one version of a game to the next.
/// </summary>
public sealed record GrownSettings
{
    /// <summary>A field every version had.</summary>
    public float Volume { get; init; } = 1f;

    /// <summary>A field a later version added.</summary>
    public float Brightness { get; init; } = 0.75f;

    /// <summary>Another, of another type.</summary>
    public string Language { get; init; } = "en";
}

/// <summary>How the settings are read and written, without reflection.</summary>
[JsonSerializable(typeof(Settings))]
[JsonSerializable(typeof(GrownSettings))]
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
    private readonly TestFolder _folder = new("bcs-user-");
    private readonly string _root;

    public PersistentTests()
    {
        _root = _folder.Path;
        UserData.Root = _root;
    }

    public void Dispose()
    {
        UserData.Root = EngineHarness.UserDirectory;
        _folder.Dispose();
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

    /// <summary>
    /// A file written before a field was added keeps the field's own default rather than its
    /// type's zero, with what the file holds read over it, and so does a save carrying the value.
    /// </summary>
    /// <remarks>
    /// A source-generated reader gives an init-only property a file leaves out the zero of its
    /// type, so settings that gained a field read it as nothing from every file a player already
    /// had. The feature test's effects page found it, its tonemapper coming back as none.
    /// </remarks>
    [Fact]
    public void AFieldAFileWasWrittenWithoutKeepsItsDefault()
    {
        File.WriteAllText(_folder.File("grown.json"), """{ "Volume": 0.25 }""");

        var settings = new Persistent<GrownSettings>("grown", SettingsJson.Default.GrownSettings, () => new GrownSettings());

        Assert.Null(settings.Problem);
        Assert.Equal(0.25f, settings.Value.Volume);
        Assert.Equal(0.75f, settings.Value.Brightness);
        Assert.Equal("en", settings.Value.Language);

        // As a save game hands the value back what it carried.
        using var carried = System.Text.Json.JsonDocument.Parse("""{ "Language": "de" }""");
        Assert.True(((IPersistentValue)settings).Read(carried.RootElement));
        Assert.Equal(("de", 1f, 0.75f), (settings.Value.Language, settings.Value.Volume, settings.Value.Brightness));
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

    [SkippableFact]
    public void BevyLoadsAFileTheGameWroteUnderUser()
    {
        // A picture the game wrote, as a screenshot or a painted decal would be.
        Directory.CreateDirectory(Path.Combine(_root, "shots"));
        File.Copy(
            Path.Combine(AppContext.BaseDirectory, "assets", "textures", "checker.png"),
            Path.Combine(_root, "shots", "checker.png"));

        using var harness = new EngineHarness(frames: 120, fps: 240);
        Needs.Renderer();

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
