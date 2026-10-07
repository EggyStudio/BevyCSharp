using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Reads the managed side's files under <c>assets://</c> from the asset folder, or from what the
/// game carries in place of it, a pack beside it or its own assembly.
/// </summary>
/// <remarks>
/// <para>
/// The managed side reads scenes, data assets and material and mesh files for itself, and those go
/// through here. Bevy reads textures, models and sounds through its own asset sources, and when the
/// game carries files, the bridge's default source is handed a reader over the same ones
/// (<see cref="Serve"/>), so one copy of each file feeds both sides and a game ships with no asset
/// folder and the bridge every game shares.
/// </para>
/// <para>
/// Three layers, the folder over a pack over the assembly's resources. A file on disk wins, so a
/// game run from its project reads what is being edited, and a shipped one reads what it carries
/// unless a file beside it replaces one. A pack (<see cref="AssetPack"/>) wins over the assembly,
/// since a pack is the easier of the two to ship again. A resource is one whose name starts with
/// <c>assets/</c>, which a build makes from the asset folder when <c>BevyCSharpEmbedAssets</c> is
/// set. Both are handed over as an app starts, from <see cref="Config.AssetAssembly"/> and
/// <see cref="Config.AssetPack"/>.
/// </para>
/// <para>
/// A pack can also be mounted under a folder of the asset root while an app runs
/// (<see cref="Mount"/>), as a scene pack fetched for the player is, and Bevy reads it from then
/// on as the managed side does, since the bridge asks for the list of carried files at each read.
/// </para>
/// <para>
/// Only reads come here. Writing a scene or a data asset writes the folder, since what a game
/// carries cannot be written, and only a project being edited is written at all.
/// </para>
/// </remarks>
public static unsafe class AssetFiles
{
    /// <summary>What a resource's name starts with when it is a file under the asset root.</summary>
    public const string ResourcePrefix = "assets/";

    private static readonly object Gate = new();
    private static Assembly? _assembly;
    private static AssetPack? _pack;

    /// <summary>The packs mounted while an app runs, by the folder each appears under.</summary>
    private static readonly Dictionary<string, AssetPack> Mounts = new(StringComparer.Ordinal);

    /// <summary>
    /// Every carried file by its path under the asset root, with how to open it, the pack's copy
    /// where both carry one.
    /// </summary>
    private static Dictionary<string, Func<Stream?>> _carried = new(StringComparer.Ordinal);

    /// <summary>Whether any file under the asset root is read from a pack or the game's assembly.</summary>
    public static bool CarriesFiles
    {
        get
        {
            lock (Gate) return _carried.Count > 0;
        }
    }

    /// <summary>Looks for the asset files in an assembly's resources, or in none, and in no pack.</summary>
    /// <param name="assembly">Usually the entry assembly, which is the game's.</param>
    public static void Use(Assembly? assembly) => Use(assembly, null);

    /// <summary>
    /// Looks for the asset files in a pack and an assembly's resources, either of which may be
    /// nothing.
    /// </summary>
    /// <param name="assembly">Usually the entry assembly, which is the game's.</param>
    /// <param name="pack">
    /// A pack beside the game, which is kept from here on, and closed when another takes its place.
    /// </param>
    /// <remarks>
    /// Names are kept with forward slashes whatever the build wrote, since a resource made from a
    /// folder on Windows is named with the separator that platform uses. The packs mounted for the
    /// app before are closed, since what they held belonged to it.
    /// </remarks>
    public static void Use(Assembly? assembly, AssetPack? pack)
    {
        AssetPack? replaced;
        AssetPack[] unmounted;
        lock (Gate)
        {
            replaced = _pack == pack ? null : _pack;
            unmounted = [.. Mounts.Values];
            Mounts.Clear();
            _assembly = assembly is { IsDynamic: false } ? assembly : null;
            _pack = pack;
            _carried = Gather();
        }

        replaced?.Dispose();
        foreach (var gone in unmounted) gone.Dispose();
    }

    /// <summary>
    /// Reads a pack's files under a folder of the asset root while an app runs, on both sides of
    /// the bridge, until <see cref="Unmount"/> or the next app, as a scene pack fetched for the
    /// player is read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pack holds its files by their paths under its own root, so a scene's model at
    /// <c>sponza.gltf</c> in a pack mounted under <c>packs/sponza</c> is loaded as
    /// <c>packs/sponza/sponza.gltf</c>, and the textures it names beside it are found beside it.
    /// The folder keeps one pack's files from another's and from the game's own, and a file on
    /// disk under it still wins, as everywhere.
    /// </para>
    /// <para>
    /// The pack is kept from here on and closed when it is unmounted, when another is mounted under
    /// the same folder, or when the next app starts. Bevy is told the new list at once, so a load
    /// asked for in the same frame finds the files.
    /// </para>
    /// </remarks>
    /// <param name="folder">Where under the asset root the pack's files appear, with forward slashes.</param>
    /// <param name="pack">The pack, from <see cref="AssetPack.Open"/>.</param>
    public static void Mount(string folder, AssetPack pack)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(pack);

        var under = folder.Replace('\\', '/').Trim('/');
        AssetPack? replaced;
        lock (Gate)
        {
            replaced = Mounts.GetValueOrDefault(under);
            Mounts[under] = pack;
            _carried = Gather();
        }

        if (replaced != pack) replaced?.Dispose();
        Serve();
    }

    /// <summary>Stops reading the pack mounted under a folder, and closes it.</summary>
    /// <returns>Whether a pack was mounted there.</returns>
    public static bool Unmount(string folder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);

        var under = folder.Replace('\\', '/').Trim('/');
        AssetPack? pack;
        lock (Gate)
        {
            if (!Mounts.Remove(under, out pack)) return false;
            _carried = Gather();
        }

        pack.Dispose();
        Serve();
        return true;
    }

    /// <summary>
    /// Every carried file with how to open it, the assembly's under the app's pack under the packs
    /// mounted while it runs. Called holding the gate.
    /// </summary>
    private static Dictionary<string, Func<Stream?>> Gather()
    {
        var found = new Dictionary<string, Func<Stream?>>(StringComparer.Ordinal);

        if (_assembly is { } assembly)
        {
            foreach (var name in assembly.GetManifestResourceNames())
            {
                var normal = name.Replace('\\', '/');
                if (normal.StartsWith(ResourcePrefix, StringComparison.Ordinal))
                    found[normal[ResourcePrefix.Length..]] = () => assembly.GetManifestResourceStream(name);
            }
        }

        if (_pack is { } pack)
        {
            foreach (var file in pack.Files) found[file] = () => pack.OpenFile(file);
        }

        foreach (var (under, mounted) in Mounts)
        {
            foreach (var file in mounted.Files) found[$"{under}/{file}"] = () => mounted.OpenFile(file);
        }

        return found;
    }

    /// <summary>Whether a file is there, on disk or among what the game carries.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    public static bool Exists(string path) => File.Exists(Full(path)) || Opener(path) is not null;

    /// <summary>Whether a file is read from what the game carries, there being none on disk.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    public static bool IsCarried(string path) => !File.Exists(Full(path)) && Opener(path) is not null;

    /// <summary>A file's text, from disk or from what the game carries.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    /// <exception cref="FileNotFoundException">Neither has it.</exception>
    public static string ReadAllText(string path) => Encoding.UTF8.GetString(ReadAllBytes(path));

    /// <summary>
    /// A JSON file's document, from disk or from what the game carries, answering one that is not
    /// JSON or holds no object with its path and what it was to hold.
    /// </summary>
    /// <remarks>
    /// What the managed side's files go through, scenes, saves, data assets and material and mesh
    /// files, so a file a player or an artist cut short or saved as something else is answered by
    /// name, where the parser's own error gives a line and a byte and no file.
    /// </remarks>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    /// <param name="holds">What the file was to hold, as a phrase such as "a scene".</param>
    /// <exception cref="FileNotFoundException">Neither has it.</exception>
    /// <exception cref="InvalidDataException">It is not JSON, or holds no object.</exception>
    internal static JsonDocument ReadJson(string path, string holds)
    {
        var text = ReadAllText(path);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text);
        }
        catch (JsonException error)
        {
            throw new InvalidDataException($"{path} is not {holds}, since it is not JSON: {error.Message}", error);
        }

        if (document.RootElement.ValueKind == JsonValueKind.Object) return document;

        document.Dispose();
        throw new InvalidDataException($"{path} is not {holds}, since it holds no JSON object.");
    }

    /// <summary>
    /// A JSON file's document as <see cref="ReadJson"/> reads it, or nothing and why, naming the
    /// file, for a loader that answers a bad file in what it returns.
    /// </summary>
    /// <remarks>
    /// One that cannot be read at all, a folder in its place or one the user may not open, is said
    /// with its path ahead of the system's reason, which may leave the path out.
    /// </remarks>
    internal static JsonDocument? TryReadJson(string path, string holds, out string? problem)
    {
        problem = null;
        try
        {
            return ReadJson(path, holds);
        }
        catch (Exception error) when (error is FileNotFoundException or InvalidDataException)
        {
            problem = error.Message;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            problem = $"{path} could not be read. {error.Message}";
        }

        return null;
    }

    /// <summary>A file's bytes, from disk or from what the game carries.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    /// <exception cref="FileNotFoundException">Neither has it.</exception>
    public static byte[] ReadAllBytes(string path)
    {
        var full = Full(path);
        if (File.Exists(full)) return File.ReadAllBytes(full);

        if (OpenCarried(path) is { } stream)
        {
            using (stream)
            {
                using var copy = new MemoryStream();
                stream.CopyTo(copy);
                return copy.ToArray();
            }
        }

        throw new FileNotFoundException($"No asset file at {path}.", full);
    }

    /// <summary>
    /// The paths under the asset root of every carried file ending in <paramref name="suffix"/>,
    /// such as the id sidecars a shipped game carries.
    /// </summary>
    public static IReadOnlyList<string> Carried(string suffix)
    {
        lock (Gate)
        {
            return [.. _carried.Keys.Where(path => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))];
        }
    }

    /// <summary>A carried file opened for reading, or nothing when the game carries no such file.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    /// <remarks>
    /// Seekable, since a resource is a view of the loaded assembly and a packed file is read at
    /// positions, so <see cref="Streaming"/> reads a part of one as it reads a part of a file on disk.
    /// </remarks>
    internal static Stream? OpenCarried(string path) => Opener(path)?.Invoke();

    /// <summary>
    /// Hands the bridge a reader over the files the game carries, so the next app built reads them
    /// through Bevy as well, or takes it back when the game carries none.
    /// </summary>
    /// <remarks>
    /// Called as an app is created, before the native app is, and again as a pack is mounted or
    /// unmounted while it runs, since the bridge's source reads the list it was last given at each
    /// read. The bridge is given the list of paths along with the reader, so whether a file or a
    /// folder is there is answered on its side, and only reading a file's bytes crosses over. A
    /// bridge built with <c>--embed</c> carries the assets itself and ignores this.
    /// </remarks>
    internal static void Serve()
    {
        string[] paths;
        lock (Gate) paths = [.. _carried.Keys];

        if (paths.Length == 0)
        {
            Native.Check(Native.bcs_assets_carried(null, null, 0), "taking back the assets the game carries");
            return;
        }

        var joined = Encoding.UTF8.GetBytes(string.Join('\n', paths));

        fixed (byte* list = joined)
        {
            Native.Check(Native.bcs_assets_carried(&ReadCarried, list, (nuint)joined.Length), "handing the bridge the assets the game carries");
        }
    }

    /// <summary>
    /// What the bridge calls for a carried file's bytes, with its path, where to copy them and how
    /// much room there is, answering its length, or a negative number for a file it does not carry.
    /// </summary>
    /// <remarks>
    /// Called from Bevy's loading threads, twice a file, once with no room to learn the length and
    /// once with room for it, so the bytes land in memory the bridge owns. A resource is a view of
    /// the loaded assembly and a packed file's length is in the pack's index, so opening one twice
    /// reads it once.
    /// </remarks>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static long ReadCarried(byte* path, nuint length, byte* dest, nuint room)
    {
        try
        {
            var name = Encoding.UTF8.GetString(path, checked((int)length));

            Func<Stream?>? open;
            lock (Gate) open = _carried.GetValueOrDefault(name);

            if (open?.Invoke() is not { } stream) return -1;

            using (stream)
            {
                var size = stream.Length;
                if (dest is not null && (ulong)size <= room) stream.ReadExactly(new Span<byte>(dest, checked((int)size)));

                return size;
            }
        }
        catch (Exception error)
        {
            // Nothing may unwind into Bevy's loader, so a file that cannot be read is one it lacks.
            EngineLog.Error(null, "assets", $"[BevyCSharp] a carried asset could not be read: {error.Message}", error);
            return -1;
        }
    }

    /// <summary>The path relative to the asset root a full one names, or nothing for one outside it.</summary>
    internal static string? Relative(string path)
    {
        if (!Path.IsPathRooted(path)) return path.Replace('\\', '/');

        var root = Path.GetFullPath(AssetIds.Root);
        var full = Path.GetFullPath(path);
        var relative = Path.GetRelativePath(root, full);

        return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
            ? null
            : relative.Replace('\\', '/');
    }

    private static string Full(string path) => Path.IsPathRooted(path) ? path : Path.Combine(AssetIds.Root, path);

    private static Func<Stream?>? Opener(string path)
    {
        if (Relative(path) is not { } relative) return null;

        lock (Gate) return _carried.GetValueOrDefault(relative);
    }
}
