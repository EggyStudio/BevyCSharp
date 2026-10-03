using System.Globalization;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Ids for asset files that survive the file being renamed or moved.
/// </summary>
/// <remarks>
/// <para>
/// A reference by path breaks when the file is renamed. Each asset file can have a sidecar beside
/// it, <c>name.ext.uid</c>, holding a random 64-bit id as sixteen hex digits, and a reference holds
/// that id as well as the path. It is resolved by the id first, through an index of every sidecar
/// under the asset root, and by the path when the id is unknown, so a file renamed outside the
/// editor is found again once the index is rebuilt, and a file whose sidecar was lost is still
/// found by where it was.
/// </para>
/// <para>
/// A sidecar rather than a <c>.meta</c> file, because Bevy reads a <c>.meta</c> beside an asset as
/// its loader's settings, and one of these there would be read as a malformed one. Unity keeps
/// GUIDs in <c>.meta</c> files and Godot 4.4 keeps <c>uid://</c> ids in <c>.uid</c> files, and the
/// second is the arrangement taken here.
/// </para>
/// <para>
/// Only something that edits the project writes a sidecar, the editor and
/// <see cref="DataAssets.Create{T}"/> among them. A game reads them. Paths are relative to the
/// asset root, with forward slashes, as everywhere else an asset path is given.
/// </para>
/// <para>
/// A shipped game carries one index, <c>uids.json</c> at the root (<see cref="WriteIndex"/>),
/// instead of a sidecar beside every file, so its asset folder holds what it loads and nothing
/// else. The index is read before the sidecars, and a sidecar wins over it, so a project that has
/// both, such as one where an export was run in place, answers with where the file is.
/// </para>
/// </remarks>
public static class AssetIds
{
    private const string Extension = ".uid";

    /// <summary>The index a shipped game carries in place of the sidecars, at the asset root.</summary>
    public const string IndexName = "uids.json";

    /// <summary>The format the index says it is in.</summary>
    public const string IndexFormat = "bevycsharp.uids.1";

    private static readonly object Gate = new();
    private static readonly Dictionary<ulong, string> Paths = [];

    /// <summary>
    /// The ids read from the game's assembly rather than the folder, which are trusted without
    /// looking for their file, since a file the bridge carries is not one the managed side can see.
    /// </summary>
    private static readonly HashSet<ulong> Carried = [];
    private static string? _indexed;

    /// <summary>The directory asset paths are relative to.</summary>
    internal static string Root =>
        Streaming.AssetRoot.Length > 0 ? Streaming.AssetRoot : Directory.GetCurrentDirectory();

    /// <summary>
    /// The id of an asset file, or zero when it has none and <paramref name="create"/> is false.
    /// </summary>
    /// <param name="path">The file, relative to the asset root.</param>
    /// <param name="create">Whether to give a file with no sidecar one.</param>
    public static ulong IdOf(string path, bool create = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var sidecar = Full(path) + Extension;
        if (File.Exists(sidecar) && Parse(File.ReadAllText(sidecar)) is { } existing)
        {
            lock (Gate) Paths[existing] = Normalized(path);
            return existing;
        }

        if (!create)
        {
            // A shipped game has the index and no sidecars, and a save written there still refers
            // to a file by the id the index gives it.
            var normalized = Normalized(path);
            lock (Gate)
            {
                if (_indexed != Root) Rebuild();
                foreach (var (id, file) in Paths)
                    if (file == normalized) return id;
            }

            return 0;
        }

        var made = Fresh();
        File.WriteAllText(sidecar, made.ToString("x16", CultureInfo.InvariantCulture) + "\n");
        lock (Gate) Paths[made] = Normalized(path);
        return made;
    }

    /// <summary>
    /// Where the file with an id is now, or <see langword="null"/> when no sidecar under the root
    /// holds it.
    /// </summary>
    /// <remarks>
    /// A path the index remembers is checked before it is trusted, and the root is read again once
    /// when it is not there, so a file renamed or moved since the index was built is still found.
    /// </remarks>
    public static string? PathOf(ulong id)
    {
        if (id == 0) return null;

        lock (Gate)
        {
            if (_indexed != Root) Rebuild();

            if (Paths.TryGetValue(id, out var known) && (Carried.Contains(id) || File.Exists(Full(known)))) return known;

            Rebuild();
            return Paths.TryGetValue(id, out var found) ? found : null;
        }
    }

    /// <summary>
    /// Moves a file and its sidecar together, so references to it keep working by id.
    /// </summary>
    /// <param name="from">Where the file is, relative to the asset root.</param>
    /// <param name="to">Where it goes.</param>
    public static void Move(string from, string to)
    {
        ArgumentException.ThrowIfNullOrEmpty(from);
        ArgumentException.ThrowIfNullOrEmpty(to);

        var target = Full(to);
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Move(Full(from), target);

        if (File.Exists(Full(from) + Extension))
        {
            File.Move(Full(from) + Extension, target + Extension);
            if (Parse(File.ReadAllText(target + Extension)) is { } id)
                lock (Gate) Paths[id] = Normalized(to);
        }
    }

    /// <summary>Reads every sidecar under the root again.</summary>
    /// <remarks>Called by the editor when the project opens and when files change under it.</remarks>
    public static void Reindex()
    {
        lock (Gate) Rebuild();
    }

    /// <summary>Whether a path is a sidecar, which a list of assets leaves out.</summary>
    public static bool IsSidecar(string path) => path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Writes every id under the root, with the path it names, to one index file, for a shipped
    /// game to carry instead of the sidecars.
    /// </summary>
    /// <param name="path">Where to write it, or nothing for <see cref="IndexName"/> at the root.</param>
    /// <returns>How many ids were written.</returns>
    /// <remarks>
    /// Sorted by id, so an index written twice from the same files is the same file. The sidecars
    /// stay, since this root is the one being edited. An export writes its index over its own copy
    /// with <see cref="IndexForShipping"/> instead, which also takes the sidecars out.
    /// </remarks>
    public static int WriteIndex(string? path = null)
    {
        KeyValuePair<ulong, string>[] entries;
        lock (Gate)
        {
            Rebuild();
            entries = [.. Paths.OrderBy(entry => entry.Key)];
        }

        return Write(path ?? Path.Combine(Root, IndexName), entries);
    }

    /// <summary>
    /// Turns the sidecars under a folder into the one index a shipped game carries, writing
    /// <see cref="IndexName"/> at its root and deleting the sidecars.
    /// </summary>
    /// <param name="root">The asset folder of an export, which is a copy and not the project's own.</param>
    /// <returns>How many ids the index holds, or zero when there were none and no index was written.</returns>
    /// <remarks>
    /// For a folder other than the root this app reads, since an export copies a project's assets
    /// somewhere of its own and the editor that runs it reads another project's. Nothing this app
    /// has indexed is touched. Deleting the sidecars makes a copy fit to ship and would leave a
    /// project being edited without its ids, so the root this app reads is refused.
    /// </remarks>
    public static int IndexForShipping(string root)
    {
        ArgumentException.ThrowIfNullOrEmpty(root);
        if (Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) == Path.GetFullPath(Root).TrimEnd(Path.DirectorySeparatorChar))
            throw new InvalidOperationException("The sidecars of the folder this app reads assets from are its ids, and are not removed.");

        var found = new Dictionary<ulong, string>();
        Scan(root, found);

        // A project that never gave a file an id ships no index, rather than one that names nothing.
        var written = found.Count == 0 ? 0 : Write(Path.Combine(root, IndexName), [.. found.OrderBy(entry => entry.Key)]);

        foreach (var sidecar in Directory.EnumerateFiles(root, "*" + Extension, SearchOption.AllDirectories).ToArray())
            File.Delete(sidecar);

        return written;
    }

    private static int Write(string target, KeyValuePair<ulong, string>[] entries)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(target))!);

        using (var stream = File.Create(target))
        using (var json = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("format", IndexFormat);
            json.WriteStartObject("ids");
            foreach (var (id, file) in entries)
                json.WriteString(id.ToString("x16", CultureInfo.InvariantCulture), file);
            json.WriteEndObject();
            json.WriteEndObject();
        }

        return entries.Length;
    }

    private static void Rebuild()
    {
        Paths.Clear();
        Carried.Clear();
        var root = Root;
        _indexed = root;

        // What the game carries first, so a file beside the game wins over it, as a file on disk
        // wins in every other read (AssetFiles).
        foreach (var sidecar in AssetFiles.Carried(Extension))
        {
            if (Parse(AssetFiles.ReadAllText(sidecar)) is { } id) Paths[id] = sidecar[..^Extension.Length];
        }

        // The index an export writes in place of the sidecars, which a pack carries and a game
        // with no asset folder at all reads from there.
        ReadIndex(IndexName);
        Scan(root, Paths);

        // An id whose file is carried and not on disk is trusted as it is, since there is no file
        // to check it against. Every other is the disk's, and checked like any other.
        foreach (var (id, path) in Paths)
        {
            if (AssetFiles.IsCarried(path)) Carried.Add(id);
        }
    }

    /// <summary>Adds the id of every sidecar under a folder whose file is there, by the file's path from the folder.</summary>
    private static void Scan(string root, Dictionary<ulong, string> into)
    {
        if (!Directory.Exists(root)) return;

        foreach (var sidecar in Directory.EnumerateFiles(root, "*" + Extension, SearchOption.AllDirectories))
        {
            var asset = sidecar[..^Extension.Length];
            if (!File.Exists(asset)) continue;

            try
            {
                if (Parse(File.ReadAllText(sidecar)) is { } id)
                    into[id] = Normalized(Path.GetRelativePath(root, asset));
            }
            catch (IOException)
            {
                // A sidecar being written as the index is read is picked up on the next read.
            }
        }
    }

    /// <summary>Reads the ids an index holds, leaving out a file it names that is not there.</summary>
    /// <remarks>
    /// An index that cannot be read is passed over rather than thrown, since the sidecars may still
    /// answer, and a reference whose id is not found falls back to its path.
    /// </remarks>
    private static void ReadIndex(string index)
    {
        if (!AssetFiles.Exists(index)) return;

        try
        {
            using var document = JsonDocument.Parse(AssetFiles.ReadAllText(index));
            var root = document.RootElement;
            if (!root.TryGetProperty("format", out var format) || format.GetString() != IndexFormat) return;
            if (!root.TryGetProperty("ids", out var ids) || ids.ValueKind != JsonValueKind.Object) return;

            foreach (var entry in ids.EnumerateObject())
            {
                if (Parse(entry.Name) is { } id
                    && entry.Value.GetString() is { Length: > 0 } file
                    && AssetFiles.Exists(file))
                    Paths[id] = Normalized(file);
            }
        }
        catch (Exception error) when (error is IOException or JsonException or InvalidOperationException)
        {
        }
    }

    /// <summary>A random id, never zero, which stands for no id.</summary>
    private static ulong Fresh()
    {
        Span<byte> bytes = stackalloc byte[8];
        ulong id;
        do
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            id = BitConverter.ToUInt64(bytes);
        }
        while (id == 0);

        return id;
    }

    private static ulong? Parse(string text) =>
        ulong.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id) && id != 0
            ? id
            : null;

    private static string Full(string path) => Path.Combine(Root, path);

    private static string Normalized(string path) => path.Replace('\\', '/');
}
