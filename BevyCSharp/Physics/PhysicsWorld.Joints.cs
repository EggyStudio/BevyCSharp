using System.Numerics;
using BepuPhysics;
using BepuPhysics.Constraints;

namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>
    /// Each slider's line, the second body's middle as a point fixed to the first and the axis in
    /// the first's space, and its drive where it has one, by the joint's number.
    /// </summary>
    private readonly Dictionary<int, (Vector3 OffsetA, Vector3 AxisA, ConstraintHandle? Drive)> _sliders = [];

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

        if (joint.Kind == 4) return ConnectSlider(a, b, first, second, joint, spring);

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

        if (joint.Cone is { } cone)
        {
            // The cone's middle as each body sees it now, and frames with their Z along it that agree
            // as the bodies are turned now, as the hinge's limit has.
            var axisA = Vector3.Normalize(ToBepu(cone.Axis));
            var world = Vector3.Transform(axisA, first.Pose.Orientation);
            var axisB = Vector3.Transform(world, Quaternion.Conjugate(second.Pose.Orientation));

            if (cone.Swing < 180f)
            {
                constraints.Add(_simulation.Solver.Add(first.Handle, second.Handle, new SwingLimit
                {
                    AxisLocalA = axisA,
                    AxisLocalB = axisB,
                    MaximumSwingAngle = cone.Swing * MathF.PI / 180f,
                    SpringSettings = spring,
                }));
            }

            if (cone.Twist < 180f)
            {
                var basisA = Toward(axisA);
                var basisB = Quaternion.Concatenate(Quaternion.Concatenate(basisA, first.Pose.Orientation), Quaternion.Conjugate(second.Pose.Orientation));
                constraints.Add(_simulation.Solver.Add(first.Handle, second.Handle, new TwistLimit
                {
                    LocalBasisA = basisA,
                    LocalBasisB = Quaternion.Normalize(basisB),
                    MinimumAngle = -cone.Twist * MathF.PI / 180f,
                    MaximumAngle = cone.Twist * MathF.PI / 180f,
                    SpringSettings = spring,
                }));
            }
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

    /// <summary>
    /// Changes how far apart a distance joint keeps its points, as a winch reeling a rope in does when
    /// it is set a little shorter each frame.
    /// </summary>
    /// <param name="joint">A joint made with <see cref="Joint.Distance"/>.</param>
    /// <param name="minimum">The nearest the points come, from the next step.</param>
    /// <param name="maximum">The furthest they go apart.</param>
    /// <returns>Whether the joint is a distance joint to change.</returns>
    /// <exception cref="ArgumentException">The minimum is negative or above the maximum.</exception>
    public bool SetDistance(JointHandle joint, float minimum, float maximum)
    {
        if (minimum < 0f || maximum < minimum) throw new ArgumentException("A distance joint's minimum is zero or more and no more than its maximum.");
        if (_disposed || !_joints.TryGetValue(joint.Id, out var held)) return false;

        var handle = held.Constraints[0];
        if (_simulation.Solver.GetConstraintReference(handle).TypeBatch.TypeId != DistanceLimit.ConstraintTypeId) return false;

        _simulation.Solver.GetDescription(handle, out DistanceLimit limit);
        (limit.MinimumDistance, limit.MaximumDistance) = (minimum, maximum);
        _simulation.Solver.ApplyDescription(handle, limit);

        // A sleeping pair is woken, so it holds to the new range.
        _simulation.Awakener.AwakenConstraint(handle);
        return true;
    }

    /// <summary>
    /// Joins two bodies as a slider, kept on a line through the second's middle and kept from turning
    /// against each other, with its travel and drive where given.
    /// </summary>
    private JointHandle ConnectSlider(Entity a, Entity b, BodyReference first, BodyReference second, Joint joint, SpringSettings spring)
    {
        var axisA = Vector3.Normalize(ToBepu(joint.AxisA));
        var offsetA = Vector3.Transform(second.Pose.Position - first.Pose.Position, Quaternion.Conjugate(first.Pose.Orientation));

        var constraints = new List<ConstraintHandle>
        {
            _simulation.Solver.Add(first.Handle, second.Handle, new PointOnLineServo
            {
                LocalOffsetA = offsetA,
                LocalOffsetB = Vector3.Zero,
                LocalDirection = axisA,
                ServoSettings = ServoSettings.Default,
                SpringSettings = spring,
            }),
            _simulation.Solver.Add(first.Handle, second.Handle, new AngularServo
            {
                TargetRelativeRotationLocalA = Quaternion.Normalize(Quaternion.Conjugate(first.Pose.Orientation) * second.Pose.Orientation),
                ServoSettings = ServoSettings.Default,
                SpringSettings = spring,
            }),
        };

        if (joint.Travel is { } travel)
        {
            constraints.Add(_simulation.Solver.Add(first.Handle, second.Handle, new LinearAxisLimit
            {
                LocalOffsetA = offsetA,
                LocalOffsetB = Vector3.Zero,
                LocalAxis = axisA,
                MinimumOffset = travel.Minimum,
                MaximumOffset = travel.Maximum,
                SpringSettings = spring,
            }));
        }

        ConstraintHandle? drive = null;
        if (joint.Drive is { } driven)
        {
            drive = _simulation.Solver.Add(first.Handle, second.Handle, Drive(offsetA, axisA, driven.Speed, driven.Force));
            constraints.Add(drive.Value);
        }

        Wake(first);
        Wake(second);

        var id = ++_nextJoint;
        _joints[id] = ([.. constraints], a, b);
        _sliders[id] = (offsetA, axisA, drive);

        var pair = ContactLog.Pair(Packed(_bodies[a]), Packed(_bodies[b]));
        _contacts.Joined[pair] = _contacts.Joined.GetValueOrDefault(pair) + 1;
        return new JointHandle(id);
    }

    private static LinearAxisMotor Drive(Vector3 offsetA, Vector3 axisA, float speed, float force) => new()
    {
        LocalOffsetA = offsetA,
        LocalOffsetB = Vector3.Zero,
        LocalAxis = axisA,
        TargetVelocity = speed,
        Settings = new MotorSettings(Math.Max(0f, force), 1e-4f),
    };

    /// <summary>Changes a slider's drive while it runs, to send a lift up or hold it where it is.</summary>
    /// <param name="joint">A slider made with <see cref="Joint.WithDrive"/>.</param>
    /// <param name="unitsPerSecond">How fast it slides from the next step, toward the axis's tip for a positive speed, zero holding it.</param>
    /// <param name="force">The most it pushes with.</param>
    /// <returns>Whether the joint is a slider with a drive to change.</returns>
    public bool SetDrive(JointHandle joint, float unitsPerSecond, float force)
    {
        if (_disposed || !_sliders.TryGetValue(joint.Id, out var slider) || slider.Drive is not { } drive || !_joints.TryGetValue(joint.Id, out var held)) return false;

        _simulation.Solver.ApplyDescription(drive, Drive(slider.OffsetA, slider.AxisA, unitsPerSecond, force));
        foreach (var entity in new[] { held.A, held.B })
        {
            if (_bodies.TryGetValue(entity, out var body) && body.Kind != BodyKind.Static) Wake(_simulation.Bodies[body.Moving]);
        }

        return true;
    }

    /// <summary>How far a slider's second body is along its axis from where it was joined, or null for a joint that is no slider.</summary>
    public float? SliderPosition(JointHandle joint)
    {
        if (_disposed || !_sliders.TryGetValue(joint.Id, out var slider) || !_joints.TryGetValue(joint.Id, out var held)) return null;

        var first = _simulation.Bodies[_bodies[held.A].Moving].Pose;
        var second = _simulation.Bodies[_bodies[held.B].Moving].Pose;
        var anchor = first.Position + Vector3.Transform(slider.OffsetA, first.Orientation);
        return Vector3.Dot(second.Position - anchor, Vector3.Transform(slider.AxisA, first.Orientation));
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
        _sliders.Remove(joint.Id);

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
