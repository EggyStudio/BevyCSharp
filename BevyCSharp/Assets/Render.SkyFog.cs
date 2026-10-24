using System.Numerics;

namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Draws fog over a camera's picture in the color the sky has at the horizon along each way,
    /// replacing any it had, so distant geometry fades into the sky behind it and the sky below the
    /// horizon takes the horizon's color. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The fog is drawn over the lit picture after Bevy's sky and before its antialiasing, bloom and
    /// effects, by a draw of the library's own that reads the camera's depth, which the camera is
    /// given for it. That needs a camera drawn once a pixel (<see cref="PostSettings.Msaa"/> of
    /// one), and a multisampled camera draws no fog. Its color
    /// along each way is the sky's a few degrees above the horizon there, read from the camera's
    /// environment map, which an atmosphere lights each frame where <see cref="SetSkyLighting"/>
    /// asked, so toward the sun it is warm and away from it blue, as the sky is, and it follows the
    /// hour. The settings' <see cref="SkyFogSettings.Color"/> stands in where the camera has no
    /// environment map.
    /// </para>
    /// <para>
    /// Games lay this over a physical sky by art direction, since an atmosphere's own haze is too
    /// faint at a few hundred meters to blend a level's far ground into the horizon. Below the horizon, where the camera draws nothing, an atmosphere draws the
    /// planet's ground as an even gray, and the fog draws the horizon's color there instead,
    /// darkening downward. The setting lasts while the app runs, and
    /// <see cref="ClearSkyFog"/> takes it off.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="settings">How thick the fog is, where, and how the band below the horizon darkens.</param>
    /// <exception cref="ArgumentNullException"><paramref name="settings"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A setting is not a number, or the visibility or the height is not above none.</exception>
    /// <example>
    /// <code>
    /// Render.SetAtmosphere(camera, new AtmosphereSettings { Medium = ScatteringTerm.EarthArc() });
    /// Render.SetSkyLighting(camera);
    /// Render.SetSkyFog(camera, new SkyFogSettings { Visibility = 800f, Height = 60f });
    /// </code>
    /// </example>
    public static void SetSkyFog(Entity camera, SkyFogSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!float.IsFinite(settings.Visibility) || settings.Visibility <= 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), settings.Visibility, "The visibility is how far the fog takes away 95% of the light, above none.");
        if (!float.IsFinite(settings.Height) || settings.Height <= 0f)
            throw new ArgumentOutOfRangeException(nameof(settings), settings.Height, "The height is how far up the fog's density falls by e, above none.");
        if (!float.IsFinite(settings.Start) || !float.IsFinite(settings.Base) || !float.IsFinite(settings.HorizonDegrees)
            || !float.IsFinite(settings.GroundShade) || !float.IsFinite(settings.BandDegrees) || !float.IsFinite(settings.Brightness))
            throw new ArgumentOutOfRangeException(nameof(settings), "A setting of the fog is not a number.");

        var draw = SkyFog.DrawFor(camera, settings);
        var (r, g, b) = settings.Color ?? (1f, 1f, 1f);
        draw.Set("fog", new Vector4(settings.Visibility, Math.Max(0f, settings.Start), settings.Height, settings.Base))
            .Set("horizon", new Vector4(Math.Clamp(settings.HorizonDegrees, 0f, 89f), Math.Clamp(settings.GroundShade, 0f, 1f),
                Math.Clamp(settings.BandDegrees, 0f, 90f), settings.Color is null ? 0f : 1f))
            .Set("color", new Vector4(r * settings.Brightness, g * settings.Brightness, b * settings.Brightness, 1f));
        Shaders.SetEnginePrepass(camera, SkyFog.Owner, 1u);
        Shaders.SetEngineViewDraws(camera, SkyFog.Owner, ViewDraw.Fixed(draw, FramePoint.BeforeTonemapping, 3, 1, DrawBlend.Alpha, writesDepth: false));
    }

    /// <summary>Takes <see cref="SetSkyFog"/>'s fog off a camera. Only valid inside a system.</summary>
    /// <param name="camera">The camera.</param>
    public static void ClearSkyFog(Entity camera)
    {
        Shaders.SetEngineViewDraws(camera, SkyFog.Owner);
        Shaders.SetEnginePrepass(camera, SkyFog.Owner, 0);
        SkyFog.Forget(camera);
    }
}

/// <summary>The draws <see cref="Render.SetSkyFog"/> puts on cameras, an instance a camera, an app's own.</summary>
internal static class SkyFog
{
    /// <summary>The name the fog's draw and its prepass are kept under on a camera beside the engine's other parts.</summary>
    internal const string Owner = "sky.fog";

    private static App? _app;

    // A camera's fog is kept once it is taken off, its draw and the settings it last had, so the
    // console turns it on again where it was, with no compile.
    private static readonly Dictionary<ulong, (ShaderInstance Draw, SkyFogSettings Settings, bool On)> Fogs = [];

    /// <summary>The fog's draw for a camera, made the first time the camera asks in an app, kept with the settings it was given.</summary>
    internal static ShaderInstance DrawFor(Entity camera, SkyFogSettings settings)
    {
        ForgetOtherApps();
        var draw = Fogs.TryGetValue(camera.Bits, out var fog) ? fog.Draw : SceneField.Draw("sky_fog.slang");
        Fogs[camera.Bits] = (draw, settings, true);
        return draw;
    }

    /// <summary>Marks a camera's fog taken off, keeping its draw and settings for <see cref="Off"/>.</summary>
    internal static void Forget(Entity camera)
    {
        ForgetOtherApps();
        if (Fogs.TryGetValue(camera.Bits, out var fog)) Fogs[camera.Bits] = fog with { On = false };
    }

    /// <summary>The cameras drawing fog in the running app, with the settings each was given and whether its draw has compiled.</summary>
    internal static IEnumerable<(Entity Camera, SkyFogSettings Settings, bool Ready)> All()
    {
        ForgetOtherApps();
        return Fogs.Where(fog => fog.Value.On)
            .Select(fog => (new Entity(fog.Key), fog.Value.Settings, fog.Value.Draw.Program.State == ShaderProgramState.Ready));
    }

    /// <summary>The cameras whose fog was taken off in the running app, with the settings each last had.</summary>
    internal static IEnumerable<(Entity Camera, SkyFogSettings Settings)> Off()
    {
        ForgetOtherApps();
        return Fogs.Where(fog => !fog.Value.On).Select(fog => (new Entity(fog.Key), fog.Value.Settings));
    }

    private static void ForgetOtherApps()
    {
        if (ReferenceEquals(_app, App.Running)) return;
        _app = App.Running;
        Fogs.Clear();
    }
}
