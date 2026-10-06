using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>A rotation part of the way between two, at an even rate or blended straight.</summary>
public sealed class QuatTests
{
    private static readonly Quat Quarter = Quat.FromRotationY(MathF.PI / 2f);

    [Fact]
    public void SlerpIsEachEndAtZeroAndOneAndHalfTheTurnBetween()
    {
        AssertSame(Quat.Identity, Quat.Slerp(Quat.Identity, Quarter, 0f));
        AssertSame(Quarter, Quat.Slerp(Quat.Identity, Quarter, 1f));
        AssertSame(Quat.FromRotationY(MathF.PI / 4f), Quat.Slerp(Quat.Identity, Quarter, 0.5f));
        AssertSame(Quat.FromRotationY(MathF.PI / 8f), Quat.Slerp(Quat.Identity, Quarter, 0.25f));
    }

    [Fact]
    public void SlerpTakesTheShorterWayRoundWhenTheOtherEndIsWrittenNegated()
    {
        // The same rotation as a quarter turn, written with every component negated.
        var negated = new Quat(-Quarter.X, -Quarter.Y, -Quarter.Z, -Quarter.W);
        AssertSame(Quat.FromRotationY(MathF.PI / 4f), Quat.Slerp(Quat.Identity, negated, 0.5f));
    }

    [Fact]
    public void LerpIsARotationAgainAndHalfwayMatchesSlerpBetweenSymmetricEnds()
    {
        var halfway = Quat.Lerp(Quat.Identity, Quarter, 0.5f);
        Assert.Equal(1f, MathF.Sqrt(halfway.X * halfway.X + halfway.Y * halfway.Y + halfway.Z * halfway.Z + halfway.W * halfway.W), 5);
        AssertSame(Quat.Slerp(Quat.Identity, Quarter, 0.5f), halfway);

        // Nearly the same rotation, where Slerp blends straight rather than divide by the angle.
        var hair = Quat.FromRotationY(0.0001f);
        AssertSame(Quat.Lerp(Quat.Identity, hair, 0.5f), Quat.Slerp(Quat.Identity, hair, 0.5f));
    }

    // The same rotation, whichever sign it is written with.
    private static void AssertSame(Quat expected, Quat actual)
    {
        var sign = expected.X * actual.X + expected.Y * actual.Y + expected.Z * actual.Z + expected.W * actual.W < 0f ? -1f : 1f;
        Assert.Equal(expected.X, actual.X * sign, 4);
        Assert.Equal(expected.Y, actual.Y * sign, 4);
        Assert.Equal(expected.Z, actual.Z * sign, 4);
        Assert.Equal(expected.W, actual.W * sign, 4);
    }
}
