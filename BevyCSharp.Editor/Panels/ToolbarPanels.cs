using Bevy;
using BevyCSharp.Editor.Behaviors;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// The buttons in the viewport's top left corner.
/// </summary>
/// <remarks>
/// <para>
/// Three panels for three corners, because a panel places one element and these are in three
/// places. What is in them is not decided here: each reads its slot from
/// <see cref="EditorToolbar"/>, so adding a button is a line of registration and the panels never
/// change.
/// </para>
/// <para>
/// The left corner is where the menu lives, and beside it whatever acts on the editor as a whole.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/barleft.html",
    Root = "#barleft",
    Dock = EditorDock.ViewportTopLeft,
    Layer = 20)]
public sealed partial class LeftBarPanel
{
    /// <summary>How many buttons the document can draw.</summary>
    public const int Buttons = 6;

    /// <summary>Which slot this panel shows.</summary>
    private const ToolbarSlot Slot = ToolbarSlot.Left;

    /// <summary>What each button says beside its icon.</summary>
    [Bind("#tltxt", Count = Buttons)]
    public string[] Labels = new string[Buttons];

    /// <summary>Which buttons stand for anything.</summary>
    [Show("#tl", Count = Buttons)]
    public bool[] Shown = new bool[Buttons];

    /// <summary>Which buttons draw a label as well as a picture.</summary>
    [Show("#tltxt", Count = Buttons)]
    public bool[] Labelled = new bool[Buttons];

    /// <summary>Which buttons draw a picture.</summary>
    [Show("#tlimg", Count = Buttons)]
    public bool[] Pictured = new bool[Buttons];

    /// <summary>Which buttons are the one in force.</summary>
    [Show("#tldot", Count = Buttons)]
    public bool[] Active = new bool[Buttons];

    /// <summary>Reads the slot.</summary>
    [OnRefresh]
    public void Fill() =>
        ToolbarSlots.Fill(
            Slot, "tl", Window, Labels, Shown, Labelled, Pictured, Active, Buttons);

    /// <summary>Runs whichever was pressed.</summary>
    [Command("#tl", Count = Buttons)]
    public void Press(int index) => ToolbarSlots.Press(Slot, index);
}

/// <summary>The buttons along the top of the viewport: what a drag does.</summary>
[EditorPanel(
    "panels/barcentre.html",
    Root = "#barcentre",
    Dock = EditorDock.ViewportTop,
    Layer = 20)]
public sealed partial class CentreBarPanel
{
    /// <summary>How many buttons the document can draw.</summary>
    public const int Buttons = 8;

    /// <summary>Which slot this panel shows.</summary>
    private const ToolbarSlot Slot = ToolbarSlot.Centre;

    /// <summary>What each button says beside its icon.</summary>
    [Bind("#tctxt", Count = Buttons)]
    public string[] Labels = new string[Buttons];

    /// <summary>Which buttons stand for anything.</summary>
    [Show("#tc", Count = Buttons)]
    public bool[] Shown = new bool[Buttons];

    /// <summary>Which buttons draw a label as well as a picture.</summary>
    [Show("#tctxt", Count = Buttons)]
    public bool[] Labelled = new bool[Buttons];

    /// <summary>Which buttons draw a picture.</summary>
    [Show("#tcimg", Count = Buttons)]
    public bool[] Pictured = new bool[Buttons];

    /// <summary>Which buttons are the one in force.</summary>
    [Show("#tcdot", Count = Buttons)]
    public bool[] Active = new bool[Buttons];

    /// <summary>Reads the slot.</summary>
    [OnRefresh]
    public void Fill() =>
        ToolbarSlots.Fill(
            Slot, "tc", Window, Labels, Shown, Labelled, Pictured, Active, Buttons);

    /// <summary>Runs whichever was pressed.</summary>
    [Command("#tc", Count = Buttons)]
    public void Press(int index) => ToolbarSlots.Press(Slot, index);
}

/// <summary>The buttons in the viewport's top right corner: what the editor is doing.</summary>
[EditorPanel(
    "panels/barright.html",
    Root = "#barright",
    Dock = EditorDock.ViewportTopRight,
    Layer = 20)]
public sealed partial class RightBarPanel
{
    /// <summary>How many buttons the document can draw.</summary>
    public const int Buttons = 4;

    /// <summary>Which slot this panel shows.</summary>
    private const ToolbarSlot Slot = ToolbarSlot.Right;

    /// <summary>What each button says beside its icon.</summary>
    [Bind("#trtxt", Count = Buttons)]
    public string[] Labels = new string[Buttons];

    /// <summary>Which buttons stand for anything.</summary>
    [Show("#tr", Count = Buttons)]
    public bool[] Shown = new bool[Buttons];

    /// <summary>Which buttons draw a label as well as a picture.</summary>
    [Show("#trtxt", Count = Buttons)]
    public bool[] Labelled = new bool[Buttons];

    /// <summary>Which buttons draw a picture.</summary>
    [Show("#trimg", Count = Buttons)]
    public bool[] Pictured = new bool[Buttons];

    /// <summary>Which buttons are the one in force.</summary>
    [Show("#trdot", Count = Buttons)]
    public bool[] Active = new bool[Buttons];

    /// <summary>Reads the slot.</summary>
    [OnRefresh]
    public void Fill() =>
        ToolbarSlots.Fill(
            Slot, "tr", Window, Labels, Shown, Labelled, Pictured, Active, Buttons);

    /// <summary>Runs whichever was pressed.</summary>
    [Command("#tr", Count = Buttons)]
    public void Press(int index) => ToolbarSlots.Press(Slot, index);
}

/// <summary>
/// The buttons in the viewport's bottom right corner, and the orientation cross beside them.
/// </summary>
/// <remarks>
/// <para>
/// The corner nearest the panels a person works in and furthest from the scene they are looking
/// at, which is where what describes the view belongs rather than what changes it.
/// </para>
/// <para>
/// The cross is not a picture. It is three lines drawn in the scene, at the point this panel
/// reports, so it turns with the camera without anything being rendered to a texture and without
/// the editor tracking a screen position of its own: the layout puts the square somewhere and the
/// square says where it ended up.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/barbottom.html",
    Root = "#barbottom",
    Dock = EditorDock.ViewportBottomRight,
    Layer = 20)]
public sealed partial class BottomBarPanel
{
    /// <summary>How many buttons the document can draw.</summary>
    public const int Buttons = 4;

    /// <summary>Which slot this panel shows.</summary>
    private const ToolbarSlot Slot = ToolbarSlot.BottomRight;

    /// <summary>What each button says beside its icon.</summary>
    [Bind("#tbtxt", Count = Buttons)]
    public string[] Labels = new string[Buttons];

    /// <summary>Which buttons stand for anything.</summary>
    [Show("#tb", Count = Buttons)]
    public bool[] Shown = new bool[Buttons];

    /// <summary>Which buttons draw a label as well as a picture.</summary>
    [Show("#tbtxt", Count = Buttons)]
    public bool[] Labelled = new bool[Buttons];

    /// <summary>Which buttons draw a picture.</summary>
    [Show("#tbimg", Count = Buttons)]
    public bool[] Pictured = new bool[Buttons];

    /// <summary>Which buttons are the one in force.</summary>
    [Show("#tbdot", Count = Buttons)]
    public bool[] Active = new bool[Buttons];

    /// <summary>Reads the slot, and reports where the cross goes.</summary>
    [OnRefresh]
    public void Fill()
    {
        ToolbarSlots.Fill(
            Slot, "tb", Window, Labels, Shown, Labelled, Pictured, Active, Buttons);

        EditorGizmoSlot.Report(Window?.Element("tb-cross") ?? Entity.None);
    }

    /// <summary>Runs whichever was pressed.</summary>
    [Command("#tb", Count = Buttons)]
    public void Press(int index) => ToolbarSlots.Press(Slot, index);

    /// <summary>Puts the camera's horizon back level.</summary>
    [Command("#tb-cross")]
    public void Level() => FlyCamera.LevelWanted = true;
}

/// <summary>What the three corner panels have in common.</summary>
/// <remarks>
/// Written once here rather than three times over: the panels differ only in which slot they read
/// and which elements they write, and both of those are their declarations rather than their
/// behavior.
/// </remarks>
internal static class ToolbarSlots
{
    /// <summary>
    /// Writes a slot's buttons into a panel's arrays, and points each picture at its file.
    /// </summary>
    /// <remarks>
    /// The picture is set here rather than bound, because an image is not one of the values a
    /// widget carries: it is a path the interface loads from, and the panel says which.
    /// </remarks>
    internal static void Fill(
        ToolbarSlot slot,
        string prefix,
        EditorWindow? window,
        string[] labels,
        bool[] shown,
        bool[] labelled,
        bool[] pictured,
        bool[] active,
        int count)
    {
        var buttons = EditorToolbar.Slot(slot);

        for (var i = 0; i < count; i++)
        {
            if (i >= buttons.Count)
            {
                labels[i] = string.Empty;
                shown[i] = false;
                labelled[i] = false;
                pictured[i] = false;
                active[i] = false;
                continue;
            }

            var button = buttons[i];
            var text = button.Label();

            labels[i] = text;
            shown[i] = true;
            labelled[i] = text.Length > 0;
            pictured[i] = button.Icon is not null;

            // The one in force wears a dot, since a class cannot be given to an element while the
            // editor runs and a mark in the text would move everything beside it.
            active[i] = button.Active?.Invoke() == true;

            if (button.Icon is { } icon && window is not null)
            {
                var element = window.Element($"{prefix}img-{i}");
                if (!element.IsNone) Xui.SetImage(element, icon);
            }
        }
    }

    /// <summary>Runs the button at an index of a slot.</summary>
    internal static void Press(ToolbarSlot slot, int index)
    {
        var buttons = EditorToolbar.Slot(slot);
        if (index >= buttons.Count) return;

        buttons[index].Run(EditorShell.Ecs);
    }
}
