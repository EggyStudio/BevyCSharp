namespace Bevy.Physics;

/// <summary>
/// How a joint holds two bodies together. Anchors and axes are in each body's own space, from its
/// center.
/// </summary>
public readonly record struct Joint
{
    internal int Kind { get; private init; }
    internal Vec3 AnchorA { get; private init; }
    internal Vec3 AnchorB { get; private init; }
    internal Vec3 AxisA { get; private init; }
    internal Vec3 AxisB { get; private init; }
    internal float Minimum { get; private init; }
    internal float Maximum { get; private init; }
    internal (float Speed, float Torque)? Motor { get; private init; }
    internal (float Lowest, float Highest)? Limits { get; private init; }

    /// <summary>
    /// A point on one body held to a point on the other, free to turn any way about it: a
    /// shoulder, a pendulum's pivot, a chain's links.
    /// </summary>
    public static Joint Ball(Vec3 anchorA, Vec3 anchorB) => new() { Kind = 0, AnchorA = anchorA, AnchorB = anchorB };

    /// <summary>
    /// The same, and turning only about one axis: a door, a wheel, an elbow. The two axes are the
    /// same line, said in each body's own space.
    /// </summary>
    public static Joint Hinge(Vec3 anchorA, Vec3 axisA, Vec3 anchorB, Vec3 axisB) =>
        new() { Kind = 1, AnchorA = anchorA, AnchorB = anchorB, AxisA = axisA, AxisB = axisB };

    /// <summary>
    /// The two held exactly as they are to each other when joined, as though glued: a sword in a
    /// hand, a part bolted onto a vehicle.
    /// </summary>
    public static Joint Weld() => new() { Kind = 2 };

    /// <summary>
    /// A point on each kept between <paramref name="minimum"/> and <paramref name="maximum"/> apart,
    /// a rope where the minimum is zero and a rod where the two are equal.
    /// </summary>
    public static Joint Distance(Vec3 anchorA, Vec3 anchorB, float minimum, float maximum) =>
        new() { Kind = 3, AnchorA = anchorA, AnchorB = anchorB, Minimum = minimum, Maximum = maximum };

    /// <summary>
    /// A hinge that turns itself, a fan or a wheel driven at a speed, pushing with no more than a
    /// torque.
    /// </summary>
    /// <param name="degreesPerSecond">
    /// How fast the second body turns against the first, about the first's axis, the right-handed
    /// way round, so a positive speed about up turns counterclockwise seen from above.
    /// </param>
    /// <param name="torque">
    /// The most it pushes with, so a motor meeting something heavier stalls rather than flinging it.
    /// </param>
    /// <exception cref="InvalidOperationException">The joint is not a hinge.</exception>
    public Joint WithMotor(float degreesPerSecond, float torque) =>
        Kind == 1
            ? this with { Motor = (degreesPerSecond, Math.Max(0f, torque)) }
            : throw new InvalidOperationException("A motor turns a hinge, which has one axis to turn about.");

    /// <summary>
    /// A hinge that stops at an angle each way, a door that opens to ninety degrees and no further.
    /// </summary>
    /// <param name="lowestDegrees">How far it turns the negative way, as a negative angle or zero.</param>
    /// <param name="highestDegrees">How far it turns the positive way.</param>
    /// <remarks>
    /// Measured from how the two bodies are turned to each other when they are joined, which is
    /// zero, so a door joined closed opens from closed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The joint is not a hinge.</exception>
    /// <exception cref="ArgumentException">The lowest angle is above the highest.</exception>
    public Joint WithLimits(float lowestDegrees, float highestDegrees)
    {
        if (Kind != 1) throw new InvalidOperationException("Limits stop a hinge, which has one angle to limit.");
        if (lowestDegrees > highestDegrees) throw new ArgumentException("The lowest angle is above the highest.", nameof(lowestDegrees));

        return this with { Limits = (lowestDegrees, highestDegrees) };
    }
}
