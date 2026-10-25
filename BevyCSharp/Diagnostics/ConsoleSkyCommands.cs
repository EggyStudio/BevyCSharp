using System.Globalization;

namespace Bevy;

/// <summary>
/// The sky's fog, read and set from the console, the command line and the editor's console alike.
/// </summary>
internal static class ConsoleSkyCommands
{
    [Command("sky.fog", "Draws fog in the sky's color over the cameras drawing it, or else those it was taken off, or else the camera the scene's distance field lies around, gone to 95% this far at its base, thinning by e this high above it, or takes it off, or says it: sky.fog [off|visibility] [height] [base]")]
    internal static string Fog(string visibility = "", float height = 80f, float @base = 0f)
    {
        var word = visibility.Trim().ToLowerInvariant();
        var fogged = SkyFog.All().ToList();
        if (word == "")
        {
            return fogged.Count == 0
                ? "no camera draws the sky's fog"
                : string.Join("\n", fogged.Select(f => string.Create(CultureInfo.InvariantCulture,
                    $"{f.Camera}: fog to 95% at {f.Settings.Visibility:0.#} m, thinning by e every {f.Settings.Height:0.#} m above {f.Settings.Base:0.#}{(f.Settings.Froxels is { } froxels ? $", in {froxels.Quality.ToString().ToLowerInvariant()} froxels to {froxels.Distance:0.#} m" : "")}, {(f.Ready ? "drawn" : "its draw compiling")}")));
        }

        // The cameras drawing fog, or else the ones it was taken off, or else the field's.
        var off = SkyFog.Off().ToList();
        var cameras = fogged.Count > 0 ? fogged.Select(f => f.Camera).ToList()
            : off.Count > 0 ? off.Select(f => f.Camera).ToList()
            : SceneField.ViewCamera is { IsNone: false } view ? [view] : [];
        if (cameras.Count == 0)
        {
            ConsoleHost.Fail("NO_CAMERA", "No camera draws the sky's fog, and the scene's distance field lies around none.");
            return "sky.fog [off|visibility] [height] [base]";
        }

        if (word == "off")
        {
            foreach (var camera in cameras) Render.ClearSkyFog(camera);
            return "the sky's fog is off";
        }

        if (!float.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out var far) || far <= 0f || height <= 0f)
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{visibility.Trim()}' is not off or a distance above none, or the height is not above none.");
            return "sky.fog [off|visibility] [height] [base]";
        }

        foreach (var camera in cameras)
        {
            var was = fogged.FirstOrDefault(f => f.Camera == camera).Settings
                ?? off.FirstOrDefault(f => f.Camera == camera).Settings
                ?? new SkyFogSettings();
            Render.SetSkyFog(camera, new SkyFogSettings
            {
                Visibility = far,
                Height = height,
                Base = @base,
                Start = was.Start,
                HorizonDegrees = was.HorizonDegrees,
                GroundShade = was.GroundShade,
                BandDegrees = was.BandDegrees,
                Color = was.Color,
                Brightness = was.Brightness,
                Froxels = was.Froxels,
            });
        }
        return string.Create(CultureInfo.InvariantCulture, $"fog to 95% at {far:0.#} m, thinning by e every {height:0.#} m above {@base:0.#}, on {cameras.Count} camera{(cameras.Count == 1 ? "" : "s")}");
    }

    [Command("sky.froxels", "Works the sky's fog out in froxels near the cameras drawing it, as far as given, where shadows cut it into shafts and lamps glow in it, or in closed form alone: sky.froxels <off|low|medium|high> [distance]")]
    internal static string Froxels(string quality, float distance = 250f)
    {
        var word = quality.Trim().ToLowerInvariant();
        var fogged = SkyFog.All().ToList();
        if (fogged.Count == 0)
        {
            ConsoleHost.Fail("NO_FOG", "No camera draws the sky's fog. Give one fog with sky.fog first.");
            return "sky.froxels <off|low|medium|high> [distance]";
        }

        FroxelQuality? asked = null;
        if (word == "low") asked = FroxelQuality.Low;
        else if (word == "medium") asked = FroxelQuality.Medium;
        else if (word == "high") asked = FroxelQuality.High;
        if ((asked is null && word != "off") || !(distance > 1f))
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{quality.Trim()}' is not off, low, medium or high, or the distance is not past a meter.");
            return "sky.froxels <off|low|medium|high> [distance]";
        }

        foreach (var (camera, was, _) in fogged)
        {
            Render.SetSkyFog(camera, new SkyFogSettings
            {
                Visibility = was.Visibility,
                Height = was.Height,
                Base = was.Base,
                Start = was.Start,
                HorizonDegrees = was.HorizonDegrees,
                GroundShade = was.GroundShade,
                BandDegrees = was.BandDegrees,
                Color = was.Color,
                Brightness = was.Brightness,
                Froxels = asked is { } level ? new FroxelFogSettings
                {
                    Quality = level,
                    Distance = distance,
                    Lumps = was.Froxels?.Lumps ?? 0.5f,
                    LumpSize = was.Froxels?.LumpSize ?? 30f,
                    Wind = was.Froxels?.Wind ?? new Vec3(1.5f, 0f, 0.8f),
                    Lights = was.Froxels?.Lights ?? 16,
                    Boxes = was.Froxels?.Boxes ?? [],
                } : null,
            });
        }
        return asked is { } q
            ? string.Create(CultureInfo.InvariantCulture, $"the fog in {q.ToString().ToLowerInvariant()} froxels to {distance:0.#} m, on {fogged.Count} camera{(fogged.Count == 1 ? "" : "s")}")
            : "the fog in closed form alone";
    }
}
