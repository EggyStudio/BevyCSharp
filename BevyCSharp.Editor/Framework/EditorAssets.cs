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

    /// <summary>
    /// The directories directly inside one, for a tree down the side of the browser.
    /// </summary>
    /// <remarks>
    /// One level at a time rather than the whole tree at once: a tree draws what is unfolded, and
    /// a deep asset directory read whole on every frame is a directory read for nothing.
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

        foreach (var path in System.IO.Directory.GetDirectories(here).OrderBy(p => p, StringComparer.Ordinal))
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
    /// For anything that offers a file to choose rather than one to open: a field holding a mesh
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

            if (extensions is { Count: > 0 }
                && !extensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
            {
                continue;
            }

            found.Add(Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'));
        }

        found.Sort(StringComparer.Ordinal);
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
        AssetKind.Mesh or AssetKind.Gltf => [".gltf", ".glb", ".obj"],
        AssetKind.Image => [".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tga", ".ktx2"],
        AssetKind.Audio => [".ogg", ".wav", ".flac", ".mp3"],
        AssetKind.Scene => [".scn", ".ron", ".gltf", ".glb"],
        AssetKind.Font => [".ttf", ".otf"],
        AssetKind.Shader => [".wgsl", ".spv"],
        _ => [],
    };

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
    /// The kind rather than the file, for everything but an image: a browser points an image tile
    /// at the image itself, since the interface loads a picture from a path and that is all it
    /// takes. A picture of what a model or a sound contains would be a thumbnail, which needs the
    /// bridge to render one and hand back an asset key.
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
