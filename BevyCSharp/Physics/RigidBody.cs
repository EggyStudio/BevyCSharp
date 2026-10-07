namespace Bevy.Physics;

/// <summary>
/// Makes an entity a body of the simulation, with a <see cref="Collider"/> beside it saying its
/// shape.
/// </summary>
/// <remarks>
/// <para>
/// A component, so a level built in the editor holds its bodies the way it holds its lights, and a
/// scene file carries them. <see cref="PhysicsPlugin"/> makes the body once an entity has both
/// components, makes it again when either of them changes, or a static body's transform does, and
/// takes it away with them or with the entity, so a game neither adds nor removes the bodies a
/// level holds.
/// </para>
/// <para>
/// A body a game adds with <see cref="PhysicsWorld.Add"/> on an entity carrying these is left
/// alone, since one made in code is the game's to keep.
/// </para>
/// </remarks>
[Behavior]
public partial struct RigidBody
{
    /// <summary>How it moves.</summary>
    [Tooltip("Dynamic falls and is pushed, kinematic follows its transform, static never moves.")]
    public BodyKind Kind;

    /// <summary>Its mass, for a dynamic body, or zero for one.</summary>
    [Range(0, 1000), ShowIf(nameof(Kind), BodyKind.Dynamic)]
    public float Mass;

    /// <summary>Whether it only reports what touches it and stops nothing, as a trigger does.</summary>
    [Tooltip("Reports what touches it in ContactStarted and ContactEnded, and stops nothing.")]
    public bool Sensor;

    /// <summary>
    /// Which of the 32 layers its body is on, 0 unless set, which decides what it collides with, as
    /// <see cref="PhysicsWorld.SetLayersCollide"/> says.
    /// </summary>
    [Range(0, 31), Tooltip("Bodies on layers that do not collide pass through each other and report nothing.")]
    public int Layer;

    /// <summary>
    /// Whether a dynamic body is swept over each step, as a ball struck hard is, so it meets a thin
    /// wall it would otherwise cross within a step. <see cref="PhysicsWorld.SetContinuous"/> says what
    /// it costs and how fast it holds.
    /// </summary>
    [Tooltip("Sweeps a fast body over each step, so it does not cross a thin wall within one."), ShowIf(nameof(Kind), BodyKind.Dynamic)]
    public bool Continuous;

    /// <summary>How much it grips what it slides on, or zero for the settings' own.</summary>
    [Range(0, 2), Foldout("Surface", Open = false)]
    public float Friction;

    /// <summary>How much of its speed it keeps bouncing off something, from none to all of it.</summary>
    [Range(0, 1), Foldout("Surface")]
    public float Bounce;
}
