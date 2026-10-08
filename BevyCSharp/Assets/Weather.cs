using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The weather, a sky around Bevy's atmosphere with clouds, fog, rain, snow and thunder, stars and
/// a moon, which an app asks for with <see cref="Config.Weather"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <c>bevy_weather</c> crate does the work, and its resources and components reflect, so they
/// are reached through the wrappers in <c>Bevy.Reflected</c>. A camera sees the weather once it
/// carries <c>WeatherCameraRef</c> (<c>ecs.Insert&lt;WeatherCameraRef&gt;(camera)</c>). The clock,
/// its latitude and whether it runs are <c>WeatherTimeRef</c>, the master switches and the quality
/// tier <c>WeatherConfigRef</c>, the conditions now and those being eased toward <c>WeatherRef</c>,
/// and the forecast that writes them from the clock <c>ProceduralWeatherRef</c>, each a resource
/// read with <c>ecs.Resource&lt;T&gt;()</c>. A light the app made becomes the weather's sun or
/// moon by carrying <c>SunLightRef</c> or <c>MoonLightRef</c>, which it then steers, and where
/// none does it makes its own.
/// </para>
/// <para>
/// A preset is not a component and does not reflect, so it is set here. The procedural forecast
/// writes the conditions it eases toward each frame while it runs, so a preset holds only with it
/// off (<c>ProceduralWeatherRef.Enabled</c>).
/// </para>
/// </remarks>
public static class Weather
{
    /// <summary>
    /// Whether the weather is running, meaning the bridge has a renderer, the app asked for it
    /// with <see cref="Config.Weather"/>, and meshlets do not run.
    /// </summary>
    public static bool Active => Native.bcs_weather_active() != 0;

    /// <summary>Sets the weather to a preset, eased into over its transition, or at once.</summary>
    /// <param name="preset">The conditions to take on.</param>
    /// <param name="immediately">
    /// Whether to take them on this frame rather than ease into them.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">The value names no preset.</exception>
    /// <exception cref="BevyNativeException">The weather is not running.</exception>
    public static void SetPreset(WeatherPreset preset, bool immediately = false)
    {
        if (!Enum.IsDefined(preset)) throw new ArgumentOutOfRangeException(nameof(preset), preset, "no such preset");

        Native.Check(Native.bcs_weather_set_preset((int)preset, immediately ? 1 : 0), $"setting the weather to {preset}");
    }
}
