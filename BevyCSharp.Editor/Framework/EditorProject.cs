using System.Text.Json.Serialization;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>How the editor's own files are read and written, without reflection.</summary>
[JsonSerializable(typeof(Dictionary<string, string>))]
internal sealed partial class EditorJson : JsonSerializerContext;

/// <summary>
/// Saving and restoring everything the editor holds.
/// </summary>
/// <remarks>
/// <para>
/// Two places, because they answer different questions and belong to different hands. The scene is
/// the thing being made and lives in the project. The settings, the arrangement of the panels among
/// them, are how one person likes the editor and live in that person's own directory
/// (<c>user://settings.json</c>, a <see cref="Persistent{T}"/>), so they follow the person rather than
/// the build output the editor runs from. Anything that saves saves both, since a person pressing
/// save means "keep what I have done".
/// </para>
/// <para>
/// Settings saved before they moved, a <c>settings.txt</c> in the assets, are read when there is no
/// settings file of the person's own yet, and the next save writes the new one.
/// </para>
/// </remarks>
public static class EditorProject
{
    private static Persistent<Dictionary<string, string>>? _settings;

    /// <summary>The settings file, made the first time it is asked for, once the app has named its directory.</summary>
    private static Persistent<Dictionary<string, string>> Settings =>
        _settings ??= new Persistent<Dictionary<string, string>>(
            "settings", EditorJson.Default.DictionaryStringString, static () => new(StringComparer.Ordinal));

    /// <summary>Writes the scene and the arrangement of the panels.</summary>
    public static void Save(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var written = EditorScene.Save(world, EditorPaths.Scene);

        Settings.Set(EditorSettings.Snapshot());
        Settings.Persist();

        Console.WriteLine(
            $"[editor] saved {written} entities to {EditorPaths.Assets} and the settings to {Settings.FullPath}");
    }

    /// <summary>Puts the saved scene and the saved arrangement back.</summary>
    /// <remarks>
    /// A project saved before the scene file existed has only a <c>world.json</c> of edits by name,
    /// which is applied the old way, and the next save writes the scene file in its place.
    /// </remarks>
    public static void Load(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (File.Exists(EditorPaths.Scene))
        {
            var loaded = EditorScene.Load(world, EditorPaths.Scene);
            RestoreLayout();

            Console.WriteLine($"[editor] loaded {loaded.Entities.Count} entities");
            foreach (var type in loaded.Unknown) Console.WriteLine($"[editor] kept {type} as written, since this build has no schema for it");
            foreach (var reason in loaded.Refused) Console.WriteLine($"[editor] Bevy refused {reason}");
            return;
        }

        var applied = File.Exists(EditorPaths.World) ? EditorWorld.Load(world, EditorPaths.World) : 0;
        RestoreLayout();

        Console.WriteLine($"[editor] applied {applied} entities from the old world file");
    }

    /// <summary>
    /// Restores the preferences, as starting up needs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Not the world, which is the project itself, and loading it is a thing a person asks for. How
    /// the editor behaves is not, and having to ask for it every time is how a tool feels like it
    /// does not remember you.
    /// </para>
    /// <para>
    /// The arrangement of the panels comes back with them, because where a panel sits and how wide
    /// it is are registered as settings like everything else a person sets.
    /// </para>
    /// </remarks>
    public static void RestoreLayout()
    {
        if (File.Exists(Settings.FullPath))
        {
            Settings.Revert();
            EditorSettings.Restore(Settings.Value);
        }
        else if (File.Exists(EditorPaths.Settings))
        {
            EditorSettings.Restore(File.ReadAllText(EditorPaths.Settings));
        }

        if (Settings.Problem is { } problem) Console.Error.WriteLine($"[editor] {problem}");
    }
}
