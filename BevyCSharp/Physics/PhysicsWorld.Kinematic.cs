using System.Numerics;
using BepuPhysics;

namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>
    /// What a kinematic body follows of its entity, so what rests on it keeps the entity's pace at
    /// every frame rate.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An entity a game moves in its update moves once a frame, and a frame holds as many fixed
    /// steps as its time comes to, none, one or several. Aiming each step at the entity's place gave
    /// the body the whole frame's distance in the first step and nothing in the rest, and friction is
    /// too weak to follow such jumps, so a crate on a platform moved at 2 rode at 1.80 at 144 frames
    /// a second, 2.13 at 60 and 0.02 at 30. Instead, the entity's place is observed once the frame's
    /// update has moved it (<see cref="Observe"/>), its velocity is that move over the frame's time,
    /// and each step aims at the observed place moved on by that velocity for the time from the
    /// observation to the end of the step. The steps run behind the frame's clock by what the fixed
    /// clock has not yet stepped through, so that time starts below zero, and a step that ends before
    /// the moment observed aims behind the place observed. The body then moves at the entity's speed
    /// through every step however the frames fall, and turns at its rate of turning, so a turning
    /// platform carries what is on it round. An entity that stops is reached in the next step.
    /// </para>
    /// <para>
    /// An entity moved in the steps themselves, by an <c>[OnFixedUpdate]</c> behavior, has not moved
    /// since the last step when the frame ends, and is followed a step at a time, each step aiming at
    /// its place, which it reaches exactly. An entity said to be placed (<see cref="MarkPlaced"/>),
    /// or one that jumps further than <see cref="PhysicsSettings.PlaceBeyond"/>, has its body put
    /// there at rest rather than moved there through whatever is between.
    /// </para>
    /// <para>
    /// A body is moved by the velocity that brings it to its aim by the end of the step and is never
    /// put anywhere, so what it meets on the way is pushed at the speed it moved and Bepu sees no
    /// jump. 3DEngine does the same for a body under a moving parent, in its <c>ParentFollowers</c>,
    /// where this was measured first.
    /// </para>
    /// </remarks>
    private sealed class Follower
    {
        // The entity's place and turn as last observed at a frame's end.
        public Vector3 Place;
        public Quaternion Turn = Quaternion.Identity;

        // Where the entity was at the last step or observation, which says whether it moved since.
        public Vector3 SteppedPlace;
        public Quaternion SteppedTurn = Quaternion.Identity;

        // The entity's velocity and spin over the frame observed, and the seconds from the moment
        // observed to the end of the last step, below zero while the steps are behind it.
        public Vector3 Velocity;
        public Vector3 Spin;
        public double Since;

        // Whether the entity moves once a frame and is followed at its velocity, or is aimed at.
        public bool ByFrame;
    }

    private readonly Dictionary<Entity, Follower> _followers = [];

    /// <summary>The kinematic bodies' entities a game said it placed since the last frame ended.</summary>
    private readonly HashSet<Entity> _placed = [];

    /// <summary>How far an entity can go in a frame or a step and have its body moved after it.</summary>
    private readonly float _placeBeyond;

    /// <summary>Whether any kinematic body follows an entity, so a frame with none observes nothing.</summary>
    internal bool Follows => _followers.Count > 0;

    /// <summary>
    /// Says that <paramref name="entity"/> was put where it is rather than moved there, as when a
    /// level starts again, so its kinematic body is put at the new place, at rest, and not carried
    /// there through whatever is between.
    /// </summary>
    /// <remarks>
    /// A kinematic body otherwise follows its entity however far the entity moves, up to
    /// <see cref="PhysicsSettings.PlaceBeyond"/>, so a platform put back to its start a few units
    /// away would sweep through what lies on the way and push it. Said in the frame the entity is
    /// put, before or after putting it, and of nothing but an entity with a kinematic body.
    /// </remarks>
    public void MarkPlaced(Entity entity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_followers.ContainsKey(entity)) _placed.Add(entity);
    }

    /// <summary>Starts following a kinematic body's entity from where the body was added.</summary>
    private void BeginFollowing(Entity entity, Transform at)
    {
        var (place, turn) = (ToBepu(at.Translation), ToBepu(at.Rotation));
        _followers[entity] = new Follower { Place = place, Turn = turn, SteppedPlace = place, SteppedTurn = turn };
    }

    /// <summary>Stops following an entity whose body went.</summary>
    private void StopFollowing(Entity entity)
    {
        _followers.Remove(entity);
        _placed.Remove(entity);
    }

    /// <summary>
    /// Observes each kinematic body's entity once the frame's update has moved it,
    /// <paramref name="frameSeconds"/> after the frame before, with the steps
    /// <paramref name="behind"/> the moment observed by the time the fixed clock has not yet stepped
    /// through.
    /// </summary>
    /// <remarks>
    /// <see cref="PhysicsPlugin"/> calls this at the end of every frame. A game calling
    /// <see cref="Step"/> on its own schedule need not, and its kinematic bodies are then aimed a
    /// step at a time at where their entities are.
    /// </remarks>
    internal void Observe(EcsWorld ecs, double frameSeconds, double behind)
    {
        foreach (var (entity, follower) in _followers)
        {
            // An entity gone, or with no transform to follow, is left to the next step.
            if (!ecs.TryGet<Transform>(entity, out var transform)) continue;

            var (place, turn) = (ToBepu(transform.Translation), ToBepu(transform.Rotation));
            var moved = place != follower.SteppedPlace || turn != follower.SteppedTurn;

            if (moved && (_placed.Contains(entity) || Vector3.Distance(place, follower.Place) > _placeBeyond))
            {
                Put(entity, follower, place, turn);
            }
            else if (moved && frameSeconds > 0)
            {
                var seconds = (float)frameSeconds;
                follower.Velocity = (place - follower.Place) / seconds;
                follower.Spin = SpinBetween(follower.Turn, turn, seconds);
                follower.ByFrame = true;
            }
            else
            {
                // Still, or moved in the steps themselves, and aimed at where it is.
                (follower.Velocity, follower.Spin, follower.ByFrame) = (Vector3.Zero, Vector3.Zero, false);
            }

            (follower.Place, follower.Turn, follower.Since) = (place, turn, -behind);
            (follower.SteppedPlace, follower.SteppedTurn) = (place, turn);
        }

        _placed.Clear();
    }

    /// <summary>
    /// Moves a kinematic body over a step of <paramref name="seconds"/> toward where its entity is
    /// to be.
    /// </summary>
    private void Follow(Entity entity, Transform transform, float seconds)
    {
        var follower = _followers[entity];
        var (place, turn) = (ToBepu(transform.Translation), ToBepu(transform.Rotation));
        var moved = place != follower.SteppedPlace || turn != follower.SteppedTurn;

        if (moved && (_placed.Remove(entity) || Vector3.Distance(place, follower.SteppedPlace) > _placeBeyond))
        {
            Put(entity, follower, place, turn);
        }
        else if (follower.ByFrame)
        {
            follower.Since += seconds;
            var ahead = (float)follower.Since;
            var aim = follower.Place + follower.Velocity * ahead;
            Toward(entity, aim, Turned(follower.Turn, follower.Spin * ahead), seconds);
        }
        else
        {
            Toward(entity, place, turn, seconds);
        }

        (follower.SteppedPlace, follower.SteppedTurn) = (place, turn);
    }

    /// <summary>
    /// Gives a kinematic body the velocity and spin that bring its entity's origin to a pose by the
    /// end of the step.
    /// </summary>
    private void Toward(Entity entity, Vector3 place, Quaternion turn, float seconds)
    {
        var body = _bodies[entity];
        var reference = _simulation.Bodies[body.Moving];

        // The body turns about its own center, which for a hull is not the entity's origin, so the
        // center is aimed where it is to be once the body has turned.
        var center = place + Vector3.Transform(body.Center, turn);
        reference.Velocity.Linear = (center - reference.Pose.Position) / seconds;
        reference.Velocity.Angular = SpinBetween(reference.Pose.Orientation, turn, seconds);
        reference.Awake = true;
    }

    /// <summary>Puts a kinematic body where its entity is, at rest, and follows the entity from there.</summary>
    private void Put(Entity entity, Follower follower, Vector3 place, Quaternion turn)
    {
        var body = _bodies[entity];
        var reference = _simulation.Bodies[body.Moving];
        reference.Pose = new RigidPose(place + Vector3.Transform(body.Center, turn), turn);
        reference.Velocity = default;
        reference.Awake = true;

        (follower.Place, follower.Turn) = (place, turn);
        (follower.Velocity, follower.Spin, follower.ByFrame) = (Vector3.Zero, Vector3.Zero, false);
    }

    /// <summary>
    /// The spin, an axis scaled by radians a second, that turns <paramref name="from"/> to
    /// <paramref name="to"/> in <paramref name="seconds"/>, the short way round.
    /// </summary>
    private static Vector3 SpinBetween(Quaternion from, Quaternion to, float seconds)
    {
        var turn = Quaternion.Normalize(to * Quaternion.Conjugate(from));
        if (turn.W < 0f) turn = -turn;

        var half = MathF.Acos(Math.Clamp(turn.W, -1f, 1f));
        var sin = MathF.Sin(half);
        return sin > 1e-6f ? new Vector3(turn.X, turn.Y, turn.Z) / sin * (2f * half / seconds) : Vector3.Zero;
    }

    /// <summary><paramref name="turn"/> turned on by <paramref name="spin"/>, an axis scaled by radians.</summary>
    private static Quaternion Turned(Quaternion turn, Vector3 spin)
    {
        var angle = spin.Length();
        return angle > 1e-6f ? Quaternion.Normalize(Quaternion.CreateFromAxisAngle(spin / angle, angle) * turn) : turn;
    }
}
