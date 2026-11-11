namespace Bevy.Interop;

/// <summary>
/// One vertex of the interface, laid out as ImGui writes one.
/// </summary>
/// <remarks>
/// Matches <c>ImDrawVert</c> and <c>BcsImGuiVertex</c> in
/// <c>native/bevy_csharp/src/imgui/mod.rs</c>. It is never built here. ImGui's own buffers are
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

    /// <summary>Its color, red in the low byte and alpha in the high one.</summary>
    public uint Color;
}
