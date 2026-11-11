namespace Bevy;

/// <summary>How many froxels the fog is worked out in. See <see cref="FroxelFogSettings.Quality"/>.</summary>
public enum FroxelQuality
{
    /// <summary>80 across, 45 down and 64 deep.</summary>
    Low = 0,

    /// <summary>120 across, 68 down and 96 deep.</summary>
    Medium = 1,

    /// <summary>160 across, 90 down and 128 deep.</summary>
    High = 2,
}
