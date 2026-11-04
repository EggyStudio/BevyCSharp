namespace Bevy;

/// <summary>How a sampler reads a texture. The default is linear and repeating.</summary>
/// <remarks>
/// A sampler whose three filters are linear reads a texture seen at a slant with sixteen samples,
/// the device's most where it offers fewer, as every texture is filtered
/// (<see cref="TextureSettings"/>). One with any filter nearest takes one, since the graphics API
/// refuses anisotropy beside a nearest filter as a validation failure.
/// </remarks>
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
    /// Whether the sampler reads one of the engine's own fields or tables rather than a texture,
    /// which keeps one sample at a slant where a texture's linear sampler takes sixteen.
    /// </summary>
    internal bool Table { get; init; }

    /// <summary>
    /// The samples a texture read at a slant takes, <see cref="TextureSettings.SlantSamples"/>
    /// where the three filters are linear, one where any is nearest or the sampler reads a table.
    /// </summary>
    internal int Anisotropy =>
        !Table && Magnify == SamplerFilter.Linear && Minify == SamplerFilter.Linear && Mipmaps == SamplerFilter.Linear
            ? (int)TextureSettings.SlantSamples
            : 1;

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

    /// <summary>
    /// Linear and clamped to the edge at one sample, for the engine's own fields and tables, the
    /// scene's distance field, the probes' light, the fog's froxels and the clouds' and sky's
    /// pictures, which are data rather than textures and keep the filtering they were measured
    /// with.
    /// </summary>
    internal static SamplerSettings ClampedTable => Clamped with { Table = true };

    /// <summary>
    /// Linear and repeating at one sample, for the engine's own tables that tile.
    /// </summary>
    internal static SamplerSettings LinearTable => Linear with { Table = true };
}
