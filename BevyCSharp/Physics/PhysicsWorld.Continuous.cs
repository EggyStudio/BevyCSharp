using BepuPhysics;
using BepuPhysics.Collidables;

namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>
    /// Sweeps an entity's body over each step to find what it would meet within it, for a body fast
    /// enough to cross a thin wall in one step, as a shot or a ball struck hard is, or stops sweeping
    /// it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A body otherwise meets what is within a tenth of a unit of it at the start of a step, so a ball
    /// of 40 units a second crosses a wall a fifth of a unit thick within one. A swept body's contacts
    /// reach as far as it moves, which costs a sweep test for each pair it nears, so it is for the few
    /// bodies a game knows are fast.
    /// </para>
    /// <para>
    /// A contact stops a body over a step rather than at once, so a wall has to be thicker the faster
    /// the body, a fifth of a unit at 100 units a second and half a unit at 300, as 3DEngine measured
    /// it with a ball a tenth of a unit across. A shot faster than that is a ray cast each frame
    /// (<see cref="Raycast(Vec3, Vec3, float, Entity)"/>) rather than a body. A static never moves, so
    /// it is left as it is, and the sweep is the body's, so one made again is not swept unless asked,
    /// or unless it is made from a <see cref="RigidBody"/> whose <see cref="RigidBody.Continuous"/> is set.
    /// </para>
    /// </remarks>
    /// <exception cref="KeyNotFoundException">The entity has no body.</exception>
    public void SetContinuous(Entity entity, bool continuous)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var body = _bodies[entity];
        if (body.Kind == BodyKind.Static) return;

        var reference = _simulation.Bodies[body.Moving];
        reference.Collidable.Continuity = continuous ? ContinuousDetection.Continuous(1e-3f, 1e-3f) : ContinuousDetection.Passive;
        reference.Collidable.MaximumSpeculativeMargin = continuous ? float.MaxValue : SpeculativeMargin;
    }

    /// <summary>Whether an entity's body is swept over each step.</summary>
    /// <exception cref="KeyNotFoundException">The entity has no body.</exception>
    public bool IsContinuous(Entity entity)
    {
        var body = _bodies[entity];
        return body.Kind != BodyKind.Static && _simulation.Bodies[body.Moving].Collidable.Continuity.Mode == ContinuousDetectionMode.Continuous;
    }
}
