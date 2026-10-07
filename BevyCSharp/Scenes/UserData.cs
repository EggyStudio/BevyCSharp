namespace Bevy;

/// <summary>
/// The directory a game keeps its own files in, named in a path by <c>user://</c>.
/// </summary>
/// <remarks>
/// <para>
/// Writable and the player's: settings, saves, anything the game writes. It is the platform's data
/// directory under the game's name, <c>$XDG_DATA_HOME</c> or <c>~/.local/share</c> on Linux,
/// <c>%APPDATA%</c> on Windows and <c>~/Library/Application Support</c> on macOS, so a game's
/// files sit where the platform's own tools and backups look for them. <c>assets://</c> is the
/// other root, the game's content, which a shipped game cannot write to.
/// </para>
/// <para>
/// The name is <see cref="Config.GameName"/>, or the title, set when an app starts. A test or a
/// tool can point <see cref="Root"/> somewhere of its own instead.
/// </para>
/// <para>
/// The managed side resolves these paths, and an app registers the directory with Bevy as its
/// <c>user</c> asset source as it starts, making it first, so a texture or a model the game wrote
/// there loads through <see cref="AssetServer"/> as <c>user://…</c>. The source is built once, so
/// a <see cref="Root"/> changed after an app starts reaches the managed side and not Bevy.
/// </para>
/// </remarks>
public static class UserData
{
    /// <summary>The prefix naming a path under the player's directory.</summary>
    public const string Prefix = "user://";

    private static string? _root;

    /// <summary>The name of the game, which its directory is called.</summary>
    /// <remarks>Characters a file name cannot hold are replaced, so any title makes a directory.</remarks>
    public static string Name { get; set; } = "BevyCSharp";

    /// <summary>
    /// The directory itself, the platform's data directory under the game's name unless set.
    /// </summary>
    /// <remarks>Set to <see langword="null"/> to go back to the platform's.</remarks>
    public static string Root
    {
        get => _root ?? Path.Combine(PlatformDirectory(), Safe(Name));
        set => _root = value;
    }

    /// <summary>The full path of a <c>user://</c> path, or of a path relative to the directory.</summary>
    public static string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (path.StartsWith(Prefix, StringComparison.Ordinal)) path = path[Prefix.Length..];
        return Path.Combine(Root, path.Replace('/', Path.DirectorySeparatorChar));
    }

    /// <summary>Writes text to a file through a temporary one renamed over it.</summary>
    /// <remarks>
    /// The rename replaces the old file whole, so a crash while writing leaves the last version
    /// rather than half of the new one. The directory is made if it is not there.
    /// </remarks>
    public static void WriteAtomically(string full, ReadOnlySpan<byte> bytes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(full))!);

        var temporary = full + ".tmp";
        using (var file = File.Create(temporary)) file.Write(bytes);
        File.Move(temporary, full, overwrite: true);
    }

    /// <summary>The platform's directory for an application's own data.</summary>
    internal static string PlatformDirectory() =>
        Environment.GetFolderPath(
            OperatingSystem.IsWindows()
                ? Environment.SpecialFolder.ApplicationData
                : Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

    /// <summary>A name with what a file name cannot hold replaced.</summary>
    private static string Safe(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string([.. name.Select(character => invalid.Contains(character) ? '_' : character)]).Trim();
        return safe.Length == 0 ? "BevyCSharp" : safe;
    }
}
