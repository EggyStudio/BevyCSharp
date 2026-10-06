namespace Bevy;

/// <summary>What a name a shader declares is.</summary>
public enum ShaderParameterKind
{
    /// <summary>Numbers: a scalar, a vector, a matrix or an array of them.</summary>
    Number,

    /// <summary>A struct or an array of them, set field by field or as bytes.</summary>
    Struct,

    /// <summary>A texture the shader samples or loads from.</summary>
    Texture,

    /// <summary>An image the shader writes.</summary>
    Image,

    /// <summary>A storage buffer.</summary>
    Buffer,

    /// <summary>A sampler.</summary>
    Sampler,

    /// <summary>
    /// An acceleration structure rays are traced through, which a <see cref="RayScene"/> is
    /// handed to. Only a compute shader compiled to SPIR-V declares one.
    /// </summary>
    RayScene,
}
