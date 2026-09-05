namespace BevyCSharp.Editor.Framework;

/// <summary>What one entry in the asset directory is.</summary>
/// <param name="Name">The file or directory's own name.</param>
/// <param name="Path">Where it is, relative to the asset root.</param>
/// <param name="IsDirectory">Whether it holds other things.</param>
/// <param name="Size">How many bytes, or zero for a directory.</param>
public readonly record struct AssetEntry(string Name, string Path, bool IsDirectory, long Size);

/// <summary>
/// The files the editor is running out of.
/// </summary>
/// <remarks>
/// <para>
/// An asset browser over the same directory the engine loads from, which is the honest thing to
/// show: what is listed here is exactly what a path in a document or a script would find. Nothing
/// is imported and nothing is catalogued, because the engine does not work that way either.
/// </para>
/// <para>
/// Selection is separate from the world's, because an asset is not an entity and a panel showing
/// one is answering a different question. Picking a file does not deselect an entity.
/// </para>
/// </remarks>
public static class EditorAssets
{
    /// <summary>Which directory is being looked at, relative to the asset root.</summary>
    public static string Directory { get; private set; } = string.Empty;

    /// <summary>Which file is selected, relative to the asset root, or <see langword="null"/>.</summary>
    public static string? Selected { get; private set; }

    /// <summary>Goes into a directory.</summary>
    public static void Enter(string relative) => Directory = relative;

    /// <summary>Goes up one, stopping at the root.</summary>
    public static void Up()
    {
        var cut = Directory.LastIndexOf('/');
        Directory = cut < 0 ? string.Empty : Directory[..cut];
    }

    /// <summary>Points the data panel at a file.</summary>
    public static void Select(string? relative)
    {
        Selected = relative;
        EditorSelection.Latest = relative is null ? SelectionKind.None : SelectionKind.Asset;
    }

    /// <summary>
    /// What is in the current directory: directories first, then files, both by name.
    /// </summary>
    /// <remarks>
    /// Read from disk each time it is asked for rather than cached. The directory is watched and
    /// edited while the editor runs, so a list that remembered would be wrong exactly when it
    /// mattered, and a few dozen entries cost nothing.
    /// </remarks>
    public static IReadOnlyList<AssetEntry> List()
    {
        var root = EditorPaths.Assets;
        var here = Directory.Length == 0 ? root : Path.Combine(root, Directory.Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.Directory.Exists(here)) return [];

        var entries = new List<AssetEntry>();

        foreach (var path in System.IO.Directory.GetDirectories(here).OrderBy(p => p, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(path);
            entries.Add(new AssetEntry(name, Join(Directory, name), true, 0));
        }

        foreach (var path in System.IO.Directory.GetFiles(here).OrderBy(p => p, StringComparer.Ordinal))
        {
            var name = Path.GetFileName(path);
            entries.Add(new AssetEntry(name, Join(Directory, name), false, new FileInfo(path).Length));
        }

        return entries;
    }

    /// <summary>What a file is, as far as the engine is concerned.</summary>
    /// <remarks>
    /// By extension, because that is what the engine's own loaders go on. A kind nothing here
    /// knows is reported as what it is rather than guessed at.
    /// </remarks>
    public static string KindOf(string relative) => Path.GetExtension(relative).ToLowerInvariant() switch
    {
        ".html" => "document",
        ".css" => "stylesheet",
        ".cs" => "behavior script",
        ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp" or ".tga" or ".ktx2" => "image",
        ".gltf" or ".glb" => "model",
        ".ogg" or ".wav" or ".flac" or ".mp3" => "sound",
        ".scn" or ".ron" => "scene",
        ".json" => "data",
        ".txt" => "text",
        "" => "file",
        var other => other.TrimStart('.'),
    };

    /// <summary>
    /// The picture a file's tile wears, under the asset root.
    /// </summary>
    /// <remarks>
    /// The kind rather than the file: a picture of what a PNG contains would be a thumbnail, which
    /// needs the bridge to render one and hand back an asset key. A picture of what it <em>is</em>
    /// needs nothing and is most of the use: in a list of forty files, telling the scripts from
    /// the stylesheets at a glance is the whole job.
    /// </remarks>
    public static string IconOf(string relative) => KindOf(relative) switch
    {
        "document" or "stylesheet" => "icons/ui/terminal.png",
        "behavior script" => "icons/ui/script.png",
        "image" => "icons/ui/image.png",
        "model" => "icons/ui/mesh.png",
        "sound" => "icons/ui/package.png",
        "scene" => "icons/ui/world.png",
        "data" => "icons/ui/data.png",
        _ => "icons/ui/file.png",
    };

    /// <summary>Whether a file is one the editor reloads while it runs.</summary>
    public static bool Reloads(string relative) =>
        Path.GetExtension(relative).ToLowerInvariant() is ".html" or ".css" or ".cs";

    /// <summary>The absolute path of something in the asset directory.</summary>
    public static string Absolute(string relative) =>
        Path.Combine(EditorPaths.Assets, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Joins two parts of a relative path, keeping forward slashes.</summary>
    private static string Join(string directory, string name) =>
        directory.Length == 0 ? name : directory + "/" + name;
}
