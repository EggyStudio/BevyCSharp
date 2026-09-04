using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The strip of buttons along the top.
/// </summary>
/// <remarks>
/// <para>
/// A toolbar is a panel whose contents are buttons. There is no toolbar type, no toolbar region
/// and no toolbar mechanism: it is three files like every other panel, and what makes it a
/// toolbar is that its document holds buttons and its placement is the top of the window.
/// </para>
/// <para>
/// Each button opens a panel or closes it again, which is the whole reason the shell registers
/// panels by instance: the toolbar constructs one when it is wanted and drops it when it is not.
/// </para>
/// </remarks>
[EditorPanel("panels/toolbar.html", Root = "#toolbar", Region = EditorRegion.Top, Layer = 10)]
public sealed partial class ToolbarPanel(Entity camera)
{
    /// <summary>Shows or hides the hierarchy.</summary>
    [Command("#tb-hierarchy")]
    public void Hierarchy() => EditorShell.Toggle(() => new HierarchyPanel());

    /// <summary>Shows or hides the inspector.</summary>
    [Command("#tb-inspector")]
    public void Inspector() => EditorShell.Toggle(() => new InspectorPanel());

    /// <summary>Shows or hides the camera's post processing.</summary>
    [Command("#tb-post")]
    public void Post() => EditorShell.Toggle(() => new PostPanel(camera));

    /// <summary>Shows or hides the status strip.</summary>
    [Command("#tb-stats")]
    public void Stats() => EditorShell.Toggle(() => new StatsPanel());

    /// <summary>
    /// Shows the key list as a flyout.
    /// </summary>
    /// <remarks>
    /// Opened at a point rather than in a region, because that is what a flyout is: a panel
    /// placed where the thing that opened it is, dismissed by a press anywhere else.
    /// </remarks>
    [Command("#tb-keys")]
    public void Keys()
    {
        if (EditorShell.Find<KeysPanel>() is { } open)
        {
            EditorShell.Hide(open);
            return;
        }

        // Under the button that opened it, which is what makes a flyout read as belonging to the
        // thing it came from rather than as another panel that happened to appear.
        var under = Window is { } window && Xui.TryRect(window.Element("tb-keys"), out var button)
            ? (button.X, button.Bottom + 6f)
            : (12f, 44f);

        EditorShell.ShowAt(new KeysPanel(), under.Item1, under.Item2);
    }

    /// <summary>Writes the edits to the world file.</summary>
    /// <remarks>
    /// Beside the panels rather than beside the program: the file lives in the asset directory,
    /// so it is edited, diffed and shipped like any other asset.
    /// </remarks>
    [Command("#tb-save")]
    public void Save()
    {
        var written = EditorWorld.Save(EditorShell.Ecs, EditorPaths.World);
        File.WriteAllText(EditorPaths.Layout, EditorShell.Layout.Describe());

        Console.WriteLine($"[editor] saved {written} entities and the layout to {EditorPaths.Assets}");
    }

    /// <summary>Puts the saved edits and the saved arrangement back.</summary>
    [Command("#tb-load")]
    public void Load()
    {
        var applied = EditorWorld.Load(EditorShell.Ecs, EditorPaths.World);

        if (File.Exists(EditorPaths.Layout))
            EditorShell.Layout.Restore(File.ReadAllText(EditorPaths.Layout));

        Console.WriteLine($"[editor] applied {applied} entities and the layout");
    }

    /// <summary>Puts every panel back where its own declaration says.</summary>
    [Command("#tb-layout")]
    public void ResetLayout() => EditorShell.Layout.ResetAll();

}
