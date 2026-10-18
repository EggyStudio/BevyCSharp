namespace Bevy;

/// <summary>How a <see cref="ScatteringTerm"/> scatters light by direction.</summary>
public enum ScatteringPhase
{
    /// <summary>Evenly in every direction.</summary>
    Isotropic = 0,

    /// <summary>As a gas of small molecules does, as much back as forward and least to the side, Rayleigh scattering.</summary>
    Rayleigh = 1,

    /// <summary>As a dust does, leaning forward by <see cref="ScatteringTerm.Asymmetry"/>, Mie scattering.</summary>
    Mie = 2,
}
