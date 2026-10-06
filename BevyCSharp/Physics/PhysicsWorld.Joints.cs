using System.Numerics;
using BepuPhysics;
using BepuPhysics.Constraints;

namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>
    /// Joins two bodies with a joint, which holds from the next step on.
    /// </summary>
    /// <remarks>
    /// Both bodies have to move, dynamic or kinematic, because a joint is solved between two
    /// velocities. To pin a body to the world, join it to a kinematic body that stays where it is.
    /// A joint goes when either of its bodies does. Two bodies a joint holds do not collide with
    /// each other, as in most engines, since a hinge's pin passing through its wheel is usual and
    /// the contact between them would hold the wheel still.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">Either entity has no body that moves.</exception>
    public JointHandle Connect(Entity a, Entity b, Joint joint)
    {
        var first = Moving(a);
        var second = Moving(b);
        var spring = new SpringSettings(30f, 1f);

        var constraint = joint.Kind switch
        {
            1 => _simulation.Solver.Add(first.Handle, second.Handle, new Hinge
            {
                LocalOffsetA = ToBepu(joint.AnchorA),
                LocalOffsetB = ToBepu(joint.AnchorB),
                LocalHingeAxisA = Vector3.Normalize(ToBepu(joint.AxisA)),
                LocalHingeAxisB = Vector3.Normalize(ToBepu(joint.AxisB)),
                SpringSettings = spring,
            }),
            2 => _simulation.Solver.Add(first.Handle, second.Handle, new Weld
            {
                // Where the second is, and how it is turned, as the first sees it now.
                LocalOffset = Vector3.Transform(second.Pose.Position - first.Pose.Position, Quaternion.Conjugate(first.Pose.Orientation)),
                LocalOrientation = Quaternion.Concatenate(second.Pose.Orientation, Quaternion.Conjugate(first.Pose.Orientation)),
                SpringSettings = spring,
            }),
            3 => _simulation.Solver.Add(first.Handle, second.Handle, new DistanceLimit(
                ToBepu(joint.AnchorA), ToBepu(joint.AnchorB), joint.Minimum, joint.Maximum, spring)),
            _ => _simulation.Solver.Add(first.Handle, second.Handle, new BallSocket
            {
                LocalOffsetA = ToBepu(joint.AnchorA),
                LocalOffsetB = ToBepu(joint.AnchorB),
                SpringSettings = spring,
            }),
        };

        Wake(first);
        Wake(second);

        var id = ++_nextJoint;
        var constraints = new List<ConstraintHandle> { constraint };

        if (joint.Motor is { } motor)
        {
            var axis = Vector3.Normalize(ToBepu(joint.AxisA));
            var driven = _simulation.Solver.Add(first.Handle, second.Handle, new AngularAxisMotor
            {
                LocalAxisA = axis,
                // Bepu's target is the first body's turn against the second, the other way round
                // from how a speed is given here.
                TargetVelocity = -motor.Speed * MathF.PI / 180f,
                Settings = new MotorSettings(motor.Torque, 1e-4f),
            });

            constraints.Add(driven);
            _motors[id] = (driven, axis);
        }

        if (joint.Limits is { } limits)
        {
            // A basis on each body whose Z is the hinge's axis, the second's chosen so the two
            // agree as the bodies are turned now, which makes the angle the joint starts at zero.
            var basisA = Toward(Vector3.Normalize(ToBepu(joint.AxisA)));
            var world = Quaternion.Concatenate(basisA, first.Pose.Orientation);
            var basisB = Quaternion.Concatenate(world, Quaternion.Conjugate(second.Pose.Orientation));

            constraints.Add(_simulation.Solver.Add(first.Handle, second.Handle, new TwistLimit
            {
                LocalBasisA = basisA,
                LocalBasisB = Quaternion.Normalize(basisB),
                MinimumAngle = limits.Lowest * MathF.PI / 180f,
                MaximumAngle = limits.Highest * MathF.PI / 180f,
                SpringSettings = spring,
            }));
        }

        _joints[id] = ([.. constraints], a, b);

        var pair = ContactLog.Pair(Packed(_bodies[a]), Packed(_bodies[b]));
        _contacts.Joined[pair] = _contacts.Joined.GetValueOrDefault(pair) + 1;

        return new JointHandle(id);
    }

    /// <summary>
    /// Changes a hinge's motor while it runs, to open a door on command or stop a fan.
    /// </summary>
    /// <param name="joint">A hinge made with <see cref="Joint.WithMotor"/>.</param>
    /// <param name="degreesPerSecond">How fast it turns from the next step, zero holding it where it is.</param>
    /// <param name="torque">The most it pushes with.</param>
    /// <returns>Whether the joint has a motor to change.</returns>
    public bool SetMotor(JointHandle joint, float degreesPerSecond, float torque)
    {
        if (_disposed || !_motors.TryGetValue(joint.Id, out var motor) || !_joints.TryGetValue(joint.Id, out var held)) return false;

        _simulation.Solver.ApplyDescription(motor.Constraint, new AngularAxisMotor
        {
            LocalAxisA = motor.Axis,
            TargetVelocity = -degreesPerSecond * MathF.PI / 180f,
            Settings = new MotorSettings(Math.Max(0f, torque), 1e-4f),
        });

        // Both woken, since a motor told to turn a sleeping door has to wake it to.
        foreach (var entity in new[] { held.A, held.B })
        {
            if (_bodies.TryGetValue(entity, out var body) && body.Kind != BodyKind.Static)
            {
                Wake(_simulation.Bodies[body.Moving]);
            }
        }

        return true;
    }

    /// <summary>The turn that takes Z onto a direction, the shortest one.</summary>
    private static Quaternion Toward(Vector3 direction)
    {
        var dot = Vector3.Dot(Vector3.UnitZ, direction);
        if (dot > 0.9999f) return Quaternion.Identity;
        if (dot < -0.9999f) return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);

        var axis = Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, direction));
        return Quaternion.CreateFromAxisAngle(axis, MathF.Acos(dot));
    }

    /// <summary>Takes a joint away, leaving both bodies free.</summary>
    /// <returns>Whether the joint was there.</returns>
    public bool Disconnect(JointHandle joint)
    {
        if (_disposed || !_joints.Remove(joint.Id, out var held)) return false;

        foreach (var constraint in held.Constraints) _simulation.Solver.Remove(constraint);
        _motors.Remove(joint.Id);

        // Colliding again once nothing holds them, the bodies still being there.
        if (_bodies.TryGetValue(held.A, out var first) && _bodies.TryGetValue(held.B, out var second))
        {
            var pair = ContactLog.Pair(Packed(first), Packed(second));
            if (_contacts.Joined.TryGetValue(pair, out var count) && count > 1) _contacts.Joined[pair] = count - 1;
            else _contacts.Joined.Remove(pair);
        }

        foreach (var entity in new[] { held.A, held.B })
        {
            if (_bodies.TryGetValue(entity, out var body) && body.Kind != BodyKind.Static)
            {
                Wake(_simulation.Bodies[body.Moving]);
            }
        }

        return true;
    }
}
