using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Bevy's easing functions, held to bevy_math's own tests of them, each family's start, end, middle
/// and the way its two halves make its in-and-out.
/// </summary>
public sealed class EaseFunctionTests
{
    // bevy_math's families that rise without turning back, each its in, its out and its in-and-out.
    private static readonly EaseFunction[][] Monotonic =
    [
        [EaseFunction.QuadraticIn, EaseFunction.QuadraticOut, EaseFunction.QuadraticInOut],
        [EaseFunction.CubicIn, EaseFunction.CubicOut, EaseFunction.CubicInOut],
        [EaseFunction.QuarticIn, EaseFunction.QuarticOut, EaseFunction.QuarticInOut],
        [EaseFunction.QuinticIn, EaseFunction.QuinticOut, EaseFunction.QuinticInOut],
        [EaseFunction.SmoothStepIn, EaseFunction.SmoothStepOut, EaseFunction.SmoothStep],
        [EaseFunction.SmootherStepIn, EaseFunction.SmootherStepOut, EaseFunction.SmootherStep],
        [EaseFunction.SineIn, EaseFunction.SineOut, EaseFunction.SineInOut],
        [EaseFunction.CircularIn, EaseFunction.CircularOut, EaseFunction.CircularInOut],
        [EaseFunction.ExponentialIn, EaseFunction.ExponentialOut, EaseFunction.ExponentialInOut],
    ];

    // bevy_math's, the same error allowed at both ends, since a start of 2e-28 is as good as zero.
    private const float Tolerance = 1e-6f;

    [Fact]
    public void EachFamilyStartsAtZeroAndEndsAtOne()
    {
        foreach (var function in Monotonic.SelectMany(family => family))
        {
            Assert.InRange(function.Sample(0f), 0f, Tolerance);
            Assert.InRange(function.Sample(1f), 1f - Tolerance, 1f);
        }
    }

    [Fact]
    public void AnInAndOutIsItsInOverTheFirstHalfAndItsOutOverTheSecond()
    {
        foreach (var family in Monotonic)
        {
            var (ease, easeOut, both) = (family[0], family[1], family[2]);
            foreach (var x in new[] { 0.1f, 0.2f, 0.3f, 0.4f })
            {
                var y = both.Sample(x);
                Assert.True(y < x, $"{both}({x}) was {y}");
                Assert.InRange(ease.Sample(2f * x) / 2f, y - Tolerance, y + Tolerance);
            }

            foreach (var x in new[] { 0.6f, 0.7f, 0.8f, 0.9f })
            {
                var y = both.Sample(x);
                Assert.True(y > x, $"{both}({x}) was {y}");
                Assert.InRange(easeOut.Sample(2f * x - 1f) / 2f + 0.5f, y - Tolerance, y + Tolerance);
            }
        }
    }

    [Fact]
    public void AnInIsBelowHalfwayAtHalfTimeAnOutAboveAndAnInAndOutOnIt()
    {
        foreach (var family in Monotonic)
        {
            Assert.True(family[0].Sample(0.5f) < 0.5f - Tolerance, $"{family[0]}(1/2) was {family[0].Sample(0.5f)}");
            Assert.True(family[1].Sample(0.5f) > 0.5f + Tolerance, $"{family[1]}(1/2) was {family[1].Sample(0.5f)}");
            Assert.InRange(family[2].Sample(0.5f), 0.5f - Tolerance, 0.5f + Tolerance);
        }
    }

    [Theory]
    [InlineData(JumpAt.Start, 4, new[] { 0f, 0.249f, 0.25f, 0.499f, 0.5f, 0.749f, 0.75f, 1f }, new[] { 0.25f, 0.25f, 0.5f, 0.5f, 0.75f, 0.75f, 1f, 1f })]
    [InlineData(JumpAt.End, 4, new[] { 0f, 0.249f, 0.25f, 0.499f, 0.5f, 0.749f, 0.75f, 0.999f, 1f }, new[] { 0f, 0f, 0.25f, 0.25f, 0.5f, 0.5f, 0.75f, 0.75f, 1f })]
    [InlineData(JumpAt.None, 5, new[] { 0f, 0.199f, 0.2f, 0.399f, 0.4f, 0.599f, 0.6f, 0.799f, 0.8f, 0.999f, 1f }, new[] { 0f, 0f, 0.25f, 0.25f, 0.5f, 0.5f, 0.75f, 0.75f, 1f, 1f, 1f })]
    [InlineData(JumpAt.Both, 4, new[] { 0f, 0.249f, 0.25f, 0.499f, 0.5f, 0.749f, 0.75f, 0.999f, 1f }, new[] { 0.2f, 0.2f, 0.4f, 0.4f, 0.6f, 0.6f, 0.8f, 0.8f, 1f })]
    public void AStaircaseRisesWhereItsJumpSays(JumpAt jump, int steps, float[] at, float[] expected)
    {
        var function = EaseFunction.Steps(steps, jump);
        for (var i = 0; i < at.Length; i++) Assert.Equal(expected[i], function.Sample(at[i]), 1e-6f);
    }

    [Fact]
    public void AProgressOutsideZeroToOneIsTakenAsTheNearerEnd()
    {
        Assert.Equal(0f, EaseFunction.CubicIn.Sample(-1f));
        Assert.Equal(1f, EaseFunction.CubicIn.Sample(2f));
        Assert.Equal(1f, EaseFunction.Elastic(50f).Sample(1.5f), 1e-6f);
    }

    [Fact]
    public void TheFamiliesThatOvershootDoSoOnTheWayAndEndWhereTheOthersDo()
    {
        Assert.True(EaseFunction.BackIn.Sample(0.2f) < 0f);
        Assert.True(EaseFunction.BackOut.Sample(0.8f) > 1f);
        Assert.True(EaseFunction.ElasticOut.Sample(0.2f) > 1f);
        // ElasticInOut starts and ends a ten-thousandth off, its sine not quite at zero at either
        // end, as Bevy's is.
        foreach (var function in new[] { EaseFunction.BackInOut, EaseFunction.ElasticInOut, EaseFunction.BounceInOut, EaseFunction.Elastic(50f) })
        {
            Assert.Equal(0f, function.Sample(0f), 1e-3f);
            Assert.Equal(1f, function.Sample(1f), 1e-3f);
        }
    }

    [Fact]
    public void AFunctionPrintsAsBevysDebugFormDoes()
    {
        Assert.Equal("SineIn", EaseFunction.SineIn.ToString());
        Assert.Equal("Steps(4, End)", EaseFunction.Steps(4, JumpAt.End).ToString());
        Assert.Equal("Elastic(50.0)", EaseFunction.Elastic(50f).ToString());
        Assert.Equal("Elastic(2.5)", EaseFunction.Elastic(2.5f).ToString());
    }

    [Fact]
    public void AFieldLeftAloneIsLinearAndAStaircaseLeftAloneJumpsAtItsEnd()
    {
        Assert.Equal(EaseFunction.Linear, default);
        Assert.Equal(JumpAt.End, default(JumpAt));
    }

    [Fact]
    public void TwoOfAKindAreEqualWhateverNumbersTheKindDoesNotRead()
    {
        Assert.Equal(EaseFunction.SineIn, EaseFunction.SineIn with { StepCount = 3 });
        Assert.NotEqual(EaseFunction.Steps(4, JumpAt.End), EaseFunction.Steps(4, JumpAt.Start));
        Assert.NotEqual(EaseFunction.Elastic(50f), EaseFunction.Elastic(40f));
        Assert.Equal(EaseFunction.Steps(4, JumpAt.End).GetHashCode(), EaseFunction.Steps(4, JumpAt.End).GetHashCode());
    }
}
