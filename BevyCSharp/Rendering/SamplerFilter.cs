namespace Bevy;

/// <summary>How a sampler reads between pixels.</summary>
public enum SamplerFilter
{
    /// <summary>Blends the nearest pixels.</summary>
    Linear = 0,

    /// <summary>Takes the nearest pixel.</summary>
    Nearest = 1,
}
