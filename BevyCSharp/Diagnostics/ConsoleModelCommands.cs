using System.Globalization;

namespace Bevy;

/// <summary>
/// The modeling commands, one for each operation of an edit mesh's recipe, so a level is blocked
/// out from the terminal, a line typed being a line of the model's recipe.
/// </summary>
/// <remarks>
/// <para>
/// Each takes the model's name first and then the operation's words as the recipe has them, so
/// <c>model.box room 8 3 6</c> runs <c>box 8 3 6</c> on the model called room, and each answers with
/// the model's counts after it, so a script reads what it did. A primitive, a join or a union makes
/// the model where there is none of the name, and any other operation on a name no model has is
/// refused, so a mistyped name makes no model.
/// </para>
/// <para>
/// The models are those <see cref="Modeling.Register"/> named as well, so a game's level answers to
/// them, and they last while the app runs. <c>model.show</c> draws one in its slots' prototype
/// materials and keeps drawing it as the commands change it.
/// </para>
/// </remarks>
internal static class ConsoleModelCommands
{
    /// <summary>Makes an empty model of a name, in place of one of that name.</summary>
    [Command("model.new", "Makes an empty model: model.new <name>")]
    internal static string New(string name)
    {
        if (!OneWord(name)) return Refuse("BAD_ARGUMENT", "model.new takes a name of one word.");
        Modeling.Register(name, new EditMesh());
        return $"{name} is an empty model";
    }

    /// <summary>Forgets a model, and takes away what draws it where it is shown.</summary>
    [Command("model.free", "Forgets a model and takes away what draws it: model.free <name>")]
    internal static string Free(string name)
    {
        if (Modeling.Entry(name) is not { } named) return Missing(name);

        // The world is asked for only where something draws the model, so a model made and
        // forgotten where no app runs, as in a script of the recipe's lines, needs none.
        if (!named.Root.IsNone) ConsoleHost.Ecs.Despawn(named.Root);
        Modeling.Forget(name);
        return $"forgot {name}";
    }

    /// <summary>Lists every model with its counts.</summary>
    [Command("model.list", "Lists the models with their counts")]
    internal static string List()
    {
        var names = Modeling.Names;
        return names.Count == 0 ? "no models" : string.Join("\n", names.Select(name => Counts(name, Modeling.Find(name)!)));
    }

    /// <summary>Says a model's counts, bounds, volume, open edges, slots, grid and recipe's length.</summary>
    [Command("model.info", "Says a model's counts, bounds, volume, open edges and slots: model.info <name>")]
    internal static string Info(string name)
    {
        if (Modeling.Find(name) is not { } mesh) return Missing(name);

        var (low, high) = mesh.Bounds;
        var culture = CultureInfo.InvariantCulture;
        return string.Join("\n",
            Counts(name, mesh),
            string.Create(culture, $"bounds ({low.X}, {low.Y}, {low.Z}) to ({high.X}, {high.Y}, {high.Z})"),
            string.Create(culture, $"volume {mesh.Volume} cubic meters, {mesh.OpenEdges} open edges, {(mesh.IsClosed ? "closed" : "open")}"),
            $"slots {string.Join(", ", mesh.Slots)}",
            string.Create(culture, $"grid {mesh.GridStep}, epsilon {mesh.BooleanEpsilon}, {mesh.Recipe.Count} lines of recipe"));
    }

    /// <summary>Prints a model's recipe as its file holds it, its header first.</summary>
    [Command("model.recipe", "Prints a model's recipe as its file holds it: model.recipe <name>")]
    internal static string Recipe(string name) =>
        Modeling.Find(name) is { } mesh ? mesh.RecipeText.TrimEnd('\n') : Missing(name);

    /// <summary>Takes back a model's last operation.</summary>
    [Command("model.undo", "Takes back a model's last operation: model.undo <name>")]
    internal static string Undo(string name)
    {
        if (Modeling.Find(name) is not { } mesh) return Missing(name);
        return mesh.Undo() ? Counts(name, mesh) : Refuse("NOTHING_TO_UNDO", $"{name} has no operation to take back.");
    }

    /// <summary>Does again the operation last taken back.</summary>
    [Command("model.redo", "Does again a model's operation last taken back: model.redo <name>")]
    internal static string Redo(string name)
    {
        if (Modeling.Find(name) is not { } mesh) return Missing(name);
        return mesh.Redo() ? Counts(name, mesh) : Refuse("NOTHING_TO_REDO", $"{name} has no operation taken back to do again.");
    }

    /// <summary>Writes a model's recipe to a file, under the asset root unless the path is full.</summary>
    [Command("model.save", "Writes a model's recipe to a file under the asset root: model.save <name> <path>")]
    internal static string Save(string line)
    {
        var (name, path) = NameAndRest(line);
        if (path.Length == 0) return Refuse("BAD_ARGUMENT", "model.save takes a model's name and a path to write it to.");
        if (Modeling.Find(name) is not { } mesh) return Missing(name);

        try
        {
            return $"wrote {name} to {mesh.Save(path)}";
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return Refuse("NOT_WRITTEN", $"{name} was not written to {path}: {error.Message}");
        }
    }

    /// <summary>Makes a model from a recipe's file, read as the game reads its assets.</summary>
    [Command("model.load", "Makes a model from a recipe's file: model.load <name> <path>")]
    internal static string Load(string line)
    {
        var (name, path) = NameAndRest(line);
        if (!OneWord(name) || path.Length == 0) return Refuse("BAD_ARGUMENT", "model.load takes a model's name of one word and the path of its file.");

        try
        {
            var mesh = EditMesh.Load(path);
            Modeling.Register(name, mesh);
            return Counts(name, mesh);
        }
        catch (FileNotFoundException)
        {
            return Refuse("NO_SUCH_FILE", $"There is no model at {path}.");
        }
        catch (FormatException error)
        {
            return Refuse("BAD_FILE", error.Message);
        }
    }

    /// <summary>Draws a model in its slots' prototype materials where a point says, and keeps drawing it as it changes.</summary>
    [Command("model.show", "Draws a model where a point says, following its edits: model.show <name> <x> <y> <z>")]
    internal static string Show(string line)
    {
        var (name, rest) = NameAndRest(line);
        var numbers = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var at = Vec3.Zero;
        if (numbers.Length != 0 && (numbers.Length != 3 || !Coordinate(numbers[0], out at.X) || !Coordinate(numbers[1], out at.Y) || !Coordinate(numbers[2], out at.Z)))
            return Refuse("BAD_ARGUMENT", "model.show takes a model's name and where to draw it, three numbers, or none for the origin.");

        if (Modeling.Entry(name) is not { } named) return Missing(name);
        if (!App.HasRenderer) return Refuse("NO_RENDERER", "This native bridge was built without Bevy's renderer, so nothing is drawn.");

        Modeling.Show(ConsoleHost.Ecs, named, at);
        return string.Create(CultureInfo.InvariantCulture, $"showing {name} at ({at.X}, {at.Y}, {at.Z})");
    }

    // One command for each line of the recipe, its words after the model's name, so the catalog,
    // bcs list and the editor's console each name every operation with what it takes.
    [Command("model.box", "Adds a box: model.box <name> <x> <y> <z> [at <x> <y> <z>] [center]")]
    internal static string Box(string line) => Run("box", line, makes: true);

    [Command("model.plane", "Adds a plane facing up: model.plane <name> <x> <z> [at <x> <y> <z>] [center]")]
    internal static string Plane(string line) => Run("plane", line, makes: true);

    [Command("model.cylinder", "Adds a cylinder: model.cylinder <name> <radius> <height> <sides> [at <x> <y> <z>] [center]")]
    internal static string Cylinder(string line) => Run("cylinder", line, makes: true);

    [Command("model.cone", "Adds a cone: model.cone <name> <radius> <height> <sides> [at <x> <y> <z>] [center]")]
    internal static string Cone(string line) => Run("cone", line, makes: true);

    [Command("model.wedge", "Adds a wedge rising along Z: model.wedge <name> <x> <y> <z> [at <x> <y> <z>] [center]")]
    internal static string Wedge(string line) => Run("wedge", line, makes: true);

    [Command("model.stairs", "Adds a flight of stairs: model.stairs <name> <steps> <rise> <run> <width> [at <x> <y> <z>] [center]")]
    internal static string Stairs(string line) => Run("stairs", line, makes: true);

    [Command("model.arch", "Adds an arch: model.arch <name> <radius> <thickness> <depth> <sides> [at <x> <y> <z>] [center]")]
    internal static string Arch(string line) => Run("arch", line, makes: true);

    [Command("model.room", "Adds a hollow room: model.room <name> <x> <y> <z> <thickness> [at <x> <y> <z>] [center]")]
    internal static string Room(string line) => Run("room", line, makes: true);

    [Command("model.ramp", "Adds a ramp: model.ramp <name> <rise> <run> <width> [at <x> <y> <z>] [center]")]
    internal static string Ramp(string line) => Run("ramp", line, makes: true);

    [Command("model.select", "Selects: model.select <name> all|none|invert|[add|remove] face <i>...|+x..-z|box <x0 y0 z0 x1 y1 z1>|slot <name>")]
    internal static string Select(string line) => Run("select", line);

    [Command("model.move", "Moves the selection: model.move <name> <x> <y> <z>")]
    internal static string Move(string line) => Run("move", line);

    [Command("model.rotate", "Turns the selection: model.rotate <name> x|y|z|<ax ay az> <degrees> [at <x> <y> <z>]")]
    internal static string Rotate(string line) => Run("rotate", line);

    [Command("model.scale", "Scales the selection: model.scale <name> <factor>|<x> <y> <z> [at <x> <y> <z>]")]
    internal static string Scale(string line) => Run("scale", line);

    [Command("model.grid", "Sets the grid step: model.grid <name> <step>")]
    internal static string Grid(string line) => Run("grid", line);

    [Command("model.snap", "Puts the selection on the grid: model.snap <name>")]
    internal static string Snap(string line) => Run("snap", line);

    [Command("model.material", "Puts the selected faces in a slot: model.material <name> <slot>")]
    internal static string Material(string line) => Run("material", line);

    [Command("model.clear", "Empties a model: model.clear <name>")]
    internal static string Clear(string line) => Run("clear", line);

    [Command("model.extrude", "Pushes the selected faces out: model.extrude <name> <distance>|<x> <y> <z>")]
    internal static string Extrude(string line) => Run("extrude", line);

    [Command("model.inset", "Insets each selected face: model.inset <name> <distance>")]
    internal static string Inset(string line) => Run("inset", line);

    [Command("model.delete", "Takes away the selection: model.delete <name> [fill]")]
    internal static string Delete(string line) => Run("delete", line);

    [Command("model.merge", "Joins the selected vertices within a distance: model.merge <name> <distance>")]
    internal static string Merge(string line) => Run("merge", line);

    [Command("model.flip", "Turns the selected faces the other way: model.flip <name>")]
    internal static string Flip(string line) => Run("flip", line);

    [Command("model.triangulate", "Cuts the selected faces into triangles: model.triangulate <name>")]
    internal static string Triangulate(string line) => Run("triangulate", line);

    [Command("model.duplicate", "Copies the selected faces in place: model.duplicate <name>")]
    internal static string Duplicate(string line) => Run("duplicate", line);

    [Command("model.join", "Adds another model from its file: model.join <name> <path>")]
    internal static string Join(string line) => Run("join", line, makes: true);

    [Command("model.shade", "Draws the selected faces smoothed or flat: model.shade <name> smooth [degrees]|flat")]
    internal static string Shade(string line) => Run("shade", line);

    [Command("model.union", "Adds another model's solid: model.union <name> <path> [at <x> <y> <z>]")]
    internal static string Union(string line) => Run("union", line, makes: true);

    [Command("model.subtract", "Takes another model's solid away: model.subtract <name> <path> [at <x> <y> <z>]")]
    internal static string Subtract(string line) => Run("subtract", line);

    [Command("model.intersect", "Keeps what both solids hold: model.intersect <name> <path> [at <x> <y> <z>]")]
    internal static string Intersect(string line) => Run("intersect", line);

    [Command("model.cut", "Cuts: model.cut <name> window <w> <h> <along> <sill>|door <w> <h> <along>|<a solid primitive's line>")]
    internal static string Cut(string line) => Run("cut", line);

    [Command("model.epsilon", "Sets how near a point is to a plane to lie in it: model.epsilon <name> <distance>")]
    internal static string Epsilon(string line) => Run("epsilon", line);

    /// <summary>Runs an operation's line on a model, made first where the operation makes one, and answers its counts.</summary>
    private static string Run(string verb, string line, bool makes = false)
    {
        var (name, rest) = NameAndRest(line);
        if (!OneWord(name)) return Refuse("BAD_ARGUMENT", $"model.{verb} takes a model's name first.");

        var mesh = Modeling.Find(name);
        if (mesh is null)
        {
            if (!makes) return Missing(name);
            Modeling.Register(name, mesh = new EditMesh());
        }

        try
        {
            mesh.Run(rest.Length == 0 ? verb : $"{verb} {rest}");
        }
        catch (Exception error) when (error is FormatException or ArgumentException or InvalidOperationException or IOException)
        {
            return Refuse("BAD_ARGUMENT", $"{verb} on {name}: {error.Message}");
        }

        return Counts(name, mesh);
    }

    /// <summary>What a model holds and what of it is selected, the answer of every operation.</summary>
    private static string Counts(string name, EditMesh mesh) =>
        $"{name}: {mesh.VertexCount} vertices, {mesh.EdgeCount} edges, {mesh.FaceCount} faces, "
        + $"{mesh.SelectedFaceCount} faces and {mesh.SelectedVertexCount} vertices selected";

    private static (string Name, string Words) NameAndRest(string line)
    {
        var split = ConsoleWorldCommands.FirstAndRest(line);
        return split.Length switch
        {
            0 => ("", ""),
            1 => (split[0], ""),
            _ => (split[0], split[1].Trim()),
        };
    }

    private static bool OneWord(string name) => name.Length > 0 && !name.Any(char.IsWhiteSpace);

    private static bool Coordinate(string word, out float value) =>
        float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out value) && float.IsFinite(value);

    private static string Missing(string name) => Refuse("NO_SUCH_MODEL", $"There is no model called {name}. model.list says which there are.");

    private static string Refuse(string code, string message)
    {
        ConsoleHost.Fail(code, message);
        return message;
    }
}
