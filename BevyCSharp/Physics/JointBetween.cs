namespace Bevy.Physics;

/// <summary>
/// A joint between the bodies of two entities, held by an entity of its own, so a level hangs a
/// door or a lamp where it stands and a scene file carries it.
/// </summary>
/// <remarks>
/// <para>
/// The entity's place in the world is where the bodies are joined, and the way its up direction
/// points is a hinge's axis, a slider's line and the middle of a ball joint's cone, so the joint is
/// placed and turned in the editor as anything else is. <see cref="PhysicsPlugin"/> makes it once
/// both bodies are made, with the two as they are then, makes it again when the component changes
/// or either body is made again, and takes it away with the component or the entity.
/// <see cref="PhysicsWorld.JointOf"/> answers the joint it made, for a game to drive its motor.
/// </para>
/// <para>
/// Both bodies have to move, dynamic or kinematic, as <see cref="PhysicsWorld.Connect"/> says. A
/// joint that cannot be made, one naming a static body or an entity with no body coming, is written
/// to the log once and not tried again until the component changes.
/// </para>
/// <para>
/// A distance joint keeps the joint's point, fixed to the first body, between
/// <see cref="MinDistance"/> and <see cref="MaxDistance"/> from the second body's middle, and as far
/// as it is when made where <see cref="MaxDistance"/> is zero.
/// </para>
/// </remarks>
[Behavior]
public partial struct JointBetween
{
    /// <summary>Which joint it is.</summary>
    [Tooltip("Placed at this entity, its up direction the axis of a hinge, the line of a slider and the middle of a ball joint's cone.")]
    public JointKind Kind;

    /// <summary>The first body's entity, which a slider slides against and a distance joint's point is fixed to.</summary>
    public Entity A;

    /// <summary>The second body's entity.</summary>
    public Entity B;

    /// <summary>How far a hinge turns the negative way, in degrees from how it was made, none where both limits are zero.</summary>
    [Range(-180, 0), Unit("deg"), ShowIf(nameof(Kind), JointKind.Hinge)]
    public float MinAngle;

    /// <summary>How far a hinge turns the positive way, in degrees from how it was made.</summary>
    [Range(0, 180), Unit("deg"), ShowIf(nameof(Kind), JointKind.Hinge)]
    public float MaxAngle;

    /// <summary>How fast a hinge's motor turns it, about its axis the right-handed way round.</summary>
    [Unit("deg/s"), ShowIf(nameof(Kind), JointKind.Hinge)]
    public float MotorSpeed;

    /// <summary>The most a hinge's motor pushes with, none where it is zero.</summary>
    [Tooltip("Zero leaves the hinge without a motor."), ShowIf(nameof(Kind), JointKind.Hinge)]
    public float MotorTorque;

    /// <summary>How far a ball joint swings from its axis, in degrees, or zero for as far as it likes.</summary>
    [Range(0, 180), Unit("deg"), ShowIf(nameof(Kind), JointKind.Ball)]
    public float Swing;

    /// <summary>How far a ball joint twists about its axis either way, in degrees, or zero for as far as it likes.</summary>
    [Range(0, 180), Unit("deg"), ShowIf(nameof(Kind), JointKind.Ball)]
    public float Twist;

    /// <summary>The nearest a distance joint lets its point come to the second body's middle.</summary>
    [Range(0, 20), ShowIf(nameof(Kind), JointKind.Distance)]
    public float MinDistance;

    /// <summary>The furthest it lets it go, or zero for as far as they are when it is made.</summary>
    [Range(0, 20), Tooltip("Zero is as far apart as they are when the joint is made."), ShowIf(nameof(Kind), JointKind.Distance)]
    public float MaxDistance;

    /// <summary>Where a slider stops going back along its line, from where it was made, none where both are zero.</summary>
    [Range(-10, 0), ShowIf(nameof(Kind), JointKind.Slider)]
    public float MinTravel;

    /// <summary>Where a slider stops going forward along its line.</summary>
    [Range(0, 10), ShowIf(nameof(Kind), JointKind.Slider)]
    public float MaxTravel;

    /// <summary>How fast a slider's drive pushes it along its line, in units a second.</summary>
    [Unit("m/s"), ShowIf(nameof(Kind), JointKind.Slider)]
    public float DriveSpeed;

    /// <summary>The most a slider's drive pushes with, none where it is zero.</summary>
    [Tooltip("Zero leaves the slider without a drive."), ShowIf(nameof(Kind), JointKind.Slider)]
    public float DriveForce;
}
