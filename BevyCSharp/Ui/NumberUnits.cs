namespace Bevy;

/// <summary>
/// The units a Feathers number input shows its value in, the four Bevy's registry holds, for
/// <see cref="Feathers.SetNumberUnits"/>.
/// </summary>
/// <remarks>
/// Typing takes a value in any unit of the dimension, such as <c>3 km</c> or <c>90 deg</c>, and the
/// value is held and reported in the dimension's base unit, meters, seconds or radians, an angle
/// being shown in degrees.
/// </remarks>
public enum NumberUnits
{
    /// <summary>A plain number, Bevy's <c>Dimensionless</c>.</summary>
    None = 0,

    /// <summary>A length held in meters, Bevy's <c>LengthMeters</c>.</summary>
    LengthMeters = 1,

    /// <summary>A time held in seconds, Bevy's <c>TimeSeconds</c>.</summary>
    TimeSeconds = 2,

    /// <summary>An angle held in radians and shown in degrees, Bevy's <c>AngleDegrees</c>.</summary>
    AngleDegrees = 3,
}
