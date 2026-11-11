using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>A text field's cursor and selection, linear colors. Mirrors <c>BcsTextCursor</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeTextCursor
{
    /// <summary>The cursor.</summary>
    public fixed float Color[4];

    /// <summary>Behind the selected text while the field has the focus.</summary>
    public fixed float Selection[4];

    /// <summary>Behind it while the field has not.</summary>
    public fixed float UnfocusedSelection[4];

    /// <summary>The selected text's own color, where <see cref="HasSelectedText"/> is non-zero.</summary>
    public fixed float SelectedText[4];

    /// <summary>Non-zero where <see cref="SelectedText"/> recolors the selected text.</summary>
    public int HasSelectedText;

    /// <summary>How round the selection's corners are, a fraction of its height up to a half.</summary>
    public float SelectionRadius;
}
