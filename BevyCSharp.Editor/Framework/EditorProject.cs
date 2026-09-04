using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Saving and restoring everything the editor holds.
/// </summary>
/// <remarks>
/// Two files, because they answer different questions and are edited by different hands: the world
/// is what the thing being made is, and the layout is how one person likes to look at it. Anything
/// that saves saves both, since a person pressing save means "keep what I have done".
/// </remarks>
public static class EditorProject
{
    /// <summary>Writes the world's edits and the arrangement of the panels.</summary>
    public static void Save(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var written = EditorWorld.Save(world, EditorPaths.World);
        File.WriteAllText(EditorPaths.Layout, EditorShell.Layout.Describe());

        Console.WriteLine($"[editor] saved {written} entities and the layout to {EditorPaths.Assets}");
    }

    /// <summary>Puts the saved edits and the saved arrangement back.</summary>
    public static void Load(EcsWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var applied = EditorWorld.Load(world, EditorPaths.World);
        RestoreLayout();

        Console.WriteLine($"[editor] applied {applied} entities and the layout");
    }

    /// <summary>Restores only the arrangement, which is what starting up wants.</summary>
    public static void RestoreLayout()
    {
        if (!File.Exists(EditorPaths.Layout)) return;

        EditorShell.Layout.Restore(File.ReadAllText(EditorPaths.Layout));
    }
}
