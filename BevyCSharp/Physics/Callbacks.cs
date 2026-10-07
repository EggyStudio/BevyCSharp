using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;

namespace Bevy.Physics;

/// <summary>
/// The pairs of collidables found touching during a step, and which collidables are sensors.
/// </summary>
/// <remarks>
/// Written from Bepu's worker threads while a step runs, so adding takes a lock; the sensors are
/// only read then, and changed between steps.
/// </remarks>
internal sealed class ContactLog
{
    /// <summary>
    /// The pairs touching this step, each in one order, with the point of their deepest contact in
    /// the world, its normal from the second toward the first of the pair as named here, and the
    /// speed they closed at along it.
    /// </summary>
    public readonly Dictionary<(uint A, uint B), (Vector3 Point, Vector3 Normal, float Speed)> Touching = [];

    /// <summary>
    /// The pairs near enough this step to meet within it and not yet touching, each with the
    /// fastest it closed at, since the solver slows a pair in the steps before it touches, so the
    /// speed it met at is read while it approaches.
    /// </summary>
    public readonly Dictionary<(uint A, uint B), float> Near = [];

    public readonly HashSet<uint> Sensors = [];

    /// <summary>Each collidable's layer, and which layers collide with which.</summary>
    public readonly CollisionLayers Layers = new();

    /// <summary>
    /// The pairs of collidables held by a joint, by how many joints hold each, which do not collide
    /// with each other.
    /// </summary>
    public readonly Dictionary<(uint A, uint B), int> Joined = [];

    /// <summary>A pair in one order, whichever way it is named.</summary>
    public static (uint A, uint B) Pair(uint a, uint b) => a < b ? (a, b) : (b, a);

    /// <summary>Each body's own material, by its packed collidable, for the bodies given one.</summary>
    public readonly Dictionary<uint, Bevy.Physics.PhysicsMaterial> Materials = [];

    /// <summary>
    /// For each bouncy collidable touching something this step, which way the surface it touched
    /// faces, out toward it.
    /// </summary>
    public readonly Dictionary<uint, Vector3> Struck = [];

    public void Strike(CollidableReference body, Vector3 outward)
    {
        lock (Struck) Struck[body.Packed] = outward;
    }

    /// <summary>
    /// Records a pair at its deepest contact, the normal from <paramref name="b"/> toward
    /// <paramref name="a"/> as Bepu gives it, touching or only near.
    /// </summary>
    public void Add(CollidableReference a, CollidableReference b, Vector3 point, Vector3 normal, float speed, bool touching)
    {
        // In one order, so a pair is the same pair whichever way Bepu names it, the normal turned
        // with it.
        var (pair, facing) = a.Packed < b.Packed ? ((a.Packed, b.Packed), normal) : ((b.Packed, a.Packed), -normal);

        if (touching)
        {
            lock (Touching) Touching[pair] = (point, facing, speed);
            return;
        }

        lock (Near) Near[pair] = MathF.Max(speed, Near.GetValueOrDefault(pair));
    }
}

/// <summary>
/// How two bodies touching behave, from each one's material or the settings' friction.
/// </summary>
/// <remarks>
/// Bepu asks this for every pair its broad phase finds and every contact it generates, so it is a
/// struct with nothing in it that allocates, and the materials are a table read by the two
/// handles, which is only written between steps.
/// </remarks>
internal struct ContactCallbacks : INarrowPhaseCallbacks
{
    public SpringSettings Spring;
    public float Friction;
    public float MaxRecoveryVelocity;
    public ContactLog Log;
    private Simulation? _simulation;

    public void Initialize(Simulation simulation) => _simulation = simulation;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) =>
        // Two things that cannot move have nothing to say to each other, and two a joint holds
        // are held as the joint says, which a hinge's pin passing through its wheel would fight.
        // Bodies on layers that do not collide pass through each other and report nothing.
        (a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic)
        && (Log.Joined.Count == 0 || !Log.Joined.ContainsKey(ContactLog.Pair(a.Packed, b.Packed)))
        && Log.Layers.Collide(a.Packed, b.Packed);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties material)
        where TManifold : unmanaged, IContactManifold<TManifold>
    {
        var first = Log.Materials.TryGetValue(pair.A.Packed, out var a) ? a : new Bevy.Physics.PhysicsMaterial(Friction);
        var second = Log.Materials.TryGetValue(pair.B.Packed, out var b) ? b : new Bevy.Physics.PhysicsMaterial(Friction);

        // Bepu shares a convex manifold's friction among its contacts, so a box resting on four
        // corners slid as though a quarter as rough, slowing by 2.45 a second each second at a
        // friction of 1 where gravity times the friction is 9.81. The coefficient is scaled by the
        // count, so it holds back what it says, as 3DEngine's does since its ed0f3aa6, which
        // leaves a manifold that is not convex as it was.
        var contacts = manifold.Convex ? Math.Max(1, manifold.Count) : 1;
        material.FrictionCoefficient = MathF.Sqrt(first.Friction * second.Friction) * contacts;
        material.MaximumRecoveryVelocity = MaxRecoveryVelocity;
        material.SpringSettings = Spring;

        // Bepu also reports contacts that are only about to happen, with a negative depth, so a
        // pair counts as touching only where some contact is within a centimeter of it. The
        // deepest contact gives the pair's point and normal, and a pair only near is recorded too,
        // for the speed it closes at before the solver slows it.
        if (manifold.Count > 0)
        {
            var deepest = 0;
            for (var i = 1; i < manifold.Count; i++)
                if (manifold.GetDepth(i) > manifold.GetDepth(deepest)) deepest = i;

            manifold.GetContact(deepest, out var offset, out var normal, out var depth, out _);
            var point = PositionOf(pair.A) + offset;

            // The narrow phase runs before the solver, so these are the velocities the bodies
            // came into the step with. Bepu's normal points from the second collidable to the
            // first, so the pair closes where the second moves toward the first along it.
            var closing = Vector3.Dot(VelocityAt(pair.B, point) - VelocityAt(pair.A, point), normal);
            var touching = depth >= -0.01f;
            Log.Add(pair.A, pair.B, point, normal, MathF.Max(0f, closing), touching);

            // Which way the surface faces, for the bounce the world gives a bouncy body after the
            // step, out of the second's surface toward the first.
            if (touching && first.Bounce > 0f && pair.A.Mobility == CollidableMobility.Dynamic) Log.Strike(pair.A, normal);
            if (touching && second.Bounce > 0f && pair.B.Mobility == CollidableMobility.Dynamic) Log.Strike(pair.B, -normal);
        }

        // A sensor reports what it touches and pushes nothing, so no constraint is made for it.
        return !Log.Sensors.Contains(pair.A.Packed) && !Log.Sensors.Contains(pair.B.Packed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

    // Where a collidable is in the world, the point its contacts are offsets from.
    private readonly Vector3 PositionOf(CollidableReference collidable) =>
        collidable.Mobility == CollidableMobility.Static
            ? _simulation!.Statics[collidable.StaticHandle].Pose.Position
            : _simulation!.Bodies[collidable.BodyHandle].Pose.Position;

    // How fast a collidable's surface moves at a point in the world, its turn included. A static
    // never moves.
    private readonly Vector3 VelocityAt(CollidableReference collidable, Vector3 point)
    {
        if (collidable.Mobility == CollidableMobility.Static) return Vector3.Zero;
        var body = _simulation!.Bodies[collidable.BodyHandle];
        var velocity = body.Velocity;
        return velocity.Linear + Vector3.Cross(velocity.Angular, point - body.Pose.Position);
    }

    public void Dispose()
    {
    }
}

/// <summary>Gravity and damping, applied to every dynamic body as it is integrated.</summary>
/// <remarks>
/// Bepu integrates bodies in bundles, several at once in vector lanes, so gravity is widened into
/// a vector once a step rather than once a body.
/// </remarks>
internal struct GravityCallbacks(Vector3 gravity, float linearDamping, float angularDamping) : IPoseIntegratorCallbacks
{
    public Vector3 Gravity = gravity;
    public float LinearDamping = linearDamping;
    public float AngularDamping = angularDamping;

    private Vector3Wide _gravityStep;
    private Vector<float> _linearKeep;
    private Vector<float> _angularKeep;

    public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

    public readonly bool AllowSubstepsForUnconstrainedBodies => false;

    public readonly bool IntegrateVelocityForKinematics => false;

    public void Initialize(Simulation simulation)
    {
    }

    public void PrepareForIntegration(float dt)
    {
        _gravityStep = Vector3Wide.Broadcast(Gravity * dt);

        // Damping as a fraction kept per step, so it means the same thing at any step length.
        _linearKeep = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - LinearDamping, 0, 1), dt));
        _angularKeep = new Vector<float>(MathF.Pow(MathHelper.Clamp(1 - AngularDamping, 0, 1), dt));
    }

    public readonly void IntegrateVelocity(
        Vector<int> bodyIndices,
        Vector3Wide position,
        QuaternionWide orientation,
        BodyInertiaWide localInertia,
        Vector<int> integrationMask,
        int workerIndex,
        Vector<float> dt,
        ref BodyVelocityWide velocity)
    {
        velocity.Linear = (velocity.Linear + _gravityStep) * _linearKeep;
        velocity.Angular *= _angularKeep;
    }
}
