namespace Bevy;

/// <summary>How a camera draws beyond what it looks at. See <see cref="Render.SetCameraFlags"/>.</summary>
[Flags]
public enum CameraFlags : uint
{
    /// <summary>As Bevy has it, drawing indirectly and culling on the CPU and the GPU.</summary>
    None = 0,

    /// <summary>
    /// Each batch drawn with a call of its own rather than Bevy's indirect draws from a buffer the
    /// GPU fills.
    /// </summary>
    NoIndirectDrawing = 1,

    /// <summary>The camera's view left out of the CPU's culling, for the GPU's alone.</summary>
    NoCpuCulling = 2,
}
