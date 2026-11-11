namespace Bevy.Interop;

/// <summary>How a sampler reads. Mirrors <c>BcsSamplerConfig</c>.</summary>
public unsafe struct NativeSamplerConfig
{
    /// <summary>0 clamp to the edge, 1 repeat, 2 mirror, for U, V and W.</summary>
    public fixed int Address[3];

    /// <summary>Non-zero for linear, for magnifying, minifying and between mip levels.</summary>
    public fixed int Linear[3];

    /// <summary>From one, which is none, to sixteen.</summary>
    public int Anisotropy;
}
