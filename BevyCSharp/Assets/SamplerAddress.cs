namespace Bevy;

/// <summary>What a sampler does reading past the edge of a texture.</summary>
public enum SamplerAddress
{
    /// <summary>Starts over from the other side.</summary>
    Repeat = 0,

    /// <summary>Reads the edge pixel.</summary>
    Clamp = 1,

    /// <summary>Reads back the way it came.</summary>
    Mirror = 2,
}
