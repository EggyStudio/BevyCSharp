using System.Globalization;

namespace Bevy;

/// <summary>
/// A camera's look, read and set from the console, the command line and the editor's console alike.
/// </summary>
internal static class ConsoleLookCommands
{
    [Command("camera.look", "Gives the cameras with a look, or else those it was taken off, or else the camera the scene's distance field lies around, the engine's curve or the vivid look, with its exposure, saturation and contrast where given and a number below zero keeping the look's own, or takes it off, or says it: camera.look [off|knee|vivid] [exposure] [saturation] [contrast]")]
    internal static string Look(string curve = "", float exposure = -1f, float saturation = -1f, float contrast = -1f)
    {
        var word = curve.Trim().ToLowerInvariant();
        var looked = Looks.All().ToList();
        if (word == "")
        {
            return looked.Count == 0
                ? "no camera has a look"
                : string.Join("\n", looked.Select(l => string.Create(CultureInfo.InvariantCulture,
                    $"{l.Camera}: {Said(l.Settings)}, {(l.Ready ? "shown" : "its pass compiling")}")));
        }

        // The cameras with a look, or else the ones it was taken off, or else the field's.
        var off = Looks.Off().ToList();
        var cameras = looked.Count > 0 ? looked.Select(l => l.Camera).ToList()
            : off.Count > 0 ? off.Select(l => l.Camera).ToList()
            : SceneField.ViewCamera is { IsNone: false } view ? [view] : [];
        if (cameras.Count == 0)
        {
            ConsoleHost.Fail("NO_CAMERA", "No camera has a look, and the scene's distance field lies around none.");
            return "camera.look [off|knee|vivid] [exposure] [saturation] [contrast]";
        }

        if (word == "off")
        {
            foreach (var camera in cameras) Render.SetLook(camera, null);
            return "the look is off, Bevy's tonemapper back";
        }

        LookSettings look;
        if (word == "knee") look = new LookSettings();
        else if (word == "vivid") look = LookSettings.Vivid();
        else
        {
            ConsoleHost.Fail("BAD_ARGUMENT", $"'{curve.Trim()}' is not off, knee or vivid.");
            return "camera.look [off|knee|vivid] [exposure] [saturation] [contrast]";
        }

        if (exposure >= 0f) look.Exposure = exposure;
        if (saturation >= 0f) look.Saturation = saturation;
        if (contrast >= 0f) look.Contrast = contrast;
        foreach (var camera in cameras) Render.SetLook(camera, look);
        return string.Create(CultureInfo.InvariantCulture, $"{Said(look)}, on {cameras.Count} camera{(cameras.Count == 1 ? "" : "s")}");
    }

    private static string Said(LookSettings look) => string.Create(CultureInfo.InvariantCulture,
        $"{(look.Curve == LookCurve.Numbered ? $"the numbered curve {look.CurveExposure:0.##}, {look.CurveWhite:0.##}, {look.CurveLower:0.##}, {look.CurveUpper:0.##}" : $"the engine's curve with its knee at {look.Knee:0.##}")} at an exposure of {look.Exposure:0.##}, saturation {look.Saturation:0.##}, contrast {look.Contrast:0.##}");
}
