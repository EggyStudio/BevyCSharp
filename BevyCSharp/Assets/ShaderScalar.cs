namespace Bevy;

/// <summary>Which kind of number a shader value holds.</summary>
public enum ShaderScalar
{
    /// <summary>A 32-bit float.</summary>
    Float = 0,

    /// <summary>A 32-bit signed integer.</summary>
    Int = 1,

    /// <summary>A 32-bit unsigned integer.</summary>
    UInt = 2,

    /// <summary>A boolean, four bytes wide, set from any integer or a <see cref="bool"/>.</summary>
    Bool = 3,
}
