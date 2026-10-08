namespace Bevy;

/// <summary>
/// A set of conditions the weather eases into (<see cref="Weather.SetPreset"/>), in the order the
/// <c>bevy_weather</c> crate declares them.
/// </summary>
public enum WeatherPreset
{
    /// <summary>Cloudless, dry, a light breeze.</summary>
    Clear,

    /// <summary>A few fair-weather cumulus.</summary>
    FewClouds,

    /// <summary>Scattered cumulus over about half the sky.</summary>
    PartlyCloudy,

    /// <summary>A solid gray deck of stratus, with no rain.</summary>
    Overcast,

    /// <summary>Thin high cirrus over a mostly blue sky.</summary>
    Hazy,

    /// <summary>A light, steady drizzle.</summary>
    Drizzle,

    /// <summary>Rain under a thick deck.</summary>
    Rain,

    /// <summary>Heavy rain, a strong wind and a low cloud base.</summary>
    Storm,

    /// <summary>A storm with frequent lightning and towering cloud.</summary>
    Thunderstorm,

    /// <summary>A gentle snowfall in still air.</summary>
    LightSnow,

    /// <summary>Steady snow under an overcast sky.</summary>
    Snow,

    /// <summary>Heavy snow in a gale, with next to nothing to be seen.</summary>
    Blizzard,

    /// <summary>Dense fog on the ground under a flat sky.</summary>
    Fog,

    /// <summary>A low mist at dawn, clearing above.</summary>
    MistyMorning,

    /// <summary>A dry, dusty haze in a hot wind.</summary>
    Sandstorm,
}
