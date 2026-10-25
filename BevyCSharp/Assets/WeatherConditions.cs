namespace Bevy;

/// <summary>
/// The weather at a moment, how much cloud and of what kind, how hard it rains or snows, how thick
/// the fog is and how the wind blows, which a <see cref="WeatherState"/> eases toward and
/// <see cref="Render.SetWeather"/> gives a camera's clouds and fog.
/// </summary>
/// <remarks>
/// Each preset (<see cref="Of"/>) is a set of these, the clouds' base and top from a height and a
/// thickness for the kind of cloud it is, and the share of a flat deck from how overcast and how
/// rainy it is.
/// </remarks>
public sealed class WeatherConditions
{
    /// <summary>How much of the sky has cloud, from none at 0 to all at 1.</summary>
    public float CloudCover { get; set; }

    /// <summary>How much of the cover is a flat deck rather than heaps, from heaps at 0 to a deck at 1.</summary>
    public float Stratus { get; set; }

    /// <summary>The clouds' base above the ground, in meters.</summary>
    public float CloudBase { get; set; } = 1500f;

    /// <summary>The clouds' top, in meters.</summary>
    public float CloudTop { get; set; } = 4000f;

    /// <summary>How dense the clouds are against the settings' density, 1 as they are.</summary>
    public float CloudDensity { get; set; } = 1f;

    /// <summary>How hard it rains, from none at 0 to a downpour at 1.</summary>
    public float Rain { get; set; }

    /// <summary>How hard it snows, from none at 0 to a blizzard at 1.</summary>
    public float Snow { get; set; }

    /// <summary>How thick the fog is, from the settings' own at 0 to some eighty meters seen at 1.</summary>
    public float Fog { get; set; }

    /// <summary>How many strikes of lightning a minute.</summary>
    public float Thunder { get; set; }

    /// <summary>How fast the wind blows, in meters a second.</summary>
    public float WindSpeed { get; set; } = 2f;

    /// <summary>Where the wind comes from, in degrees clockwise from north, a north wind at 0 blowing south.</summary>
    public float WindFrom { get; set; }

    /// <summary>How much the wind gusts, from steady at 0 to its speed swinging by more than half at 1.</summary>
    public float Gusts { get; set; } = 0.15f;

    /// <summary>The air's temperature, in degrees Celsius, under which rain falls as snow.</summary>
    public float Temperature { get; set; } = 18f;

    /// <summary>A copy, to change without changing these.</summary>
    public WeatherConditions Copy() => Lerp(this, this, 0f);

    /// <summary>The weather part of the way from one set of conditions to another, each number that far between the two.</summary>
    /// <param name="from">The conditions at none of the way.</param>
    /// <param name="to">The conditions at all of it.</param>
    /// <param name="share">How far, from 0 to 1, held there.</param>
    public static WeatherConditions Lerp(WeatherConditions from, WeatherConditions to, float share)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        var t = Math.Clamp(share, 0f, 1f);
        float Mix(float a, float b) => a + ((b - a) * t);

        // The wind's direction turns the short way round.
        var turn = (((to.WindFrom - from.WindFrom) % 360f) + 540f) % 360f - 180f;
        return new WeatherConditions
        {
            CloudCover = Mix(from.CloudCover, to.CloudCover),
            Stratus = Mix(from.Stratus, to.Stratus),
            CloudBase = Mix(from.CloudBase, to.CloudBase),
            CloudTop = Mix(from.CloudTop, to.CloudTop),
            CloudDensity = Mix(from.CloudDensity, to.CloudDensity),
            Rain = Mix(from.Rain, to.Rain),
            Snow = Mix(from.Snow, to.Snow),
            Fog = Mix(from.Fog, to.Fog),
            Thunder = Mix(from.Thunder, to.Thunder),
            WindSpeed = Mix(from.WindSpeed, to.WindSpeed),
            WindFrom = ((from.WindFrom + (turn * t)) % 360f + 360f) % 360f,
            Gusts = Mix(from.Gusts, to.Gusts),
            Temperature = Mix(from.Temperature, to.Temperature),
        };
    }

    /// <summary>The conditions a preset names.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The number names no preset.</exception>
    public static WeatherConditions Of(WeatherPreset preset) => preset switch
    {
        WeatherPreset.Clear => new() { WindSpeed = 2f },
        WeatherPreset.FewClouds => new() { CloudCover = 0.3f, CloudBase = 1500f, CloudTop = 3500f, WindSpeed = 3f },
        WeatherPreset.PartlyCloudy => new() { CloudCover = 0.5f, CloudBase = 1400f, CloudTop = 4500f, WindSpeed = 4.5f, Gusts = 0.25f },
        WeatherPreset.Overcast => new() { CloudCover = 1f, Stratus = 0.9f, CloudBase = 1100f, CloudTop = 2200f, CloudDensity = 1.3f, WindSpeed = 5f, Gusts = 0.2f, Temperature = 13f },
        WeatherPreset.Hazy => new() { CloudCover = 0.35f, Stratus = 0.6f, CloudBase = 6000f, CloudTop = 7000f, CloudDensity = 0.25f, Fog = 0.12f, Temperature = 24f },
        WeatherPreset.Drizzle => new() { CloudCover = 0.9f, Stratus = 0.8f, CloudBase = 900f, CloudTop = 1700f, CloudDensity = 1.3f, Rain = 0.2f, Fog = 0.12f, WindSpeed = 4f, Temperature = 11f },
        WeatherPreset.Rain => new() { CloudCover = 1f, Stratus = 0.7f, CloudBase = 800f, CloudTop = 2400f, CloudDensity = 1.6f, Rain = 0.55f, Fog = 0.18f, WindSpeed = 7f, Gusts = 0.35f, Temperature = 10f },
        WeatherPreset.Storm => new() { CloudCover = 1f, Stratus = 0.4f, CloudBase = 600f, CloudTop = 3500f, CloudDensity = 2f, Rain = 0.85f, Fog = 0.28f, Thunder = 0.4f, WindSpeed = 16f, Gusts = 0.7f, Temperature = 9f },
        WeatherPreset.Thunderstorm => new() { CloudCover = 1f, Stratus = 0.2f, CloudBase = 500f, CloudTop = 6000f, CloudDensity = 2.2f, Rain = 1f, Fog = 0.3f, Thunder = 4f, WindSpeed = 20f, Gusts = 0.85f, Temperature = 12f },
        WeatherPreset.LightSnow => new() { CloudCover = 0.85f, Stratus = 0.8f, CloudBase = 900f, CloudTop = 1700f, Snow = 0.25f, Fog = 0.1f, WindSpeed = 2f, Gusts = 0.3f, Temperature = -2f },
        WeatherPreset.Snow => new() { CloudCover = 1f, Stratus = 0.85f, CloudBase = 800f, CloudTop = 2000f, CloudDensity = 1.4f, Snow = 0.6f, Fog = 0.25f, WindSpeed = 5f, Gusts = 0.45f, Temperature = -6f },
        WeatherPreset.Blizzard => new() { CloudCover = 1f, Stratus = 0.9f, CloudBase = 500f, CloudTop = 2300f, CloudDensity = 1.8f, Snow = 1f, Fog = 0.7f, WindSpeed = 26f, Gusts = 1f, Temperature = -15f },
        WeatherPreset.Fog => new() { CloudCover = 0.6f, Stratus = 0.9f, CloudBase = 1000f, CloudTop = 1500f, CloudDensity = 0.5f, Fog = 0.9f, WindSpeed = 0.6f, Gusts = 0.05f, Temperature = 8f },
        WeatherPreset.MistyMorning => new() { CloudCover = 0.25f, CloudBase = 2200f, CloudTop = 3000f, Fog = 0.55f, WindSpeed = 1f, Gusts = 0.08f, Temperature = 7f },
        WeatherPreset.Sandstorm => new() { CloudCover = 0.1f, Fog = 0.8f, WindSpeed = 18f, Gusts = 0.8f, Temperature = 32f },
        _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "The number names no preset."),
    };
}
