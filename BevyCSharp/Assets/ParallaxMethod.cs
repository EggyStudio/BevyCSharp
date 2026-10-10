namespace Bevy;

/// <summary>
/// How a material with a <see cref="MaterialSettings.DepthMap"/> finds where a ray from the camera
/// meets the surface the map describes.
/// </summary>
/// <remarks>
/// Both cut the depth map into layers and step the ray through them until it is below the map,
/// as many layers as <see cref="MaterialSettings.ParallaxLayers"/> where the surface is seen edge
/// on and one where it is seen head on. They differ in how they refine the layer that step lands
/// in. Every material in a scene is best given the same one, since each is a shader of its own.
/// </remarks>
public enum ParallaxMethod
{
    /// <summary>
    /// Parallax occlusion mapping, which blends between the last two layers, one more sample.
    /// </summary>
    /// <remarks>
    /// The cheaper of the two, and it can skip a small detail and show a surface that seems to
    /// writhe as the camera moves.
    /// </remarks>
    Occlusion = 0,

    /// <summary>
    /// Relief mapping, which searches between the last two layers by halves, a sample each step,
    /// for as many steps as <see cref="MaterialSettings.ReliefSteps"/>.
    /// </summary>
    /// <remarks>
    /// Fewer steps' worth of jagged edges than occlusion mapping shows, so a few layers and a few
    /// steps can look better than many layers without them, for less.
    /// </remarks>
    Relief = 1,
}
