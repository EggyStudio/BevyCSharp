using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The fog over the map in clear weather, the clear air of the Sky module's Earth, which the
/// weather thickens for rain and fog, thinning upward and worked out in froxels near the camera so
/// the zones' shadows cut a thick fog into shafts and their lamps glow in it.
/// </summary>
/// <remarks>
/// <para>
/// The Sky module gives the Earth's clear air a visibility of 178 km by Koschmieder's law, where a
/// black thing's contrast against the horizon falls to 2%, an optical depth of 3.912. The fog's
/// visibility is where 95% of a point's light is gone, an optical depth of ln 20, so the same air
/// is 136.3 km of it, and a wall 25 m off loses 0.055% of its light to it, as clear air takes it. A
/// fog of a few hundred meters faded the level's far edge before its rooms were walled, and veiled
/// a wall 25 m off by a tenth of its light with sunlit gray, which a scene's courtyard showed as
/// the air of kilometers.
/// </para>
/// <para>
/// The file uses nothing else of the game, since the test suite compiles it in for its test.
/// </para>
/// </remarks>
internal static class MapFog
{
    /// <summary>
    /// The visibility the Sky module gives the Earth's clear air, in meters, by Koschmieder's 2%
    /// contrast.
    /// </summary>
    internal const float ClearAir = 178_000f;

    /// <summary>
    /// Where the fog has taken away 95% of a point's light at its base, in meters, the clear air's
    /// visibility on that measure.
    /// </summary>
    internal static float Visibility => ClearAir * MathF.Log(20f) / 3.912f;

    /// <summary>The fog's settings, its froxels at a quality.</summary>
    internal static SkyFogSettings Of(FroxelQuality quality) =>
        new() { Visibility = Visibility, Height = 40f, Froxels = new FroxelFogSettings { Quality = quality } };
}
