namespace Bevy;

/// <summary>
/// The antialiasing that runs as a pass over the finished picture.
/// </summary>
/// <remarks>
/// Separate from <see cref="PostSettings.Msaa"/>, which works while the scene is rasterized and
/// smooths the edges of geometry only. A pass sees the picture instead, so it also catches edges
/// that come from a texture or a shader, at the cost of some sharpness.
/// </remarks>
public enum AntiAliasPass
{
    /// <summary>No pass. Multisampling alone, or nothing at all.</summary>
    None = 0,

    /// <summary>Cheap and slightly soft. What a game reaches for first.</summary>
    Fxaa = 1,

    /// <summary>Costlier and sharper, and better on near-horizontal edges.</summary>
    Smaa = 2,

    /// <summary>
    /// Resolved from the frames before it, which catches what the other two cannot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every frame is drawn from a slightly different point and averaged with its predecessors,
    /// so an edge is sampled many times over rather than guessed at from one. That catches the
    /// aliasing a texture or a specular highlight produces, which a pass looking at a single
    /// finished frame has no way to tell from detail.
    /// </para>
    /// <para>
    /// The cost is a trail behind anything whose motion the renderer reports wrongly, and a
    /// picture that is softer than the other two. It needs a 3D camera and
    /// <see cref="PostSettings.Msaa"/> set to 1, since there is no history to resolve from a
    /// multisampled target.
    /// </para>
    /// </remarks>
    Temporal = 3,
}
