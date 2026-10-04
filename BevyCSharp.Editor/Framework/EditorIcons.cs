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
/// Paths rather than pictures, because what loads one is the interface's own texture table and it
/// loads by path, once, whenever a name is first drawn. Under <c>editor://</c>, the editor's own
/// folder, so they are found whatever project's assets are open.
/// </para>
/// </remarks>
public static class EditorIcons
{
    /// <summary>Everything the editor can do.</summary>
    public static string Menu { get; set; } = "editor://icons/ui/menu.png";

    /// <summary>Run the game in a window of its own.</summary>
    public static string Play { get; set; } = "editor://icons/ui/play.png";

    /// <summary>Take back the last change.</summary>
    public static string Undo { get; set; } = "editor://icons/ui/undo.png";

    /// <summary>Put it back.</summary>
    public static string Redo { get; set; } = "editor://icons/ui/redo.png";

    /// <summary>Write the world and the settings.</summary>
    public static string Save { get; set; } = "editor://icons/ui/save.png";

    /// <summary>Read them back.</summary>
    public static string Load { get; set; } = "editor://icons/ui/folder.png";

    /// <summary>Add something.</summary>
    public static string Add { get; set; } = "editor://icons/ui/add.png";

    /// <summary>Take something off what is selected.</summary>
    public static string Remove { get; set; } = "editor://icons/ui/remove.png";

    /// <summary>Take something out of the world.</summary>
    public static string Delete { get; set; } = "editor://icons/ui/delete.png";

    /// <summary>What the editor is doing, or what the keys do.</summary>
    public static string Info { get; set; } = "editor://icons/ui/info.png";

    /// <summary>The panel is against the window's edge.</summary>
    public static string Pinned { get; set; } = "editor://icons/ui/pin.png";

    /// <summary>And is floating over the scene.</summary>
    public static string Loose { get; set; } = "editor://icons/ui/pinned.png";

    /// <summary>Snapping to a grid.</summary>
    public static string Snap { get; set; } = "editor://icons/ui/snap.png";

    /// <summary>The ground grid.</summary>
    public static string Grid { get; set; } = "editor://icons/ui/grid.png";

    /// <summary>The panels themselves.</summary>
    public static string Interface { get; set; } = "editor://icons/ui/interface.png";

    /// <summary>What the view shows.</summary>
    public static string View { get; set; } = "editor://icons/ui/image.png";

    /// <summary>The project, as a thing on disk.</summary>
    public static string Project { get; set; } = "editor://icons/ui/package.png";

    /// <summary>Something to hear, wearing a project's picture until there is a better one.</summary>
    public static string Sound { get; set; } = "editor://icons/ui/package.png";

    /// <summary>A thing in the world with nothing on it yet.</summary>
    public static string Entity { get; set; } = "editor://icons/ui/entity.png";

    /// <summary>A box.</summary>
    public static string Cube { get; set; } = "editor://icons/ui/cube.png";

    /// <summary>Anything else with a shape.</summary>
    public static string Mesh { get; set; } = "editor://icons/ui/mesh.png";

    /// <summary>Something that lights the scene.</summary>
    public static string Light { get; set; } = "editor://icons/ui/light.png";

    /// <summary>Something that looks at it.</summary>
    public static string Camera { get; set; } = "editor://icons/ui/camera.png";

    /// <summary>A saved world.</summary>
    public static string World { get; set; } = "editor://icons/ui/world.png";

    /// <summary>What a behavior is written in.</summary>
    public static string Script { get; set; } = "editor://icons/ui/script.png";

    /// <summary>A picture.</summary>
    public static string Image { get; set; } = "editor://icons/ui/image.png";

    /// <summary>Numbers in a file.</summary>
    public static string Data { get; set; } = "editor://icons/ui/data.png";

    /// <summary>Words in one.</summary>
    public static string Text { get; set; } = "editor://icons/ui/terminal.png";

    /// <summary>Anything the editor has nothing better to say about.</summary>
    public static string File { get; set; } = "editor://icons/ui/file.png";

    /// <summary>A folder.</summary>
    public static string Folder { get; set; } = "editor://icons/ui/folder.png";

    /// <summary>What is selected.</summary>
    public static string Select { get; set; } = "editor://icons/ui/select.png";

    /// <summary>The handles that move it.</summary>
    public static string Move { get; set; } = "editor://icons/ui/move.png";

    /// <summary>The picture for a tool, which is named after it.</summary>
    /// <param name="tool">Which tool.</param>
    public static string For(EditorTool tool) =>
        $"editor://icons/ui/{tool.ToString().ToLowerInvariant()}.png";
}
