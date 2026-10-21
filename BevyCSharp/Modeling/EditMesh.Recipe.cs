using System.Globalization;
using System.Text;

namespace Bevy;

// The recipe, the operations that made the mesh as lines of text, which is its history, its undo
// and its file. Every operation writes its own line once it has done what it says, in the words
// the console's model commands take, so a line typed is a line of the recipe, and running the
// lines from an empty mesh makes the mesh again.
public sealed partial class EditMesh
{
    /// <summary>What a recipe's file name ends in.</summary>
    public const string Extension = ".model";

    /// <summary>The line a recipe's file starts with, its form and the form's version.</summary>
    public const string Header = "model 1";

    private readonly List<string> _recipe = [];
    private readonly Stack<string> _undone = new();
    private bool _replaying;

    /// <summary>The operations that made the mesh, a line each, the oldest first.</summary>
    public IReadOnlyList<string> Recipe => _recipe;

    /// <summary>The recipe as the text of its file, its header first.</summary>
    public string RecipeText
    {
        get
        {
            var text = new StringBuilder(Header).Append('\n');
            foreach (var line in _recipe) text.Append(line).Append('\n');
            return text.ToString();
        }
    }

    /// <summary>Whether there is an operation to undo.</summary>
    public bool CanUndo => _recipe.Count > 0;

    /// <summary>Whether there is an undone operation to do again.</summary>
    public bool CanRedo => _undone.Count > 0;

    /// <summary>Takes back the last operation, by making the mesh again from the lines before it.</summary>
    /// <returns>Whether there was one to take back.</returns>
    /// <remarks>
    /// The mesh is made again from nothing rather than the last operation reversed, so an undo is
    /// right for every operation by the same code that made it, and costs what the recipe costs to
    /// run, which for a level's model is a few milliseconds. The selection is the one the lines
    /// left, since selecting is a line of the recipe too.
    /// </remarks>
    public bool Undo()
    {
        if (_recipe.Count == 0) return false;

        var lines = _recipe.ToArray();
        _undone.Push(lines[^1]);
        Rebuild(lines[..^1]);
        return true;
    }

    /// <summary>Does again the operation last taken back.</summary>
    /// <returns>Whether there was one to do again.</returns>
    /// <remarks>An operation done after an undo forgets what was taken back, as a modeling program's history does.</remarks>
    public bool Redo()
    {
        if (!_undone.TryPop(out var line)) return false;

        _replaying = true;
        try
        {
            Run(line);
        }
        finally
        {
            _replaying = false;
        }

        return true;
    }

    /// <summary>Runs one line of a recipe on the mesh, as though its operation were called.</summary>
    /// <param name="line">The line, such as <c>box 8 3 6</c> or <c>select +y</c>, with a comment after <c>#</c> left out.</param>
    /// <returns>This mesh, for the next operation.</returns>
    /// <exception cref="FormatException">The line is not an operation, or its measures are not the operation's.</exception>
    /// <exception cref="ArgumentException">A measure is out of the operation's range.</exception>
    public EditMesh Run(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var hash = line.IndexOf('#');
        var words = (hash >= 0 ? line[..hash] : line).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return this;

        new Reader(words, line).Apply(this);
        return this;
    }

    /// <summary>Makes a mesh from a recipe's text, its lines run in order from an empty mesh.</summary>
    /// <param name="text">The recipe, its header line first, a line an operation, blank lines and comments after <c>#</c> skipped.</param>
    /// <param name="name">What the text is called in an error, its file's path where it came from one.</param>
    /// <exception cref="FormatException">A line is not an operation, or the header names another version.</exception>
    public static EditMesh FromRecipe(string text, string name = "the recipe")
    {
        ArgumentNullException.ThrowIfNull(text);

        var mesh = new EditMesh();
        var lines = text.ReplaceLineEndings("\n").Split('\n');
        var number = 0;

        foreach (var line in lines)
        {
            number++;
            var hash = line.IndexOf('#');
            var meaningful = (hash >= 0 ? line[..hash] : line).Trim();
            if (meaningful.Length == 0) continue;

            if (meaningful.StartsWith("model ", StringComparison.Ordinal))
            {
                if (meaningful != Header) throw new FormatException($"Line {number} of {name} is {meaningful}, and this reads {Header}.");
                continue;
            }

            try
            {
                mesh.Run(meaningful);
            }
            catch (Exception error) when (error is FormatException or ArgumentException or InvalidOperationException or IOException)
            {
                throw new FormatException($"Line {number} of {name}, {meaningful}: {error.Message}", error);
            }
        }

        // A recipe read is the mesh's history from here, nothing undone.
        mesh._undone.Clear();
        return mesh;
    }

    /// <summary>Makes a mesh from a recipe's file, read as the game reads its assets.</summary>
    /// <param name="path">The file, relative to the asset root unless it is a full path, ending in <see cref="Extension"/>.</param>
    /// <exception cref="FileNotFoundException">There is no such file, on disk or among what the game carries.</exception>
    /// <exception cref="FormatException">A line of it is not an operation.</exception>
    public static EditMesh Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!AssetFiles.Exists(path)) throw new FileNotFoundException($"There is no model at {path}.", path);
        return FromRecipe(AssetFiles.ReadAllText(path), path);
    }

    /// <summary>Writes the recipe to a file, from which <see cref="Load"/> makes the mesh again.</summary>
    /// <param name="path">The file, relative to the asset root unless it is a full path.</param>
    /// <returns>The file's full path.</returns>
    /// <remarks>
    /// The recipe rather than the vertices, so a level's model is a short text a reader follows and a
    /// diff reads line by line, and the mesh is made again when it loads, which for a level's model
    /// takes milliseconds. Written under a name of its own and moved into place, so a game reading
    /// it while it is written reads the old file or the new one whole.
    /// </remarks>
    public string Save(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var full = Path.IsPathRooted(path) ? path : Path.Combine(AssetIds.Root, path);
        var folder = Path.GetDirectoryName(full);
        if (!string.IsNullOrEmpty(folder)) Directory.CreateDirectory(folder);

        var part = full + ".part";
        File.WriteAllText(part, RecipeText, new UTF8Encoding(false));
        File.Move(part, full, overwrite: true);
        return full;
    }

    /// <summary>Writes an operation's line to the recipe, once it has done what it says.</summary>
    private EditMesh Did(string line)
    {
        _recipe.Add(line);
        if (!_replaying) _undone.Clear();
        return this;
    }

    /// <summary>Makes the mesh again from nothing, from the lines given.</summary>
    private void Rebuild(string[] lines)
    {
        _positions.Clear();
        _faces.Clear();
        _slots.RemoveRange(1, _slots.Count - 1);
        _selectedVertices.Clear();
        _selectedFaces.Clear();
        _recipe.Clear();
        GridStep = 1f;
        Changed();

        _replaying = true;
        try
        {
            foreach (var line in lines) Run(line);
        }
        finally
        {
            _replaying = false;
        }
    }

    private static string F(float value) => value.ToString(CultureInfo.InvariantCulture);

    private static string V(Vec3 value) => $"{F(value.X)} {F(value.Y)} {F(value.Z)}";

    private static string Placement(Vec3 at, bool centered) => (at == Vec3.Zero ? "" : " at " + V(at)) + (centered ? " center" : "");

    private static string Mode(SelectionMode mode) => mode switch
    {
        SelectionMode.Add => "add ",
        SelectionMode.Remove => "remove ",
        _ => "",
    };

    private static string Way((int Axis, bool Negative) way) => (way.Negative ? "-" : "+") + "xyz"[way.Axis];

    /// <summary>Reads one line's words into the operation they name, and calls it.</summary>
    private sealed class Reader(string[] words, string line)
    {
        private int _at = 1;

        public void Apply(EditMesh mesh)
        {
            switch (words[0])
            {
                case "box": mesh.Box(Vector(), Where(out var centered), centered); break;
                case "plane": mesh.Plane(Number(), Number(), Where(out centered), centered); break;
                case "wedge": mesh.Wedge(Vector(), Where(out centered), centered); break;
                case "cylinder": mesh.Cylinder(Number(), Number(), Count(), Where(out centered), centered); break;
                case "cone": mesh.Cone(Number(), Number(), Count(), Where(out centered), centered); break;
                case "stairs": mesh.Stairs(Count(), Number(), Number(), Number(), Where(out centered), centered); break;
                case "arch": mesh.Arch(Number(), Number(), Number(), Count(), Where(out centered), centered); break;
                case "select": Select(mesh); break;
                case "move": mesh.Move(Vector()); break;
                case "rotate": Rotate(mesh); break;
                case "scale": Scale(mesh); break;
                case "grid": mesh.Grid(Number()); break;
                case "snap": mesh.Snap(); break;
                case "material": mesh.Material(Word("a slot's name")); break;
                case "clear": mesh.Clear(); break;
                case "extrude" when words.Length == 2: mesh.Extrude(Number()); break;
                case "extrude": mesh.Extrude(Vector()); break;
                case "inset": mesh.Inset(Number()); break;
                case "delete": mesh.Delete(Optional("fill")); break;
                case "merge": mesh.Merge(Number()); break;
                case "flip": mesh.Flip(); break;
                case "triangulate": mesh.Triangulate(); break;
                case "duplicate": mesh.Duplicate(); break;
                case "join": mesh.Join(Word("a model's path")); break;
                case "shade": Shade(mesh); break;
                default: throw new FormatException($"{words[0]} is not an operation of a model's recipe.");
            }

            if (_at < words.Length) throw new FormatException($"{line.Trim()} has words after its operation's, from {words[_at]}.");
        }

        private void Select(EditMesh mesh)
        {
            var what = Word("what to select");
            switch (what)
            {
                case "all": mesh.SelectAll(); return;
                case "none": mesh.SelectNone(); return;
                case "invert": mesh.SelectInvert(); return;
            }

            var mode = SelectionMode.Replace;
            if (what is "add" or "remove")
            {
                mode = what == "add" ? SelectionMode.Add : SelectionMode.Remove;
                what = Word("what to select");
            }

            switch (what)
            {
                case "face":
                    var faces = new List<int>();
                    while (_at < words.Length) faces.Add(Count());
                    if (faces.Count == 0) throw new FormatException("select face takes a face's index or more.");
                    mesh.SelectFaces(faces, mode);
                    return;
                case "box":
                    mesh.SelectInside(Vector(), Vector(), mode);
                    return;
                case "slot":
                    mesh.SelectSlot(Word("a slot's name"), mode);
                    return;
                case { Length: 2 } way when way[0] is '+' or '-' && way[1] is 'x' or 'y' or 'z':
                    var axis = way[1] - 'x';
                    var sign = way[0] == '-' ? -1f : 1f;
                    mesh.SelectFacing(new Vec3(axis == 0 ? sign : 0f, axis == 1 ? sign : 0f, axis == 2 ? sign : 0f), mode);
                    return;
                default:
                    throw new FormatException($"select {what} is none of all, none, invert, face, box, slot or a way along an axis such as +y.");
            }
        }

        private void Rotate(EditMesh mesh)
        {
            Vec3 axis;
            if (_at < words.Length && words[_at] is "x" or "y" or "z")
            {
                var named = words[_at++][0] - 'x';
                axis = new Vec3(named == 0 ? 1f : 0f, named == 1 ? 1f : 0f, named == 2 ? 1f : 0f);
            }
            else
            {
                axis = Vector();
            }

            mesh.Rotate(axis, Number(), Pivot());
        }

        private void Scale(EditMesh mesh)
        {
            // One factor or three, told apart by whether a fourth number follows the first.
            var first = Number();
            if (_at < words.Length && words[_at] != "at")
            {
                mesh.Scale(new Vec3(first, Number(), Number()), Pivot());
                return;
            }

            mesh.Scale(first, Pivot());
        }

        private void Shade(EditMesh mesh)
        {
            switch (Word("smooth or flat"))
            {
                case "flat":
                    mesh.Shade(false);
                    return;
                case "smooth":
                    mesh.Shade(true, _at < words.Length ? Number() : DefaultSmoothAngle);
                    return;
                default:
                    throw new FormatException("shade takes smooth, with an angle in degrees if wanted, or flat.");
            }
        }

        private Vec3 Where(out bool centered)
        {
            var at = Vec3.Zero;
            if (_at < words.Length && words[_at] == "at")
            {
                _at++;
                at = Vector();
            }

            centered = Optional("center");
            return at;
        }

        private Vec3? Pivot()
        {
            if (_at >= words.Length || words[_at] != "at") return null;
            _at++;
            return Vector();
        }

        private bool Optional(string word)
        {
            if (_at >= words.Length || words[_at] != word) return false;
            _at++;
            return true;
        }

        private Vec3 Vector() => new(Number(), Number(), Number());

        private float Number()
        {
            if (_at >= words.Length) throw new FormatException($"{words[0]} needs another number after {words[_at - 1]}.");
            var word = words[_at++];
            return float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && float.IsFinite(value)
                ? value
                : throw new FormatException($"{word} is not a number, which {words[0]} takes there.");
        }

        private int Count()
        {
            if (_at >= words.Length) throw new FormatException($"{words[0]} needs another whole number after {words[_at - 1]}.");
            var word = words[_at++];
            return int.TryParse(word, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                ? value
                : throw new FormatException($"{word} is not a whole number, which {words[0]} takes there.");
        }

        private string Word(string what)
        {
            if (_at >= words.Length) throw new FormatException($"{words[0]} needs {what} after {words[_at - 1]}.");
            return words[_at++];
        }
    }
}
