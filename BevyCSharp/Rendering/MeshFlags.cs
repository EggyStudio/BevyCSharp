namespace Bevy;

/// <summary>How a mesh is treated beyond what it looks like. See <see cref="Render.SetMeshFlags"/>.</summary>
[Flags]
public enum MeshFlags : uint
{
    /// <summary>As Bevy has it, culled when out of view and casting and receiving shadows.</summary>
    None = 0,

    /// <summary>Never culled for being out of view, for a mesh its shader moves.</summary>
    NoFrustumCulling = 1,

    /// <summary>Casts no shadow.</summary>
    NoShadowCasting = 2,

    /// <summary>Has no shadow cast on it.</summary>
    NoShadowReceiving = 4,

    /// <summary>
    /// Drawn alone rather than batched with the meshes it shares a mesh and a material with, as a
    /// stress test does to measure what batching saves.
    /// </summary>
    NoAutomaticBatching = 8,

    /// <summary>
    /// Left out of the CPU's culling for the GPU's alone, for a mesh whose place the GPU writes,
    /// where the CPU's idea of it is stale.
    /// </summary>
    NoCpuCulling = 16,
}
