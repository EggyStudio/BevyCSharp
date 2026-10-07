using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The project's own settings as the editor edits them, in <see cref="ProjectSettings.FileName"/>
/// beside the scene.
/// </summary>
/// <remarks>
/// <para>
/// Read the first time something asks and written each time something changes, as the Project
/// page, the export row and the style tab change them. Beside the scene rather than with the
/// person's settings, since the startup scene and the fixed step are the game's, and the export's
/// choices and a theme asked to ship travel with the project to whoever opens it next.
/// </para>
/// <para>
/// A file that does not read is said once in the console and treated as empty, and the next change
/// writes a whole one over it, since every setting in it has a default.
/// </para>
/// </remarks>
internal static class EditorProjectFile
{
    private static ProjectSettings? _settings;

    /// <summary>Where the file is.</summary>
    internal static string Path => EditorPaths.Asset(ProjectSettings.FileName);

    /// <summary>The settings, read the first time they are asked for.</summary>
    internal static ProjectSettings Settings => _settings ??= Read();

    /// <summary>Changes the settings and writes the file.</summary>
    internal static void Change(Action<ProjectSettings> change)
    {
        ArgumentNullException.ThrowIfNull(change);

        change(Settings);

        try
        {
            Settings.Write(Path);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"[editor] {ProjectSettings.FileName} was not written: {error.Message}");
        }
    }

    /// <summary>Forgets what was read, so the next ask reads the file again.</summary>
    internal static void Forget() => _settings = null;

    private static ProjectSettings Read()
    {
        var read = ProjectSettings.ReadFrom(EditorPaths.Assets);
        if (read.Problem is { } problem)
            Console.WriteLine($"[editor] {ProjectSettings.FileName} was not read, so the project starts from defaults. {problem}");

        return read;
    }
}
