namespace Bevy.Physics;

/// <summary>How a body moves.</summary>
public enum BodyKind
{
    /// <summary>Moved by the simulation, so it falls, collides and is pushed.</summary>
    Dynamic,

    /// <summary>
    /// Moved by its <see cref="Transform"/>, as a game moves it, and pushes dynamic bodies out of its
    /// way without being pushed back. A moving platform or a door.
    /// </summary>
    Kinematic,

    /// <summary>Never moves. The ground, a wall.</summary>
    Static,
}
