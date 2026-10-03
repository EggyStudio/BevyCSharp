using System.Reflection;
using System.Text;

namespace Bevy;

/// <summary>
/// Reads the managed side's files under <c>assets://</c> from the asset folder, or from the game's
/// own assembly when the files were compiled into it.
/// </summary>
/// <remarks>
/// <para>
/// Bevy reads textures, models and sounds through its own asset sources, which a bridge built with
/// <c>--embed</c> serves from bytes compiled into it. The managed side reads scenes, data assets and
/// material and mesh files for itself, and those go through here, so a game whose assets are
/// embedded on both sides ships with no asset folder at all.
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
public static class AssetFiles
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
