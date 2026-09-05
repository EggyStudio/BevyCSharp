using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Saving and restoring everything the editor holds.
/// </summary>
/// <remarks>
/// Three files, because they answer different questions and are edited by different hands: the
/// world is what the thing being made is, the layout is how one person likes to look at it, and the
/// settings are how they like it to behave. Anything that saves saves all three, since a person
/// pressing save means "keep what I have done".
/// </remarks>
public static class EditorProject
{
    /// <summary>Writes the world's edits and the arrangement of the panels.</summary>
    public static void Save(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var written = EditorWorld.Save(world, EditorPaths.World);

        File.WriteAllText(EditorPaths.Layout, EditorShell.Layout.Describe());
        File.WriteAllText(EditorPaths.Settings, EditorSettings.Describe());

        Console.WriteLine(
            $"[editor] saved {written} entities, the layout and the settings to {EditorPaths.Assets}");
    }

    /// <summary>Puts the saved edits and the saved arrangement back.</summary>
    public static void Load(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var applied = EditorWorld.Load(world, EditorPaths.World);
        RestoreLayout();

        Console.WriteLine($"[editor] applied {applied} entities and the layout");
    }

    /// <summary>
    /// Restores the arrangement and the preferences, which is what starting up wants.
    /// </summary>
    /// <remarks>
    /// Not the world: the world is what the project is, and loading it is a thing a person asks
    /// for. How the editor looks and behaves is not, and having to ask for it every time is how a
    /// tool feels like it does not remember you.
    /// </remarks>
    public static void RestoreLayout()
    {
        if (File.Exists(EditorPaths.Layout))
        {
            EditorShell.Layout.Restore(File.ReadAllText(EditorPaths.Layout));
        }

        if (File.Exists(EditorPaths.Settings))
        {
            EditorSettings.Restore(File.ReadAllText(EditorPaths.Settings));
        }
    }
}
