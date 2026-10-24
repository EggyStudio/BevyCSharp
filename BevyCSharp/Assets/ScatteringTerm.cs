namespace Bevy;

/// <summary>
/// One gas or dust of an air of a game's own, which <see cref="AtmosphereSettings.Medium"/> scatters
/// light through in place of the planet's.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's <c>ScatteringTerm</c>. An air is a few of these, each absorbing and scattering its own
/// amounts of red, green and blue light a meter where it is densest, thinning with height in its
/// own way, and scattering more toward some directions than others. Earth's is a gas that scatters
/// blue far more than red, which is the blue overhead, a dust low down that scatters every color
/// forward, which is the glare around the sun, and a layer of ozone that only absorbs.
/// </para>
/// <para>
/// The amounts are per meter, so earth's gas scatters about 0.0000058 red, 0.0000136 green and
/// 0.0000331 blue, and a term whose amounts are thousands of times larger fills the sky with it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // A gas that scatters red more than blue, under a sky the color of rust.
/// Render.SetAtmosphere(camera, new AtmosphereSettings
/// {
///     Medium =
///     [
///         new ScatteringTerm
///         {
///             Scattering = new Vec3(33.1e-6f, 13.6e-6f, 5.8e-6f),
///             Falloff = ScatteringFalloff.Exponential,
///             FalloffScale = 0.13f,
///             Phase = ScatteringPhase.Rayleigh,
///         },
///     ],
/// });
/// </code>
/// </example>
public sealed class ScatteringTerm
{
    /// <summary>How much red, green and blue light it absorbs a meter at its densest.</summary>
    /// <remarks>None by default. Absorbed light darkens the sky toward the colors absorbed least.</remarks>
    public Vec3 Absorption { get; set; }

    /// <summary>How much red, green and blue light it scatters a meter at its densest.</summary>
    /// <remarks>None by default. Scattered light is what colors the sky.</remarks>
    public Vec3 Scattering { get; set; }

    /// <summary>How it thins from the ground to where the air ends, linearly unless set.</summary>
    public ScatteringFalloff Falloff { get; set; } = ScatteringFalloff.Linear;

    /// <summary>
    /// For an exponential falloff, how quickly it thins, as a share of the air's height.
    /// </summary>
    /// <remarks>
    /// Near zero it hugs the ground, and far from zero it thins almost linearly. Earth's gas is
    /// 0.13 of its air and its dust 0.02. A negative scale thins slowly first and steeply after.
    /// </remarks>
    public float FalloffScale { get; set; } = 1f;

    /// <summary>For a tent falloff, the height it is densest at, from 1 at the ground to 0 where the air ends.</summary>
    /// <remarks>
    /// Bevy counts down from the ground, so earth's ozone, densest at 0.75, lies a quarter of the
    /// way up its air of 100 kilometers, 25 kilometers above the ground, as Hillaire has it.
    /// </remarks>
    public float TentCenter { get; set; } = 0.5f;

    /// <summary>For a tent falloff, how wide the layer is, as a share of the air's height.</summary>
    public float TentWidth { get; set; } = 0.5f;

    /// <summary>How it scatters by direction, evenly unless set.</summary>
    public ScatteringPhase Phase { get; set; } = ScatteringPhase.Isotropic;

    /// <summary>
    /// For a dust, how far its scattering leans forward, from just above -1, all back toward the
    /// light, to just below 1, all forward away from it.
    /// </summary>
    /// <remarks>Earth's dust is 0.8, which is the bright glare around the sun.</remarks>
    public float Asymmetry { get; set; } = 0.8f;

    /// <summary>
    /// Earth's air as Bevy describes it, the gas, the dust and the ozone of
    /// <see cref="SkyPlanet.Earth"/>, a new list a game may change before it sets it.
    /// </summary>
    /// <remarks>
    /// The gas's and the ozone's amounts are Bruneton's, as Hillaire's 2020 sky takes them, each
    /// color's taken at one wavelength, 680, 550 and 440 nanometers. A game that bends earth's air
    /// a little starts from this rather than writing the three terms out.
    /// </remarks>
    public static ScatteringTerm[] Earth() =>
    [
        new()
        {
            Scattering = new Vec3(5.802e-6f, 13.558e-6f, 33.1e-6f),
            Falloff = ScatteringFalloff.Exponential,
            FalloffScale = 8f / 60f,
            Phase = ScatteringPhase.Rayleigh,
        },
        new()
        {
            Absorption = new Vec3(3.996e-6f),
            Scattering = new Vec3(0.444e-6f),
            Falloff = ScatteringFalloff.Exponential,
            FalloffScale = 1.2f / 60f,
            Phase = ScatteringPhase.Mie,
            Asymmetry = 0.8f,
        },
        new()
        {
            Absorption = new Vec3(0.65e-6f, 1.881e-6f, 0.085e-6f),
            Falloff = ScatteringFalloff.Tent,
            TentCenter = 0.75f,
            TentWidth = 0.3f,
        },
    ];

    /// <summary>
    /// Earth's air with the gas's and the ozone's amounts weighed over the whole visible spectrum
    /// rather than read at one wavelength a color, as the Atmosphere Rendering Parameter Calculator
    /// works them out, its falloffs and its dust as <see cref="Earth"/>'s.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bruneton's amounts read each of red, green and blue at one wavelength, and an eye sees each
    /// over a band of them. The calculator (ARPC, MIT) takes the gas's scattering at sea level and
    /// the ozone's absorption at its densest at every wavelength the CIE 1931 observer covers and
    /// averages them weighed by the observer taken to sRGB, which gives a gas that scatters red a
    /// little more and blue a little less, 6.605, 12.345 and 29.413 a millionth of a meter where
    /// Bruneton's gives 5.802, 13.558 and 33.1, and an ozone that absorbs red where Bruneton's
    /// absorbs green, 2.291, 1.540 and none where 0.650, 1.881 and 0.085. The sunset band turns from
    /// orange under purple to gold under a gray green, as a spectral reference of Hillaire's sky
    /// does.
    /// </para>
    /// <para>
    /// The amounts are the calculator's for the U.S. Standard Atmosphere of 1976, worked out again
    /// from its data here, and its scale height of 8.70 kilometers and ozone layer are left as
    /// Bevy's, so the two airs differ in the six amounts alone.
    /// </para>
    /// </remarks>
    public static ScatteringTerm[] EarthArc()
    {
        var air = Earth();
        air[0].Scattering = new Vec3(6.605e-6f, 12.345e-6f, 29.413e-6f);
        air[2].Absorption = new Vec3(2.291e-6f, 1.540e-6f, 0f);
        return air;
    }
}
