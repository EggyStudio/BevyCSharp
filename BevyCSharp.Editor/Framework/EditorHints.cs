namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What the keys do, for whatever the editor is currently in.
/// </summary>
/// <remarks>
/// <para>
/// One table, read by the strip that shows it, and made of what the editor actually binds rather
/// than of a paragraph somebody typed. A key that stops working stops being listed, because there
/// is nowhere else for the list to come from.
/// </para>
/// <para>
/// The list follows the mode, which is what makes it worth reading: in the move tool it says what
/// a drag does, and while flying it says what W and S do. A list of every key in the editor is a
/// list nobody reads twice.
/// </para>
/// </remarks>
public static class EditorHints
{
    /// <summary>What is worth saying right now.</summary>
    public static IEnumerable<(string Key, string Does)> Current()
    {
        yield return ("RMB", "look, WASD move");

        switch (EditorTools.Current)
        {
            case EditorTool.Move:
                yield return ("drag", "move along an axis");
                break;

            case EditorTool.Rotate:
                yield return ("drag", "turn about an axis");
                break;

            case EditorTool.Scale:
                yield return ("drag", "stretch along an axis");
                break;

            default:
                yield return ("LMB", "select");
                break;
        }

        yield return ("Q W E R", "select move rotate scale");

        if (EditorTools.Current != EditorTool.Select)
        {
            yield return ("Ctrl", EditorTools.Snap ? "snapping on" : "hold to snap");
        }

        yield return ("F", "frame the selection");
        yield return ("Del", "delete it");
        yield return ("Ctrl Z", "undo");
        yield return ("F1", "menu");
        yield return ("Esc", "quit");
    }
}
