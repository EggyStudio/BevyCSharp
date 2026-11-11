namespace Bevy;

/// <summary>How <see cref="AtmosphereSettings"/> works the sky out.</summary>
/// <remarks>
/// The same air either way. What differs is how close to right the sky is from far off and how
/// much of a frame it costs, which is Bevy's own choice of rendering method.
/// </remarks>
public enum SkyMethod
{
    /// <summary>
    /// From lookup textures computed once a frame, Bevy's own and quick, for a scene inside the air.
    /// Less exact far from the origin and for the sharp shadows fog and clouds cast.
    /// </summary>
    LookupTexture = 0,

    /// <summary>
    /// By marching rays through the air for every pixel, slower, and right from any distance, a
    /// planet seen from orbit included, with sharp shadows in fog and clouds.
    /// </summary>
    Raymarched = 1,
}
