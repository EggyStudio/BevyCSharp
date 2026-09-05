using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// How the editor and the world are doing.
/// </summary>
/// <remarks>
/// <para>
/// Opened from the information button rather than kept on screen, because what it says is glanced
/// at rather than worked in. Unpinned it behaves as a flyout and a click anywhere else dismisses
/// it; pinned it stays until it is closed.
/// </para>
/// <para>
/// Pinning changes when it goes away and nothing else. Moving it into a column as well would put
/// it under the panel that describes the selection, which is a long way from the button that
/// opened it and a long way from where somebody's eye already is.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/info.html",
    Root = "#info",
    Dock = EditorDock.ViewportTopRight,
    Y = 34f,
    Dismiss = PanelDismiss.OnOutsideClick,
    Layer = 40)]
public sealed partial class InfoPanel
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 20;

    /// <summary>Each row's label.</summary>
    [Bind("#iname", Count = Rows)]
    public string[] Names = new string[Rows];

    /// <summary>Each row's value.</summary>
    [Bind("#ivalue", Count = Rows)]
    public string[] Values = new string[Rows];

    /// <summary>Which rows stand for anything.</summary>
    [Show("#irow", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>The frame rate, in the title bar where it is glanced at.</summary>
    [Bind("#i-rate", Mode = BindMode.OneWay)]
    public string Rate { get; private set; } = string.Empty;

    /// <summary>Whether the panel has been pinned into the column.</summary>
    private bool _pinned;

    /// <summary>What the pin is currently pointing at, so it is only written when it changes.</summary>
    private string _pinIcon = string.Empty;

    /// <summary>What the last walk of the world found.</summary>
    private Census _census;

    /// <summary>What a walk of the world counts.</summary>
    /// <remarks>
    /// Taken a few times a second rather than every frame: it visits every entity and asks each
    /// one what it carries, and nothing on this panel is worth that sixty times a second. A stats
    /// readout that costs frames is measuring itself.
    /// </remarks>
    private readonly record struct Census(
        int Entities, int Meshes, int Lights, int Cameras, int Interface, int Scripted);

    /// <summary>The slowest frame seen since the panel was last opened.</summary>
    private float _worst;

    /// <summary>Reads the frame and the world.</summary>
    [OnRefresh]
    public void Read()
    {
        if (EditorShell.Context is not { } ctx) return;

        var frame = (float)ctx.Time.RawDeltaSeconds * 1000f;
        if (frame > _worst) _worst = frame;

        if (ctx.Time.FrameCount % 30 == 0) _census = Count(ctx.Ecs);

        Rate = $"{ctx.Time.SmoothedFps:F0} fps";

        Wear(_pinned ? "icons/ui/pinned.png" : "icons/ui/pin.png");

        var written = 0;
        var (windowWidth, windowHeight) = Bevy.Window.Size();
        var selected = EditorSelection.Current;

        Head(ref written, "Frame");
        Write(ref written, "Rate", $"{ctx.Time.SmoothedFps:F0} fps");
        Write(ref written, "This frame", $"{frame:F2} ms");
        Write(ref written, "Worst", $"{_worst:F2} ms");
        Write(ref written, "Window", $"{windowWidth} x {windowHeight}");

        Head(ref written, "Scene");
        Write(ref written, "Entities", _census.Entities.ToString());
        Write(ref written, "Meshes", _census.Meshes.ToString());
        Write(ref written, "Lights", _census.Lights.ToString());
        Write(ref written, "Cameras", _census.Cameras.ToString());
        Write(ref written, "Behaviors on entities", _census.Scripted.ToString());
        Write(ref written, "Interface entities", _census.Interface.ToString());

        Head(ref written, "Editor");
        Write(ref written, "Selected", selected.IsNone
            ? "nothing"
            : ctx.Ecs.NameOf(selected) ?? $"entity {selected.Index}");
        Write(ref written, "Tool", $"{EditorTools.Current}, {Space}");
        Write(ref written, "Panels open", Showing().ToString());
        Write(ref written, "Interface rebuilds", Xui.Generation.ToString());
        Write(ref written, "Last change", EditorHistory.Last ?? "none");

        Head(ref written, "Machine");
        Write(ref written, "Renderer", Adapter);
        Write(ref written, "Managed memory", $"{GC.GetTotalMemory(false) / (1024 * 1024)} MB");

        for (var i = written; i < Rows; i++)
        {
            Names[i] = string.Empty;
            Values[i] = string.Empty;
            Shown[i] = false;
        }
    }

    /// <summary>Points the pin at whichever picture says what pressing it will do.</summary>
    /// <remarks>
    /// A picture rather than a character, because the font has no pin in it and a letter standing in
    /// for one says nothing. Set rather than bound, since an image is a path the interface loads
    /// from and not a value a widget carries.
    /// </remarks>
    private void Wear(string icon)
    {
        if (_pinIcon == icon) return;
        if (Window is not { IsOpen: true } window) return;

        var element = window.Element("i-pin-icon");
        if (element.IsNone) return;

        Xui.SetImage(element, icon);
        _pinIcon = icon;
    }

    /// <summary>Which axes the handles are drawn along, in the words the toolbar uses.</summary>
    private static string Space =>
        EditorTools.Space == ToolSpace.Local ? "local" : "global";

    /// <summary>How many panels are on screen rather than merely loaded.</summary>
    private static int Showing()
    {
        var count = 0;

        foreach (var panel in EditorShell.Open)
        {
            if (EditorShell.IsShowing(panel)) count++;
        }

        return count;
    }

    /// <summary>What is drawing, asked once and kept.</summary>
    /// <remarks>
    /// The name of the adapter never changes while the program runs, and asking for it copies a
    /// string across the ABI. The first answer is the only one there will be.
    /// </remarks>
    private static string Adapter =>
        _adapter ??= App.DescribeAdapter() is { Length: > 0 } name ? Shorten(name) : "unknown";

    /// <summary>The adapter's name once it has been asked for.</summary>
    private static string? _adapter;

    /// <summary>Just the part of the adapter line that names the hardware.</summary>
    private static string Shorten(string line)
    {
        var parts = line.Split('|');
        return parts.Length > 1 ? parts[1].Trim() : line.Trim();
    }

    /// <summary>Walks the world once and counts what is in it.</summary>
    private static Census Count(EcsWorld world)
    {
        var entities = 0;
        var meshes = 0;
        var lights = 0;
        var cameras = 0;
        var chrome = 0;
        var scripted = 0;

        foreach (var entity in world.All())
        {
            entities++;

            if (EditorEntity.IsInterface(world, entity))
            {
                chrome++;
                continue;
            }

            // Asked through the same table the hierarchy draws its pictures from, so the two can
            // never disagree about what a thing is.
            switch (EditorKinds.IconFor(world, entity))
            {
                case "icons/ui/mesh.png":
                    meshes++;
                    break;

                case "icons/ui/light.png":
                    lights++;
                    break;

                case "icons/ui/camera.png":
                    cameras++;
                    break;

                case EditorKinds.Scripted:
                    scripted++;
                    break;
            }
        }

        return new Census(entities, meshes, lights, cameras, chrome, scripted);
    }

    /// <summary>Adds a heading, which is a row with nothing on its right.</summary>
    private void Head(ref int written, string name)
    {
        if (written >= Rows) return;

        Names[written] = name.ToUpperInvariant();
        Values[written] = string.Empty;
        Shown[written] = true;
        written++;
    }

    /// <summary>Adds one fact.</summary>
    private void Write(ref int written, string name, string value)
    {
        if (written >= Rows) return;

        Names[written] = "  " + name;
        Values[written] = value;
        Shown[written] = true;
        written++;
    }

    /// <summary>
    /// Pins the panel into the column, or lets it float again.
    /// </summary>
    /// <remarks>
    /// The placement is not touched. It sits in the viewport's top right corner either way, which
    /// is under the button that opened it and inside the part of the screen the scene has.
    /// </remarks>
    [Command("#i-pin")]
    public void Pin()
    {
        _pinned = !_pinned;

        EditorShell.Pin(this, _pinned);
    }
}
