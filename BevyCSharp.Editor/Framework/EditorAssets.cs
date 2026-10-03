using Bevy;

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
/// show, since what is listed here is exactly what a path in a document or a script would find.
/// Nothing is imported and nothing is cataloged, because the engine does not work that way either.
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

        foreach (var path in System.IO.Directory.GetDirectories(here).OrderBy(p => p, EditorSort.Comparer))
        {
            var name = Path.GetFileName(path);
            entries.Add(new AssetEntry(name, Join(Directory, name), true, 0));
        }

        foreach (var path in System.IO.Directory.GetFiles(here).OrderBy(p => p, EditorSort.Comparer))
        {
            // A sidecar holds a file's id and belongs to the file beside it, so it is not a thing to
            // open, and it moves with its file when the file is moved.
            if (AssetIds.IsSidecar(path)) continue;

            var name = Path.GetFileName(path);
            entries.Add(new AssetEntry(name, Join(Directory, name), false, new FileInfo(path).Length));
        }

        return entries;
    }

    /// <summary>
    /// The directories directly inside one, for a tree down the side of the browser.
    /// </summary>
    /// <remarks>
    /// One level at a time rather than the whole tree at once, because a tree draws what is
    /// unfolded, and a deep asset directory read whole on every frame is a directory read for
    /// nothing.
    /// </remarks>
    /// <param name="relative">Which directory to look inside, or empty for the root.</param>
    public static IReadOnlyList<(string Path, string Name)> Directories(string relative)
    {
        ArgumentNullException.ThrowIfNull(relative);

        var root = EditorPaths.Assets;

        var here = relative.Length == 0
            ? root
            : Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

        if (!System.IO.Directory.Exists(here)) return [];

        var found = new List<(string Path, string Name)>();

        foreach (var path in System.IO.Directory.GetDirectories(here).OrderBy(p => p, EditorSort.Comparer))
        {
            var name = Path.GetFileName(path);
            found.Add((Join(relative, name), name));
        }

        return found;
    }

    /// <summary>
    /// Every file under the asset root, whatever directory the browser is looking at.
    /// </summary>
    /// <remarks>
    /// For anything that offers a file to choose rather than one to open. A field holding a mesh
    /// is not asking about the directory somebody last browsed to. Read from disk on each call,
    /// for the same reason the listing is, and capped so that an asset tree nobody expected cannot
    /// make a menu that takes a second to build.
    /// </remarks>
    /// <param name="extensions">
    /// Which files to answer with, lower case and with their dots, or nothing for all of them.
    /// </param>
    /// <param name="most">The most to answer with.</param>
    public static IReadOnlyList<string> Every(
        IReadOnlyCollection<string>? extensions = null, int most = 200)
    {
        var root = EditorPaths.Assets;
        if (!System.IO.Directory.Exists(root)) return [];

        var found = new List<string>();

        foreach (var path in System.IO.Directory.EnumerateFiles(
            root, "*", SearchOption.AllDirectories))
        {
            if (found.Count >= most) break;
            if (AssetIds.IsSidecar(path)) continue;

            if (extensions is { Count: > 0 }
                && !extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            {
                continue;
            }

            found.Add(Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'));
        }

        found.Sort(EditorSort.Naturally);
        return found;
    }

    /// <summary>
    /// The file extensions that go with an asset type.
    /// </summary>
    /// <remarks>
    /// What an asset field offers when it was not told. The engine decides what it can load by
    /// extension too, so this is the same list from the other side.
    /// </remarks>
    public static IReadOnlyList<string> ExtensionsFor(string kind) => kind switch
    {
        AssetKind.Mesh or AssetKind.Gltf or AssetKind.StandardMaterial => [".gltf", ".glb", ".obj"],
        AssetKind.Image => [".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tga", ".ktx2"],
        AssetKind.Audio => [".ogg", ".wav", ".flac", ".mp3"],
        AssetKind.Scene => [".scn", ".ron", ".gltf", ".glb"],
        AssetKind.Font => [".ttf", ".otf"],
        _ => [],
    };

    /// <summary>What a file is, as far as the engine is concerned.</summary>
    /// <remarks>
    /// By extension, because the engine's own loaders go on that. A kind nothing here knows is
    /// reported as what it is rather than guessed at.
    /// </remarks>
    public static string KindOf(string relative) =>
        relative.EndsWith(".scene.json", StringComparison.OrdinalIgnoreCase) ? "scene"
        : MaterialFiles.IsMaterialFile(relative) ? "material"
        : MeshFiles.IsMeshFile(relative) ? "mesh"
        : KindByExtension(relative);

    /// <summary>What sort of file it is by its last extension alone.</summary>
    /// <remarks>A scene file ends in <c>.json</c> as a data asset does, so the two are told apart before this.</remarks>
    private static string KindByExtension(string relative) => Path.GetExtension(relative).ToLowerInvariant() switch
    {
        ".cs" => "behavior script",
        ".png" or ".jpg" or ".jpeg" or ".webp" or ".bmp" or ".tga" or ".ktx2" => "image",
        ".gltf" or ".glb" => "model",
        ".ogg" or ".wav" or ".flac" or ".mp3" => "sound",
        ".scn" or ".ron" => "scene",
        ".slang" => "shader",
        ".json" => "data",
        ".txt" => "text",
        "" => "file",
        var other => other.TrimStart('.'),
    };

    /// <summary>
    /// The picture a file's tile wears, under the asset root.
    /// </summary>
    /// <remarks>
    /// The kind, for everything the tile cannot show as itself. An image tile draws the image
    /// instead, which the assets panel does rather than asking here, since the interface loads a
    /// picture from a path and that is all it takes. A picture of what a model or a sound contains
    /// would be a thumbnail, which needs a camera pointed at a render target.
    /// </remarks>
    public static string IconOf(string relative) => KindOf(relative) switch
    {
        "behavior script" or "shader" => EditorIcons.Script,
        "text" => EditorIcons.Text,
        "image" => EditorIcons.Image,
        "model" => EditorIcons.Mesh,
        "sound" => EditorIcons.Sound,
        "scene" => EditorIcons.World,
        "material" => EditorIcons.Image,
        "mesh" => EditorIcons.Mesh,
        "data" => EditorIcons.Data,
        _ => EditorIcons.File,
    };

    /// <summary>
    /// Renames or moves a file or a folder under the asset root, taking each file's id along.
    /// </summary>
    /// <param name="from">What to move, relative to the asset root.</param>
    /// <param name="to">Where it goes, relative to the asset root.</param>
    /// <returns>Why it was refused, or <see langword="null"/> once it has moved.</returns>
    /// <remarks>
    /// <para>
    /// A file goes through <see cref="AssetIds.Move"/>, which carries its sidecar, so a scene or a
    /// data asset referring to it by id still finds it. A folder moves whole, sidecars inside it,
    /// and the index is read again so every id under it answers with where it is.
    /// </para>
    /// <para>
    /// Nothing is overwritten, and a folder cannot go inside itself. A selection or an open folder
    /// under what moved follows it, so the panel goes on showing the same thing.
    /// </para>
    /// </remarks>
    public static string? Move(string from, string to)
    {
        from = Clean(from);
        to = Clean(to);

        if (from.Length == 0 || to.Length == 0) return "a move needs both where from and where to";
        if (from == to) return null;

        var source = Absolute(from);
        var target = Absolute(to);
        var isFolder = System.IO.Directory.Exists(source);

        if (!isFolder && !File.Exists(source)) return $"nothing called {from} under the asset root";
        if (File.Exists(target) || System.IO.Directory.Exists(target)) return $"{to} is already there";
        if (isFolder && Under(to, from)) return $"{from} cannot go inside itself";

        try
        {
            if (isFolder)
            {
                System.IO.Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                System.IO.Directory.Move(source, target);
                AssetIds.Reindex();
            }
            else
            {
                AssetIds.Move(from, to);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return error.Message;
        }

        if (Selected is { } selected && Under(selected, from)) Select(to + selected[from.Length..]);
        if (Under(Directory, from)) Directory = to + Directory[from.Length..];
        return null;
    }

    /// <summary>
    /// Deletes a file and its id, or a folder and everything in it.
    /// </summary>
    /// <param name="relative">What to delete, relative to the asset root.</param>
    /// <returns>Why it was refused, or <see langword="null"/> once it is gone.</returns>
    /// <remarks>
    /// The sidecar goes with its file, since an id naming nothing would only be found by a reference
    /// that then fails to load. The root itself is refused. A selection or an open folder under what
    /// was deleted is let go.
    /// </remarks>
    public static string? Delete(string relative)
    {
        relative = Clean(relative);
        if (relative.Length == 0) return "the asset root itself cannot be deleted";

        var full = Absolute(relative);

        try
        {
            if (System.IO.Directory.Exists(full))
            {
                System.IO.Directory.Delete(full, recursive: true);
            }
            else if (File.Exists(full))
            {
                File.Delete(full);
                if (File.Exists(full + ".uid")) File.Delete(full + ".uid");
            }
            else
            {
                return $"nothing called {relative} under the asset root";
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return error.Message;
        }

        AssetIds.Reindex();
        if (Selected is { } selected && Under(selected, relative)) Select(null);
        if (Under(Directory, relative)) Directory = Parent(relative);
        return null;
    }

    /// <summary>The folder something is in, relative to the asset root.</summary>
    public static string Parent(string relative)
    {
        var cut = relative.LastIndexOf('/');
        return cut < 0 ? string.Empty : relative[..cut];
    }

    /// <summary>Whether a path is another or inside it.</summary>
    private static bool Under(string path, string folder) =>
        path == folder || path.StartsWith(folder + "/", StringComparison.Ordinal);

    /// <summary>A path as the panel keeps them: forward slashes, none at either end.</summary>
    private static string Clean(string path) => path.Trim().Replace('\\', '/').Trim('/');

    /// <summary>The absolute path of something in the asset directory.</summary>
    public static string Absolute(string relative) =>
        Path.Combine(EditorPaths.Assets, relative.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Joins two parts of a relative path, keeping forward slashes.</summary>
    private static string Join(string directory, string name) =>
        directory.Length == 0 ? name : directory + "/" + name;
}
