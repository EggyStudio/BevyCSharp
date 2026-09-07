using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the editor offers the console.
/// </summary>
/// <remarks>
/// The commands that are about this program rather than about consoles in general. Each is a
/// static method with an attribute on it, found at compile time, so adding one is writing one and
/// nothing has to be registered anywhere.
/// </remarks>
internal static class EditorConsoleCommands
{
    /// <summary>Says what is selected.</summary>
    [Command("selection", "Says what is selected")]
    internal static string Selection()
    {
        if (!EditorSelection.Any) return "nothing selected";

        var world = EditorShell.Ecs;
        var names = EditorSelection.All.Select(entity =>
            world.NameOf(entity) is { Length: > 0 } named ? named : $"entity {entity.Index}");

        return string.Join(", ", names);
    }

    /// <summary>Selects an entity by name.</summary>
    [Command("select", "Selects the first entity with a name: select <name>")]
    internal static string Select(string name)
    {
        if (name.Length == 0) return "select <name>";

        var world = EditorShell.Ecs;

        foreach (var entity in world.All())
        {
            if (world.NameOf(entity) != name) continue;

            EditorSelection.Select(entity);
            return $"selected {name}";
        }

        return $"no entity called {name}";
    }

    /// <summary>Counts what is in the world.</summary>
    [Command("entities", "Counts the entities in the world")]
    internal static string Entities()
    {
        var world = EditorShell.Ecs;
        var total = 0;
        var chrome = 0;

        foreach (var entity in world.All())
        {
            total++;
            if (EditorEntity.IsInterface(world, entity)) chrome++;
        }

        return $"{total} entities, of which {chrome} are the interface";
    }

    /// <summary>Lists what a selected entity carries.</summary>
    [Command("components", "Lists what the selection carries")]
    internal static string Components()
    {
        if (!EditorSelection.Any) return "nothing selected";

        var world = EditorShell.Ecs;
        var names = new List<string>();

        foreach (var id in world.ComponentsOf(EditorSelection.Current))
        {
            names.Add(ComponentSchemas.For(id)?.Name ?? Short(world.ComponentName(id)));
        }

        return names.Count == 0 ? "nothing" : string.Join(", ", names);
    }

    /// <summary>
    /// The last part of a Rust path, which is the name somebody would recognise.
    /// </summary>
    /// <remarks>
    /// A console answers in one line, and a line of thirteen fully qualified paths is one nobody
    /// reads to the end of.
    /// </remarks>
    private static string Short(string name)
    {
        var generic = name.IndexOf('<');
        var bare = generic < 0 ? name : name[..generic];

        var cut = bare.LastIndexOf("::", StringComparison.Ordinal);
        return cut < 0 ? bare : bare[(cut + 2)..];
    }

    /// <summary>Takes back the last change.</summary>
    [Command("undo", "Takes back the last change")]
    internal static string Undo()
    {
        var last = EditorHistory.Last;
        EditorHistory.Undo(EditorShell.Ecs);

        return last is { Length: > 0 } ? $"took back {last}" : "nothing to take back";
    }

    /// <summary>Puts back what undo took.</summary>
    [Command("redo", "Puts back what undo took")]
    internal static string Redo()
    {
        EditorHistory.Redo(EditorShell.Ecs);
        return "redone";
    }

    /// <summary>Says what is on screen.</summary>
    [Command("panels", "Lists the panels that are open")]
    internal static string OpenPanels()
    {
        var names = EditorShell.Open
            .Where(EditorShell.IsShowing)
            .Select(panel => panel.GetType().Name);

        var listed = string.Join(", ", names);
        return listed.Length == 0 ? "none" : listed;
    }

    /// <summary>Closes the editor.</summary>
    [Command("quit", "Closes the editor")]
    internal static string Quit()
    {
        EditorShell.Context?.Exit();
        return "closing";
    }
}
