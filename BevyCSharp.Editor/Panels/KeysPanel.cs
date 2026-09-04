using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Panels;

/// <summary>
/// What the keys do, as a flyout.
/// </summary>
/// <remarks>
/// <para>
/// A flyout is a panel that a press outside dismisses. Nothing else about it differs: same three
/// files, same bindings, same shell. That is the point of putting dismissal in the panel's
/// declaration rather than building a separate kind of window for it.
/// </para>
/// <para>
/// The list is here rather than in the document because the keys belong to the program: a key
/// that stops working should stop being listed, and it cannot if the list is a paragraph someone
/// typed.
/// </para>
/// </remarks>
[EditorPanel(
    "panels/keys.html",
    Root = "#keys",
    Handle = "#keys-title",
    Dismiss = PanelDismiss.OnOutsideClick,
    Layer = 50)]
public sealed partial class KeysPanel
{
    /// <summary>How many rows the document declares.</summary>
    public const int Rows = 16;

    /// <summary>What the camera and the window do, which is not a table anything else holds.</summary>
    private static readonly (string Key, string Does)[] Camera =
    [
        ("Right drag", "look around"),
        ("W A S D", "move while looking"),
        ("Q E", "down and up"),
        ("Shift Ctrl", "faster, slower"),
        ("Middle drag", "slide the view"),
        ("Alt drag", "orbit ahead"),
        ("Wheel", "move forward"),
        ("F", "frame the origin"),
        ("Escape", "close the editor"),
    ];

    /// <summary>Every line of the list: the panel keys first, then the camera.</summary>
    /// <remarks>
    /// The panel half is read from the table the keys are actually bound against, so a key that
    /// stops working stops being listed.
    /// </remarks>
    private static IEnumerable<(string Key, string Does)> Bindings =>
        EditorKeys.Panels.Select(binding => (binding.Name, binding.Does))
            .Concat(EditorKeys.Editing.Select(binding => (binding.Name, binding.Does)))
            .Concat(Camera);

    /// <summary>Each row of the list.</summary>
    [Bind("#key", Count = Rows)]
    public string[] Lines = new string[Rows];

    /// <summary>Which rows stand for a binding.</summary>
    [Show("#key", Count = Rows)]
    public bool[] Shown = new bool[Rows];

    /// <summary>Fills the list, which does not change while it is open.</summary>
    [OnRefresh]
    public void Fill()
    {
        var listed = 0;

        foreach (var (key, does) in Bindings)
        {
            if (listed >= Rows) break;

            Lines[listed] = $"{key}   {does}";
            Shown[listed] = true;
            listed++;
        }

        for (var i = listed; i < Rows; i++)
        {
            Lines[i] = string.Empty;
            Shown[i] = false;
        }
    }
}
