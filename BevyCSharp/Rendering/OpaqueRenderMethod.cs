namespace Bevy;

/// <summary>
/// Whether an opaque or masked material is drawn forward, lit as it is drawn, or deferred, written
/// into the G-buffer and lit after the scene.
/// </summary>
/// <remarks>
/// A blended material is drawn forward whatever this says, since the G-buffer holds one surface a
/// pixel and a blended one shows what is behind it.
/// </remarks>
public enum OpaqueRenderMethod
{
    /// <summary>
    /// As every other material is drawn, deferred while <see cref="Render.SetDeferredRendering"/>
    /// has it on and forward otherwise. The default.
    /// </summary>
    Auto = 0,

    /// <summary>Forward, even while the rest are deferred.</summary>
    /// <remarks>
    /// For a surface the G-buffer cannot hold, such as a specular tint, which it has no room for,
    /// or for a few materials whose cost should not grow with the screen.
    /// </remarks>
    Forward = 1,

    /// <summary>
    /// Deferred, even while the rest are forward, drawn only by a camera that draws the G-buffer
    /// (<see cref="Shaders.SetPrepass"/> with <c>deferred</c>).
    /// </summary>
    Deferred = 2,
}
