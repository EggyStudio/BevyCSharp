using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>How a text field behaves. Mirrors <c>BcsEditableTextConfig</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeEditableTextConfig
{
    /// <summary>The most characters it holds, or zero for no limit.</summary>
    public int MaxCharacters;

    /// <summary>How many glyphs wide, or zero for its node's width.</summary>
    public float VisibleWidth;

    /// <summary>How many lines tall, or zero for one.</summary>
    public float VisibleLines;

    /// <summary>Non-zero to let Enter start a new line.</summary>
    public int AllowNewlines;

    /// <summary>A <see cref="TextReadWriteMode"/>.</summary>
    public int Mode;
}
