using Bevy;

namespace BevyCSharp.Examples;

// A helper the examples share, as Bevy 0.20's examples share the free camera of its camera
// controller crate, which the bridge does not compile in. It goes when that camera can be asked
// for, and until then build/examples-on-package.sh lists it as one of the two helpers an example
// may lean on.

/// <summary>
/// Flies its camera as Bevy's <c>FreeCamera</c> does: WASD across, E and Q up and down, Shift to
/// run, and the mouse turning it while its left button is held.
/// </summary>
/// <remarks>
/// <para>
/// Bevy grabs the cursor while the button is held, which an example here leaves to the platform,
/// and an offscreen run, with no pointer, turns it not at all.
/// </para>
/// <para>
/// The transform is written only in a frame the camera moves or turns, as Bevy's controller writes
/// it, since a write marks it changed whatever it holds, and Bevy's path tracer reads a changed
/// transform as a moved view and starts its gathering of samples over.
/// </para>
/// </remarks>
[Behavior]
public partial struct FreeCamera
{
    /// <summary>Units a second walking, and three times that running.</summary>
    public float Speed;

    private float _yaw;
    private float _pitch;
    private bool _started;

    [OnUpdate]
    [MainThread]
    public void Fly(BehaviorContext ctx, in Transform transform)
    {
        if (!_started)
        {
            var euler = transform.Rotation.ToEuler();
            (_pitch, _yaw, _started) = (euler.X, euler.Y, true);
            if (Speed <= 0f) Speed = 5f;
        }

        var input = ctx.Input;
        if (input.MouseDown(MouseButton.Left))
        {
            // A fifth of a degree a pixel, Bevy's own sensitivity.
            const float Sensitivity = 0.2f * MathF.PI / 180f;
            _yaw -= input.MouseDeltaX * Sensitivity;
            _pitch = Math.Clamp(_pitch - input.MouseDeltaY * Sensitivity, -MathF.PI / 2f, MathF.PI / 2f);
        }

        var moved = transform;
        moved.Rotation = Quat.FromRotationY(_yaw) * Quat.FromRotationX(_pitch);

        var forward = moved.Rotation * -Vec3.UnitZ;
        var right = moved.Rotation * Vec3.UnitX;
        var move = Vec3.Zero;
        if (input.KeyDown(Key.W)) move += forward;
        if (input.KeyDown(Key.S)) move -= forward;
        if (input.KeyDown(Key.D)) move += right;
        if (input.KeyDown(Key.A)) move -= right;
        if (input.KeyDown(Key.E)) move += Vec3.UnitY;
        if (input.KeyDown(Key.Q)) move -= Vec3.UnitY;

        var speed = Speed * (input.KeyDown(Key.ShiftLeft) ? 3f : 1f);
        moved.Translation += move * (speed * ctx.Time.Delta);

        if (moved.Rotation != transform.Rotation || moved.Translation != transform.Translation) ctx.Ecs.Set(ctx.Entity, moved);
    }
}
