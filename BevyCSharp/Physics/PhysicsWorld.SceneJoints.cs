using System.Numerics;

namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>What a <see cref="JointBetween"/> asked for, and the joint made from it.</summary>
    /// <param name="Asked">The component as it was when last read.</param>
    /// <param name="Made">The joint, or nothing while its bodies are coming or where it was refused.</param>
    /// <param name="Refused">Whether it cannot be made as it is, which its next change tries again.</param>
    private readonly record struct SceneJoint(JointBetween Asked, JointHandle? Made, bool Refused);

    /// <summary>The joints entities describe, by the entity holding each.</summary>
    private readonly Dictionary<Entity, SceneJoint> _sceneJoints = [];

    /// <summary>
    /// The joint an entity's <see cref="JointBetween"/> made, for a game to drive its motor with
    /// <see cref="SetMotor"/> or <see cref="SetDrive"/>, or nothing while its bodies are still to
    /// come or where it could not be made.
    /// </summary>
    /// <remarks>
    /// A joint made again, as it is when the component or either body changes, has a handle of its
    /// own, so a game asks again rather than keeping the first.
    /// </remarks>
    public JointHandle? JointOf(Entity entity) =>
        _sceneJoints.TryGetValue(entity, out var joint) && joint.Made is { } made && _joints.ContainsKey(made.Id) ? made : null;

    /// <summary>
    /// Makes the joints entities describe whose bodies are made, again where they changed or a body
    /// was made again, and takes away those whose component or entity has gone.
    /// </summary>
    /// <remarks>
    /// After the bodies, so a level's joints and the bodies they join come in the same sync. The
    /// component changed since <paramref name="since"/> is listed by the world, and the joints
    /// already held are few, so each is asked after on its own.
    /// </remarks>
    private void SyncJoints(EcsWorld ecs, uint since)
    {
        var candidates = new HashSet<Entity>();
        var tick = since;
        foreach (var entity in ecs.ChangedSince<JointBetween>(ref tick)) candidates.Add(entity);

        if (_sceneJoints.Count > 0)
        {
            var held = _sceneJoints.Keys.ToArray();
            var present = new bool[held.Length];
            ecs.HasMany<JointBetween>(held, present);

            for (var i = 0; i < held.Length; i++)
            {
                var joint = _sceneJoints[held[i]];
                if (!present[i])
                {
                    if (joint.Made is { } made) Disconnect(made);
                    _sceneJoints.Remove(held[i]);
                    candidates.Remove(held[i]);
                    continue;
                }

                // Waiting for its bodies, or gone with a body made again since.
                if (!joint.Refused && (joint.Made is not { } alive || !_joints.ContainsKey(alive.Id))) candidates.Add(held[i]);
            }
        }

        foreach (var entity in candidates) MakeJoint(ecs, entity);
    }

    /// <summary>Makes an entity's joint from its component, again where the component changed.</summary>
    private void MakeJoint(EcsWorld ecs, Entity entity)
    {
        if (!ecs.TryGet<JointBetween>(entity, out var asked)) return;

        if (_sceneJoints.TryGetValue(entity, out var was))
        {
            var alive = was.Made is { } held && _joints.ContainsKey(held.Id);
            if (was.Asked.Equals(asked) && (alive || was.Refused)) return;
            if (was.Made is { } old) Disconnect(old);
        }

        if (!_bodies.TryGetValue(asked.A, out var first) || !_bodies.TryGetValue(asked.B, out var second))
        {
            // A body still to be made, as one waiting for its mesh is, comes at a later sync.
            if (Coming(ecs, asked.A) && Coming(ecs, asked.B))
            {
                _sceneJoints[entity] = new SceneJoint(asked, null, false);
                return;
            }

            Refuse(entity, asked, $"joins {asked.A} and {asked.B}, and one of them has no body and none coming");
            return;
        }

        if (first.Kind == BodyKind.Static || second.Kind == BodyKind.Static || asked.A == asked.B)
        {
            Refuse(entity, asked, "joins a static body, or a body to itself, where both have to move");
            return;
        }

        var (point, up) = Placed(ecs, entity);
        var a = _simulation.Bodies[first.Moving].Pose;
        var b = _simulation.Bodies[second.Moving].Pose;

        // The joint's point and axis in each body's own space, from its middle, as Joint takes them.
        var anchorA = FromBepu(Vector3.Transform(ToBepu(point) - a.Position, Quaternion.Conjugate(a.Orientation)));
        var anchorB = FromBepu(Vector3.Transform(ToBepu(point) - b.Position, Quaternion.Conjugate(b.Orientation)));
        var axisA = FromBepu(Vector3.Transform(ToBepu(up), Quaternion.Conjugate(a.Orientation)));
        var axisB = FromBepu(Vector3.Transform(ToBepu(up), Quaternion.Conjugate(b.Orientation)));

        try
        {
            var joint = asked.Kind switch
            {
                JointKind.Hinge => Hinge(asked, anchorA, axisA, anchorB, axisB),
                JointKind.Weld => Joint.Weld(),
                JointKind.Distance => Distance(asked, anchorA, Vector3.Distance(ToBepu(point), b.Position)),
                JointKind.Slider => Slider(asked, axisA),
                _ => Ball(asked, anchorA, anchorB, axisA),
            };

            _sceneJoints[entity] = new SceneJoint(asked, Connect(asked.A, asked.B, joint), false);
        }
        catch (ArgumentException refusal)
        {
            Refuse(entity, asked, refusal.Message);
        }
    }

    // Whether an entity will have a body once what it waits for comes, carrying both components.
    private static bool Coming(EcsWorld ecs, Entity entity) =>
        ecs.IsAlive(entity) && ecs.Has<RigidBody>(entity) && ecs.Has<Collider>(entity);

    // Remembers a joint that cannot be made as it is, said once, until its component changes.
    private void Refuse(Entity entity, JointBetween asked, string why)
    {
        _sceneJoints[entity] = new SceneJoint(asked, null, true);
        Log.Warn($"The joint on {entity} is not made, since it {why}.");
    }

    private static Joint Hinge(JointBetween asked, Vec3 anchorA, Vec3 axisA, Vec3 anchorB, Vec3 axisB)
    {
        var hinge = Joint.Hinge(anchorA, axisA, anchorB, axisB);
        if (asked.MinAngle != 0f || asked.MaxAngle != 0f) hinge = hinge.WithLimits(asked.MinAngle, asked.MaxAngle);
        if (asked.MotorTorque > 0f) hinge = hinge.WithMotor(asked.MotorSpeed, asked.MotorTorque);
        return hinge;
    }

    private static Joint Ball(JointBetween asked, Vec3 anchorA, Vec3 anchorB, Vec3 axisA)
    {
        var ball = Joint.Ball(anchorA, anchorB);
        if (asked.Swing <= 0f && asked.Twist <= 0f) return ball;

        // Zero for either leaves that way free, as 180 does.
        return ball.WithCone(axisA, asked.Swing > 0f ? asked.Swing : 180f, asked.Twist > 0f ? asked.Twist : 180f);
    }

    private static Joint Distance(JointBetween asked, Vec3 anchorA, float apart)
    {
        var maximum = asked.MaxDistance > 0f ? asked.MaxDistance : apart;
        return Joint.Distance(anchorA, Vec3.Zero, Math.Clamp(asked.MinDistance, 0f, maximum), maximum);
    }

    private static Joint Slider(JointBetween asked, Vec3 axisA)
    {
        var slider = Joint.Slider(axisA);
        if (asked.MinTravel != 0f || asked.MaxTravel != 0f) slider = slider.WithTravel(asked.MinTravel, asked.MaxTravel);
        if (asked.DriveForce > 0f) slider = slider.WithDrive(asked.DriveSpeed, asked.DriveForce);
        return slider;
    }

    /// <summary>
    /// Where an entity is in the world and which way its up direction points, from its transform
    /// and those of the entities above it.
    /// </summary>
    /// <remarks>
    /// Carried up through the parents rather than read from the entity's global transform, which a
    /// level spawned this frame has not had worked out yet.
    /// </remarks>
    private static (Vec3 Point, Vec3 Up) Placed(EcsWorld ecs, Entity entity)
    {
        var point = Vec3.Zero;
        var up = Vec3.UnitY;
        for (var at = entity; !at.IsNone; at = ecs.ParentOf(at))
        {
            var transform = ecs.TryGet<Transform>(at, out var placed) ? placed : Transform.Identity;
            point = transform.Translation + transform.Rotation * new Vec3(point.X * transform.Scale.X, point.Y * transform.Scale.Y, point.Z * transform.Scale.Z);
            up = transform.Rotation * new Vec3(up.X * transform.Scale.X, up.Y * transform.Scale.Y, up.Z * transform.Scale.Z);
        }

        return (point, up.Length > 1e-6f ? up.Normalized : Vec3.UnitY);
    }
}
