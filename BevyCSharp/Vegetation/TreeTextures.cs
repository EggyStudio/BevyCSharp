using System.Text.Json;

namespace Bevy;

/// <summary>
/// The vegetation's texture collection, the bark and leaf sets the library carries by name, which a
/// species names for its bark and its leaves and a game may add its own to.
/// </summary>
/// <remarks>
/// <para>
/// The library carries each set it was built with inside its assembly, three KTX2 files a set that
/// <c>build/textures.py</c> imports by a recipe in its manifest, and reads them under
/// <see cref="Folder"/> in the asset root, so Bevy's own loader reads them by their paths and a game
/// ships no folder for them. A tree whose species names a set the library does not carry is drawn
/// with the bark and leaves the tree's generator makes.
/// </para>
/// <para>
/// A game adds a folder of its own sets with <see cref="Add"/>, as the import writes them, each in a
/// folder of its name beside a <c>textures.json</c> naming them, and a set of the same name as one
/// the library carries stands in its place.
/// </para>
/// </remarks>
public static class TreeTextures
{
    /// <summary>The folder of the asset root the library's sets are read under, each in a folder of its name.</summary>
    public const string Folder = "bevycsharp/vegetation";

    // What the library's resources are named from, the manifest and each set's files.
    private const string Prefix = "Bevy.Vegetation/";

    private static readonly Lock Gate = new();
    private static readonly List<string> Added = [];
    private static Dictionary<string, TreeTextureSet>? _sets;
    private static int _carriedFor = -1;

    /// <summary>Every set, the library's and those added, in the order of their names.</summary>
    public static IReadOnlyList<TreeTextureSet> All
    {
        get
        {
            lock (Gate) return [.. Sets().Values.OrderBy(set => set.Name, StringComparer.Ordinal)];
        }
    }

    /// <summary>The set of a name, or nothing where the collection has none.</summary>
    /// <param name="name">The set's name, as <c>silver-birch-bark</c>.</param>
    public static TreeTextureSet? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        lock (Gate) return Sets().GetValueOrDefault(name);
    }

    /// <summary>Adds the sets of a folder on this machine, each in a folder of its name beside a <c>textures.json</c> naming them.</summary>
    /// <param name="directory">The folder, as <c>build/textures.py --out</c> writes one.</param>
    /// <returns>The sets it added, in the order the folder names them.</returns>
    /// <exception cref="InvalidDataException">The folder's <c>textures.json</c> is missing or names a set none of whose files are there.</exception>
    /// <remarks>
    /// The files are read where they are, under a folder of <see cref="Folder"/>, and a set of the
    /// same name as one the collection has stands in its place, so a game's own bark replaces the
    /// library's of that name.
    /// </remarks>
    public static IReadOnlyList<TreeTextureSet> Add(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var full = Path.GetFullPath(directory);
        var manifest = Path.Combine(full, "textures.json");
        if (!File.Exists(manifest)) throw new InvalidDataException($"{full} holds no textures.json naming its sets, which build/textures.py writes beside them.");

        lock (Gate)
        {
            var under = $"{Folder}/added/{Added.Count}";
            var sets = Listed(File.ReadAllText(manifest), under, file => File.Exists(Path.Combine(full, file)), manifest);
            Added.Add(full);
            foreach (var set in sets) Sets()[set.Name] = set;
            _carriedFor = -1;
            return sets;
        }
    }

    /// <summary>
    /// Sees that the sets are read under the asset root in this app, since each app takes its files
    /// afresh as it starts. Called before a set's files are loaded.
    /// </summary>
    internal static void Carry()
    {
        lock (Gate)
        {
            if (_carriedFor == AssetFiles.Started) return;
            if (Sets().Count > 0) AssetFiles.Mount(Folder, typeof(TreeTextures).Assembly, Prefix);
            for (var i = 0; i < Added.Count; i++) AssetFiles.Mount($"{Folder}/added/{i}", Added[i]);
            _carriedFor = AssetFiles.Started;
        }
    }

    /// <summary>The library's own sets, read once from its manifest, and those added since.</summary>
    private static Dictionary<string, TreeTextureSet> Sets()
    {
        if (_sets is { } known) return known;

        var assembly = typeof(TreeTextures).Assembly;
        var names = assembly.GetManifestResourceNames().Select(name => name.Replace('\\', '/')).ToHashSet(StringComparer.Ordinal);
        using var stream = assembly.GetManifestResourceStream(Prefix + "textures.json")
            ?? throw new InvalidDataException("The library carries no manifest of its texture collection, Bevy.Vegetation/textures.json.");
        using var reader = new StreamReader(stream);

        // A set the manifest names whose files the library does not carry is one the manifest marks
        // waiting, and is left out.
        var sets = Listed(reader.ReadToEnd(), Folder, file => names.Contains(Prefix + file), "the library's textures.json");
        return _sets = sets.ToDictionary(set => set.Name, StringComparer.Ordinal);
    }

    /// <summary>The sets a manifest names whose three files are there, read under a folder of the asset root.</summary>
    private static List<TreeTextureSet> Listed(string json, string under, Func<string, bool> exists, string what)
    {
        var sets = new List<TreeTextureSet>();
        using var document = JsonDocument.Parse(json);
        foreach (var entry in document.RootElement.GetProperty("entries").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString() ?? throw new InvalidDataException($"{what} names a set with no name.");
            var kind = entry.GetProperty("kind").GetString() switch
            {
                "bark" => TreeTextureKind.Bark,
                "leaves" => TreeTextureKind.Leaves,
                var other => throw new InvalidDataException($"{what} gives the set {name} the kind {other}, which is neither bark nor leaves."),
            };

            string[] files = ["albedo.ktx2", "normal.ktx2", "surface.ktx2"];
            var present = files.Count(file => exists($"{name}/{file}"));
            if (present == 0 && under == Folder) continue;
            if (present < files.Length) throw new InvalidDataException($"{what} names the set {name}, and {files.Length - present} of its three files are not there.");

            var set = new TreeTextureSet(name, kind, $"{under}/{name}/albedo.ktx2", $"{under}/{name}/normal.ktx2", $"{under}/{name}/surface.ktx2");
            if (entry.TryGetProperty("size", out var size)) set = set with { Size = (size[0].GetInt32(), size[1].GetInt32()) };
            if (entry.TryGetProperty("color", out var color)) set = set with { MeanColor = Color.FromSrgb(color[0].GetSingle(), color[1].GetSingle(), color[2].GetSingle()) };
            if (entry.TryGetProperty("leaves", out var leaves) && leaves.GetArrayLength() > 0)
            {
                set = set with { LeafBoxes = [.. leaves.EnumerateArray().Select(box => (box[0].GetSingle(), box[1].GetSingle(), box[2].GetSingle(), box[3].GetSingle()))] };
            }

            sets.Add(set);
        }

        return sets;
    }
}
