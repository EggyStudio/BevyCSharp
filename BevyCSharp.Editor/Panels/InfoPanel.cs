using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// How the editor and the world are doing.
/// </summary>
/// <remarks>
/// <para>
/// Opened from the information button rather than kept on screen, because what it says is glanced
/// at rather than worked in. Unpinned it behaves as a flyout and a click anywhere else dismisses
/// it; pinned it moves into the right column and stays until it is closed.
/// </para>
/// <para>
/// That is the whole of pinning: a placement and a dismissal rule, both of which are already
/// things a panel has. Nothing about it needed a new kind of window.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/info.html",
    Root = "#info",
    Handle = "#info-title",
    Dismiss = PanelDismiss.OnOutsideClick,
    Layer = 40)]
public sealed partial class InfoPanel
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 6;

    /// <summary>Each row's label.</summary>
    [Bind("#iname", Count = Rows)]
    public string[] Names = new string[Rows];

    /// <summary>Each row's value.</summary>
    [Bind("#ivalue", Count = Rows)]
    public string[] Values = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#irow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>The pin, which is what keeps the panel where it is.</summary>
    [Bind("#i-pin", Mode = BindMode.OneWay)]
    public string PinIcon => _pinned ? EditorIcons.Pin : EditorIcons.Unpin;

    /// <summary>Whether the panel has been pinned into the column.</summary>
    private bool _pinned;

    /// <summary>How many entities there were when the count was last taken.</summary>
    private int _entities;

    /// <summary>Reads the frame and the world.</summary>
    /// <remarks>
    /// The entity count is taken a few times a second rather than every frame: it walks the whole
    /// world, and nothing about it is worth that sixty times a second.
    /// </remarks>
    [OnRefresh]
    public void Read()
    {
        if (EditorShell.Context is not { } ctx) return;

        if (ctx.Time.FrameCount % 30 == 0) _entities = ctx.Ecs.All().Length;

        var selected = EditorSelection.Current;

        Write(0, "Frame rate", $"{ctx.Time.SmoothedFps:F0} fps");
        Write(1, "Entities", _entities.ToString());
        Write(2, "Selected", selected.IsNone
            ? "nothing"
            : ctx.Ecs.NameOf(selected) ?? $"entity {selected.Index}");
        Write(3, "Tool", EditorTools.Current.ToString());
        Write(4, "Last change", EditorHistory.Last ?? "none");
        Write(5, "Panels", EditorShell.Open.Count.ToString());
    }

    /// <summary>Fills in one row.</summary>
    private void Write(int row, string name, string value)
    {
        Names[row] = name;
        Values[row] = value;
        Shown[row] = true;
    }

    /// <summary>Pins the panel into the column, or lets it float again.</summary>
    [Command("#i-pin")]
    public void Pin()
    {
        _pinned = !_pinned;

        EditorShell.Pin(this, _pinned);
        EditorShell.Layout.Place(
            this,
            _pinned
                ? PanelPlacement.In(EditorDock.Right, order: 20)
                : Chrome.Placement.MovedTo(
                    Window?.Measure()?.X ?? 40f,
                    Window?.Measure()?.Y ?? 40f));
    }
}
