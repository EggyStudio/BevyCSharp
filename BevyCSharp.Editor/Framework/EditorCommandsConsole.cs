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
    /// <summary>
    /// Runs a menu command by its path, or lists what there is to run.
    /// </summary>
    /// <remarks>
    /// One command rather than one per row, so what the console offers stays a short list and the
    /// menu stays the one place the editor's commands are written down. Somebody who found a
    /// command in the menu can type it the next time, and a script can run anything a person can.
    /// </remarks>
    /// <param name="path">Which row to run, quoted when it has spaces in it.</param>
    [Command("do", "Runs a menu command by its path, or lists them: do Spawn/Cube")]
    internal static string Do(string path)
    {
        if (path.Length == 0)
        {
            var paths = new List<string>();

            foreach (var item in EditorMenu.All)
            {
                if (item.Kind is MenuKind.Separator or MenuKind.Submenu) continue;
                if (item.Run is null) continue;

                paths.Add(item.Path);
            }

            paths.Sort(StringComparer.Ordinal);

            return string.Join("\n", paths);
        }

        if (EditorMenu.Find(path) is not { Run: { } run }) return $"nothing at {path}";

        run(EditorShell.Ecs);
        return $"ran {path}";
    }

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


    /// <summary>Closes the editor.</summary>
    [Command("quit", "Closes the editor")]
    internal static string Quit()
    {
        EditorShell.Context?.Exit();
        return "closing";
    }
}
