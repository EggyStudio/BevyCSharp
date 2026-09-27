using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuUtilities;

namespace Bevy.Physics;

/// <summary>
/// How any two bodies touching behave: one friction and one springiness for every pair.
/// </summary>
/// <remarks>
/// Bepu asks this for every pair its broad phase finds and every contact it generates, so it is a
/// struct with nothing in it that allocates. Per-body materials would be a table read here, keyed
/// by the two handles, which is where they go when they are wanted.
/// </remarks>
internal struct ContactCallbacks : INarrowPhaseCallbacks
{
    public SpringSettings Spring;
    public float Friction;
    public float MaxRecoveryVelocity;

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
        material.FrictionCoefficient = Friction;
        material.MaximumRecoveryVelocity = MaxRecoveryVelocity;
        material.SpringSettings = Spring;
        return true;
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
