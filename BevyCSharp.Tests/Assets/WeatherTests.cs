using Bevy;
using Bevy.Reflected;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the weather: off unless an app asks for it, and where it runs, its clock, its forecast
/// and its conditions reached through the wrappers and a preset set through the bridge.
/// </summary>
/// <remarks>
/// Each run keeps the clouds at the lowest tier, since a software renderer marches them for every
/// pixel of sky, and the tests are about what reaches the weather rather than how it looks.
/// </remarks>
[Collection("engine")]
public sealed class WeatherTests
{
    /// <summary>
    /// An app that did not ask for the weather has none, and none of its resources.
    /// </summary>
    [SkippableFact]
    public void TheWeatherIsOffUnlessAskedFor()
    {
        Needs.Renderer();

        var active = true;
        var time = true;

        var run = new PictureRun
        {
            Scene = ecs =>
            {
                active = Weather.Active;
                time = ecs.Resource<WeatherTimeRef>() is not null;
            },
        };

        run.Wait(2).Go();

        Assert.False(active);
        Assert.False(time);
    }

    /// <summary>
    /// Where the weather runs, its clock stopped at noon puts the sun up and at midnight puts it
    /// down, and a preset taken at once with the forecast off is the conditions the weather holds.
    /// </summary>
    [SkippableFact]
    public void TheClockAndAPresetReachTheWeather()
    {
        Needs.Renderer();

        var active = false;
        var noon = float.NaN;
        var midnight = float.NaN;
        var coverage = float.NaN;
        var target = float.NaN;

        var run = new PictureRun
        {
            Configure = config => config.Weather = true,
            Scene = ecs =>
            {
                active = Weather.Active;
                if (!active) return;

                var camera = PictureRun.Camera(ecs);
                ecs.Insert<WeatherCameraRef>(camera);

                var settings = ecs.Resource<WeatherConfigRef>()!.Value;
                settings.Quality = WeatherConfigRef.QualityVariant.Potato;
            },
        };

        static EcsWorld Ecs(World world) => world.Resource<EcsWorld>();

        run.Do("noon, with the forecast off and the sky overcast at once", world =>
            {
                if (!active) return;

                var time = Ecs(world).Resource<WeatherTimeRef>()!.Value;
                time.Paused = true;
                time.TimeOfDay = 0.5f;
                var forecast = Ecs(world).Resource<ProceduralWeatherRef>()!.Value;
                forecast.Enabled = false;
                Weather.SetPreset(WeatherPreset.Overcast, immediately: true);
            })
            .Wait(3)
            .Do("reading noon", world =>
            {
                if (!active) return;

                noon = Ecs(world).Resource<CelestialBodiesRef>()!.Value.SunAltitude;
                var weather = Ecs(world).Resource<WeatherRef>()!.Value;
                coverage = weather.CurrentCloudCoverage;
                target = weather.TargetCloudCoverage;
                var clock = Ecs(world).Resource<WeatherTimeRef>()!.Value;
                clock.TimeOfDay = 0f;
            })
            .Wait(3)
            .Do("reading midnight", world =>
            {
                if (active) midnight = Ecs(world).Resource<CelestialBodiesRef>()!.Value.SunAltitude;
            })
            .Go();

        Assert.True(active, "the weather did not run where it was asked for");
        Assert.True(noon > 0f, $"the sun stood at {noon} at noon");
        Assert.True(midnight < 0f, $"the sun stood at {midnight} at midnight");
        Assert.Equal(0.95f, target, 3);
        Assert.Equal(0.95f, coverage, 2);
    }

    /// <summary>
    /// A number that names no preset is refused on this side, before it reaches the bridge.
    /// </summary>
    [Fact]
    public void ANumberNamingNoPresetIsRefused() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Weather.SetPreset((WeatherPreset)99));

    /// <summary>
    /// Where meshlets run the weather is kept out, since a camera under its atmosphere would end
    /// the app there, and the app runs on without it.
    /// </summary>
    [SkippableFact]
    public void TheWeatherIsKeptOutWhereMeshletsRun()
    {
        Needs.Renderer();

        var meshlets = false;
        var weather = true;

        var run = new PictureRun
        {
            Configure = config =>
            {
                config.MeshletClusters = 1 << 20;
                config.Weather = true;
            },
            Scene = _ =>
            {
                meshlets = Render.MeshletsActive;
                weather = Weather.Active;
            },
        };

        run.Wait(2).Go();

        Needs.Meshlets(meshlets);
        Assert.False(weather);
    }
}
