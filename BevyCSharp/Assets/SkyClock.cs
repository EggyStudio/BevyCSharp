namespace Bevy;

/// <summary>
/// The sky's clock, where the sun, the moon and the stars stand at an hour of a day of the year
/// for a latitude, worked out by the usual spherical astronomy.
/// </summary>
/// <remarks>
/// <para>
/// North is along negative Z and east along positive X, up along positive Y, so a program aims a
/// directional light from <see cref="SunDirection"/> and the sun rises in the east. The hour is
/// the local solar time, noon when the sun stands highest, and the day of the year moves the sun's
/// declination by the earth's <see cref="AxialTilt"/>, so summer days are long and winter's short,
/// and a tilt of none keeps every day the equinox's, the sun rising at six and setting at eighteen.
/// </para>
/// <para>
/// The moon goes about the sky once a synodic month behind the sun, <see cref="MoonAge"/> days
/// after a new moon, full half way, along the ecliptic, as high as the sun would stand at its
/// place there. Its tilt to the ecliptic of five degrees is left out. <see cref="SkyTurn"/> is how
/// far the stars have turned about the pole, the local sidereal time, which the night sky draws
/// them by (<see cref="Render.SetNightSky"/>).
/// </para>
/// </remarks>
public sealed class SkyClock
{
    /// <summary>The days of a synodic month, from one new moon to the next.</summary>
    public const float SynodicMonth = 29.530589f;

    /// <summary>The local solar time, from midnight at 0 to 24, noon unless set.</summary>
    public float Hour { get; set; } = 12f;

    /// <summary>The day of the year, from 0 on the first of January to 365, the March equinox's, 79, unless set.</summary>
    public float DayOfYear { get; set; } = 79f;

    /// <summary>The latitude, in degrees north of the equator, 45 unless set.</summary>
    public float Latitude { get; set; } = 45f;

    /// <summary>The tilt of the earth's axis, in degrees, earth's 23.44 unless set, none keeping every day the equinox.</summary>
    public float AxialTilt { get; set; } = 23.44f;

    /// <summary>The moon's age on day 0 of the year, the days since a new moon, 0 unless set.</summary>
    public float MoonAgeAtStart { get; set; }

    /// <summary>Moves the clock on by hours, the day of the year with it.</summary>
    /// <param name="hours">How many hours, back where under none.</param>
    public void Advance(float hours)
    {
        var total = Hour + hours;
        var days = MathF.Floor(total / 24f);
        Hour = total - (days * 24f);
        DayOfYear = ((DayOfYear + days) % 365f + 365f) % 365f;
    }

    /// <summary>The moon's age now, the days since the last new moon, from 0 to a synodic month.</summary>
    public float MoonAge => Wrap(MoonAgeAtStart + DayOfYear + (Hour / 24f), SynodicMonth);

    /// <summary>The moon's phase, from 0 at a new moon through 0.5 when full back to 1.</summary>
    public float MoonPhase => MoonAge / SynodicMonth;

    /// <summary>The share of the moon's face the sun lights as seen from here, from none new to all full.</summary>
    public float MoonLit => (1f - MathF.Cos(MoonPhase * 2f * MathF.PI)) / 2f;

    /// <summary>The way toward the sun.</summary>
    public Vec3 SunDirection => Toward(SunLongitude);

    /// <summary>The way toward the moon.</summary>
    public Vec3 MoonDirection => Toward(SunLongitude + (MoonPhase * 2f * MathF.PI));

    /// <summary>How high the sun stands above the horizon, in degrees, under it below 0.</summary>
    public float SunAltitude => Degrees(MathF.Asin(Math.Clamp(SunDirection.Y, -1f, 1f)));

    /// <summary>How high the moon stands above the horizon, in degrees.</summary>
    public float MoonAltitude => Degrees(MathF.Asin(Math.Clamp(MoonDirection.Y, -1f, 1f)));

    /// <summary>The way toward the pole star, north and as high as the latitude.</summary>
    public Vec3 Pole
    {
        get
        {
            var phi = Radians(Latitude);
            return new Vec3(0f, MathF.Sin(phi), -MathF.Cos(phi));
        }
    }

    /// <summary>How far the stars have turned about the pole, the local sidereal time, in radians.</summary>
    /// <remarks>
    /// The sun's hour angle, which the hour gives, and its right ascension, so the sun stands where
    /// the hour puts it and every star turns with it a little faster, a full turn a sidereal day.
    /// </remarks>
    public float SkyTurn => Wrap(((Hour - 12f) * MathF.PI / 12f) + RightAscension(SunLongitude), 2f * MathF.PI);

    /// <summary>Which part of the day the sun's height makes it.</summary>
    public DayPhase Phase
    {
        get
        {
            var altitude = SunAltitude;
            if (altitude > 6f) return DayPhase.Day;
            if (altitude < -12f) return DayPhase.Night;
            return Hour < 12f ? DayPhase.Dawn : DayPhase.Dusk;
        }
    }

    /// <summary>
    /// The way toward a point of the sky given in its own coordinates, its right ascension and
    /// declination in radians, as the stars stand at this hour.
    /// </summary>
    public Vec3 TowardSky(float rightAscension, float declination) => Local(SkyTurn - rightAscension, declination);

    // The sun's longitude along the ecliptic, 0 at the March equinox.
    private float SunLongitude => (DayOfYear + (Hour / 24f) - 79f) / 365.2422f * 2f * MathF.PI;

    private float RightAscension(float longitude) =>
        MathF.Atan2(MathF.Cos(Radians(AxialTilt)) * MathF.Sin(longitude), MathF.Cos(longitude));

    private float Declination(float longitude) => MathF.Asin(MathF.Sin(Radians(AxialTilt)) * MathF.Sin(longitude));

    // The way toward a body at a longitude along the ecliptic, its hour angle the sidereal time
    // less its right ascension.
    private Vec3 Toward(float longitude) => Local(SkyTurn - RightAscension(longitude), Declination(longitude));

    // The way toward a body at an hour angle and a declination, for the latitude.
    private Vec3 Local(float hourAngle, float declination)
    {
        var phi = Radians(Latitude);
        var east = -MathF.Cos(declination) * MathF.Sin(hourAngle);
        var north = (MathF.Sin(declination) * MathF.Cos(phi)) - (MathF.Cos(declination) * MathF.Cos(hourAngle) * MathF.Sin(phi));
        var up = (MathF.Sin(declination) * MathF.Sin(phi)) + (MathF.Cos(declination) * MathF.Cos(hourAngle) * MathF.Cos(phi));
        return new Vec3(east, up, -north).Normalized;
    }

    private static float Radians(float degrees) => degrees * MathF.PI / 180f;

    private static float Degrees(float radians) => radians * 180f / MathF.PI;

    private static float Wrap(float value, float period) => ((value % period) + period) % period;
}
