namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The pictures the editor draws, by name.
/// </summary>
/// <remarks>
/// <para>
/// One table, because a button's picture is a thing somebody might want to change without going
/// looking for the line that draws the button, and because the same picture stands for the same
/// idea in three places at once. A mesh is a mesh in the menu that spawns one, in the list of what
/// is in the world, and in the browser that shows the file.
/// </para>
/// <para>
/// Paths under the asset root rather than pictures, because what loads one is the interface's own
/// texture table and it loads by path, once, whenever a name is first drawn.
/// </para>
/// </remarks>
public static class EditorIcons
{
    /// <summary>Everything the editor can do.</summary>
    public static string Menu { get; set; } = "icons/ui/menu.png";

    /// <summary>Take back the last change.</summary>
    public static string Undo { get; set; } = "icons/ui/undo.png";

    /// <summary>Put it back.</summary>
    public static string Redo { get; set; } = "icons/ui/redo.png";

    /// <summary>Write the world and the settings.</summary>
    public static string Save { get; set; } = "icons/ui/save.png";

    /// <summary>Read them back.</summary>
    public static string Load { get; set; } = "icons/ui/folder.png";

    /// <summary>Add something.</summary>
    public static string Add { get; set; } = "icons/ui/add.png";

    /// <summary>Take something off what is selected.</summary>
    public static string Remove { get; set; } = "icons/ui/remove.png";

    /// <summary>Take something out of the world.</summary>
    public static string Delete { get; set; } = "icons/ui/delete.png";

    /// <summary>What the editor is doing, or what the keys do.</summary>
    public static string Info { get; set; } = "icons/ui/info.png";

    /// <summary>The panel is against the window's edge.</summary>
    public static string Pinned { get; set; } = "icons/ui/pin.png";

    /// <summary>And is floating over the scene.</summary>
    public static string Loose { get; set; } = "icons/ui/pinned.png";

    /// <summary>Snapping to a grid.</summary>
    public static string Snap { get; set; } = "icons/ui/snap.png";

    /// <summary>The ground grid.</summary>
    public static string Grid { get; set; } = "icons/ui/grid.png";

    /// <summary>The panels themselves.</summary>
    public static string Interface { get; set; } = "icons/ui/interface.png";

    /// <summary>What the view shows.</summary>
    public static string View { get; set; } = "icons/ui/image.png";

    /// <summary>The project, as a thing on disk.</summary>
    public static string Project { get; set; } = "icons/ui/package.png";

    /// <summary>Something to hear, wearing a project's picture until there is a better one.</summary>
    public static string Sound { get; set; } = "icons/ui/package.png";

    /// <summary>A thing in the world with nothing on it yet.</summary>
    public static string Entity { get; set; } = "icons/ui/entity.png";

    /// <summary>A box.</summary>
    public static string Cube { get; set; } = "icons/ui/cube.png";

    /// <summary>Anything else with a shape.</summary>
    public static string Mesh { get; set; } = "icons/ui/mesh.png";

    /// <summary>Something that lights the scene.</summary>
    public static string Light { get; set; } = "icons/ui/light.png";

    /// <summary>Something that looks at it.</summary>
    public static string Camera { get; set; } = "icons/ui/camera.png";

    /// <summary>A saved world.</summary>
    public static string World { get; set; } = "icons/ui/world.png";

    /// <summary>What a behavior is written in.</summary>
    public static string Script { get; set; } = "icons/ui/script.png";

    /// <summary>A picture.</summary>
    public static string Image { get; set; } = "icons/ui/image.png";

    /// <summary>Numbers in a file.</summary>
    public static string Data { get; set; } = "icons/ui/data.png";

    /// <summary>Words in one.</summary>
    public static string Text { get; set; } = "icons/ui/terminal.png";

    /// <summary>Anything the editor has nothing better to say about.</summary>
    public static string File { get; set; } = "icons/ui/file.png";

    /// <summary>A folder.</summary>
    public static string Folder { get; set; } = "icons/ui/folder.png";

    /// <summary>What is selected.</summary>
    public static string Select { get; set; } = "icons/ui/select.png";

    /// <summary>The handles that move it.</summary>
    public static string Move { get; set; } = "icons/ui/move.png";

    /// <summary>The picture for a tool, which is named after it.</summary>
    /// <param name="tool">Which tool.</param>
    public static string For(EditorTool tool) =>
        $"icons/ui/{tool.ToString().ToLowerInvariant()}.png";
}
