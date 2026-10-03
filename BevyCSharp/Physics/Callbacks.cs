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
    public readonly HashSet<(uint A, uint B)> Touching = [];
    public readonly HashSet<uint> Sensors = [];

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

    public void Add(CollidableReference a, CollidableReference b)
    {
        // In one order, so a pair is the same pair whichever way Bepu names it.
        var pair = a.Packed < b.Packed ? (a.Packed, b.Packed) : (b.Packed, a.Packed);
        lock (Touching) Touching.Add(pair);
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

    public void Initialize(Simulation simulation)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin) =>
        // Two things that cannot move have nothing to say to each other.
        a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties material)
        where TManifold : unmanaged, IContactManifold<TManifold>
    {
        var first = Log.Materials.TryGetValue(pair.A.Packed, out var a) ? a : new Bevy.Physics.PhysicsMaterial(Friction);
        var second = Log.Materials.TryGetValue(pair.B.Packed, out var b) ? b : new Bevy.Physics.PhysicsMaterial(Friction);

        material.FrictionCoefficient = MathF.Sqrt(first.Friction * second.Friction);
        material.MaximumRecoveryVelocity = MaxRecoveryVelocity;
        material.SpringSettings = Spring;

        // Bepu also reports contacts that are only about to happen, with a negative depth, so a
        // pair counts as touching only where some contact is within a centimeter of it.
        for (var i = 0; i < manifold.Count; i++)
        {
            if (manifold.GetDepth(i) < -0.01f) continue;

            Log.Add(pair.A, pair.B);

            // Which way the surface faces, for the bounce the world gives a bouncy body after the
            // step. Bepu's normal points from the second collidable to the first, so out of the
            // second's surface toward the first.
            var normal = manifold.GetNormal(i);
            if (first.Bounce > 0f && pair.A.Mobility == CollidableMobility.Dynamic) Log.Strike(pair.A, normal);
            if (second.Bounce > 0f && pair.B.Mobility == CollidableMobility.Dynamic) Log.Strike(pair.B, -normal);
            break;
        }

        // A sensor reports what it touches and pushes nothing, so no constraint is made for it.
        return !Log.Sensors.Contains(pair.A.Packed) && !Log.Sensors.Contains(pair.B.Packed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold) => true;

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
