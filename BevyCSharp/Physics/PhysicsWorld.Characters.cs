using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>What the step needs of a character's body that its components do not say.</summary>
    /// <param name="Radius">How far its side is from its middle.</param>
    /// <param name="HalfHeight">How far its foot is below its middle.</param>
    private sealed record Character(float Radius, float HalfHeight);

    /// <summary>The bodies that are characters, made so from a <see cref="CharacterController"/>.</summary>
    private readonly Dictionary<Entity, Character> _characters = [];

    /// <summary>The settings' gravity, which a character on a slope has taken out of its velocity.</summary>
    private Vector3 _gravity;

    /// <summary>
    /// How fast a character off the ground turns its velocity toward the one asked for, in units a
    /// second squared, which is slow enough that a jump carries on the way it began and quick
    /// enough to steer a landing.
    /// </summary>
    private const float AirAcceleration = 20f;

    /// <summary>How far below its foot a character still counts as standing, for a step down a slope.</summary>
    private const float GroundReach = 0.08f;

    /// <summary>
    /// Where the ground probes start across a character's foot, as fractions of the spread, its
    /// middle and four points around it, so an edge under one side of the foot still holds it.
    /// </summary>
    private static readonly Vector3[] ProbeDirections = [Vector3.Zero, Vector3.UnitX, -Vector3.UnitX, Vector3.UnitZ, -Vector3.UnitZ];

    /// <summary>Whether an entity's body is a character's.</summary>
    /// <remarks>
    /// A body becomes one at the sync after a <see cref="CharacterController"/> is put beside a
    /// dynamic <see cref="RigidBody"/>, and stops being one at the sync after the controller is
    /// taken away, so this says what the next step walks rather than what the components say.
    /// </remarks>
    public bool IsCharacter(Entity entity) => _characters.ContainsKey(entity);

    /// <summary>
    /// Makes a dynamic body that was made from components a character: upright, unturnable,
    /// frictionless and never asleep.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Upright whatever its entity's rotation, since a character is a capsule standing along Y
    /// and the rotation is the game's, to face the way it walks. Unturnable, by an inertia that
    /// no push turns, so walking into a corner does not tip it over. Frictionless, so a wall met
    /// at an angle is slid along rather than held to, which is the step's job and not the
    /// contact's. Never asleep, since a character standing still is asked to walk at any moment
    /// and a body asleep does not move until something wakes it.
    /// </para>
    /// <para>
    /// The radius and the height are the collider's, as fitted, so a box or a ball made a
    /// character is probed as the box around it.
    /// </para>
    /// </remarks>
    private void MakeCharacter(Entity entity, ColliderFit fit, Transform at)
    {
        var body = _bodies[entity];
        var reference = _simulation.Bodies[body.Moving];

        reference.SetLocalInertia(new BodyInertia { InverseMass = reference.LocalInertia.InverseMass });
        reference.Pose = new RigidPose(ToBepu(at.Translation) + body.Center, Quaternion.Identity);
        reference.Velocity.Angular = Vector3.Zero;
        reference.Activity.SleepThreshold = -1f;

        _contacts.Materials[Packed(body)] = new PhysicsMaterial(0f, 0f);
        _characters[entity] = new Character(MathF.Max(fit.Size.X, fit.Size.Z) * 0.5f, fit.Size.Y * 0.5f);
    }

    /// <summary>
    /// Before a step of <paramref name="seconds"/>, finds each character's ground and sets its
    /// velocity to walk where its <see cref="CharacterController"/> asks, along that ground.
    /// </summary>
    /// <remarks>
    /// <para>
    /// On the ground it walks along the ground's surface, so a walk up or down a slope follows it
    /// rather than leaving it, and the step's pull of gravity along the slope is taken out
    /// beforehand, since the integrator adds it during the step and a character standing still on
    /// a slope would otherwise creep down it. Standing on something that moves, it walks relative
    /// to that, so a platform carries it.
    /// </para>
    /// <para>
    /// In the air, or against ground too steep to stand on, it steers toward the velocity asked
    /// for at a limited rate, and asked for nothing it keeps its own motion, so a steep slope
    /// slides it down and a jump carries on.
    /// </para>
    /// <para>
    /// What it found is written back to the component only where it differs, so a character
    /// standing still on flat ground marks nothing changed.
    /// </para>
    /// </remarks>
    private void MoveCharacters(EcsWorld ecs, float seconds)
    {
        if (_characters.Count == 0) return;

        foreach (var (entity, character) in _characters)
        {
            if (!_bodies.TryGetValue(entity, out var body) || !ecs.TryGet<CharacterController>(entity, out var controller)) continue;

            var reference = _simulation.Bodies[body.Moving];
            var self = Packed(body);
            var center = reference.Pose.Position;

            var steepest = controller.MaxSlope > 0f ? Math.Clamp(controller.MaxSlope, 0f, 89f) : 45f;
            var flatEnough = MathF.Cos(float.DegreesToRadians(steepest));
            var asked = ToBepu(controller.Move) with { Y = 0f };

            // Five rays down from inside the body, at its middle and around its foot, the nearest
            // ground no steeper than its steepest winning. A ray at the side starts above the round
            // of the foot, so it is shortened by as much as the foot curves up there.
            var grounded = false;
            var normal = Vector3.UnitY;
            var groundVelocity = Vector3.Zero;
            var nearest = float.MaxValue;
            var spread = character.Radius * 0.7f;

            foreach (var around in ProbeDirections)
            {
                var offset = around * spread;
                var rise = character.Radius - MathF.Sqrt(MathF.Max(0f, character.Radius * character.Radius - offset.LengthSquared()));
                var length = character.HalfHeight + GroundReach - rise;

                if (!Cast(center + offset, -Vector3.UnitY, length, self, out var hit) || hit.Normal.Y < flatEnough) continue;
                if (hit.T >= nearest) continue;

                nearest = hit.T;
                grounded = true;
                normal = hit.Normal;
                groundVelocity = VelocityAt(hit.Collidable, center + offset - (Vector3.UnitY * hit.T));
            }

            var velocity = reference.Velocity.Linear;
            if (grounded)
            {
                var stepHeight = controller.StepHeight > 0f ? controller.StepHeight : character.Radius;
                ClimbStep(ref reference, character, self, asked, stepHeight, flatEnough);

                var along = asked - (Vector3.Dot(asked, normal) * normal);
                if (along != Vector3.Zero) along = Vector3.Normalize(along) * asked.Length();

                var pull = _gravity - (Vector3.Dot(_gravity, normal) * normal);

                // What it walks at, on what it stands on, less the pull along the slope, and with
                // any speed it has away from the ground kept, so it can leave it but not sink.
                velocity = groundVelocity + along - (pull * seconds) + (MathF.Min(0f, Vector3.Dot(velocity - groundVelocity, normal)) * normal);

                if (controller.Jump > 0f)
                {
                    velocity.Y = controller.Jump;
                    grounded = false;
                }
            }
            else if (asked != Vector3.Zero)
            {
                var change = asked - (velocity with { Y = 0f });
                var most = AirAcceleration * seconds;
                if (change.Length() > most) change = Vector3.Normalize(change) * most;
                velocity += change;
            }

            reference.Velocity.Linear = velocity;
            reference.Velocity.Angular = Vector3.Zero;
            reference.Awake = true;

            var groundNormal = FromBepu(normal);
            if (controller.Jump == 0f && controller.Grounded == grounded && controller.GroundNormal == groundNormal) continue;

            controller.Jump = 0f;
            controller.Grounded = grounded;
            controller.GroundNormal = groundNormal;
            ecs.Set(entity, controller);
        }
    }

    /// <summary>
    /// Lifts a grounded character walking into a ledge no higher than a step onto it, where there
    /// is room for it above, so it walks up stairs rather than stopping at each.
    /// </summary>
    private void ClimbStep(ref BodyReference reference, Character character, uint self, Vector3 asked, float stepHeight, float flatEnough)
    {
        if (asked.LengthSquared() < 1e-6f) return;

        var way = Vector3.Normalize(asked);
        var center = reference.Pose.Position;
        var feet = center - new Vector3(0f, character.HalfHeight, 0f);

        // Something too steep to stand on, close ahead at the foot.
        if (!Cast(feet + new Vector3(0f, 0.02f, 0f), way, character.Radius + 0.15f, self, out var wall) || wall.Normal.Y >= flatEnough) return;

        // Its top, found from above, no higher than a step and flat enough to stand on.
        var above = feet + (way * (wall.T + 0.05f)) + new Vector3(0f, stepHeight + 0.02f, 0f);
        if (!Cast(above, -Vector3.UnitY, stepHeight + 0.02f, self, out var top) || top.Normal.Y < flatEnough) return;

        var rise = above.Y - top.T - feet.Y;
        if (rise <= 0.01f || rise > stepHeight) return;

        // Room for its head that much higher.
        if (Cast(center, Vector3.UnitY, character.HalfHeight + rise, self, out _)) return;

        reference.Pose.Position = center + new Vector3(0f, rise + 0.01f, 0f);
    }

    /// <summary>How fast a point of what a ray met moves, from its linear and angular velocity, or nothing for a static.</summary>
    private Vector3 VelocityAt(CollidableReference collidable, Vector3 point)
    {
        if (collidable.Mobility == CollidableMobility.Static) return Vector3.Zero;

        var other = _simulation.Bodies[collidable.BodyHandle];
        return other.Velocity.Linear + Vector3.Cross(other.Velocity.Angular, point - other.Pose.Position);
    }

    /// <summary>
    /// The nearest solid thing a ray meets, other than the body <paramref name="self"/> names,
    /// passing through sensors, which a character neither stands on nor walks into.
    /// </summary>
    private bool Cast(Vector3 origin, Vector3 direction, float distance, uint self, out NearestHit hit)
    {
        hit = new NearestHit { T = float.MaxValue, Skipping = true, Skip = self, Sensors = _contacts.Sensors };
        _simulation.RayCast(origin, direction, distance, _pool, ref hit);
        if (hit.T == float.MaxValue) return false;

        hit.Normal = hit.Normal.LengthSquared() > 0f ? Vector3.Normalize(hit.Normal) : Vector3.UnitY;
        return true;
    }
}
