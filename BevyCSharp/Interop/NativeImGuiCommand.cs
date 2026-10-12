namespace Bevy.Interop;

/// <summary>One draw call, a run of indices clipped to a rectangle and reading from one picture.</summary>
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
