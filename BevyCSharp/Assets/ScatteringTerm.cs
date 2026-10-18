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

    /// <summary>For a tent falloff, the height it is densest at, from 0 at the ground to 1 where the air ends.</summary>
    /// <remarks>Earth's ozone is densest at 0.75 of its air.</remarks>
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
}
