using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using BepuShapes = BepuPhysics.Collidables;

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

/// <summary>What a body collides as, in world units, centered on its entity.</summary>
/// <remarks>
/// The shape is not scaled by the entity's <see cref="Transform.Scale"/>, since a collision shape
/// is sized for what it collides as rather than for how the mesh drawn over it was modeled.
/// </remarks>
public readonly record struct PhysicsShape
{
    internal int Kind { get; private init; }
    internal Vec3 Size { get; private init; }
    internal Vec3[]? Positions { get; private init; }
    internal uint[]? Indices { get; private init; }

    /// <summary>A box of the given full size along each axis.</summary>
    public static PhysicsShape Box(Vec3 size) => new() { Kind = 0, Size = size };

    /// <summary>A sphere of the given radius.</summary>
    public static PhysicsShape Sphere(float radius) => new() { Kind = 1, Size = new Vec3(radius, 0f, 0f) };

    /// <summary>
    /// A capsule standing along Y: a cylinder of <paramref name="length"/> with a half sphere of
    /// <paramref name="radius"/> at each end, the shape a character usually collides as.
    /// </summary>
    public static PhysicsShape Capsule(float radius, float length) => new() { Kind = 2, Size = new Vec3(radius, length, 0f) };

    /// <summary>A cylinder standing along Y.</summary>
    public static PhysicsShape Cylinder(float radius, float length) => new() { Kind = 3, Size = new Vec3(radius, length, 0f) };

    /// <summary>
    /// Triangles, as a level's floors and walls are, from positions and three indices a triangle.
    /// </summary>
    /// <remarks>
    /// For a static or kinematic body. A triangle collides from its front, the side its corners
    /// wind counterclockwise around as Bevy draws it, so a floor is solid from above and something
    /// below it passes up through. A dynamic mesh is allowed and costly, and its mass is spread as
    /// though the mesh were closed.
    /// </remarks>
    public static PhysicsShape Mesh(ReadOnlySpan<Vec3> positions, ReadOnlySpan<uint> indices)
    {
        if (indices.Length < 3 || indices.Length % 3 != 0)
            throw new ArgumentException("A mesh shape needs three indices a triangle, and at least one triangle.", nameof(indices));

        return new() { Kind = 4, Positions = positions.ToArray(), Indices = indices.ToArray() };
    }

    /// <summary>The same, from a mesh read back with <see cref="Render.TryReadMesh"/>.</summary>
    public static PhysicsShape Mesh(MeshData mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var order = mesh.Indices ?? Enumerable.Range(0, mesh.Positions.Length).Select(index => (uint)index).ToArray();
        return Mesh(mesh.Positions, order);
    }

    /// <summary>
    /// The smallest convex shape holding every point, as a rock or an odd crate tumbles.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a dynamic body whose shape is none of the simple ones, where a mesh shape would be
    /// slow to collide and wrong to tumble, since a mesh is a surface and a hull is a solid. The
    /// points are in the entity's own space, from its origin, and need not be on the hull, so a
    /// model's vertices serve as they are. Bepu builds the hull around its own center, and the
    /// body turns about that center while the entity keeps its origin, so the transform written
    /// back is where the entity's origin went. A joint's anchors on such a body are from the
    /// hull's center.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">Fewer than four points, which hold no volume.</exception>
    public static PhysicsShape Hull(ReadOnlySpan<Vec3> points)
    {
        if (points.Length < 4)
            throw new ArgumentException("A hull needs at least four points, which is the fewest that hold a volume.", nameof(points));

        return new() { Kind = 5, Positions = points.ToArray() };
    }

    /// <summary>The same, around the vertices of a mesh read back with <see cref="Render.TryReadMesh"/>.</summary>
    public static PhysicsShape Hull(MeshData mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        return Hull(mesh.Positions);
    }
}

/// <summary>How a body's surface behaves where it touches another.</summary>
/// <param name="Friction">
/// How hard it is to slide, from zero for ice upward. Two bodies touching slide against each other
/// with the geometric mean of their frictions, so ice on rubber is still slippery.
/// </param>
/// <param name="Bounce">
/// How much of its speed into a surface it keeps coming back out, from zero, which lands and stays,
/// to one, which bounces about as high as it fell. Two bodies touching bounce as the bouncier does.
/// </param>
/// <remarks>
/// Bepu has no restitution coefficient, and its contacts are springs integrated stiffly enough to
/// stay stable, which takes nearly all of a bounce's speed out at a game's step rate. So a bounce
/// is given after the step instead. A bouncy body that met a surface moving into it faster than a
/// fifth of a unit a second leaves it at <paramref name="Bounce"/> times that speed, along the
/// surface's normal, keeping the speed it has along the surface. Measured against the surface as
/// though it held still, which is right for a floor or a wall and close for anything slower than
/// the body.
/// </remarks>
public readonly record struct PhysicsMaterial(float Friction = 1f, float Bounce = 0f);

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

/// <summary>A joint between two bodies, from <see cref="PhysicsWorld.Connect"/>.</summary>
/// <param name="Id">What the joint is known by.</param>
public readonly record struct JointHandle(int Id);

/// <summary>Two bodies started touching.</summary>
/// <remarks>
/// Sent on the message bus the step they first touch, once for the pair, whichever of them moved.
/// A sensor sends this for what enters it without pushing it, which is how a trigger volume works.
/// </remarks>
/// <param name="A">One of the two entities.</param>
/// <param name="B">The other.</param>
public readonly record struct ContactStarted(Entity A, Entity B);

/// <summary>Two bodies that were touching stopped, or one of them was removed.</summary>
/// <param name="A">One of the two entities.</param>
/// <param name="B">The other.</param>
public readonly record struct ContactEnded(Entity A, Entity B);

/// <summary>Where a ray met a body.</summary>
/// <param name="Entity">The entity whose body it met.</param>
/// <param name="Point">Where, in world space.</param>
/// <param name="Normal">Which way the surface faces there.</param>
/// <param name="Distance">How far along the ray, in world units.</param>
public readonly record struct PhysicsHit(Entity Entity, Vec3 Point, Vec3 Normal, float Distance);

/// <summary>
/// Rigid bodies for the entities given one, simulated by BepuPhysics and written back to each
/// entity's <see cref="Transform"/> every fixed step.
/// </summary>
/// <remarks>
/// <para>
/// A resource, put in the world by <see cref="PhysicsPlugin"/> and reached from a behavior as
/// <c>ctx.Res&lt;PhysicsWorld&gt;()</c>. The simulation runs on the managed side, so nothing new
/// crosses to the engine. A body's pose reaches Bevy as the <see cref="Transform"/> write any
/// system makes, and propagation and the renderer take it from there.
/// </para>
/// <para>
/// It steps once per <see cref="Stage.FixedUpdate"/>, so Bevy's fixed timestep does the
/// accumulating and a slow frame is caught up in whole steps, which keeps a simulation the same on
/// every machine. A body starts where its entity's transform is when it is added. A dynamic body
/// is written back each step; a kinematic one is read each step, and its velocity worked out from
/// how far it moved, so what it pushes is pushed at the speed it moved.
/// </para>
/// <para>
/// A body belongs to its entity. Despawning the entity removes the body on the next step, and
/// <see cref="Remove"/> takes it off without despawning anything. No Bepu type appears here, so
/// the engine underneath can be replaced without a game changing.
/// </para>
/// </remarks>
public sealed class PhysicsWorld : IDisposable
{
    private readonly BufferPool _pool = new();
    private readonly ThreadDispatcher _threads;
    private readonly Simulation _simulation;

    /// <summary>
    /// A body, whichever kind of handle Bepu gave it, and where its shape's center is from the
    /// entity's origin, which is nothing but for a hull.
    /// </summary>
    private readonly record struct Body(BodyKind Kind, BodyHandle Moving, StaticHandle Fixed, TypedIndex Shape, Vector3 Center = default);

    private readonly Dictionary<Entity, Body> _bodies = [];
    private readonly Dictionary<BodyHandle, Entity> _byBody = [];
    private readonly Dictionary<StaticHandle, Entity> _byStatic = [];

    /// <summary>Where each kinematic body was, for its velocity from how far it moved.</summary>
    private readonly Dictionary<Entity, Vec3> _kinematicWas = [];

    /// <summary>What the narrow phase found touching this step, and which bodies are sensors.</summary>
    private readonly ContactLog _contacts = new();

    /// <summary>The pairs of entities touching at the end of the last step.</summary>
    private HashSet<(Entity A, Entity B)> _touching = [];

    /// <summary>Every joint, by its handle's number, with the two entities it holds.</summary>
    /// <remarks>
    /// A joint can be several of Bepu's constraints between the same two bodies, a hinge with its
    /// motor and its limit, all taken away together.
    /// </remarks>
    private readonly Dictionary<int, (ConstraintHandle[] Constraints, Entity A, Entity B)> _joints = [];

    /// <summary>The motor of each joint that has one, by the joint's number.</summary>
    private readonly Dictionary<int, (ConstraintHandle Constraint, Vector3 Axis)> _motors = [];

    private int _nextJoint;

    /// <summary>Each bouncy body's velocity before the last step, and what struck something in it.</summary>
    private Dictionary<Entity, Vector3>? _beforeLast;

    private HashSet<uint> _struckLast = [];

    /// <summary>How many steps in a row each touching pair has gone unreported.</summary>
    private readonly Dictionary<(Entity A, Entity B), int> _missing = [];

    /// <summary>Steps a pair has to go unreported before it counts as having separated.</summary>
    /// <remarks>
    /// Eight, which is an eighth of a second at Bevy's default rate of sixty-four a second. A body
    /// landing lifts a millimeter or two off what it landed on as it settles, for a few steps,
    /// during which Bepu reports no contact for the pair at all, and that is not a separation a
    /// game means.
    /// </remarks>
    private const int SeparatedAfter = 8;

    private bool _disposed;

    /// <summary>Makes an empty simulation.</summary>
    public PhysicsWorld(PhysicsSettings? settings = null)
    {
        settings ??= new PhysicsSettings();

        _threads = new ThreadDispatcher(Math.Max(1, Environment.ProcessorCount - 1));

        _simulation = Simulation.Create(
            _pool,
            new ContactCallbacks
            {
                Log = _contacts,
                Friction = settings.Friction,
                MaxRecoveryVelocity = 2f,
                Spring = new BepuPhysics.Constraints.SpringSettings(30f, 1f),
            },
            new GravityCallbacks(ToBepu(settings.Gravity), settings.LinearDamping, settings.AngularDamping),
            new SolveDescription(Math.Max(1, settings.Iterations), 1));
    }

    /// <summary>Whether <see cref="PhysicsPlugin"/> leaves the simulation where it is.</summary>
    /// <remarks>
    /// For a game's pause, which is a state the game enters and nothing the simulation knows of,
    /// so a ball rolling as the game paused would roll on under the pause menu. Every body keeps
    /// its pose and velocity and goes on from them when this is set back, and no contact starts or
    /// ends meanwhile. <see cref="Step"/> called directly still steps, for a game stepping on its own
    /// schedule, such as a replay going a frame at a time while paused.
    /// </remarks>
    public bool Paused { get; set; }

    /// <summary>How many bodies there are.</summary>
    public int Count => _bodies.Count;

    /// <summary>Whether an entity has a body.</summary>
    public bool Has(Entity entity) => _bodies.ContainsKey(entity);

    /// <summary>
    /// Gives an entity a body, starting where <paramref name="at"/> puts it.
    /// </summary>
    /// <param name="entity">The entity whose transform the body moves or follows.</param>
    /// <param name="shape">What it collides as.</param>
    /// <param name="kind">How it moves.</param>
    /// <param name="at">Where it starts, which is usually the entity's own transform.</param>
    /// <param name="mass">Its mass, for a dynamic body. Ignored for the other kinds.</param>
    /// <param name="sensor">
    /// Whether it only reports what it touches, through <see cref="ContactStarted"/> and
    /// <see cref="ContactEnded"/>, and pushes nothing. A trigger volume is a static sensor.
    /// </param>
    /// <param name="material">
    /// How its surface slides and bounces, or nothing for the settings' friction and no bounce.
    /// </param>
    /// <exception cref="InvalidOperationException">The entity already has a body.</exception>
    public void Add(
        Entity entity, PhysicsShape shape, BodyKind kind, Transform at, float mass = 1f, bool sensor = false, PhysicsMaterial? material = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_bodies.ContainsKey(entity))
            throw new InvalidOperationException($"{entity} already has a body. Remove it first to give it another.");

        var (index, inertia, center) = AddShape(shape, Math.Max(mass, 1e-4f));
        var orientation = ToBepu(at.Rotation);
        var pose = new RigidPose(ToBepu(at.Translation) + Vector3.Transform(center, orientation), orientation);

        switch (kind)
        {
            case BodyKind.Static:
            {
                var handle = _simulation.Statics.Add(new StaticDescription(pose, index));
                _bodies[entity] = new Body(kind, default, handle, index, center);
                _byStatic[handle] = entity;
                if (sensor) _contacts.Sensors.Add(new CollidableReference(handle).Packed);
                break;
            }

            case BodyKind.Kinematic:
            {
                var handle = _simulation.Bodies.Add(BodyDescription.CreateKinematic(pose, Collidable(index), new BodyActivityDescription(-1f)));
                _bodies[entity] = new Body(kind, handle, default, index, center);
                _byBody[handle] = entity;
                _kinematicWas[entity] = at.Translation;
                if (sensor) _contacts.Sensors.Add(new CollidableReference(CollidableMobility.Kinematic, handle).Packed);
                break;
            }

            default:
            {
                var handle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, Collidable(index), new BodyActivityDescription(0.01f)));
                _bodies[entity] = new Body(kind, handle, default, index, center);
                _byBody[handle] = entity;
                if (sensor) _contacts.Sensors.Add(new CollidableReference(CollidableMobility.Dynamic, handle).Packed);
                break;
            }
        }

        if (material is { } surface) SetMaterial(entity, surface);
    }

    /// <summary>Changes how a body's surface slides and bounces, from the next step.</summary>
    /// <remarks>
    /// Kept in a table the contact callback reads by the two bodies' handles, which Bepu asks on
    /// worker threads during a step. So it is changed between steps, as everything else here is,
    /// and never while one runs.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">The entity has no body.</exception>
    public void SetMaterial(Entity entity, PhysicsMaterial material)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_bodies.TryGetValue(entity, out var body)) throw new KeyNotFoundException($"{entity} has no body.");

        _contacts.Materials[Packed(body)] = material with
        {
            Friction = Math.Max(0f, material.Friction),
            Bounce = Math.Clamp(material.Bounce, 0f, 1f),
        };
    }

    /// <summary>A body's collidable as the contact callback names it.</summary>
    private static uint Packed(Body body) => body.Kind switch
    {
        BodyKind.Static => new CollidableReference(body.Fixed).Packed,
        BodyKind.Kinematic => new CollidableReference(CollidableMobility.Kinematic, body.Moving).Packed,
        _ => new CollidableReference(CollidableMobility.Dynamic, body.Moving).Packed,
    };

    /// <summary>
    /// Joins two bodies with a joint, which holds from the next step on.
    /// </summary>
    /// <remarks>
    /// Both bodies have to move, dynamic or kinematic, because a joint is solved between two
    /// velocities. To pin a body to the world, join it to a kinematic body that stays where it is.
    /// A joint goes when either of its bodies does. Two bodies a joint holds do not collide with
    /// each other, as in most engines, since a hinge's pin passing through its wheel is usual and
    /// the contact between them would hold the wheel still.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">Either entity has no body that moves.</exception>
    public JointHandle Connect(Entity a, Entity b, Joint joint)
    {
        var first = Moving(a);
        var second = Moving(b);
        var spring = new SpringSettings(30f, 1f);

        var constraint = joint.Kind switch
        {
            1 => _simulation.Solver.Add(first.Handle, second.Handle, new Hinge
            {
                LocalOffsetA = ToBepu(joint.AnchorA),
                LocalOffsetB = ToBepu(joint.AnchorB),
                LocalHingeAxisA = Vector3.Normalize(ToBepu(joint.AxisA)),
                LocalHingeAxisB = Vector3.Normalize(ToBepu(joint.AxisB)),
                SpringSettings = spring,
            }),
            2 => _simulation.Solver.Add(first.Handle, second.Handle, new Weld
            {
                // Where the second is, and how it is turned, as the first sees it now.
                LocalOffset = Vector3.Transform(second.Pose.Position - first.Pose.Position, Quaternion.Conjugate(first.Pose.Orientation)),
                LocalOrientation = Quaternion.Concatenate(second.Pose.Orientation, Quaternion.Conjugate(first.Pose.Orientation)),
                SpringSettings = spring,
            }),
            3 => _simulation.Solver.Add(first.Handle, second.Handle, new DistanceLimit(
                ToBepu(joint.AnchorA), ToBepu(joint.AnchorB), joint.Minimum, joint.Maximum, spring)),
            _ => _simulation.Solver.Add(first.Handle, second.Handle, new BallSocket
            {
                LocalOffsetA = ToBepu(joint.AnchorA),
                LocalOffsetB = ToBepu(joint.AnchorB),
                SpringSettings = spring,
            }),
        };

        first.Awake = true;
        second.Awake = true;

        var id = ++_nextJoint;
        var constraints = new List<ConstraintHandle> { constraint };

        if (joint.Motor is { } motor)
        {
            var axis = Vector3.Normalize(ToBepu(joint.AxisA));
            var driven = _simulation.Solver.Add(first.Handle, second.Handle, new AngularAxisMotor
            {
                LocalAxisA = axis,
                // Bepu's target is the first body's turn against the second, the other way round
                // from how a speed is given here.
                TargetVelocity = -motor.Speed * MathF.PI / 180f,
                Settings = new MotorSettings(motor.Torque, 1e-4f),
            });

            constraints.Add(driven);
            _motors[id] = (driven, axis);
        }

        if (joint.Limits is { } limits)
        {
            // A basis on each body whose Z is the hinge's axis, the second's chosen so the two
            // agree as the bodies are turned now, which makes the angle the joint starts at zero.
            var basisA = Toward(Vector3.Normalize(ToBepu(joint.AxisA)));
            var world = Quaternion.Concatenate(basisA, first.Pose.Orientation);
            var basisB = Quaternion.Concatenate(world, Quaternion.Conjugate(second.Pose.Orientation));

            constraints.Add(_simulation.Solver.Add(first.Handle, second.Handle, new TwistLimit
            {
                LocalBasisA = basisA,
                LocalBasisB = Quaternion.Normalize(basisB),
                MinimumAngle = limits.Lowest * MathF.PI / 180f,
                MaximumAngle = limits.Highest * MathF.PI / 180f,
                SpringSettings = spring,
            }));
        }

        _joints[id] = ([.. constraints], a, b);

        var pair = ContactLog.Pair(Packed(_bodies[a]), Packed(_bodies[b]));
        _contacts.Joined[pair] = _contacts.Joined.GetValueOrDefault(pair) + 1;

        return new JointHandle(id);
    }

    /// <summary>
    /// Changes a hinge's motor while it runs, to open a door on command or stop a fan.
    /// </summary>
    /// <param name="joint">A hinge made with <see cref="Joint.WithMotor"/>.</param>
    /// <param name="degreesPerSecond">How fast it turns from the next step, zero holding it where it is.</param>
    /// <param name="torque">The most it pushes with.</param>
    /// <returns>Whether the joint has a motor to change.</returns>
    public bool SetMotor(JointHandle joint, float degreesPerSecond, float torque)
    {
        if (_disposed || !_motors.TryGetValue(joint.Id, out var motor) || !_joints.TryGetValue(joint.Id, out var held)) return false;

        _simulation.Solver.ApplyDescription(motor.Constraint, new AngularAxisMotor
        {
            LocalAxisA = motor.Axis,
            TargetVelocity = -degreesPerSecond * MathF.PI / 180f,
            Settings = new MotorSettings(Math.Max(0f, torque), 1e-4f),
        });

        // Both woken, since a motor told to turn a sleeping door has to wake it to.
        foreach (var entity in new[] { held.A, held.B })
        {
            if (_bodies.TryGetValue(entity, out var body) && body.Kind != BodyKind.Static)
            {
                var reference = _simulation.Bodies[body.Moving];
                reference.Awake = true;
            }
        }

        return true;
    }

    /// <summary>The turn that takes Z onto a direction, the shortest one.</summary>
    private static Quaternion Toward(Vector3 direction)
    {
        var dot = Vector3.Dot(Vector3.UnitZ, direction);
        if (dot > 0.9999f) return Quaternion.Identity;
        if (dot < -0.9999f) return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);

        var axis = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, direction));
        return Quaternion.CreateFromAxisAngle(axis, MathF.Acos(dot));
    }

    /// <summary>Takes a joint away, leaving both bodies free.</summary>
    /// <returns>Whether the joint was there.</returns>
    public bool Disconnect(JointHandle joint)
    {
        if (_disposed || !_joints.Remove(joint.Id, out var held)) return false;

        foreach (var constraint in held.Constraints) _simulation.Solver.Remove(constraint);
        _motors.Remove(joint.Id);

        // Colliding again once nothing holds them, the bodies still being there.
        if (_bodies.TryGetValue(held.A, out var first) && _bodies.TryGetValue(held.B, out var second))
        {
            var pair = ContactLog.Pair(Packed(first), Packed(second));
            if (_contacts.Joined.TryGetValue(pair, out var count) && count > 1) _contacts.Joined[pair] = count - 1;
            else _contacts.Joined.Remove(pair);
        }

        foreach (var entity in new[] { held.A, held.B })
        {
            if (_bodies.TryGetValue(entity, out var body) && body.Kind != BodyKind.Static)
            {
                var reference = _simulation.Bodies[body.Moving];
                reference.Awake = true;
            }
        }

        return true;
    }

    /// <summary>Takes an entity's body away, leaving the entity where it is.</summary>
    /// <returns>Whether it had one.</returns>
    public bool Remove(Entity entity)
    {
        if (_disposed || !_bodies.ContainsKey(entity)) return false;

        // Its joints first, since a constraint holding a body that is gone holds nothing.
        foreach (var joint in _joints.Where(pair => pair.Value.A == entity || pair.Value.B == entity).Select(pair => pair.Key).ToList())
        {
            Disconnect(new JointHandle(joint));
        }

        _bodies.Remove(entity, out var body);
        _contacts.Materials.Remove(Packed(body));

        if (body.Kind == BodyKind.Static)
        {
            _contacts.Sensors.Remove(new CollidableReference(body.Fixed).Packed);
            _simulation.Statics.Remove(body.Fixed);
            _byStatic.Remove(body.Fixed);
        }
        else
        {
            var mobility = body.Kind == BodyKind.Kinematic ? CollidableMobility.Kinematic : CollidableMobility.Dynamic;
            _contacts.Sensors.Remove(new CollidableReference(mobility, body.Moving).Packed);
            _simulation.Bodies.Remove(body.Moving);
            _byBody.Remove(body.Moving);
            _kinematicWas.Remove(entity);
        }

        // And the memory behind it, which for a mesh is its triangles.
        _simulation.Shapes.RemoveAndDispose(body.Shape, _pool);
        return true;
    }

    /// <summary>A body's velocity: how fast it moves, and how fast it turns about each axis.</summary>
    /// <exception cref="KeyNotFoundException">The entity has no body that moves.</exception>
    public (Vec3 Linear, Vec3 Angular) Velocity(Entity entity)
    {
        var velocity = Moving(entity).Velocity;
        return (FromBepu(velocity.Linear), FromBepu(velocity.Angular));
    }

    /// <summary>Sets a dynamic body's velocity, waking it if it had come to rest.</summary>
    /// <exception cref="KeyNotFoundException">The entity has no body that moves.</exception>
    public void SetVelocity(Entity entity, Vec3 linear, Vec3 angular = default)
    {
        var body = Moving(entity);
        body.Velocity.Linear = ToBepu(linear);
        body.Velocity.Angular = ToBepu(angular);
        body.Awake = true;
    }

    /// <summary>
    /// Pushes a dynamic body with an impulse, a change in momentum, at a point
    /// <paramref name="offset"/> from its center, which turns it as well where the point is off
    /// center. A jump, a shot, an explosion.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The entity has no body that moves.</exception>
    public void ApplyImpulse(Entity entity, Vec3 impulse, Vec3 offset = default)
    {
        var body = Moving(entity);
        body.ApplyImpulse(ToBepu(impulse), ToBepu(offset));
        body.Awake = true;
    }

    /// <summary>Whether a dynamic body has come to rest and stopped being simulated.</summary>
    public bool IsAsleep(Entity entity) => !Moving(entity).Awake;

    /// <summary>
    /// The nearest body a ray meets within <paramref name="distance"/>, or null for none.
    /// </summary>
    /// <param name="origin">Where the ray starts.</param>
    /// <param name="direction">Which way it goes. Need not be of length one.</param>
    /// <param name="distance">How far it looks, in world units.</param>
    public PhysicsHit? Raycast(Vec3 origin, Vec3 direction, float distance)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var along = ToBepu(direction);
        var length = along.Length();
        if (length <= 0f) return null;

        var handler = new NearestHit { T = float.MaxValue };
        _simulation.RayCast(ToBepu(origin), along / length, distance, _pool, ref handler);

        if (handler.T == float.MaxValue) return null;

        var entity = handler.Collidable.Mobility == CollidableMobility.Static
            ? _byStatic.GetValueOrDefault(handler.Collidable.StaticHandle)
            : _byBody.GetValueOrDefault(handler.Collidable.BodyHandle);

        var normal = handler.Normal.LengthSquared() > 0f ? Vector3.Normalize(handler.Normal) : Vector3.Zero;
        var point = origin + direction * (handler.T / length);
        return new PhysicsHit(entity, point, FromBepu(normal), handler.T);
    }

    /// <summary>
    /// Advances the simulation by <paramref name="seconds"/>: kinematic bodies follow their
    /// entities, everything is stepped, and dynamic bodies are written back.
    /// </summary>
    /// <remarks>
    /// <see cref="PhysicsPlugin"/> calls this once per fixed step. It is public for a game that
    /// steps on its own schedule instead, such as a replay stepping as fast as it can. Contacts that
    /// start and end are sent on <paramref name="messages"/> where one is given.
    /// </remarks>
    public void Step(EcsWorld ecs, float seconds, MessageBus? messages = null)
    {
        ArgumentNullException.ThrowIfNull(ecs);
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (seconds <= 0f) return;

        // A body whose entity is gone goes with it, so a despawn is all a game has to do.
        List<Entity>? gone = null;

        foreach (var (entity, body) in _bodies)
        {
            if (!ecs.IsAlive(entity))
            {
                (gone ??= []).Add(entity);
                continue;
            }

            if (body.Kind != BodyKind.Kinematic || !ecs.TryGet<Transform>(entity, out var transform)) continue;

            // Moved by the game, and given the velocity that move took, so it pushes what it
            // meets rather than passing through it.
            var reference = _simulation.Bodies[body.Moving];
            var was = _kinematicWas[entity];

            reference.Pose.Orientation = ToBepu(transform.Rotation);
            reference.Pose.Position = ToBepu(transform.Translation) + Vector3.Transform(body.Center, reference.Pose.Orientation);
            reference.Velocity.Linear = ToBepu((transform.Translation - was) * (1f / seconds));
            reference.Awake = true;

            _kinematicWas[entity] = transform.Translation;
        }

        if (gone is not null)
        {
            foreach (var entity in gone) Remove(entity);
        }

        // How fast each bouncy body was going before the step, which with the step before is the
        // speed it hit with.
        Dictionary<Entity, Vector3>? before = null;
        foreach (var (packed, material) in _contacts.Materials)
        {
            if (material.Bounce <= 0f || Entity(packed) is not { } bouncy || !_bodies.TryGetValue(bouncy, out var body)) continue;
            if (body.Kind != BodyKind.Dynamic) continue;

            (before ??= [])[bouncy] = _simulation.Bodies[body.Moving].Velocity.Linear;
        }

        _contacts.Touching.Clear();
        _contacts.Struck.Clear();
        _simulation.Timestep(seconds, _threads);
        Report(messages);
        Bounce(before);

        _beforeLast = before;
        _struckLast = [.. _contacts.Struck.Keys];

        foreach (var (entity, body) in _bodies)
        {
            if (body.Kind != BodyKind.Dynamic) continue;

            var reference = _simulation.Bodies[body.Moving];
            if (!reference.Awake) continue;

            // The scale is the entity's own, and the pose is the simulation's.
            var transform = ecs.TryGet<Transform>(entity, out var current) ? current : Transform.Identity;
            // The entity's origin, which for a hull is not the center the body turns about.
            transform.Translation = FromBepu(reference.Pose.Position - Vector3.Transform(body.Center, reference.Pose.Orientation));
            transform.Rotation = FromBepu(reference.Pose.Orientation);
            ecs.Set(entity, transform);
        }
    }

    /// <summary>
    /// Gives each bouncy body that struck something this step its speed back out of the surface.
    /// </summary>
    /// <remarks>
    /// A body already resting on the surface struck it at no speed, which is under the threshold,
    /// so it rests rather than buzzing on the spot.
    /// </remarks>
    private void Bounce(Dictionary<Entity, Vector3>? before)
    {
        if (before is null) return;

        foreach (var (packed, outward) in _contacts.Struck)
        {
            // Once a contact, on the step it began. One that goes on is a body resting or rolling.
            if (_struckLast.Contains(packed)) continue;

            if (Entity(packed) is not { } entity || !before.TryGetValue(entity, out var hit)) continue;
            if (!_bodies.TryGetValue(entity, out var body) || !_contacts.Materials.TryGetValue(packed, out var material)) continue;

            // A speculative contact slows a body the step before it touches, closing the gap
            // exactly, so the speed it hit with may be the one it had a step earlier.
            var normal = Vector3.Normalize(outward);
            var into = Vector3.Dot(hit, normal);
            if (_beforeLast?.TryGetValue(entity, out var earlier) == true) into = MathF.Min(into, Vector3.Dot(earlier, normal));
            if (into > -0.2f) continue;

            var reference = _simulation.Bodies[body.Moving];
            var now = reference.Velocity.Linear;
            reference.Velocity.Linear = now - (Vector3.Dot(now, normal) * normal) - (material.Bounce * into * normal);
            reference.Awake = true;
        }
    }

    /// <summary>
    /// Sends the pairs that started touching this step and the ones that stopped, by entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A body that came to rest drops out of the narrow phase, so a pair of sleeping bodies is kept
    /// as touching rather than reported as ended, which would otherwise end every resting stack.
    /// </para>
    /// <para>
    /// A pair also has to go unreported for a few steps in a row before it counts as separated.
    /// A body settling onto another hops clear of it by a millimeter or so for a step or three,
    /// and a game told it left and landed again would play the landing twice. A body that is
    /// removed ends its pairs at once.
    /// </para>
    /// </remarks>
    private void Report(MessageBus? messages)
    {
        var now = new HashSet<(Entity A, Entity B)>();

        foreach (var (a, b) in _contacts.Touching)
        {
            if (Entity(a) is { } first && Entity(b) is { } second)
            {
                now.Add(first.Bits < second.Bits ? (first, second) : (second, first));
            }
        }

        foreach (var pair in _touching)
        {
            if (now.Contains(pair)) continue;

            var present = _bodies.ContainsKey(pair.A) && _bodies.ContainsKey(pair.B);

            if (present && Resting(pair.A) && Resting(pair.B))
            {
                now.Add(pair);
                continue;
            }

            var missing = _missing.GetValueOrDefault(pair) + 1;

            if (present && missing < SeparatedAfter)
            {
                _missing[pair] = missing;
                now.Add(pair);
                continue;
            }

            _missing.Remove(pair);
            messages?.Send(new ContactEnded(pair.A, pair.B));
        }

        foreach (var pair in now)
        {
            if (!_touching.Contains(pair)) messages?.Send(new ContactStarted(pair.A, pair.B));
        }

        // A pair reported this step starts counting again from nothing.
        foreach (var (a, b) in _contacts.Touching)
        {
            if (Entity(a) is { } first && Entity(b) is { } second)
            {
                _missing.Remove(first.Bits < second.Bits ? (first, second) : (second, first));
            }
        }

        _touching = now;
    }

    /// <summary>Whether an entity's body is out of the narrow phase, asleep or never moving.</summary>
    private bool Resting(Entity entity) =>
        !_bodies.TryGetValue(entity, out var body)
        || body.Kind == BodyKind.Static
        || !_simulation.Bodies[body.Moving].Awake;

    /// <summary>The entity a packed collidable belongs to, or null for one that is gone.</summary>
    private Entity? Entity(uint packed)
    {
        var reference = new CollidableReference { Packed = packed };

        if (reference.Mobility == CollidableMobility.Static)
            return _byStatic.TryGetValue(reference.StaticHandle, out var fixedOne) ? fixedOne : null;

        return _byBody.TryGetValue(reference.BodyHandle, out var moving) ? moving : null;
    }

    /// <summary>Tears the simulation down, returning its memory.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _simulation.Dispose();
        _threads.Dispose();
        _pool.Clear();
    }

    private BodyReference Moving(Entity entity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!_bodies.TryGetValue(entity, out var body) || body.Kind == BodyKind.Static)
            throw new KeyNotFoundException($"{entity} has no body that moves.");

        return _simulation.Bodies[body.Moving];
    }

    /// <summary>How a moving body collides, with contacts generated up to a tenth of a unit ahead.</summary>
    private static CollidableDescription Collidable(TypedIndex shape) => new(shape, 0.1f);

    private (TypedIndex Index, BodyInertia Inertia, Vector3 Center) AddShape(PhysicsShape shape, float mass)
    {
        var size = ToBepu(shape.Size);

        switch (shape.Kind)
        {
            case 1:
            {
                var sphere = new BepuShapes.Sphere(size.X);
                return (_simulation.Shapes.Add(sphere), sphere.ComputeInertia(mass), default);
            }
            case 2:
            {
                var capsule = new BepuShapes.Capsule(size.X, size.Y);
                return (_simulation.Shapes.Add(capsule), capsule.ComputeInertia(mass), default);
            }
            case 3:
            {
                var cylinder = new BepuShapes.Cylinder(size.X, size.Y);
                return (_simulation.Shapes.Add(cylinder), cylinder.ComputeInertia(mass), default);
            }
            case 5:
            {
                var points = shape.Positions!.Select(ToBepu).ToArray();
                var hull = new ConvexHull(points, _pool, out var center);
                return (_simulation.Shapes.Add(hull), hull.ComputeInertia(mass), center);
            }
            case 4:
            {
                var positions = shape.Positions!;
                var indices = shape.Indices!;
                var count = indices.Length / 3;

                _pool.Take<Triangle>(count, out var triangles);

                for (var i = 0; i < count; i++)
                {
                    // Bepu's triangles face the other way from Bevy's, so two corners swap.
                    triangles[i] = new Triangle(
                        ToBepu(positions[indices[i * 3]]),
                        ToBepu(positions[indices[i * 3 + 2]]),
                        ToBepu(positions[indices[i * 3 + 1]]));
                }

                var mesh = new BepuShapes.Mesh(triangles, Vector3.One, _pool);
                return (_simulation.Shapes.Add(mesh), mesh.ComputeClosedInertia(mass), default);
            }
            default:
            {
                var box = new BepuShapes.Box(size.X, size.Y, size.Z);
                return (_simulation.Shapes.Add(box), box.ComputeInertia(mass), default);
            }
        }
    }

    private static Vector3 ToBepu(Vec3 value) => new(value.X, value.Y, value.Z);

    private static Quaternion ToBepu(Quat value) => Quaternion.Normalize(new Quaternion(value.X, value.Y, value.Z, value.W));

    private static Vec3 FromBepu(Vector3 value) => new(value.X, value.Y, value.Z);

    private static Quat FromBepu(Quaternion value) => new(value.X, value.Y, value.Z, value.W);

    /// <summary>Keeps the nearest thing a ray meets.</summary>
    private struct NearestHit : IRayHitHandler
    {
        public float T;
        public Vector3 Normal;
        public CollidableReference Collidable;

        public readonly bool AllowTest(CollidableReference collidable) => true;

        public readonly bool AllowTest(CollidableReference collidable, int childIndex) => true;

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (t >= T) return;

            T = t;
            Normal = normal;
            Collidable = collidable;

            // Anything farther than this one is no longer worth testing.
            maximumT = t;
        }
    }
}
