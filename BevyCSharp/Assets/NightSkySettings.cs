namespace Bevy;

/// <summary>
/// The sky behind the atmosphere <see cref="Render.SetNightSky"/> draws, the stars, the galaxy and
/// the moon, standing where <see cref="Clock"/> puts them.
/// </summary>
/// <remarks>
/// <para>
/// Each is dimmed by as much of its light as the camera's atmosphere lets through and added over
/// the sky, so near the horizon they fade and redden. The stars and the galaxy come out as the sun
/// goes down, whole once it is some nine degrees under the horizon and gone by three over it, as
/// the day's sky outshines them, and the moon shows pale by day as well. Their brightness is in
/// nits, which the camera's exposure takes as it takes the scene, so a night exposed brighter shows
/// more of them.
/// </para>
/// <para>
/// The moon's disc is drawn here, lit from the sun's direction, so its phase follows the clock. A
/// directional light standing for the moon would draw a second disc of Bevy's own, which
/// <see cref="Render.SetSunDisk"/> with <see cref="SunDisk.Off"/> takes away.
/// </para>
/// </remarks>
public sealed class NightSkySettings
{
    /// <summary>The clock the sky stands by, which the program moves on and the sky reads every frame.</summary>
    public SkyClock Clock { get; set; } = new();

    /// <summary>The brightest star's light, in nits, 600 unless set, most a small share of it.</summary>
    public float Stars { get; set; } = 600f;

    /// <summary>How many cells the stars' grid has along each face of the cube about the sky, a star in one cell of five, 160 unless set.</summary>
    public float StarGrid { get; set; } = 160f;

    /// <summary>How much the stars twinkle near the horizon, from none at 0, 0.35 unless set.</summary>
    public float Twinkle { get; set; } = 0.35f;

    /// <summary>The galaxy's light where its band is brightest, in nits, 25 unless set.</summary>
    public float Galaxy { get; set; } = 25f;

    /// <summary>The full moon's light, in nits, 4,000 unless set.</summary>
    public float Moon { get; set; } = 4000f;

    /// <summary>How wide the moon looks, in degrees, 0.7 unless set, a little larger than the real one's half a degree.</summary>
    public float MoonSize { get; set; } = 0.7f;

    /// <summary>The share of the full moon's light the earth sends to its dark part, 0.012 unless set.</summary>
    public float Earthshine { get; set; } = 0.012f;
}
