using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>The weather on a camera (<see cref="Render.SetWeather"/>), which moves its clouds, its fog and its rain.</summary>
[Collection("engine")]
public sealed class SkyWeatherTests
{
    [SkippableFact]
    public void ARainyDeckCoversTheSkyThickensTheFogAndRains()
    {
        Needs.Shaders();
        var weather = new WeatherState();
        weather.Set(WeatherPreset.Rain, immediately: true);
        CloudSettings? given = null;
        SkyFogSettings? fog = null;
        PrecipitationSettings? rained = null;
        ThunderSettings? stormed = null;
        var run = new PictureRun
        {
            Width = 320,
            Height = 180,
            Frames = 1500,
            Scene = ecs =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings { FieldOfView = 60f });
                var tilt = 30f * MathF.PI / 180f;
                ecs.Add(camera, Transform.LookingAt(new Vec3(0f, 2f, 0f), new Vec3(-MathF.Cos(tilt), 2f + MathF.Sin(tilt), 0f), Vec3.UnitY));
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetAtmosphere(camera, new AtmosphereSettings());
                Render.SetSkyLighting(camera);
                var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 12_500f, Shadows = false });
                ecs.Add(sun, Transform.LookingAt(new Vec3(-0.5f, 0.8f, -0.3f), Vec3.Zero, Vec3.UnitY));
                // The clouds shadowing the sun, so the shadow's dispatch takes the weather's numbers
                // every frame beside the march.
                Render.SetWeather(camera, weather, new CloudSettings { ShadowLight = sun }, new SkyFogSettings { Visibility = 2000f }, new PrecipitationSettings { Roofs = false }, new ThunderSettings { Sound = false });
            },
        };
        run.Wait(10)
            .Until("compiled", _ => Clouds.Ready() && SkyFog.All().All(f => f.Ready) && Precipitation.Ready())
            .Until("ready", _ => Render.PipelinesReady())
            .Wait(60)
            .Capture("sky")
            .Do("reading", _ => (given, fog, rained, stormed) = (Clouds.All().Single().Settings, SkyFog.All().Single().Settings, Precipitation.All().Single().Settings, SkyThunder.All().Single().Settings))
            .Go();

        var rain = WeatherConditions.Of(WeatherPreset.Rain);
        Assert.Equal((rain.CloudCover, rain.Stratus, rain.CloudBase), (given!.Coverage, given.Stratus, given.Bottom));
        Assert.True(fog!.Visibility < 1500f, $"the rain's haze leaves the fog seen to {fog.Visibility}");
        Assert.Equal(rain.Rain, rained!.Rain, 3);
        Assert.True(rained.Wind.Length > 3f, $"the rain drifts on a wind of {rained.Wind.Length} meters a second");
        Assert.Equal(rain.Thunder, stormed!.Rate);

        // The sky above the horizon a gray deck, little of its blue left.
        var picture = run.Picture("sky");
        var blue = 0;
        var all = 0;
        for (var y = 0u; y < picture.Height / 2; y += 2)
            for (var x = 0u; x < picture.Width; x += 2)
            {
                var (r, _, b, _) = picture.At(x, y);
                if (b - r > 30) blue++;
                all++;
            }
        Assert.True(blue < all / 10, $"{blue} of {all} of the sky's pixels are still blue under the rain's deck");
    }
}
