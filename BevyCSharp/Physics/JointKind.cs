namespace Bevy.Physics;

/// <summary>Which joint a <see cref="JointBetween"/> makes, as <see cref="Joint"/>'s makers name them.</summary>
public enum JointKind
{
    /// <summary>Free to turn any way about the point, as a shoulder or a pendulum's pivot.</summary>
    Ball,

    /// <summary>Turning only about the axis, as a door or a wheel.</summary>
    Hinge,

    /// <summary>Held as the two bodies are placed, as a part bolted on.</summary>
    Weld,

    /// <summary>The point on the first kept within a range of the second's middle, as a rope or a rod.</summary>
    Distance,

    /// <summary>The second sliding along the axis against the first, as a drawer or a lift.</summary>
    Slider,
}
