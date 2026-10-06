namespace Bevy.Physics;

/// <summary>Settings for the simulation as a whole.</summary>
public sealed class PhysicsSettings
{
    /// <summary>Acceleration every dynamic body feels, in units a second squared.</summary>
    public Vec3 Gravity { get; set; } = new(0f, -9.81f, 0f);

    /// <summary>How much of its speed a body loses a second, from zero to one.</summary>
    public float LinearDamping { get; set; } = 0.03f;

    /// <summary>How much of its spin a body loses a second, from zero to one.</summary>
    public float AngularDamping { get; set; } = 0.03f;

    /// <summary>Friction for a body given no material of its own (<see cref="PhysicsMaterial"/>).</summary>
    public float Friction { get; set; } = 1f;

    /// <summary>
    /// How many solver passes a step makes. More holds a tall stack steadier and costs more.
    /// </summary>
    public int Iterations { get; set; } = 8;

    /// <summary>
    /// How far, in units, an entity can move in one frame or one step and have its kinematic body
    /// carried after it, past which the body is put at the new place, at rest.
    /// </summary>
    /// <remarks>
    /// A kinematic body is moved after its entity by a velocity, so it pushes what it meets on the
    /// way. A move this long is a placing, a door put back or a level begun again, and carried there
    /// it would fling whatever it passed through. A placing shorter than this is said with
    /// <see cref="PhysicsWorld.MarkPlaced"/>. 100 units, as in 3DEngine.
    /// </remarks>
    public float PlaceBeyond { get; set; } = 100f;
}
