namespace Bevy;

/// <summary>What a Slang stage is compiled to.</summary>
public enum ShaderTarget
{
    /// <summary>WGSL, which Bevy reads on every backend and checks before it runs.</summary>
    Wgsl,

    /// <summary>
    /// SPIR-V, handed to the driver untouched, for what WGSL cannot say. Compute stages only.
    /// </summary>
    SpirV,
}
