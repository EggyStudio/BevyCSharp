using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using BepuShapes = BepuPhysics.Collidables;

namespace Bevy.Physics;

/// <summary>How a body moves.</summary>
public enum BodyKind
{
    /// <summary>Moved by the simulation: falls, collides and is pushed.</summary>
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
}

/// <summary>Settings for the simulation as a whole.</summary>
public sealed class PhysicsSettings
{
    /// <summary>Acceleration every dynamic body feels, in units a second squared.</summary>
    public Vec3 Gravity { get; set; } = new(0f, -9.81f, 0f);

    /// <summary>How much of its speed a body loses a second, from zero to one.</summary>
    public float LinearDamping { get; set; } = 0.03f;

    /// <summary>How much of its spin a body loses a second, from zero to one.</summary>
    public float AngularDamping { get; set; } = 0.03f;

    /// <summary>Friction between any two bodies in contact.</summary>
    public float Friction { get; set; } = 1f;

    /// <summary>
    /// How many solver passes a step makes. More holds a tall stack steadier and costs more.
    /// </summary>
    public int Iterations { get; set; } = 8;
}

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
/// crosses to the engine: a body's pose reaches Bevy as the <see cref="Transform"/> write any
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

    /// <summary>A body, whichever kind of handle Bepu gave it.</summary>
    private readonly record struct Body(BodyKind Kind, BodyHandle Moving, StaticHandle Fixed, TypedIndex Shape);

    private readonly Dictionary<Entity, Body> _bodies = [];
    private readonly Dictionary<BodyHandle, Entity> _byBody = [];
    private readonly Dictionary<StaticHandle, Entity> _byStatic = [];

    /// <summary>Where each kinematic body was, for its velocity from how far it moved.</summary>
    private readonly Dictionary<Entity, Vec3> _kinematicWas = [];

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
                Friction = settings.Friction,
                MaxRecoveryVelocity = 2f,
                Spring = new BepuPhysics.Constraints.SpringSettings(30f, 1f),
            },
            new GravityCallbacks(ToBepu(settings.Gravity), settings.LinearDamping, settings.AngularDamping),
            new SolveDescription(Math.Max(1, settings.Iterations), 1));
    }

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
    /// <exception cref="InvalidOperationException">The entity already has a body.</exception>
    public void Add(Entity entity, PhysicsShape shape, BodyKind kind, Transform at, float mass = 1f)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_bodies.ContainsKey(entity))
            throw new InvalidOperationException($"{entity} already has a body. Remove it first to give it another.");

        var pose = new RigidPose(ToBepu(at.Translation), ToBepu(at.Rotation));
        var (index, inertia) = AddShape(shape, Math.Max(mass, 1e-4f));

        switch (kind)
        {
            case BodyKind.Static:
            {
                var handle = _simulation.Statics.Add(new StaticDescription(pose, index));
                _bodies[entity] = new Body(kind, default, handle, index);
                _byStatic[handle] = entity;
                break;
            }

            case BodyKind.Kinematic:
            {
                var handle = _simulation.Bodies.Add(BodyDescription.CreateKinematic(pose, new CollidableDescription(index, 0.1f), new BodyActivityDescription(-1f)));
                _bodies[entity] = new Body(kind, handle, default, index);
                _byBody[handle] = entity;
                _kinematicWas[entity] = at.Translation;
                break;
            }

            default:
            {
                var handle = _simulation.Bodies.Add(BodyDescription.CreateDynamic(pose, inertia, new CollidableDescription(index, 0.1f), new BodyActivityDescription(0.01f)));
                _bodies[entity] = new Body(kind, handle, default, index);
                _byBody[handle] = entity;
                break;
            }
        }
    }

    /// <summary>Takes an entity's body away, leaving the entity where it is.</summary>
    /// <returns>Whether it had one.</returns>
    public bool Remove(Entity entity)
    {
        if (_disposed || !_bodies.Remove(entity, out var body)) return false;

        if (body.Kind == BodyKind.Static)
        {
            _simulation.Statics.Remove(body.Fixed);
            _byStatic.Remove(body.Fixed);
        }
        else
        {
            _simulation.Bodies.Remove(body.Moving);
            _byBody.Remove(body.Moving);
            _kinematicWas.Remove(entity);
        }

        _simulation.Shapes.Remove(body.Shape);
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
    /// steps on its own schedule instead, such as a replay stepping as fast as it can.
    /// </remarks>
    public void Step(EcsWorld ecs, float seconds)
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

            reference.Pose.Position = ToBepu(transform.Translation);
            reference.Pose.Orientation = ToBepu(transform.Rotation);
            reference.Velocity.Linear = ToBepu((transform.Translation - was) * (1f / seconds));
            reference.Awake = true;

            _kinematicWas[entity] = transform.Translation;
        }

        if (gone is not null)
        {
            foreach (var entity in gone) Remove(entity);
        }

        _simulation.Timestep(seconds, _threads);

        foreach (var (entity, body) in _bodies)
        {
            if (body.Kind != BodyKind.Dynamic) continue;

            var reference = _simulation.Bodies[body.Moving];
            if (!reference.Awake) continue;

            // The scale is the entity's own, and the pose is the simulation's.
            var transform = ecs.TryGet<Transform>(entity, out var current) ? current : Transform.Identity;
            transform.Translation = FromBepu(reference.Pose.Position);
            transform.Rotation = FromBepu(reference.Pose.Orientation);
            ecs.Set(entity, transform);
        }
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

    private (TypedIndex Index, BodyInertia Inertia) AddShape(PhysicsShape shape, float mass)
    {
        var size = ToBepu(shape.Size);

        switch (shape.Kind)
        {
            case 1:
            {
                var sphere = new BepuShapes.Sphere(size.X);
                return (_simulation.Shapes.Add(sphere), sphere.ComputeInertia(mass));
            }
            case 2:
            {
                var capsule = new BepuShapes.Capsule(size.X, size.Y);
                return (_simulation.Shapes.Add(capsule), capsule.ComputeInertia(mass));
            }
            case 3:
            {
                var cylinder = new BepuShapes.Cylinder(size.X, size.Y);
                return (_simulation.Shapes.Add(cylinder), cylinder.ComputeInertia(mass));
            }
            default:
            {
                var box = new BepuShapes.Box(size.X, size.Y, size.Z);
                return (_simulation.Shapes.Add(box), box.ComputeInertia(mass));
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
