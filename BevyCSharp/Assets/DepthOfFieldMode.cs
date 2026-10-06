namespace Bevy;

/// <summary>How the parts of a picture that are out of focus are blurred.</summary>
public enum DepthOfFieldMode
{
    /// <summary>Everything is drawn sharp, whatever its distance.</summary>
    None = 0,

    /// <summary>A plain blur, which is cheaper and reads as softness.</summary>
    Gaussian = 1,

    /// <summary>
    /// Each point of light spreads into a disc, as a lens spreads it, which makes a highlight
    /// behind the subject into a circle.
    /// </summary>
    Bokeh = 2,
}
