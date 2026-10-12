namespace Bevy.Interop;

/// <summary>Everything ImGui produced for one frame.</summary>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public unsafe struct NativeImGuiFrame
{
    /// <summary>The vertices of every window, in one run.</summary>
    public NativeImGuiVertex* Vertices;

    /// <summary>How many vertices <see cref="Vertices"/> holds.</summary>
    public uint VertexCount;

    /// <summary>The indices, two bytes each.</summary>
    public ushort* Indices;

    /// <summary>How many indices <see cref="Indices"/> holds.</summary>
    public uint IndexCount;

    /// <summary>The draw calls, in the order they are to be made.</summary>
    public NativeImGuiCommand* Commands;

    /// <summary>How many draw calls <see cref="Commands"/> holds.</summary>
    public uint CommandCount;

    /// <summary>Where the interface's top left is, in logical pixels.</summary>
    public float DisplayX;

    /// <inheritdoc cref="DisplayX"/>
    public float DisplayY;

    /// <summary>How wide the interface is, in logical pixels.</summary>
    public float DisplayWidth;

    /// <inheritdoc cref="DisplayWidth"/>
    public float DisplayHeight;

    /// <summary>How many physical pixels a logical one is, across.</summary>
    public float ScaleX;

    /// <inheritdoc cref="ScaleX"/>
    public float ScaleY;
}
