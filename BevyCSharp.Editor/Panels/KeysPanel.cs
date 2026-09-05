using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// What the keys do, written over the scene rather than in a panel.
/// </summary>
/// <remarks>
/// <para>
/// A strip of pale text along the bottom, with no box around it. A key list is read at a glance
/// and then ignored, and a panel with a border and a background would keep asking to be noticed
/// long after it had been read. Unreal puts its viewport hints exactly here, for the same reason.
/// </para>
/// <para>
/// On the same row as the tabs, and after them, because the bottom of the window is one band: two
/// strips of text at two heights read as an accident, and the row is the full width of the window
/// whatever the columns are doing above it.
/// </para>
/// <para>
/// What it lists follows the tool. A person in the move tool wants to know what a drag does now,
/// not what every key in the editor does, and a list that changes with the mode is shorter and
/// truer than one that does not.
/// </para>
/// </remarks>
[EditorPanel("panels/keys.html", Root = "#keys", Dock = EditorDock.Strip, Order = 1, Layer = 5)]
public sealed partial class KeysPanel
{
    /// <summary>How many hints the document can draw.</summary>
    public const int Hints = 10;

    /// <summary>What each hint says.</summary>
    [Bind("#key", Count = Hints)]
    public string[] Labels = new string[Hints];

    /// <summary>Which hints stand for anything.</summary>
    [Show("#key", Count = Hints)]
    public bool[] Shown = new bool[Hints];

    /// <summary>Fills the strip from whatever the editor is currently in.</summary>
    [OnRefresh]
    public void Fill()
    {
        var written = 0;

        foreach (var (key, does) in EditorHints.Current())
        {
            if (written >= Hints) break;

            Labels[written] = $"{key}  {does}";
            Shown[written] = true;
            written++;
        }

        for (var i = written; i < Hints; i++)
        {
            Labels[i] = string.Empty;
            Shown[i] = false;
        }
    }
}
