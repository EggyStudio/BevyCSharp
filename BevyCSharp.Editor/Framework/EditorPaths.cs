namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Where the editor keeps what it writes.
/// </summary>
/// <remarks>
/// Beside the panels, in the asset directory, so that the world's edits and the arrangement of
/// the windows are ordinary files: edited by hand, diffed, and shipped with everything else the
/// editor is made of. A file written anywhere else would be a second kind of state with a second
/// set of rules.
/// </remarks>
public static class EditorPaths
{
    /// <summary>The asset directory this build is running out of.</summary>
    public static string Assets => Path.Combine(AppContext.BaseDirectory, "assets");

    /// <summary>A file in the asset directory.</summary>
    public static string Asset(string name) => Path.Combine(Assets, name);

    /// <summary>The edits made to the world.</summary>
    public static string World => Asset("world.json");

    /// <summary>Where the panels are.</summary>
    public static string Layout => Asset("layout.txt");
}
