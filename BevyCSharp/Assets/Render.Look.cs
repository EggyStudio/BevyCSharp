using System.Numerics;

namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Gives a camera a look, which turns its linear picture into the one the screen shows in place
    /// of Bevy's tonemapper, replacing any it had, or with <see langword="null"/> takes it off and
    /// gives the picture back to the tonemapper. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The look runs after Bevy's bloom, effects and the passes before tonemapping have done their
    /// work on the linear picture, and before Bevy's antialiasing, which smooths the picture as the
    /// screen shows it. It needs a camera drawing in high dynamic range
    /// (<see cref="PostSettings.Hdr"/>), since one drawing in eight bits a channel tonemaps in its
    /// materials, and on such a camera Bevy's tonemapper stays and the engine says so once.
    /// </para>
    /// <para>
    /// The camera's tonemapper (<see cref="PostSettings.Tonemapper"/>) is left out while the look is
    /// on and comes back when it goes, and with it the parts Bevy applies as it tonemaps, its grade
    /// (<see cref="SetColorGrading"/>), its dithering and its auto exposure, which do nothing under
    /// a look. The look dithers by itself where <see cref="LookSettings.Dither"/> asks. The setting
    /// lasts while the app runs.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="look">The look, or null to take it off.</param>
    /// <exception cref="ArgumentOutOfRangeException">A setting is not a number.</exception>
    /// <example>
    /// <code>
    /// Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
    /// Render.SetLook(camera, LookSettings.Vivid());
    /// </code>
    /// </example>
    public static void SetLook(Entity camera, LookSettings? look)
    {
        if (look is null)
        {
            Shaders.SetEnginePasses(camera, Looks.Owner);
            Looks.Forget(camera);
            return;
        }

        float[] numbers =
        [
            look.Exposure, look.Knee, look.KneeStrength, look.CurveExposure, look.CurveWhite, look.CurveLower,
            look.CurveUpper, look.Saturation, look.Tint.R, look.Tint.G, look.Tint.B, look.Contrast,
        ];
        if (!numbers.All(float.IsFinite))
            throw new ArgumentOutOfRangeException(nameof(look), "A setting of the look is not a number.");

        // Held where the curves are defined, as 3DEngine holds them, the curve's own numbers off
        // zero so its powers and its white stay finite.
        var pass = Looks.PassFor(camera, look);
        pass.Set("curve", new Vector4(look.Curve == LookCurve.Numbered ? 1f : 0f, Math.Max(0f, look.Exposure),
                Math.Clamp(look.Knee, 0f, 1f), Math.Clamp(look.KneeStrength, 0f, 1f)))
            .Set("numbers", new Vector4(Math.Max(0f, look.CurveExposure), Math.Max(0.01f, look.CurveWhite),
                Math.Max(0.01f, look.CurveLower), Math.Max(0.01f, look.CurveUpper)))
            .Set("grade", new Vector4(Math.Max(0f, look.Contrast), Math.Max(0f, look.Saturation), look.Dither ? 1f : 0f, 0f))
            .Set("tint", new Vector4(Math.Max(0f, look.Tint.R), Math.Max(0f, look.Tint.G), Math.Max(0f, look.Tint.B), 1f));
        Shaders.SetEnginePasses(camera, Looks.Owner, new ShaderPass(pass) { EnginePlace = Looks.Place });
    }
}

/// <summary>The passes <see cref="Render.SetLook"/> puts on cameras, an instance a camera, an app's own.</summary>
internal static class Looks
{
    /// <summary>The name the look's pass is kept under on a camera beside the engine's other parts.</summary>
    internal const string Owner = "look";

    /// <summary>The place of the frame in the tonemapper's place, as the bridge numbers it.</summary>
    internal const int Place = 3;

    private static App? _app;

    // A camera's look is kept once it is taken off, its pass and the settings it last had, so the
    // console turns it on again where it was, with no compile.
    private static readonly Dictionary<ulong, (ShaderInstance Pass, LookSettings Settings, bool On)> Kept = [];

    /// <summary>The look's pass for a camera, made the first time the camera asks in an app, kept with the settings it was given.</summary>
    internal static ShaderInstance PassFor(Entity camera, LookSettings settings)
    {
        ForgetOtherApps();
        var pass = Kept.TryGetValue(camera.Bits, out var look) ? look.Pass : SceneField.Pass("look.slang");
        Kept[camera.Bits] = (pass, settings, true);
        return pass;
    }

    /// <summary>Marks a camera's look taken off, keeping its pass and settings for <see cref="Off"/>.</summary>
    internal static void Forget(Entity camera)
    {
        ForgetOtherApps();
        if (Kept.TryGetValue(camera.Bits, out var look)) Kept[camera.Bits] = look with { On = false };
    }

    /// <summary>The cameras with a look in the running app, with the settings each was given and whether its pass has compiled.</summary>
    internal static IEnumerable<(Entity Camera, LookSettings Settings, bool Ready)> All()
    {
        ForgetOtherApps();
        return Kept.Where(look => look.Value.On)
            .Select(look => (new Entity(look.Key), look.Value.Settings, look.Value.Pass.Program.State == ShaderProgramState.Ready));
    }

    /// <summary>The cameras whose look was taken off in the running app, with the settings each last had.</summary>
    internal static IEnumerable<(Entity Camera, LookSettings Settings)> Off()
    {
        ForgetOtherApps();
        return Kept.Where(look => !look.Value.On).Select(look => (new Entity(look.Key), look.Value.Settings));
    }

    private static void ForgetOtherApps()
    {
        if (ReferenceEquals(_app, App.Running)) return;
        _app = App.Running;
        Kept.Clear();
    }
}
