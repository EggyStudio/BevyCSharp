namespace Bevy;

/// <summary>Which of Bevy's lights to spawn.</summary>
public enum LightKind
{
    /// <summary>Parallel rays, as from the sun. Intensity is illuminance in lux.</summary>
    Directional = 0,

    /// <summary>Rays from a point. Intensity is luminous power in lumens.</summary>
    Point = 1,

    /// <summary>
    /// A cone of rays, aimed down the entity's negative Z. Intensity is luminous power in lumens.
    /// </summary>
    Spot = 2,
}
