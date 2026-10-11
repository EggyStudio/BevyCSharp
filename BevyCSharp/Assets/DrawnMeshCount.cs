namespace Bevy;

/// <summary>What <see cref="Render.DrawnMeshes"/> could say of the meshes drawn.</summary>
public enum DrawnMeshCount
{
    /// <summary>The count is the meshes the opaque pass drew in a recent frame.</summary>
    Known,

    /// <summary>Counting is on, and no count has come back from the GPU yet.</summary>
    Pending,

    /// <summary>
    /// The GPU culls but cannot say how many draws its culling left, which needs its multi-draw
    /// indirect count, so the meshes drawn cannot be counted.
    /// </summary>
    Uncountable,

    /// <summary>
    /// The GPU does not cull, as on a device without compute or GPU preprocessing, so occlusion
    /// culling has no effect and every mesh in view is drawn.
    /// </summary>
    NoGpuCulling,
}
