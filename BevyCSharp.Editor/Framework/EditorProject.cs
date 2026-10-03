using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Saving and restoring everything the editor holds.
/// </summary>
/// <remarks>
/// Three files, because they answer different questions and are edited by different hands. The
/// scene is the thing being made, the layout is how one person likes to look at it, and the
/// settings are how they like it to behave. Anything that saves saves all three, since a person
/// pressing save means "keep what I have done".
/// </remarks>
public static class EditorProject
{
    /// <summary>Writes the scene and the arrangement of the panels.</summary>
    public static void Save(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var written = EditorScene.Save(world, EditorPaths.Scene);

        File.WriteAllText(EditorPaths.Settings, EditorSettings.Describe());

        Console.WriteLine(
            $"[editor] saved {written} entities and the settings to {EditorPaths.Assets}");
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
        if (File.Exists(EditorPaths.Settings))
        {
            EditorSettings.Restore(File.ReadAllText(EditorPaths.Settings));
        }
    }
}
