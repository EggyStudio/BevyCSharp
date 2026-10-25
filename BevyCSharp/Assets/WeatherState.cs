namespace Bevy;

/// <summary>
/// The weather as it goes, its conditions easing toward the ones it is given or a forecast writes,
/// the wind gusting, the ground wetting in rain and whitening under snow.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Current"/> eases toward <see cref="Target"/> by half the way every
/// <see cref="HalfLife"/> seconds, so a front comes in over a minute rather than at once.
/// <see cref="Set(WeatherPreset, bool)"/> gives it a preset to ease toward or to take at once. With
/// <see cref="Forecast"/> on, the target is written every step from the clock's day and hour, a
/// storminess rising and falling over <see cref="SystemsPerDay"/> weather systems a day by
/// <see cref="Climate"/>, between clear skies and thunderstorms, rain falling as snow below a degree,
/// and a mist on calm mornings, the same for the same <see cref="Seed"/>.
/// </para>
/// <para>
/// <see cref="Wind"/> is the wind's velocity this moment, its speed swinging with the conditions'
/// gusts and its direction wavering, and <see cref="Carried"/> how far it has carried what it
/// carries, which the clouds and the fog drift by. <see cref="Wetness"/> and
/// <see cref="SnowCover"/> are the ground's, for a program's materials to darken and whiten by.
/// <see cref="Render.SetWeather"/> moves a state on every frame and gives a camera's clouds and fog
/// its conditions, and a program reading it without a camera moves it on with
/// <see cref="Advance"/>.
/// </para>
/// </remarks>
public sealed class WeatherState
{
    /// <summary>The conditions now.</summary>
    public WeatherConditions Current { get; private set; } = WeatherConditions.Of(WeatherPreset.Clear);

    /// <summary>The conditions being eased toward.</summary>
    public WeatherConditions Target { get; private set; } = WeatherConditions.Of(WeatherPreset.Clear);

    /// <summary>How many seconds of the game's clock the conditions take to come half the way to the target, 30 unless set.</summary>
    public float HalfLife { get; set; } = 30f;

    /// <summary>Whether a forecast writes the target from the clock every step, off unless set.</summary>
    public bool Forecast { get; set; }

    /// <summary>What weather the forecast brings, <see cref="WeatherClimate.Temperate"/> unless set.</summary>
    public WeatherClimate Climate { get; set; } = WeatherClimate.Temperate;

    /// <summary>How many weather systems pass a day in the forecast, 1.5 unless set.</summary>
    public float SystemsPerDay { get; set; } = 1.5f;

    /// <summary>The seed the forecast is made from, so two worlds' weathers differ, 0 unless set.</summary>
    public uint Seed { get; set; }

    /// <summary>The clock the forecast reads the day and the hour from, or none to keep its own.</summary>
    public SkyClock? Clock { get; set; }

    /// <summary>The wind's velocity now, in meters a second, gusts and all.</summary>
    public Vec3 Wind { get; private set; }

    /// <summary>How far the wind has carried what it carries since the state began, in meters.</summary>
    public Vec3 Carried { get; private set; }

    /// <summary>How wet the ground is, from dry at 0 to soaked at 1.</summary>
    public float Wetness { get; private set; }

    /// <summary>How much of the ground lies under snow, from none at 0 to all at 1.</summary>
    public float SnowCover { get; private set; }

    // The state's own time, which the gusts follow, and its own day and hour where no clock is given.
    private double _seconds;
    private float _ownDay = 79f;
    private float _ownHour = 12f;

    /// <summary>Gives the weather a preset to ease toward, or to take at once.</summary>
    public void Set(WeatherPreset preset, bool immediately = false) => Set(WeatherConditions.Of(preset), immediately);

    /// <summary>Gives the weather conditions to ease toward, or to take at once.</summary>
    public void Set(WeatherConditions target, bool immediately = false)
    {
        ArgumentNullException.ThrowIfNull(target);
        Target = target.Copy();
        if (immediately) Current = target.Copy();
    }

    /// <summary>Moves the weather on, its conditions, its wind and the ground.</summary>
    /// <param name="seconds">How many seconds of the game's clock.</param>
    public void Advance(float seconds)
    {
        var step = Math.Max(seconds, 0f);
        _seconds += step;
        if (Clock is null)
        {
            _ownHour += step / 3600f;
            if (_ownHour >= 24f)
            {
                _ownHour -= 24f;
                _ownDay = (_ownDay + 1f) % 365f;
            }
        }

        if (Forecast) Target = ForecastAt(Clock?.DayOfYear ?? _ownDay, Clock?.Hour ?? _ownHour);
        var share = HalfLife > 0f ? 1f - MathF.Pow(0.5f, step / HalfLife) : 1f;
        Current = WeatherConditions.Lerp(Current, Target, share);

        Blow(step);
        Ground(step);
    }

    /// <summary>The conditions the forecast gives a day of the year and an hour.</summary>
    public WeatherConditions ForecastAt(float dayOfYear, float hour)
    {
        var (mean, swing, warmth, seasons) = Climate switch
        {
            WeatherClimate.Maritime => (0.55f, 0.7f, 11f, 6f),
            WeatherClimate.Arid => (0.12f, 0.5f, 27f, 8f),
            WeatherClimate.Tropical => (0.5f, 0.9f, 27f, 2f),
            WeatherClimate.Polar => (0.45f, 0.7f, -12f, 10f),
            _ => (0.4f, 0.9f, 12f, 10f),
        };

        var time = (dayOfYear + (hour / 24f)) * SystemsPerDay;
        var noise = (0.5f * Smooth(time, 1)) + (0.3f * Smooth((time * 2.1f) + 17f, 2)) + (0.2f * Smooth((time * 4.3f) + 31f, 3));
        var storminess = Math.Clamp(mean + ((noise - 0.5f) * swing * 2f), 0f, 1f);

        // Up a ladder of presets from clear to a thunderstorm, between the two either side.
        (float At, WeatherPreset Preset)[] ladder =
        [
            (0f, WeatherPreset.Clear), (0.18f, WeatherPreset.FewClouds), (0.36f, WeatherPreset.PartlyCloudy),
            (0.55f, WeatherPreset.Overcast), (0.7f, WeatherPreset.Rain), (0.85f, WeatherPreset.Storm), (1f, WeatherPreset.Thunderstorm),
        ];
        var above = 1;
        while (above < ladder.Length - 1 && ladder[above].At < storminess) above++;
        var below = ladder[above - 1];
        var conditions = WeatherConditions.Lerp(
            WeatherConditions.Of(below.Preset), WeatherConditions.Of(ladder[above].Preset),
            (storminess - below.At) / (ladder[above].At - below.At));

        // Coldest in the middle of January and warmest at three in the afternoon, a hemisphere
        // turned where the clock's latitude is south.
        var south = (Clock?.Latitude ?? 45f) < 0f ? MathF.PI : 0f;
        conditions.Temperature = warmth - (seasons * MathF.Cos((2f * MathF.PI * (dayOfYear - 15f) / 365f) + south))
            + (4f * MathF.Sin(2f * MathF.PI * (hour - 9f) / 24f));
        if (conditions.Temperature < 1f)
        {
            conditions.Snow = Math.Max(conditions.Snow, conditions.Rain);
            conditions.Rain = 0f;
            conditions.Thunder *= 0.1f;
        }

        // A mist on calm mornings, thickest a little after six.
        var dawn = MathF.Exp(-MathF.Pow((hour - 6.25f) / 1.25f, 2f));
        conditions.Fog = Math.Min(1f, conditions.Fog + (0.4f * dawn * (1f - storminess)));

        // A westerly that wanders.
        conditions.WindFrom = 270f + ((Smooth((time * 0.5f) + 99f, 4) - 0.5f) * 140f);
        return conditions;
    }

    // The wind this moment, its speed swinging with the gusts and its direction wavering.
    private void Blow(float seconds)
    {
        var t = (float)(_seconds % 100_000.0);
        var gust = (0.6f * Smooth((t * 0.35f) + 7f, 5)) + (0.4f * Smooth((t * 1.3f) + 3f, 6));
        var speed = Current.WindSpeed * (1f + (Current.Gusts * ((gust * 2f) - 1f) * 0.6f));
        var from = (Current.WindFrom + ((Smooth((t * 0.2f) + 11f, 7) - 0.5f) * 30f * Current.Gusts)) * MathF.PI / 180f;

        // Coming from the north, at 0, it blows south, along positive Z.
        Wind = new Vec3(-MathF.Sin(from), 0f, MathF.Cos(from)) * Math.Max(speed, 0f);
        Carried += Wind * seconds;
    }

    // The ground wetting in rain and drying after, the snow lying where it falls cold and melting
    // in the warm.
    private void Ground(float seconds)
    {
        var raining = Current.Rain > 0.02f && Current.Temperature >= 1f;
        Wetness = raining
            ? Wetness + ((1f - Wetness) * (1f - MathF.Exp(-seconds * Current.Rain / 20f)))
            : Wetness * MathF.Exp(-seconds / 400f);

        if (Current.Snow > 0.02f && Current.Temperature < 1f)
            SnowCover = Math.Min(1f, SnowCover + (Current.Snow * seconds / 300f));
        else if (Current.Temperature > 1f)
            SnowCover = Math.Max(0f, SnowCover - (seconds / 600f * Math.Min(Current.Temperature / 5f, 2f)));
    }

    // Value noise over a line, from 0 to 1, smooth between whole numbers, a different line for each
    // channel and seed.
    private float Smooth(float x, int channel)
    {
        var cell = MathF.Floor(x);
        var f = x - cell;
        f = f * f * (3f - (2f * f));
        return Hash((int)cell, channel) + ((Hash((int)cell + 1, channel) - Hash((int)cell, channel)) * f);
    }

    private float Hash(int cell, int channel)
    {
        var h = (uint)cell * 0x9E3779B1u ^ ((uint)channel * 0x85EBCA77u) ^ (Seed * 0xC2B2AE3Du);
        h ^= h >> 15;
        h *= 0x2C1B3C6Du;
        h ^= h >> 12;
        h *= 0x297A2D39u;
        h ^= h >> 15;
        return h / (float)uint.MaxValue;
    }
}
