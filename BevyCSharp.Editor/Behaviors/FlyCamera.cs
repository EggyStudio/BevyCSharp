using Bevy;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor.Behaviors;

/// <summary>
/// Drives a camera the way an editor's scene view does: hold a mouse button and steer.
/// </summary>
/// <remarks>
/// <para>
/// The controls follow Unity's scene view, because that is the arrangement most people already
/// have in their hands:
/// </para>
/// <list type="bullet">
/// <item>Hold the right button to look around, and steer with W, A, S, D while it is held. Q
/// lowers and E raises. Shift moves faster and Control slower, and the wheel sets how fast the
/// unmodified speed is.</item>
/// <item>Hold the middle button to slide the view sideways and up, which moves the camera rather
/// than turning it.</item>
/// <item>Roll the wheel on its own to move along the view direction.</item>
/// <item>Hold Alt and the left button to swing around a point in front of the camera, which is
/// what a scene view orbits about.</item>
/// <item>Press F to frame the origin from wherever the camera is looking.</item>
/// </list>
/// <para>
/// Position and rotation are written to Bevy's own <see cref="Transform"/>, so nothing here is a
/// parallel camera model: the engine's propagation and the renderer read exactly what this
/// writes. The transform is only written on a frame where something actually moved, so a camera
/// nobody is steering does not report a change to Bevy every frame.
/// </para>
/// <para>
/// Yaw and pitch are kept here rather than read back out of the rotation each frame. A
/// quaternion has more than one decomposition, and pitch has to be clamped short of straight up
/// to keep the horizon level, which is a decision this needs to remember rather than rediscover.
/// </para>
/// <para>
/// The same behavior the sample carries, and deliberately a copy rather than something shared:
/// both are applications, and a camera with opinions about which button orbits does not belong in
/// the library, where it would join the schedule of every app that referenced it.
/// </para>
/// </remarks>
[Behavior]
public partial struct FlyCamera
{
    /// <summary>Turn about the vertical axis, in radians.</summary>
    public float Yaw;

    /// <summary>Turn about the camera's own right axis, in radians. Positive looks up.</summary>
    public float Pitch;

    /// <summary>Metres per second with no modifier held.</summary>
    public float Speed;

    /// <summary>How far in front of the camera an orbit swings, in metres.</summary>
    public float PivotDistance;

    /// <summary>Radians of turn per pixel of mouse movement.</summary>
    private const float LookSensitivity = 0.003f;

    /// <summary>Metres of slide per pixel of mouse movement, at the pivot distance.</summary>
    private const float PanSensitivity = 0.01f;

    /// <summary>Metres moved per notch of the wheel.</summary>
    private const float DollyPerNotch = 1.2f;

    /// <summary>What Shift and Control do to the speed.</summary>
    private const float FastFactor = 4f;

    /// <summary>What Control does to the speed.</summary>
    private const float SlowFactor = 0.25f;

    /// <summary>How far short of straight up or down the pitch stops.</summary>
    /// <remarks>
    /// Looking exactly along the vertical axis leaves the roll undetermined, which shows up as
    /// the horizon spinning as the camera passes through it. Stopping a degree short avoids the
    /// question rather than answering it.
    /// </remarks>
    private const float PitchLimit = 1.553343f;

    /// <summary>Whether this behavior currently holds the cursor.</summary>
    /// <remarks>
    /// Tracked because the cursor is shared: anything else in the editor may lock it too, and
    /// the window offers no way to read back which mode is in force. Releasing only what was
    /// taken here keeps the two from fighting over a button neither is holding.
    /// </remarks>
    private static bool _holdingCursor;

    /// <summary>
    /// A camera at <paramref name="eye"/> already looking at <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// Derived from the direction rather than written down beside it, so the first mouse movement
    /// carries on from where the camera was pointed instead of snapping somewhere else.
    /// </remarks>
    public static FlyCamera LookingAt(Vec3 eye, Vec3 target, float speed = 6f)
    {
        var forward = (target - eye).Normalized;

        return new FlyCamera
        {
            Pitch = MathF.Asin(Math.Clamp(forward.Y, -1f, 1f)),
            Yaw = MathF.Atan2(-forward.X, -forward.Z),
            Speed = speed,
            PivotDistance = (target - eye).Length,
        };
    }

    /// <summary>Where the camera is pointed, from its yaw and pitch.</summary>
    /// <remarks>
    /// Forward is negative Z at rest, which is Bevy's convention and what
    /// <see cref="Transform.LookingAt"/> produces.
    /// </remarks>
    private readonly Vec3 Forward
    {
        get
        {
            var (sy, cy) = (MathF.Sin(Yaw), MathF.Cos(Yaw));
            var (sp, cp) = (MathF.Sin(Pitch), MathF.Cos(Pitch));
            return new Vec3(-sy * cp, sp, -cy * cp);
        }
    }

    /// <summary>The camera's right axis, level with the ground whatever the pitch.</summary>
    private readonly Vec3 Right => new(MathF.Cos(Yaw), 0f, -MathF.Sin(Yaw));

    /// <summary>The camera's own up axis.</summary>
    private readonly Vec3 Up => Vec3.Cross(Right, Forward);

    /// <summary>
    /// What F should look at, and from how far.
    /// </summary>
    /// <remarks>
    /// The selection when there is one and the origin when there is not, which is the answer
    /// every editor gives. The distance is the object's own extent with a little in hand, so a
    /// small thing is approached and a large one is backed away from.
    /// </remarks>
    private static (Vec3 Target, float Distance) Framed()
    {
        if (EditorSelection.Any
            && Render.TryGetBounds(EditorSelection.Current, out var min, out var max))
        {
            var centre = (min + max) * 0.5f;
            var extent = (max - min) * 0.5f;
            var reach = MathF.Max(MathF.Max(extent.X, extent.Y), extent.Z);

            return (centre, MathF.Max(reach * 3f, 1f));
        }

        return (Vec3.Zero, PivotDefault);
    }

    /// <summary>How far to stand off something with no size of its own.</summary>
    private const float PivotDefault = 8f;

    /// <summary>Reads the mouse and keyboard and moves the camera.</summary>
    [OnUpdate]
    public void Steer(BehaviorContext ctx)
    {
        if (!App.HasRenderer) return;

        var input = ctx.Input;
        var alt = input.AnyKeyDown([Key.AltLeft, Key.AltRight]);

        var flying = input.MouseDown(MouseButton.Right);
        var panning = input.MouseDown(MouseButton.Middle);
        var orbiting = alt && input.MouseDown(MouseButton.Left);

        HoldCursor(flying || panning || orbiting);

        var (dx, dy) = input.MouseDelta;
        var wheel = input.WheelY;

        // The difference between this copy of the camera and the sample's. A wheel over a panel
        // belongs to that panel: rolling it over a list should scroll the list rather than fly
        // the camera through the wall behind it. A drag is not affected, because a drag that
        // began over the viewport should keep working wherever the pointer goes.
        if (wheel != 0f && EditorShell.PointerOverPanel(input.MouseX, input.MouseY)) wheel = 0f;
        var position = ctx.Ecs.GetOrDefault<Transform>(ctx.Entity).Translation;
        var moved = false;

        if (flying)
        {
            // The wheel sets how fast the camera flies rather than moving it, which is what
            // stops a fly-through from being a series of overshoots in a small scene.
            if (wheel != 0f)
            {
                Speed = Math.Clamp(Speed * MathF.Pow(1.2f, wheel), 0.05f, 500f);
                Console.WriteLine($"[FlyCamera] {Speed:F2} m/s");
            }

            moved |= Look(dx, dy);
            moved |= Walk(ctx, ref position);
        }
        else if (orbiting)
        {
            // The camera swings around a point ahead of it and keeps facing that point, so the
            // thing being looked at stays in the middle while the view goes around it.
            var pivot = position + (Forward * PivotDistance);

            if (Look(dx, dy))
            {
                position = pivot - (Forward * PivotDistance);
                moved = true;
            }
        }
        else if (panning)
        {
            // The scene follows the cursor, so the camera goes the other way. Scaled by the
            // pivot distance, because a drag should cover the same amount of what is on screen
            // whether the camera is close to it or far off.
            if (dx != 0f || dy != 0f)
            {
                var scale = PanSensitivity * MathF.Max(PivotDistance, 1f);
                position -= Right * dx * scale;
                position += Up * dy * scale;
                moved = true;
            }
        }
        else if (wheel != 0f)
        {
            position += Forward * wheel * DollyPerNotch;
            moved = true;
        }

        if (input.KeyPressed(Key.F))
        {
            // What F does in an editor: keep looking the way the camera already is, and back off
            // far enough to see what is selected. The distance comes from the thing's own size,
            // so framing a cube and framing a landscape both end up with it filling the view.
            var (target, size) = Framed();

            PivotDistance = size;
            position = target - (Forward * size);
            moved = true;
        }

        if (!moved) return;

        // Only on a frame that moved. Writing every frame would tell Bevy the transform changed
        // when it did not, and every reader watching for that would wake up for nothing.
        ref var transform = ref ctx.Ecs.GetRef<Transform>(ctx.Entity);
        transform.Translation = position;
        transform.Rotation = Quat.FromRotationY(Yaw) * Quat.FromRotationX(Pitch);
    }

    /// <summary>Turns the camera by a mouse movement, and says whether it turned at all.</summary>
    private bool Look(float dx, float dy)
    {
        if (dx == 0f && dy == 0f) return false;

        Yaw -= dx * LookSensitivity;
        Pitch = Math.Clamp(Pitch - (dy * LookSensitivity), -PitchLimit, PitchLimit);
        return true;
    }

    /// <summary>Walks the camera along its own axes, and says whether it moved at all.</summary>
    private readonly bool Walk(BehaviorContext ctx, ref Vec3 position)
    {
        var input = ctx.Input;

        var forward = Held(input, Key.W) - Held(input, Key.S);
        var strafe = Held(input, Key.D) - Held(input, Key.A);
        var rise = Held(input, Key.E) - Held(input, Key.Q);

        if (forward == 0f && strafe == 0f && rise == 0f) return false;

        var speed = Speed;
        if (input.AnyKeyDown([Key.ShiftLeft, Key.ShiftRight])) speed *= FastFactor;
        if (input.AnyKeyDown([Key.ControlLeft, Key.ControlRight])) speed *= SlowFactor;

        // Normalised, so that going diagonally is not faster than going straight.
        var direction = (Forward * forward) + (Right * strafe) + (Vec3.UnitY * rise);
        position += direction.Normalized * speed * ctx.Time.Delta;
        return true;

        static float Held(Input input, Key key) => input.KeyDown(key) ? 1f : 0f;
    }

    /// <summary>Takes or gives back the cursor, doing nothing if it is already where it should be.</summary>
    private static void HoldCursor(bool wanted)
    {
        if (wanted == _holdingCursor) return;

        _holdingCursor = wanted;
        Window.SetCursor(wanted ? CursorGrab.Locked : CursorGrab.None, !wanted);
    }
}
