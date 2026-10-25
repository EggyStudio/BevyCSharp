namespace Bevy;

/// <summary>What weather a <see cref="WeatherState"/>'s forecast brings, how stormy and how cold. See <see cref="WeatherState.Climate"/>.</summary>
public enum WeatherClimate
{
    /// <summary>Fair spells and rainy ones in turn, cold enough in winter for snow now and then.</summary>
    Temperate = 0,

    /// <summary>Often cloudy and wet off a sea, mild all year.</summary>
    Maritime = 1,

    /// <summary>Mostly clear and dry and hot, a rare storm.</summary>
    Arid = 2,

    /// <summary>Warm and humid, the cloud building and the rain coming in heavy bursts.</summary>
    Tropical = 3,

    /// <summary>Cold all year, its storms snow.</summary>
    Polar = 4,
}
