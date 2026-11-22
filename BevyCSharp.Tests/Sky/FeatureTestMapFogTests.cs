using Bevy;
using BevyCSharp.FeatureTest.Behaviors;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The feature test's fog over its map in clear weather (<see cref="MapFog"/>, one file of the game
/// compiled in for this test), held to the clear air of the Atmosphere module's Earth.
/// </summary>
public sealed class FeatureTestMapFogTests
{
    /// <summary>
    /// A wall 25 m off loses under a tenth of a percent of its light to the map's fog in clear
    /// weather, which leaves the fog at its base visibility, as the clear air takes 0.055% of it,
    /// where a fog of 700 m took a tenth with sunlit gray.
    /// </summary>
    [Fact]
    public void AWall25MetersOffLosesUnderATenthOfAPercentOfItsLightToTheMapsFogInClearWeather()
    {
        var fog = MapFog.Of(FroxelQuality.Low);

        // The fog's visibility is where 95% of a point's light is gone, an optical depth of ln 20.
        var hidden = 1.0 - Math.Exp(-Math.Log(20) * 25 / fog.Visibility);
        Assert.True(hidden < 0.001, $"the map's fog of {fog.Visibility:0} m hides {hidden:P3} of a wall 25 m off");
    }
}
