namespace Bevy;

/// <summary>
/// A graphics API the renderer can be pinned to.
/// </summary>
/// <remarks>
/// These map onto wgpu's <c>Backends</c> flags, which Bevy's renderer is built on. Only backends
/// the host platform supports are meaningful, since Direct3D 12 is Windows-only and Metal is
/// Apple-only.
/// </remarks>
public enum GraphicsBackend
{
    /// <summary>Let wgpu choose. Prefers Vulkan on Linux and Windows, Metal on Apple.</summary>
    Automatic = 0,

    /// <summary>Vulkan. Available on Linux, Windows and Android.</summary>
    Vulkan = 1,

    /// <summary>Direct3D 12. Windows only.</summary>
    Direct3D12 = 2,

    /// <summary>Metal. macOS and iOS only.</summary>
    Metal = 3,

    /// <summary>OpenGL or OpenGL ES. A fallback for machines with no modern driver.</summary>
    OpenGL = 4,
}
