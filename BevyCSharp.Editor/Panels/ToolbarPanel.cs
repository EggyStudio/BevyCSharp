using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The tools, along the top.
/// </summary>
/// <remarks>
/// <para>
/// Tools and nothing else. A toolbar full of buttons that open panels is a menu that has escaped
/// its menu: what opens a panel belongs in the hamburger, where it can be grouped and searched for
/// and where adding one costs a line rather than a button. What belongs here is the handful of
/// things that change what a drag in the viewport does, because those are switched constantly and
/// have to be visible while working.
/// </para>
/// <para>
/// The information button is the exception, and it is Unity's exception too: what the editor is
/// doing is glanced at rather than opened, so it is one button that shows a panel which can then
/// be pinned.
/// </para>
/// </remarks>
[EditorPanel("panels/toolbar.html", Root = "#toolbar", Dock = EditorDock.Top, Layer = 10)]
public sealed partial class ToolbarPanel
{
    /// <summary>Picks what is under the pointer.</summary>
    [Bind("#t-select", Mode = BindMode.OneWay)]
    public string SelectLabel => Mark(EditorTool.Select, "Select");

    /// <summary>Drags the selection along an axis.</summary>
    [Bind("#t-move", Mode = BindMode.OneWay)]
    public string MoveLabel => Mark(EditorTool.Move, "Move");

    /// <summary>Turns it about one.</summary>
    [Bind("#t-rotate", Mode = BindMode.OneWay)]
    public string RotateLabel => Mark(EditorTool.Rotate, "Rotate");

    /// <summary>Stretches it along one.</summary>
    [Bind("#t-scale", Mode = BindMode.OneWay)]
    public string ScaleLabel => Mark(EditorTool.Scale, "Scale");

    /// <summary>Whether a drag lands on round numbers.</summary>
    [Bind("#t-snap", Mode = BindMode.OneWay)]
    public string SnapLabel => EditorTools.Snap ? EditorIcons.Selected + " Snap" : "  Snap";

    /// <summary>What the editor is doing.</summary>
    [Bind("#t-info", Mode = BindMode.OneWay)]
    public string InfoLabel => EditorIcons.Info;

    /// <summary>Chooses the selection tool.</summary>
    [Command("#t-select")]
    public void Select() => EditorTools.Current = EditorTool.Select;

    /// <summary>Chooses the move tool.</summary>
    [Command("#t-move")]
    public void Move() => EditorTools.Current = EditorTool.Move;

    /// <summary>Chooses the rotate tool.</summary>
    [Command("#t-rotate")]
    public void Rotate() => EditorTools.Current = EditorTool.Rotate;

    /// <summary>Chooses the scale tool.</summary>
    [Command("#t-scale")]
    public void Scale() => EditorTools.Current = EditorTool.Scale;

    /// <summary>Turns snapping on and off.</summary>
    /// <remarks>
    /// Locked on, as opposed to the Control key which turns it on while it is held. Without the
    /// distinction, letting go of Control would quietly undo the button.
    /// </remarks>
    [Command("#t-snap")]
    public void Snap()
    {
        EditorKeys.SnapLocked = !EditorKeys.SnapLocked;
        EditorTools.Snap = EditorKeys.SnapLocked;
    }

    /// <summary>Shows what the editor is doing, under the button that asks.</summary>
    [Command("#t-info")]
    public void Info()
    {
        if (EditorShell.Find<InfoPanel>() is { } open)
        {
            EditorShell.Hide(open);
            return;
        }

        var below = Window is { } window && Xui.TryRect(window.Element("t-info"), out var button)
            ? (button.X - 180f, button.Bottom + 8f)
            : (12f, 44f);

        EditorShell.ShowAt(new InfoPanel(), below.Item1, below.Item2);
    }

    /// <summary>A tool's label, marked when it is the one in force.</summary>
    private static string Mark(EditorTool tool, string label) =>
        EditorTools.Current == tool ? EditorIcons.Selected + " " + label : "  " + label;
}
