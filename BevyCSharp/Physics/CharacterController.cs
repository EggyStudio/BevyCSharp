namespace Bevy.Physics;

/// <summary>
/// Makes a dynamic body a character, walked at the velocity a game asks for rather than pushed
/// about, which walls stop and slopes hold.
/// </summary>
/// <remarks>
/// <para>
/// Beside a dynamic <see cref="RigidBody"/> and its <see cref="Collider"/>, a capsule standing
/// along Y being the shape a character usually has. Before every step the body is walked toward
/// <see cref="Move"/> along the ground it stands on. It slides along a wall it meets rather than
/// sticking to it, since a character's contacts have no friction, rides over an edge lower than
/// about half its radius on the round of its foot, climbs a step up to <see cref="StepHeight"/>,
/// and stands still on a slope up to <see cref="MaxSlope"/> where it would slide off a steeper
/// one. It never turns over or falls asleep, and it pushes what is lighter, being a body like any
/// other.
/// </para>
/// <para>
/// The body stays upright whatever the entity's rotation, and the step writes back only where the
/// entity is, so a game turns its character to face the way it walks by setting the rotation,
/// which no step undoes.
/// </para>
/// <para>
/// A game writes <see cref="Move"/>, <see cref="Jump"/> and <see cref="Height"/>, and reads
/// <see cref="Grounded"/> and <see cref="GroundNormal"/>, which each step writes back. Asked of a
/// body that is not dynamic, none of it does anything.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Behavior]
/// public partial struct Player
/// {
///     [OnUpdate]
///     public void Walk(BehaviorContext ctx, ref CharacterController character)
///     {
///         var x = (ctx.Input.KeyDown(Key.D) ? 1f : 0f) - (ctx.Input.KeyDown(Key.A) ? 1f : 0f);
///         character.Move = new Vec3(x * 4f, 0f, 0f);
///         if (character.Grounded &amp;&amp; ctx.Input.KeyPressed(Key.Space)) character.Jump = 5f;
///     }
/// }
/// </code>
/// </example>
[Behavior]
public partial struct CharacterController
{
    /// <summary>
    /// The velocity it walks at, in units a second, followed along the ground it stands on. The
    /// vertical part is ignored, since gravity and the ground decide how it falls.
    /// </summary>
    [Tooltip("How fast and which way it walks, followed along the ground. Up and down are left to gravity.")]
    public Vec3 Move;

    /// <summary>
    /// How fast it leaves the ground upward at the next step, if it stands on any then. Set back to
    /// zero by that step whether it jumped or not, so a jump asked for in the air is not saved up
    /// for the landing.
    /// </summary>
    [Unit("m/s")]
    public float Jump;

    /// <summary>The steepest ground it stands and walks on, in degrees, or zero for 45.</summary>
    [Range(0, 89), Unit("deg"), Tooltip("Steeper ground than this it slides off. Zero is 45 degrees.")]
    public float MaxSlope;

    /// <summary>The highest step it climbs onto walking into it, or zero for its radius.</summary>
    [Range(0, 2), Tooltip("The highest ledge it steps up onto. Zero is its radius.")]
    public float StepHeight;

    /// <summary>
    /// How tall it stands, with its feet where they are, as crouching and standing set it, or zero
    /// for the height its collider made it. A height it has no room for overhead is taken once there
    /// is, so a character crouched under a ledge stands as it walks out. It is no shorter than it is
    /// wide, and a ball made a character keeps its size.
    /// </summary>
    [Range(0, 3), Tooltip("How tall it stands, lower to crouch. Zero is its collider's height.")]
    public float Height;

    /// <summary>
    /// Whether it flies, at <see cref="Move"/> up and down as well as along, with gravity held off,
    /// where walls, floors and ceilings stop it still.
    /// </summary>
    /// <remarks>
    /// A game's creative mode, or a ghost that does not pass through the level. It hovers where
    /// asked for nothing, a jump does nothing, and <see cref="Grounded"/> still says whether it is
    /// over ground close enough to stand on, which a game reads to land it.
    /// </remarks>
    [Tooltip("Flies at Move, up and down included, with gravity held off. Walls still stop it.")]
    public bool Fly;

    /// <summary>Whether it stood on ground no steeper than <see cref="MaxSlope"/> at the last step.</summary>
    [ReadOnly]
    public bool Grounded;

    /// <summary>Which way the ground under it faces, or straight up where it stands on nothing.</summary>
    [ReadOnly]
    public Vec3 GroundNormal;
}
