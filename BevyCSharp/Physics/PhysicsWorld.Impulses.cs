using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints.Contact;

namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>
    /// The last impulse of each pair asked about, by its two collidables, which a pair asleep since is
    /// answered with, forgotten for a body that goes, whose collidable is given out again.
    /// </summary>
    private readonly Dictionary<(uint A, uint B), float> _impulses = [];

    /// <summary>
    /// The push the last step gave two touching bodies along the normals of their contacts, in mass
    /// times units a second, or zero for a pair not touching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It says how hard a pair presses, as a crate's weight on a pressure plate, where a contact's
    /// <see cref="ContactStarted.Speed"/> says how fast they met. Divided by the step it is the force
    /// between them, so a box on the floor presses it by its mass times gravity times the step. The
    /// friction along the surface and the twist about the normal are left out, so a box dragged and
    /// turned across the floor presses it by its weight as one at rest does.
    /// </para>
    /// <para>
    /// A sleeping pair's contact is kept with its island rather than where awake pairs are, and
    /// nothing between them changes while it sleeps, so a pair asked about before it slept goes on
    /// being answered with what it was then, as a crate long at rest on a plate does. One asked about
    /// first while asleep is woken by the question and answered from its next step.
    /// </para>
    /// </remarks>
    public float ContactImpulse(Entity a, Entity b)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_bodies.TryGetValue(a, out var first) || !_bodies.TryGetValue(b, out var second)) return 0f;

        var (ra, rb) = (Collidable(first), Collidable(second));
        var key = ContactLog.Pair(ra.Packed, rb.Packed);

        ref var mapping = ref _simulation.NarrowPhase.PairCache.Mapping;
        var pair = new CollidablePair(ra, rb);
        var found = mapping.TryGetValue(ref pair, out var cache);
        if (!found)
        {
            pair = new CollidablePair(rb, ra);
            found = mapping.TryGetValue(ref pair, out cache);
        }

        if (found && _simulation.Solver.ConstraintExists(cache.ConstraintHandle))
        {
            // The sum of each contact's push along its normal, the friction along the surface and
            // the twist about the normal left out, being other numbers in other units, which gave a
            // crate dragged and turned across a plate more than twice its weight.
            var sum = new PushSum();
            if (_simulation.NarrowPhase.TryExtractSolverContactData(cache.ConstraintHandle, ref sum)) return _impulses[key] = sum.Total;
        }

        var sleeping = (first.Kind != BodyKind.Static && !_simulation.Bodies[first.Moving].Awake)
                       || (second.Kind != BodyKind.Static && !_simulation.Bodies[second.Moving].Awake);
        if (!sleeping)
        {
            _impulses.Remove(key);
            return 0f;
        }

        if (_impulses.TryGetValue(key, out var rested)) return rested;

        foreach (var body in new[] { first, second })
        {
            if (body.Kind != BodyKind.Static) Wake(_simulation.Bodies[body.Moving]);
        }

        return 0f;
    }

    // Forgets what a body's pairs pressed, as the body goes.
    private void ForgetImpulses(uint packed)
    {
        if (_impulses.Count == 0) return;
        foreach (var key in _impulses.Keys.Where(key => key.A == packed || key.B == packed).ToArray()) _impulses.Remove(key);
    }

    private static CollidableReference Collidable(Body body) => body.Kind switch
    {
        BodyKind.Static => new CollidableReference(body.Fixed),
        BodyKind.Kinematic => new CollidableReference(CollidableMobility.Kinematic, body.Moving),
        _ => new CollidableReference(CollidableMobility.Dynamic, body.Moving),
    };

    // Adds a contact constraint's pushes, which Bepu keeps in the first lane of each of its wide
    // numbers for the one constraint it hands over.
    private struct PushSum : ISolverContactDataExtractor
    {
        public float Total;

        public void ConvexOneBody<TPrestep, TAccumulatedImpulses>(BodyHandle a, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, IConvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses> => AddConvex(ref impulses);

        public void ConvexTwoBody<TPrestep, TAccumulatedImpulses>(BodyHandle a, BodyHandle b, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, ITwoBodyConvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, IConvexContactAccumulatedImpulses<TAccumulatedImpulses> => AddConvex(ref impulses);

        public void NonconvexOneBody<TPrestep, TAccumulatedImpulses>(BodyHandle a, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, INonconvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses> => AddNonconvex(ref impulses);

        public void NonconvexTwoBody<TPrestep, TAccumulatedImpulses>(BodyHandle a, BodyHandle b, ref TPrestep prestep, ref TAccumulatedImpulses impulses)
            where TPrestep : struct, ITwoBodyNonconvexContactPrestep<TPrestep>
            where TAccumulatedImpulses : struct, INonconvexContactAccumulatedImpulses<TAccumulatedImpulses> => AddNonconvex(ref impulses);

        private void AddConvex<T>(ref T impulses)
            where T : struct, IConvexContactAccumulatedImpulses<T>
        {
            for (var i = 0; i < T.ContactCount; i++) Total += T.GetPenetrationImpulseForContact(ref impulses, i)[0];
        }

        private void AddNonconvex<T>(ref T impulses)
            where T : struct, INonconvexContactAccumulatedImpulses<T>
        {
            for (var i = 0; i < T.ContactCount; i++) Total += T.GetImpulsesForContact(ref impulses, i).Penetration[0];
        }
    }
}
