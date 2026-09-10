namespace Bevy.Interop;

/// <summary>
/// One vertex of the interface, laid out as ImGui writes one.
/// </summary>
/// <remarks>
/// Matches <c>ImDrawVert</c> and <c>BcsImGuiVertex</c> in
/// <c>native/bevy_csharp/src/imgui/mod.rs</c>. It is never built here: ImGui's own buffers are
/// handed straight over, and this says what is in them.
/// </remarks>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct NativeImGuiVertex
{
    /// <summary>Where the vertex is, in logical pixels from the top left.</summary>
    public float X;

    /// <inheritdoc cref="X"/>
    public float Y;

    /// <summary>Where it reads from its picture.</summary>
    public float U;

    /// <inheritdoc cref="U"/>
    public float V;

    /// <summary>Its colour, red in the low byte and alpha in the high one.</summary>
    public uint Color;
}

/// <summary>One draw call: a run of indices, clipped to a rectangle, reading from one picture.</summary>
[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct NativeImGuiCommand
{
    /// <summary>Which picture the triangles read from.</summary>
    public ulong Texture;

    /// <summary>Left of what may be drawn, in logical pixels.</summary>
    public float ClipLeft;

    /// <inheritdoc cref="ClipLeft"/>
    public float ClipTop;

    /// <inheritdoc cref="ClipLeft"/>
    public float ClipRight;

    /// <inheritdoc cref="ClipLeft"/>
    public float ClipBottom;

    /// <summary>Where in the index buffer this call's indices start.</summary>
    public uint Index;

    /// <summary>What to add to every index, which is where this call's vertices start.</summary>
    public uint Vertex;

    /// <summary>How many indices it draws.</summary>
    public uint Elements;
}

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
