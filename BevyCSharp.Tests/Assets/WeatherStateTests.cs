using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>The engine's weather (<see cref="WeatherState"/>), its presets, its easing, its forecast, its wind and its ground.</summary>
public sealed class WeatherStateTests
{
    [Fact]
    public void ThePresetsCarryTheCratesSkies()
    {
        Assert.Equal(0f, WeatherConditions.Of(WeatherPreset.Clear).CloudCover);
        var overcast = WeatherConditions.Of(WeatherPreset.Overcast);
        Assert.True(overcast.CloudCover == 1f && overcast.Stratus > 0.5f && overcast.Rain == 0f, "an overcast is a deck with no rain");
        Assert.True(WeatherConditions.Of(WeatherPreset.Snow).Temperature < 0f && WeatherConditions.Of(WeatherPreset.Snow).Snow > 0f, "snow falls cold");
        Assert.True(WeatherConditions.Of(WeatherPreset.Thunderstorm).Thunder > WeatherConditions.Of(WeatherPreset.Storm).Thunder, "a thunderstorm strikes more often than a storm");
        Assert.Throws<ArgumentOutOfRangeException>(() => WeatherConditions.Of((WeatherPreset)99));
    }

    [Fact]
    public void TheWeatherComesHalfTheWayInAHalfLife()
    {
        var weather = new WeatherState { HalfLife = 20f };
        weather.Set(WeatherPreset.Overcast);
        for (var i = 0; i < 20; i++) weather.Advance(1f);
        Assert.InRange(weather.Current.CloudCover, 0.49f, 0.51f);

        weather.Set(WeatherPreset.Rain, immediately: true);
        Assert.Equal(WeatherConditions.Of(WeatherPreset.Rain).Rain, weather.Current.Rain);
    }

    [Fact]
    public void TheForecastIsTheSameForASeedAndMovesOverTheDays()
    {
        var a = new WeatherState { Seed = 7 };
        var b = new WeatherState { Seed = 7 };
        Assert.Equal(a.ForecastAt(120f, 14f).CloudCover, b.ForecastAt(120f, 14f).CloudCover);

        var covers = Enumerable.Range(0, 60).Select(day => a.ForecastAt(100f + day, 14f).CloudCover).ToArray();
        Assert.True(covers.Max() - covers.Min() > 0.5f, $"the cover ranges only from {covers.Min()} to {covers.Max()} over two months");
    }

    [Fact]
    public void AnAridClimateIsDrierThanAMaritimeOne()
    {
        float Wet(WeatherClimate climate)
        {
            var weather = new WeatherState { Climate = climate, Seed = 3 };
            return Enumerable.Range(0, 120).Average(day => weather.ForecastAt(day * 3f % 365f, 14f).Rain + weather.ForecastAt(day * 3f % 365f, 14f).Snow);
        }
        Assert.True(Wet(WeatherClimate.Arid) < Wet(WeatherClimate.Maritime) * 0.5f, $"arid {Wet(WeatherClimate.Arid)}, maritime {Wet(WeatherClimate.Maritime)}");
    }

    [Fact]
    public void AColdWinterSnowsWhereAWarmOneRains()
    {
        var polar = new WeatherState { Climate = WeatherClimate.Polar, Seed = 5 };
        var days = Enumerable.Range(0, 365).Select(day => polar.ForecastAt(day, 14f)).ToArray();
        Assert.True(days.Any(d => d.Snow > 0.2f), "the polar year has snowy days");
        Assert.All(days.Where(d => d.Temperature < 1f), d => Assert.Equal(0f, d.Rain));
    }

    [Fact]
    public void TheWindBlowsFromWhereItSaysWithinItsGusts()
    {
        var weather = new WeatherState();
        weather.Set(new WeatherConditions { WindSpeed = 10f, WindFrom = 0f, Gusts = 0.5f }, immediately: true);
        for (var i = 0; i < 600; i++)
        {
            weather.Advance(0.1f);
            var speed = weather.Wind.Length;
            Assert.InRange(speed, 10f * (1f - 0.31f), 10f * (1f + 0.31f));
            Assert.True(weather.Wind.Z > 0.9f * speed, $"a north wind blows along {weather.Wind}");
        }
        Assert.True(weather.Carried.Z > 400f, $"a minute of it carries {weather.Carried}");
    }

    [Fact]
    public void TheGroundWetsInRainAndDriesAndTheSnowLiesAndMelts()
    {
        var weather = new WeatherState();
        weather.Set(WeatherPreset.Rain, immediately: true);
        for (var i = 0; i < 120; i++) weather.Advance(1f);
        Assert.True(weather.Wetness > 0.9f, $"two minutes of rain leave the ground {weather.Wetness} wet");

        weather.Set(WeatherPreset.Clear, immediately: true);
        for (var i = 0; i < 600; i++) weather.Advance(1f);
        Assert.True(weather.Wetness < 0.3f, $"ten dry minutes leave it {weather.Wetness} wet");

        weather.Set(WeatherPreset.Snow, immediately: true);
        for (var i = 0; i < 300; i++) weather.Advance(1f);
        var lying = weather.SnowCover;
        Assert.True(lying > 0.5f, $"five minutes of snow cover {lying} of the ground");
        weather.Set(WeatherPreset.Clear, immediately: true);
        for (var i = 0; i < 300; i++) weather.Advance(1f);
        Assert.True(weather.SnowCover < lying, $"the warm melts it from {lying} to {weather.SnowCover}");
    }
}
