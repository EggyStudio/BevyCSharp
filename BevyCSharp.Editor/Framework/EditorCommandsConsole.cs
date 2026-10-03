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

    /// <summary>Plays the scene being edited in the player, or stops what is playing.</summary>
    [Command("play.scene", "Plays the scene being edited in a window of its own, or stops what is playing")]
    internal static string PlayScene() =>
        EditorPlay.PlayScene() ?? (EditorPlay.Running ? $"playing {EditorPlay.PlayedScene}" : "stopped");

    /// <summary>Exports the project as the Play tab's Export does, for a runtime identifier.</summary>
    [Command("project.export", "Exports the project for a platform, with its assets in the bridge if asked: project.export <rid> [embed]")]
    internal static string Export(string line)
    {
        // One line rather than two arguments, so both words may be left out. A bare export is for
        // this machine, with the assets as files.
        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var target = words.FirstOrDefault(word => word != "embed") ?? EditorPlay.Targets[0];
        var embedding = words.Contains("embed");

        return EditorPlay.Export(target, embedding)
               ?? $"exporting to {EditorPlay.ExportFolder(target)}; the Play tab follows it";
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
    [Command("assets.open", "Opens a folder, or a model's parts, in the assets panel: assets.open <folder or model>")]
    internal static string OpenAssets(string folder)
    {
        var path = folder.Trim();

        if (path.Length > 0 && !Directory.Exists(EditorAssets.Absolute(path)) && !EditorAssets.IsModel(path))
            return $"no folder or model called {path} under the asset root";

        EditorAssets.Enter(path);
        EditorShell.Open("Assets");

        return path.Length > 0 ? $"opened {path}" : "opened the asset root";
    }

    /// <summary>Renames or moves a file or a folder under the asset root, with its id.</summary>
    /// <remarks>What dragging a tile onto a folder and the tile's Rename do, as the same call.</remarks>
    [Command("assets.move", "Renames or moves a file or folder with its id: assets.move <from> <to>")]
    internal static string MoveAsset(string from, string to) =>
        EditorAssets.Move(from, to) ?? $"moved {from.Trim()} to {to.Trim()}";

    /// <summary>Deletes a file and its id, or a folder and what is in it.</summary>
    /// <remarks>The tile's Delete, without the question it asks first, since a command is asked on purpose.</remarks>
    [Command("assets.delete", "Deletes a file with its id, or a folder: assets.delete <path>")]
    internal static string DeleteAsset(string path) =>
        EditorAssets.Delete(path) ?? $"deleted {path.Trim()}";

    /// <summary>Places a model in the scene as an instance.</summary>
    /// <remarks>What a model tile's Place in the scene does, as the same call.</remarks>
    [Command("scene.place", "Places a model as an instance: scene.place <path under assets>")]
    internal static string PlaceModel(string path)
    {
        var file = path.Trim();
        if (!File.Exists(EditorAssets.Absolute(file.Split('#')[0]))) return $"no file called {file} under the asset root";

        var root = EditorCommands.Place(EditorShell.Ecs, file);
        return $"placed {EditorShell.Ecs.NameOf(root)} from {file}";
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

    /// <summary>Writes the scene being edited to a scene file.</summary>
    /// <remarks>
    /// The scene and not the editor, so the editor's own cameras and previews are left out, as
    /// Project/Save leaves them out.
    /// </remarks>
    [Command("world.save", "Writes the scene being edited to a scene file: world.save <path>")]
    internal static string WorldSave(string path)
    {
        if (path.Length == 0) return "world.save <path>";

        var written = EditorScene.Save(EditorShell.Ecs, path);
        return $"wrote {written} entities to {path}";
    }

    /// <summary>Replaces the scene being edited with the one in a scene file.</summary>
    [Command("world.load", "Replaces the scene being edited with a scene file: world.load <path>")]
    internal static string WorldLoad(string path)
    {
        if (path.Length == 0) return "world.load <path>";

        if (!File.Exists(SceneFile.Resolve(path)))
        {
            ConsoleHost.Fail("NO_SUCH_FILE", $"There is no file at {path}.");
            return $"no file at {path}";
        }

        var loaded = EditorScene.Load(EditorShell.Ecs, path);
        return $"loaded {loaded.Entities.Count} entities from {path}";
    }

    /// <summary>Closes the editor.</summary>
    [Command("quit", "Closes the editor")]
    internal static string Quit()
    {
        EditorShell.Context?.Exit();
        return "closing";
    }
}
