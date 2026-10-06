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
}
