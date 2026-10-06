namespace Bevy;

/// <summary>How a sampler reads a texture. The default is linear and repeating.</summary>
public readonly record struct SamplerSettings
{
    /// <summary>What reading past either side does across.</summary>
    public SamplerAddress AddressU { get; init; }

    /// <summary>What reading past either side does down.</summary>
    public SamplerAddress AddressV { get; init; }

    /// <summary>What reading past either side does in depth.</summary>
    public SamplerAddress AddressW { get; init; }

    /// <summary>How a texture drawn larger than it is reads between pixels.</summary>
    public SamplerFilter Magnify { get; init; }

    /// <summary>How a texture drawn smaller than it is reads between pixels.</summary>
    public SamplerFilter Minify { get; init; }

    /// <summary>How it reads between mip levels.</summary>
    public SamplerFilter Mipmaps { get; init; }

    /// <summary>
    /// How many samples a texture seen at a slant takes, from one to sixteen. Above one every
    /// filter has to be linear, and is made so.
    /// </summary>
    public int Anisotropy { get; init; }

    /// <summary>Linear and repeating.</summary>
    public static SamplerSettings Linear => default;

    /// <summary>Nearest pixel and repeating, for pixel art.</summary>
    public static SamplerSettings Nearest => new()
    {
        Magnify = SamplerFilter.Nearest,
        Minify = SamplerFilter.Nearest,
        Mipmaps = SamplerFilter.Nearest,
    };

    /// <summary>Linear and clamped to the edge, for anything not meant to tile.</summary>
    public static SamplerSettings Clamped => new()
    {
        AddressU = SamplerAddress.Clamp,
        AddressV = SamplerAddress.Clamp,
        AddressW = SamplerAddress.Clamp,
    };
}
