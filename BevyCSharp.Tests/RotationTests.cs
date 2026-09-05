using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers turning points by a rotation and reading a rotation back as three angles.
/// </summary>
/// <remarks>
/// Both exist for the same reason: an inspector shows a rotation as three boxes and a gizmo turns
/// a handle about an axis, and neither can be written without them. Pure maths, so no engine.
/// </remarks>
public sealed class RotationTests
{
    /// <summary>How close two numbers have to be, given a float's worth of trigonometry.</summary>
    private const float Tolerance = 1e-4f;

    [Fact]
    public void ARotationTurnsAPoint()
    {
        var quarter = Quat.FromRotationY(MathF.PI / 2f);
        var turned = quarter * new Vec3(1f, 0f, 0f);

        // A quarter turn about Y takes X onto negative Z, which is the right-handed answer and
        // the one Bevy's own transforms give.
        Assert.Equal(0f, turned.X, Tolerance);
        Assert.Equal(0f, turned.Y, Tolerance);
        Assert.Equal(-1f, turned.Z, Tolerance);
    }

    [Fact]
    public void TurningByNothingLeavesAPointAlone()
    {
        var point = new Vec3(3f, -2f, 0.5f);
        var same = Quat.Identity * point;

        Assert.Equal(point.X, same.X, Tolerance);
        Assert.Equal(point.Y, same.Y, Tolerance);
        Assert.Equal(point.Z, same.Z, Tolerance);
    }

    [Fact]
    public void ThreeAnglesSurviveTheRoundTrip()
    {
        foreach (var angles in new[]
        {
            new Vec3(0.3f, -0.7f, 1.1f),
            new Vec3(-1.2f, 0.2f, 0f),
            new Vec3(0f, 0f, 0f),
            new Vec3(0.9f, 1.4f, -2.2f),
        })
        {
            var rotation = Quat.FromEuler(angles.X, angles.Y, angles.Z);
            var read = rotation.ToEuler();
            var again = Quat.FromEuler(read.X, read.Y, read.Z);

            // The angles themselves may come back differently, because a rotation has more than
            // one decomposition. What has to hold is that they describe the same turn, which is
            // what an inspector's boxes are read and written through.
            var point = new Vec3(0.3f, 0.5f, -0.8f);
            var first = rotation * point;
            var second = again * point;

            Assert.Equal(first.X, second.X, Tolerance);
            Assert.Equal(first.Y, second.Y, Tolerance);
            Assert.Equal(first.Z, second.Z, Tolerance);
        }
    }

    [Fact]
    public void LookingStraightUpStillAnswers()
    {
        // The pole: pitch at a right angle, where the other two angles stop being separable. It
        // has to answer with something usable rather than a not-a-number.
        var up = Quat.FromEuler(MathF.PI / 2f, 0f, 0f);
        var read = up.ToEuler();

        Assert.False(float.IsNaN(read.X) || float.IsNaN(read.Y) || float.IsNaN(read.Z));
        Assert.Equal(MathF.PI / 2f, read.X, 1e-3f);
    }

    [Fact]
    public void TurningOnTheSpotReadsAsOneAngleAllTheWayRound()
    {
        // What an editor shows for a thing spinning where it stands. Y is the outermost angle, so
        // it has the whole circle to itself and the other two stay at nothing: the alternative is
        // that a third of the way round the reading jumps to a half turn on both of its neighbours
        // and walks the middle angle backwards, which is the same rotation and unreadable.
        for (var degrees = -175f; degrees <= 175f; degrees += 5f)
        {
            var radians = degrees * MathF.PI / 180f;
            var read = Quat.FromRotationY(radians).ToEuler();

            Assert.Equal(0f, read.X, 1e-3f);
            Assert.Equal(radians, read.Y, 1e-3f);
            Assert.Equal(0f, read.Z, 1e-3f);
        }
    }
}
