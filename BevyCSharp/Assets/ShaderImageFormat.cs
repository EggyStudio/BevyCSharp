namespace Bevy;

/// <summary>What an image a compute shader writes holds per pixel.</summary>
/// <remarks>
/// The shader's <c>RWTexture</c> declares the same format with <c>[format(...)]</c>, named in
/// brackets after each value here.
/// </remarks>
public enum ShaderImageFormat
{
    /// <summary>Eight bits a channel, from zero to one (<c>rgba8</c>).</summary>
    Rgba8 = 0,

    /// <summary>A half float a channel, which holds light brighter than white (<c>rgba16f</c>).</summary>
    Rgba16Float = 1,

    /// <summary>A float a channel (<c>rgba32f</c>).</summary>
    Rgba32Float = 2,

    /// <summary>One float (<c>r32f</c>).</summary>
    R32Float = 3,

    /// <summary>One unsigned integer (<c>r32ui</c>).</summary>
    R32UInt = 4,

    /// <summary>One signed integer (<c>r32i</c>).</summary>
    R32Int = 5,

    /// <summary>Two floats (<c>rg32f</c>).</summary>
    Rg32Float = 6,

    /// <summary>Four unsigned integers (<c>rgba32ui</c>).</summary>
    Rgba32UInt = 7,

    /// <summary>Four unsigned bytes (<c>rgba8ui</c>).</summary>
    Rgba8UInt = 8,

    /// <summary>One half float (<c>r16f</c>).</summary>
    R16Float = 9,

    /// <summary>
    /// Block-compressed color with one bit of alpha, eight bytes a four by four block. This and the
    /// compressed formats after it are only read, through a sampler. They are made empty or from
    /// blocks and filled a block at a time with <see cref="Shaders.WriteImage{T}"/>, and a streamed
    /// texture's cache is kept in them.
    /// </summary>
    Bc1 = 10,

    /// <summary>One block-compressed channel, eight bytes a block: a height, a mask, a roughness.</summary>
    Bc4 = 11,

    /// <summary>Two block-compressed channels, sixteen bytes a block, which is a normal map's form.</summary>
    Bc5 = 12,

    /// <summary>Block-compressed color and alpha at the best quality, sixteen bytes a block.</summary>
    Bc7 = 13,

    /// <summary>As <see cref="Bc7"/>, read as sRGB, the space a color texture is stored
    /// in.</summary>
    Bc7Srgb = 14,

    /// <summary>Block-compressed color brighter than white, sixteen bytes a block, for light and skies.</summary>
    Bc6hFloat = 15,
}
