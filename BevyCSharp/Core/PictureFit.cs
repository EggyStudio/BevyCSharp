namespace Bevy;

/// <summary>
/// How a picture of a shape unlike its window's stands in it, the choices of <c>render.fit</c>.
/// </summary>
public enum PictureFit
{
    /// <summary>
    /// The picture's shape kept, as large as the window holds, in its middle with black round it.
    /// </summary>
    Fit = 0,

    /// <summary>The picture over the whole window, its shape given up.</summary>
    Stretch = 1,
}
