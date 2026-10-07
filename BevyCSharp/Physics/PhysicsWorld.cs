using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using BepuShapes = BepuPhysics.Collidables;

namespace Bevy.Physics;

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
/// is written back each step. A kinematic one follows its entity at the speed and rate of turning
/// the entity moves at, whether the game moves it once a frame or once a step, so what it pushes is
/// pushed at that speed and what rests on it is carried at that pace whatever the frame rate.
/// <see cref="MarkPlaced"/> puts one where its entity was put instead.
/// </para>
/// <para>
/// A body belongs to its entity. Despawning the entity removes the body on the next step, and
/// <see cref="Remove"/> takes it off without despawning anything. No Bepu type appears here, so
/// the engine underneath can be replaced without a game changing.
/// </para>
/// </remarks>
public sealed partial class PhysicsWorld : IDisposable
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

    /// <summary>What the narrow phase found touching this step, and which bodies are sensors.</summary>
    private readonly ContactLog _contacts = new();

    /// <summary>The pairs of entities touching at the end of the last step.</summary>
    private HashSet<(Entity A, Entity B)> _touching = [];

    /// <summary>
    /// The fastest each pair near but not yet touching has closed at, which its contact reports as it
    /// starts, forgotten once it starts or moves off.
    /// </summary>
    private readonly Dictionary<(uint A, uint B), float> _approached = [];

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
        _gravity = ToBepu(settings.Gravity);
        _placeBeyond = Math.Max(0f, settings.PlaceBeyond);

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
        center += ToBepu(shape.Offset);
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
                BeginFollowing(entity, at);
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

        // Bepu gives the collidable out again, and the next body to have it is on layer 0 unless set.
        _contacts.Layers.Set(Packed(body), 0);
        ForgetImpulses(Packed(body));
        _characters.Remove(entity);

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
            StopFollowing(entity);
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
        Wake(body);
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
        Wake(body);
    }

    /// <summary>Whether a dynamic body has come to rest and stopped being simulated.</summary>
    public bool IsAsleep(Entity entity) => !Moving(entity).Awake;

    /// <summary>
    /// The nearest body a ray meets within <paramref name="distance"/>, or null for none.
    /// </summary>
    /// <param name="origin">Where the ray starts.</param>
    /// <param name="direction">Which way it goes. Need not be of length one.</param>
    /// <param name="distance">How far it looks, in world units.</param>
    public PhysicsHit? Raycast(Vec3 origin, Vec3 direction, float distance) => Cast(origin, direction, distance, null);

    /// <summary>
    /// The nearest body a ray cast from <paramref name="from"/>'s body meets within
    /// <paramref name="distance"/>, passing through that body and what its layer does not collide
    /// with, or null for none.
    /// </summary>
    /// <remarks>
    /// For a ray a body casts, a shot from a gun or a look from a character's eyes, which starts
    /// inside the body and would meet it first, and which sees what that body would hit, so the
    /// player's shots on a layer that passes through the player pass through the player here too.
    /// </remarks>
    /// <param name="origin">Where the ray starts.</param>
    /// <param name="direction">Which way it goes. Need not be of length one.</param>
    /// <param name="distance">How far it looks, in world units.</param>
    /// <param name="from">The entity whose body casts it.</param>
    /// <exception cref="KeyNotFoundException">The entity has no body.</exception>
    public PhysicsHit? Raycast(Vec3 origin, Vec3 direction, float distance, Entity from) => Cast(origin, direction, distance, Packed(_bodies[from]));

    private PhysicsHit? Cast(Vec3 origin, Vec3 direction, float distance, uint? from)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var along = ToBepu(direction);
        var length = along.Length();
        if (length <= 0f) return null;

        var handler = new NearestHit { T = float.MaxValue, Layers = _contacts.Layers, From = -1 };
        if (from is { } skip) (handler.Skipping, handler.Skip, handler.From) = (true, skip, _contacts.Layers.Of(skip));
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

        // A body whose entity is gone goes with it, so a despawn is all a game has to do. Asked of
        // every body in one call, since a call each is most of what a step costs a level of
        // bodies at rest.
        List<Entity>? gone = null;
        var held = _bodies.Keys.ToArray();
        var alive = new bool[held.Length];
        ecs.AliveMany(held, alive);

        for (var i = 0; i < held.Length; i++)
        {
            var entity = held[i];
            if (!alive[i])
            {
                (gone ??= []).Add(entity);
                continue;
            }

            // Moved by the game, and moved after it at the speed it moves, so it pushes what it
            // meets rather than passing through it, and carries what rests on it at its pace.
            if (_bodies[entity].Kind != BodyKind.Kinematic || !ecs.TryGet<Transform>(entity, out var transform)) continue;
            Follow(entity, transform, seconds);
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

        // Characters walked toward what their games ask, which needs the world's ground as it is
        // before the step moves anything.
        MoveCharacters(ecs, seconds);

        _contacts.Touching.Clear();
        _contacts.Near.Clear();
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

            // A character stays upright and its entity faces wherever the game turned it.
            if (!_characters.ContainsKey(entity)) transform.Rotation = FromBepu(reference.Pose.Orientation);
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
        foreach (var (key, speed) in _contacts.Near) _approached[key] = MathF.Max(speed, _approached.GetValueOrDefault(key));
        if (_approached.Count > _contacts.Near.Count)
        {
            foreach (var key in _approached.Keys.Where(key => !_contacts.Near.ContainsKey(key) && !_contacts.Touching.ContainsKey(key)).ToArray())
                _approached.Remove(key);
        }

        var now = new HashSet<(Entity A, Entity B)>();
        var met = new Dictionary<(Entity A, Entity B), (Vector3 Point, Vector3 Normal, float Speed)>();

        foreach (var (key, touch) in _contacts.Touching)
        {
            if (Entity(key.A) is not { } first || Entity(key.B) is not { } second) continue;

            // Named in the order of the entities' bits, the normal turned to point from the second
            // toward the first, and the speed the faster of now and the approach.
            var (pair, normal) = first.Bits < second.Bits ? ((first, second), touch.Normal) : ((second, first), -touch.Normal);
            var speed = MathF.Max(touch.Speed, _approached.Remove(key, out var approached) ? approached : 0f);
            now.Add(pair);
            met[pair] = (touch.Point, normal, speed);
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
            if (_touching.Contains(pair)) continue;

            var (point, normal, speed) = met.GetValueOrDefault(pair);
            messages?.Send(new ContactStarted(pair.A, pair.B, FromBepu(point), FromBepu(normal), speed));
        }

        // A pair reported this step starts counting again from nothing.
        foreach (var pair in met.Keys) _missing.Remove(pair);

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

    /// <summary>Wakes a body a game has asked to move, so it moves in the next step.</summary>
    /// <remarks>
    /// Bepu decides at the start of a step which bodies sleep, from how fast each went in the step
    /// before, and a body that has rested long enough is a candidate however it was told to move
    /// since. Setting <c>Awake</c> on one that is awake changes nothing, so a crate at rest for
    /// half a second, given a speed of 3, was put to sleep at the start of the next step and never
    /// moved. Its candidacy is cleared with the wake, and the step after is judged on the speed it
    /// was given.
    /// </remarks>
    private static void Wake(BodyReference body)
    {
        body.Awake = true;
        body.Activity.SleepCandidate = false;
        body.Activity.TimestepsUnderThresholdCount = 0;
    }

    /// <summary>How a moving body collides, with contacts generated up to a tenth of a unit ahead.</summary>
    /// <summary>How far from a body, in units, contacts are made ahead of its meeting what it nears, unless it is swept (<see cref="SetContinuous"/>).</summary>
    private const float SpeculativeMargin = 0.1f;

    private static CollidableDescription Collidable(TypedIndex shape) => new(shape, SpeculativeMargin);

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

        /// <summary>Whether <see cref="Skip"/> names a collidable the ray passes through, a character's own body.</summary>
        public bool Skipping;
        public uint Skip;

        /// <summary>The sensors, which the ray passes through where given.</summary>
        public HashSet<uint>? Sensors;

        /// <summary>
        /// The layer the ray is cast from, whose layer table says what it passes through, or below
        /// zero for a ray that sees every layer.
        /// </summary>
        public int From;
        public CollisionLayers? Layers;

        public readonly bool AllowTest(CollidableReference collidable) =>
            !(Skipping && collidable.Packed == Skip) && (Sensors is null || !Sensors.Contains(collidable.Packed))
            && (From < 0 || Layers is null || Layers.Collide(From, Layers.Of(collidable.Packed)));

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
