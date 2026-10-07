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
}
