using Atmosphere;
using Bevy;
using Diagnostics;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the sky's settings in the Diagnostics module's registry on the panel's sky page, each
/// refused where no camera draws its part or the weather gives it, and a console line of each
/// setting a camera's fog, its froxels and its clouds as the sky's commands set them, the rest of
/// the part as it was.
/// </summary>
[Collection("engine")]
public sealed class SkySettingsTests
{
    [Fact]
    public void EverySettingIsTheRegistrysOnTheSkyPageAndRefusedWhereNoCameraDrawsItsPart()
    {
        Assert.NotEmpty(SkySettings.All);
        foreach (var setting in SkySettings.All)
        {
            Assert.Same(setting, Registry.Process.FindSetting(setting.Name));
            Assert.StartsWith("sky.", setting.Name, StringComparison.Ordinal);
            Assert.False(setting.Saved, setting.Name);
        }

        foreach (var line in new[] { "sky.fog_height 20", "sky.froxel_quality high", "sky.cloud_cover 0.5", "sky.forecast on" })
            Assert.Equal(Answer.Refused, Registry.Process.Run(line).Code);
    }

    [SkippableFact]
    public void ALineSetsTheCamerasFogFroxelsAndCloudsAndTakesThemOff()
    {
        Needs.Renderer();
        var seen = new List<string>();
        SkyFogSettings? fog = null;
        CloudSettings? clouds = null;

        var run = new PictureRun
        {
            Scene = _ =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings());
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetSkyFog(camera, new SkyFogSettings { Visibility = 600f, Color = (1f, 0f, 0f) });
            },
        };
        run.Wait(2).Do("lines", world =>
        {
            string[] lines =
            [
                "sky.fog_visibility 300", "sky.fog_height 20", "sky.fog_base 5", "sky.froxel_quality high", "sky.froxel_distance 120",
                "sky.cloud_quality low", "sky.cloud_cover 0.7", "sky.forecast on",
            ];
            foreach (var line in lines) seen.Add($"{line}: {ConsoleHost.Submit(line, world).Code ?? "done"}");

            fog = Assert.Single(SkyFog.All()).Settings;
            clouds = Assert.Single(Clouds.All()).Settings;
            seen.Add($"read {Registry.Process.FindSetting("sky.froxel_quality")!.Text} {Registry.Process.FindSetting("sky.cloud_quality")!.Text}");

            foreach (var line in new[] { "sky.cloud_quality off", "sky.fog_visibility 0" }) seen.Add($"{line}: {ConsoleHost.Submit(line, world).Code ?? "done"}");
            seen.Add($"left {SkyFog.All().Count()} fogs and {Clouds.All().Count()} clouds, read {Registry.Process.FindSetting("sky.fog_visibility")!.Text}");
        }).Go();

        Assert.Equal(
            [
                "sky.fog_visibility 300: done", "sky.fog_height 20: done", "sky.fog_base 5: done", "sky.froxel_quality high: done",
                "sky.froxel_distance 120: done", "sky.cloud_quality low: done", "sky.cloud_cover 0.7: done", $"sky.forecast on: {Answer.Refused}",
                "read High Low", "sky.cloud_quality off: done", "sky.fog_visibility 0: done", "left 0 fogs and 0 clouds, read 0",
            ],
            seen);
        Assert.Equal((300f, 20f, 5f, FroxelQuality.High, 120f), (fog!.Visibility, fog.Height, fog.Base, fog.Froxels!.Quality, fog.Froxels.Distance));
        Assert.Equal((1f, 0f, 0f), fog.Color);
        Assert.Equal((CloudQuality.Low, 0.7f), (clouds!.Quality, clouds.Coverage));
    }

    // The weather gives its camera the fog and the clouds again each frame, so their settings
    // refuse there, and the forecast is the weather's own to set.
    [SkippableFact]
    public void WhereTheWeatherGivesTheFogAndCloudsTheirSettingsRefuseAndTheForecastSets()
    {
        Needs.Renderer();
        var weather = new WeatherState();
        var seen = new List<string>();

        var run = new PictureRun
        {
            Scene = _ =>
            {
                var camera = Render.SpawnCamera3d(new CameraSettings());
                Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Msaa = 1 });
                Render.SetWeather(camera, weather, new CloudSettings(), new SkyFogSettings());
            },
        };
        run.Wait(2).Do("lines", world =>
        {
            foreach (var line in new[] { "sky.fog_visibility 300", "sky.fog_height 20", "sky.froxel_quality high", "sky.cloud_cover 0.7", "sky.cloud_quality off", "sky.forecast on" })
            {
                var answer = ConsoleHost.Submit(line, world);
                seen.Add($"{line}: {answer.Code ?? "done"}{(answer.Text.Contains("weather gives", StringComparison.Ordinal) ? " by the weather" : "")}");
            }
        }).Go();

        Assert.Equal(
            [
                $"sky.fog_visibility 300: {Answer.Refused} by the weather", $"sky.fog_height 20: {Answer.Refused} by the weather",
                $"sky.froxel_quality high: {Answer.Refused} by the weather", $"sky.cloud_cover 0.7: {Answer.Refused} by the weather",
                $"sky.cloud_quality off: {Answer.Refused} by the weather", "sky.forecast on: done",
            ],
            seen);
        Assert.True(weather.Forecast);
    }
}
