using System.Text.Json;

namespace Bevy;

/// <summary>
/// What a project says about itself rather than about one scene, kept in <c>project.json</c> at
/// the root of its assets.
/// </summary>
/// <remarks>
/// <para>
/// A project has several scenes and one of each of these, so they are a file of their own beside
/// the scenes rather than a part of one. They are which scene a game starts in, how fast its fixed
/// step runs, how the editor last exported it, and the editor's look when it was asked to ship
/// with the project. Read through <see cref="AssetFiles"/>, so a game carrying its assets in its assembly or
/// a pack carries this too, and written by the editor's Project settings.
/// </para>
/// <para>
/// Versioned as data assets are, by a <c>format</c> line. A file of a newer version is read as far
/// as its fields match, and one with no file at all is a project with every setting at its default,
/// which is what an app run from a folder with nothing in it should get.
/// </para>
/// </remarks>
public sealed class ProjectSettings
{
    /// <summary>The file's name, at the root of the assets.</summary>
    public const string FileName = "project.json";

    /// <summary>The format the file is written in.</summary>
    public const string Format = "bevycsharp.project.1";

    /// <summary>The scene a game starts in, relative to the assets, or nothing to leave it to the game.</summary>
    public string? StartupScene { get; set; }

    /// <summary>How many times a second the fixed step runs, or zero for Bevy's own sixty-four.</summary>
    /// <remarks>An app takes it where its <see cref="Config.FixedHz"/> is left at zero.</remarks>
    public double FixedHz { get; set; }

    /// <summary>The platform the editor last exported for, such as <c>linux-x64</c>, or nothing.</summary>
    public string? ExportTarget { get; set; }

    /// <summary>How the editor last shipped the assets, <c>files</c>, <c>assembly</c> or <c>pack</c>, or nothing.</summary>
    public string? ExportAssets { get; set; }

    /// <summary>The editor's theme as its lines of text, when it was asked to ship with the project.</summary>
    public string? Theme { get; set; }

    /// <summary>The settings in the assets an app reads, or every one at its default when there is no file.</summary>
    /// <exception cref="InvalidDataException">The file is there and is not a project file.</exception>
    public static ProjectSettings Read() =>
        AssetFiles.Exists(FileName) ? Parse(AssetFiles.ReadAllText(FileName), FileName) : new ProjectSettings();

    /// <summary>The settings in a folder of assets on disk, for a tool reading them before any app exists.</summary>
    /// <param name="assets">The folder.</param>
    /// <exception cref="InvalidDataException">The file is there and is not a project file.</exception>
    public static ProjectSettings ReadFrom(string assets)
    {
        var path = Path.Combine(assets, FileName);
        return File.Exists(path) ? Parse(File.ReadAllText(path), path) : new ProjectSettings();
    }

    /// <summary>Reads settings from the text of a project file.</summary>
    /// <exception cref="InvalidDataException">The text is not a project file.</exception>
    public static ProjectSettings Parse(string text) => Parse(text, "The text");

    /// <summary>Reads settings from the text of a project file, naming where it came from in what it throws.</summary>
    private static ProjectSettings Parse(string text, string from)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("format", out var format)
                || format.ValueKind != JsonValueKind.String
                || format.GetString() is not { } named
                || !named.StartsWith("bevycsharp.project.", StringComparison.Ordinal))
                throw new InvalidDataException($"{from} is not a project file in the {Format} format.");

            return new ProjectSettings
            {
                StartupScene = Text(root, "startupScene"),
                FixedHz = root.TryGetProperty("fixedHz", out var hz) && hz.TryGetDouble(out var rate) ? rate : 0d,
                ExportTarget = Text(root, "exportTarget"),
                ExportAssets = Text(root, "exportAssets"),
                Theme = Text(root, "theme"),
            };
        }
        catch (JsonException error)
        {
            throw new InvalidDataException($"{from} is not a project file, since it is not JSON: {error.Message}", error);
        }
    }

    /// <summary>Writes the settings to a project file, leaving out the ones at their default.</summary>
    /// <param name="path">Where, usually <see cref="FileName"/> in the project's assets.</param>
    /// <remarks>
    /// Through a file beside it renamed over the old one, so a crash while writing leaves the last
    /// one whole, and with the settings in a fixed order so the file diffs as what changed.
    /// </remarks>
    public void Write(string path)
    {
        var temporary = path + ".tmp";

        using (var stream = File.Create(temporary))
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("format", Format);
            if (StartupScene is { Length: > 0 }) json.WriteString("startupScene", StartupScene);
            if (FixedHz > 0) json.WriteNumber("fixedHz", FixedHz);
            if (ExportTarget is { Length: > 0 }) json.WriteString("exportTarget", ExportTarget);
            if (ExportAssets is { Length: > 0 }) json.WriteString("exportAssets", ExportAssets);
            if (Theme is { Length: > 0 }) json.WriteString("theme", Theme);
            json.WriteEndObject();
        }

        File.Move(temporary, path, overwrite: true);
    }

    private static string? Text(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
