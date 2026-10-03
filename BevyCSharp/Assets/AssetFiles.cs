using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Reads the managed side's files under <c>assets://</c> from the asset folder, or from the game's
/// own assembly when the files were compiled into it.
/// </summary>
/// <remarks>
/// <para>
/// The managed side reads scenes, data assets and material and mesh files for itself, and those go
/// through here. Bevy reads textures, models and sounds through its own asset sources, and when the
/// assembly carries files, the bridge's default source is handed a reader over the same resources
/// (<see cref="Serve"/>), so one copy of each file feeds both sides and a game ships with no asset
/// folder and the bridge every game shares.
/// </para>
/// <para>
/// Two layers, the folder over the resources. A file on disk wins, so a game run from its project
/// reads what is being edited, and a shipped one reads what it carries unless a file beside it
/// replaces one. A resource is one whose name starts with <c>assets/</c>, which a build makes from
/// the asset folder when <c>BevyCSharpEmbedAssets</c> is set (the Play tab's export sets it when it
/// embeds). The entry assembly is the one looked in, set as an app starts.
/// </para>
/// <para>
/// Only reads come here. Writing a scene or a data asset writes the folder, since what is compiled
/// into an assembly cannot be written, and only a project being edited is written at all.
/// </para>
/// </remarks>
public static unsafe class AssetFiles
{
    /// <summary>What a resource's name starts with when it is a file under the asset root.</summary>
    public const string ResourcePrefix = "assets/";

    private static readonly object Gate = new();
    private static Assembly? _assembly;
    private static Dictionary<string, string> _resources = new(StringComparer.Ordinal);

    /// <summary>Whether any file under the asset root is read from the game's assembly.</summary>
    public static bool HasResources
    {
        get
        {
            lock (Gate) return _resources.Count > 0;
        }
    }

    /// <summary>
    /// Looks for the asset files in an assembly's resources, or in none.
    /// </summary>
    /// <param name="assembly">Usually the entry assembly, which is the game's.</param>
    /// <remarks>
    /// Names are kept with forward slashes whatever the build wrote, since a resource made from a
    /// folder on Windows is named with the separator that platform uses.
    /// </remarks>
    public static void Use(Assembly? assembly)
    {
        var found = new Dictionary<string, string>(StringComparer.Ordinal);

        if (assembly is not null && !assembly.IsDynamic)
        {
            foreach (var name in assembly.GetManifestResourceNames())
            {
                var normal = name.Replace('\\', '/');
                if (normal.StartsWith(ResourcePrefix, StringComparison.Ordinal)) found[normal[ResourcePrefix.Length..]] = name;
            }
        }

        lock (Gate)
        {
            _assembly = assembly;
            _resources = found;
        }
    }

    /// <summary>Whether a file is there, on disk or among the resources.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    public static bool Exists(string path) => File.Exists(Full(path)) || Resource(path) is not null;

    /// <summary>A file's text, from disk or from the resources.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    /// <exception cref="FileNotFoundException">Neither has it.</exception>
    public static string ReadAllText(string path) => Encoding.UTF8.GetString(ReadAllBytes(path));

    /// <summary>A file's bytes, from disk or from the resources.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    /// <exception cref="FileNotFoundException">Neither has it.</exception>
    public static byte[] ReadAllBytes(string path)
    {
        var full = Full(path);
        if (File.Exists(full)) return File.ReadAllBytes(full);

        if (Resource(path) is { } name && Open(name) is { } stream)
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
    /// The paths under the asset root of every resource ending in <paramref name="suffix"/>, such
    /// as the id sidecars a shipped game carries in its assembly.
    /// </summary>
    public static IReadOnlyList<string> Embedded(string suffix)
    {
        lock (Gate)
        {
            return [.. _resources.Keys.Where(path => path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))];
        }
    }

    /// <summary>A carried file opened for reading, or nothing when the assembly carries no such file.</summary>
    /// <param name="path">A full path, or one relative to the asset root.</param>
    /// <remarks>
    /// Seekable, since a resource is a view of the loaded assembly, so <see cref="Streaming"/> reads
    /// a part of one as it reads a part of a file on disk.
    /// </remarks>
    internal static Stream? OpenCarried(string path) => Resource(path) is { } name ? Open(name) : null;

    /// <summary>
    /// Hands the bridge a reader over the files the assembly carries, so the next app built reads
    /// them through Bevy as well, or takes it back when the assembly carries none.
    /// </summary>
    /// <remarks>
    /// Called as an app is created, before the native app is, since Bevy builds its asset sources as
    /// the app is built and never again. The bridge is given the list of paths along with the
    /// reader, so whether a file or a folder is there is answered on its side, and only reading a
    /// file's bytes crosses over. A bridge built with <c>--embed</c> carries the assets itself and
    /// ignores this.
    /// </remarks>
    internal static void Serve()
    {
        string[] paths;
        lock (Gate) paths = [.. _resources.Keys];

        if (paths.Length == 0)
        {
            Native.Check(Native.bcs_assets_carried(null, null, 0), "taking back the assets the assembly carries");
            return;
        }

        var joined = Encoding.UTF8.GetBytes(string.Join('\n', paths));

        fixed (byte* list = joined)
        {
            Native.Check(Native.bcs_assets_carried(&ReadCarried, list, (nuint)joined.Length), "handing the bridge the assets the assembly carries");
        }
    }

    /// <summary>
    /// What the bridge calls for a carried file's bytes, with its path, where to copy them and how
    /// much room there is, answering its length, or a negative number for a file it does not carry.
    /// </summary>
    /// <remarks>
    /// Called from Bevy's loading threads, twice a file, once with no room to learn the length and
    /// once with room for it, so the bytes land in memory the bridge owns. A resource is a view of
    /// the loaded assembly, so opening it twice costs no read of the file.
    /// </remarks>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static long ReadCarried(byte* path, nuint length, byte* dest, nuint room)
    {
        try
        {
            var name = Encoding.UTF8.GetString(path, checked((int)length));

            string? resource;
            lock (Gate) resource = _resources.GetValueOrDefault(name);

            if (resource is null || Open(resource) is not { } stream) return -1;

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
            Console.Error.WriteLine($"[BevyCSharp] a carried asset could not be read: {error.Message}");
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

    private static string? Resource(string path)
    {
        if (Relative(path) is not { } relative) return null;

        lock (Gate) return _resources.GetValueOrDefault(relative);
    }

    private static Stream? Open(string name)
    {
        lock (Gate) return _assembly?.GetManifestResourceStream(name);
    }
}
