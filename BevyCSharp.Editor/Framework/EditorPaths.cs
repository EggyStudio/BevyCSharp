namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Where the editor keeps what it writes.
/// </summary>
/// <remarks>
/// Beside the page, in the asset directory, so that the world's edits and the editor's
/// preferences are ordinary files, edited by hand, diffed, and shipped with everything else the
/// editor is made of. A file written anywhere else would be a second kind of state with a second
/// set of rules.
/// </remarks>
public static class EditorPaths
{
    /// <summary>The editor's own files, its fonts, icons and theme, beside the running build.</summary>
    public static string Own => Path.Combine(AppContext.BaseDirectory, "assets");

    /// <summary>The name the editor's own folder is read under by Bevy, as <c>editor://</c>.</summary>
    public const string OwnSource = "editor";

    /// <summary>
    /// The assets being edited, the project's when one was opened (<see cref="Project"/>) and the
    /// editor's own otherwise.
    /// </summary>
    /// <remarks>
    /// Where the scene, the scripts, the project file and everything the asset browser shows live,
    /// and the root Bevy reads assets from, so a project's files load by the paths the game uses.
    /// </remarks>
    public static string Assets { get; set; } = Own;

    /// <summary>The project folder opened with <c>--project</c>, or nothing for the editor's own assets.</summary>
    public static string? Project { get; set; }

    /// <summary>A file in the asset directory.</summary>
    public static string Asset(string name) => Path.Combine(Assets, name);

    /// <summary>The edits made to the world.</summary>
    public static string World => Asset("world.json");

    private static string? _scene;

    /// <summary>The scene the editor saves and loads, which is the project's document.</summary>
    /// <remarks>
    /// <para>
    /// The project's startup scene when its <c>project.json</c> names one, so a game whose levels
    /// are in a folder of their own opens on the one it starts in, and <c>world.scene.json</c> for
    /// a project that names none. Settled the first time it is asked for, so naming another
    /// startup scene later does not move a save made afterward.
    /// </para>
    /// <para>
    /// Set by opening another scene or saving under another name, as <c>world.load</c> and
    /// <c>world.save</c> do, so the next save goes where the scene being edited came from.
    /// </para>
    /// </remarks>
    public static string Scene
    {
        get => _scene ??= EditorProjectFile.Settings.StartupScene is { Length: > 0 } startup
            ? Path.GetFullPath(Asset(startup))
            : Asset("world.scene.json");
        set => _scene = value;
    }

    /// <summary>Where the editor's own preferences are kept.</summary>
    public static string Settings => Asset("settings.txt");
}
