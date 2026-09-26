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

    /// <summary>Opens a folder under the asset root in the assets panel.</summary>
    /// <remarks>
    /// What a person does by clicking a tile, reachable from the console and so from <c>bcs</c>.
    /// Checking a tile from outside the window means driving the editor to a particular folder, and
    /// it is the same call the tile makes.
    /// </remarks>
    [Command("assets.open", "Opens a folder in the assets panel: assets.open <folder>")]
    internal static string OpenAssets(string folder)
    {
        var path = folder.Trim();

        if (path.Length > 0 && !Directory.Exists(EditorAssets.Absolute(path)))
            return $"no folder called {path} under the asset root";

        EditorAssets.Enter(path);
        EditorShell.Open("Assets");

        return path.Length > 0 ? $"opened {path}" : "opened the asset root";
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
            names.Add(ComponentSchemas.For(id)?.Name ?? EditorText.Short(world.ComponentName(id)));
        }

        return names.Count == 0 ? "nothing" : string.Join(", ", names);
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


    /// <summary>Compiles a fragment of C# and runs it here.</summary>
    /// <remarks>
    /// What a catalog of commands cannot cover. The world is in scope as <c>world</c>, a fragment
    /// with no semicolon is an expression, and anything else has to return to say something:
    /// <c>eval world.All().Length</c>, or <c>eval var e = world.Spawn(); world.SetName(e, "x");
    /// return e.Index;</c>.
    /// </remarks>
    [Command("eval", "Runs a fragment of C# here: eval <expression or statements>")]
    internal static string Eval(string code) => EditorEval.Run(code);

    /// <summary>The same, from a file.</summary>
    [Command("eval.file", "Runs a file of C# here: eval.file <path>")]
    internal static string EvalFile(string path) => EditorEval.RunFile(path);

    /// <summary>Writes the world to a file.</summary>
    /// <remarks>
    /// The named entities and their described components, which make up an edit. Loading it back
    /// puts those values onto the entities of the same names, so it is a file of changes over a
    /// scene rather than the scene itself.
    /// </remarks>
    [Command("world.save", "Writes the named entities to a file: world.save <path>")]
    internal static string WorldSave(string path)
    {
        if (path.Length == 0) return "world.save <path>";

        var written = EditorWorld.Save(EditorShell.Ecs, path);
        return $"wrote {written} entities to {path}";
    }

    /// <summary>Reads one back.</summary>
    [Command("world.load", "Reads entity values back from a file: world.load <path>")]
    internal static string WorldLoad(string path)
    {
        if (path.Length == 0) return "world.load <path>";

        if (!File.Exists(path))
        {
            ConsoleHost.Fail("NO_SUCH_FILE", $"There is no file at {path}.");
            return $"no file at {path}";
        }

        var read = EditorWorld.Load(EditorShell.Ecs, path);
        return $"applied {read} entities from {path}";
    }

    /// <summary>Closes the editor.</summary>
    [Command("quit", "Closes the editor")]
    internal static string Quit()
    {
        EditorShell.Context?.Exit();
        return "closing";
    }
}
