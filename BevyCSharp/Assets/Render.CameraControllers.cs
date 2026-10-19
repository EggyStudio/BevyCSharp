using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// Puts Bevy's free camera on a camera, or changes the settings of the one there.
    /// </summary>
    /// <remarks>
    /// The camera moves where the app asked for <see cref="CameraControllers.Free"/>, and while it
    /// is the one camera with the free camera, since Bevy's controller moves a single camera. What
    /// the controller holds as it runs, its turn and its speed, stays as it was when the settings
    /// change. The controller writes the camera's <see cref="Transform"/> each frame it moves it,
    /// and only then.
    /// </remarks>
    /// <param name="camera">A camera entity.</param>
    /// <param name="settings">How the free camera moves it.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 1f, 6f), Vec3.Zero, Vec3.UnitY));
    /// Render.SetFreeCamera(camera, new FreeCameraSettings { WalkSpeed = 3f, RunSpeed = 9f });
    /// </code>
    /// </example>
    public static void SetFreeCamera(Entity camera, FreeCameraSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = ToNative(settings);
        Native.Check(Native.bcs_free_camera_set(camera.Bits, &native), $"putting the free camera on {camera}");
    }

    /// <summary>The free camera's settings on a camera, or null where it has none.</summary>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static FreeCameraSettings? FreeCameraOf(Entity camera)
    {
        NativeFreeCamera native;
        var answer = Native.bcs_free_camera_get(camera.Bits, &native);
        if (answer == NativeStatus.NotPresent) return null;

        Native.Check(answer, $"reading the free camera on {camera}");
        return FromNative(native);
    }

    /// <summary>Takes the free camera off a camera, which then stays where it is.</summary>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static void ClearFreeCamera(Entity camera) =>
        Native.Check(Native.bcs_free_camera_remove(camera.Bits), $"taking the free camera off {camera}");

    /// <summary>
    /// What the free camera holds about a camera as it moves it, or null where it has no free
    /// camera.
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static FreeCameraState? FreeCameraStateOf(Entity camera)
    {
        NativeFreeCameraState native;
        var answer = Native.bcs_free_camera_state(camera.Bits, &native);
        if (answer == NativeStatus.NotPresent) return null;

        Native.Check(answer, $"reading what the free camera on {camera} holds");
        return new FreeCameraState
        {
            Enabled = native.Enabled != 0,
            Pitch = native.Pitch,
            Yaw = native.Yaw,
            SpeedMultiplier = native.SpeedMultiplier,
            Velocity = new Vec3(native.VelocityX, native.VelocityY, native.VelocityZ),
        };
    }

    /// <summary>
    /// Writes what the free camera holds about a camera, as a settings screen turns it off or a
    /// game resets its speed.
    /// </summary>
    /// <remarks>
    /// The controller turns the camera to <see cref="FreeCameraState.Pitch"/> and
    /// <see cref="FreeCameraState.Yaw"/> the next time the mouse turns it, so a game that turns the
    /// camera itself writes them as well.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, is not a camera or has no free camera, or this build has no renderer.
    /// </exception>
    public static void SetFreeCameraState(Entity camera, FreeCameraState state)
    {
        var native = new NativeFreeCameraState
        {
            Enabled = state.Enabled ? 1u : 0u,
            Pitch = state.Pitch,
            Yaw = state.Yaw,
            SpeedMultiplier = state.SpeedMultiplier,
            VelocityX = state.Velocity.X,
            VelocityY = state.Velocity.Y,
            VelocityZ = state.Velocity.Z,
        };
        Native.Check(Native.bcs_free_camera_set_state(camera.Bits, &native), $"writing what the free camera on {camera} holds");
    }

    /// <summary>
    /// Bevy's own description of the free camera's controls on a camera, a line a control with
    /// Bevy's names for the keys, which its example shows.
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, is not a camera or has no free camera, or this build has no renderer.
    /// </exception>
    public static string FreeCameraControls(Entity camera) =>
        Native.ReadText(
            (buffer, capacity) => Native.bcs_free_camera_text(camera.Bits, buffer, capacity),
            $"describing the free camera on {camera}");

    /// <summary>
    /// Puts Bevy's pan camera on a 2D camera, or changes the settings of the one there.
    /// </summary>
    /// <remarks>
    /// The camera moves where the app asked for <see cref="CameraControllers.Pan"/>. The zoom is
    /// the camera's scale, which the controller writes into its <see cref="Transform"/>, and a drag
    /// moves the view under the pointer, so <see cref="PanCameraSettings.MousePan"/> needs the
    /// window's pointer.
    /// </remarks>
    /// <param name="camera">A camera entity, usually from <see cref="Render2d.SpawnCamera2d"/>.</param>
    /// <param name="settings">How the pan camera moves it.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static void SetPanCamera(Entity camera, PanCameraSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = ToNative(settings);
        Native.Check(Native.bcs_pan_camera_set(camera.Bits, &native), $"putting the pan camera on {camera}");
    }

    /// <summary>
    /// The pan camera's settings on a camera, the zoom it has come to among them, or null where it
    /// has none.
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static PanCameraSettings? PanCameraOf(Entity camera)
    {
        NativePanCamera native;
        var answer = Native.bcs_pan_camera_get(camera.Bits, &native);
        if (answer == NativeStatus.NotPresent) return null;

        Native.Check(answer, $"reading the pan camera on {camera}");
        return FromNative(native);
    }

    /// <summary>Takes the pan camera off a camera, which then stays where it is.</summary>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static void ClearPanCamera(Entity camera) =>
        Native.Check(Native.bcs_pan_camera_remove(camera.Bits), $"taking the pan camera off {camera}");

    /// <summary>
    /// Bevy's own description of the pan camera's controls on a camera, a line a control with
    /// Bevy's names for the keys, which its example shows.
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, is not a camera or has no pan camera, or this build has no renderer.
    /// </exception>
    public static string PanCameraControls(Entity camera) =>
        Native.ReadText(
            (buffer, capacity) => Native.bcs_pan_camera_text(camera.Bits, buffer, capacity),
            $"describing the pan camera on {camera}");

    /// <summary>
    /// Moves a pan-orbit camera to another projection over Bevy's dolly zoom, keeping the point it
    /// orbits the same size on the screen as the field of view narrows or widens.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>DollyZoomTrigger</c>, to its default perspective or its default orthographic
    /// projection in 3D. The camera backs away as its field of view narrows toward an orthographic
    /// view, and the other way, over Bevy's <c>DollyZoom</c> resource's time and curve.
    /// </remarks>
    /// <param name="camera">A camera with the reflected <c>PanOrbitCameraRef</c>.</param>
    /// <param name="projection">The projection to end at.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, the app did not ask for
    /// <see cref="CameraControllers.PanOrbit"/>, or this build has no renderer.
    /// </exception>
    public static void DollyZoom(Entity camera, CameraProjection projection) =>
        Native.Check(
            Native.bcs_pan_orbit_dolly_zoom(camera.Bits, projection == CameraProjection.Orthographic ? 1 : 0),
            $"dolly zooming {camera}");

    /// <summary>
    /// Turns a pan-orbit camera to face a direction, about the point it orbits, over Bevy's look-to
    /// animation.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>LookToTrigger.auto_snap_up_direction</c>, which keeps as the camera's up the axis
    /// nearest the up it has, so turning to look down from the top keeps the view's top where it
    /// was.
    /// </remarks>
    /// <param name="camera">A camera with the reflected <c>PanOrbitCameraRef</c>.</param>
    /// <param name="direction">The direction to face, in the world.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, is not a camera or has no pan-orbit camera, the app did not ask for
    /// <see cref="CameraControllers.PanOrbit"/>, or this build has no renderer.
    /// </exception>
    public static void LookTo(Entity camera, Vec3 direction) =>
        Native.Check(
            Native.bcs_pan_orbit_look_to(camera.Bits, direction.X, direction.Y, direction.Z),
            $"turning {camera} to face {direction}");

    internal static NativeFreeCamera ToNative(FreeCameraSettings settings)
    {
        var native = new NativeFreeCamera
        {
            Sensitivity = settings.Sensitivity,
            WalkSpeed = settings.WalkSpeed,
            RunSpeed = settings.RunSpeed,
            ScrollFactor = settings.ScrollFactor,
            Friction = settings.Friction,
            RotationSpeed = settings.RotationSpeed,
            VerticalMovement = settings.LocalVerticalMovement ? 1 : 0,
            GrabButton = (int)settings.GrabButton,
        };

        Key[] keys =
        [
            settings.Forward, settings.Back, settings.Left, settings.Right, settings.Up, settings.Down,
            settings.Run, settings.ToggleGrab, settings.SnapReverse, settings.SnapTop, settings.SnapRight,
            settings.SnapFront,
        ];
        for (var i = 0; i < NativeFreeCamera.KeyCount; i++) native.Keys[i] = (int)keys[i];
        return native;
    }

    // A key outside the managed table comes back as -1, which no Key names, so the setting keeps
    // its default rather than holding a value a switch over keys would never meet.
    internal static FreeCameraSettings FromNative(NativeFreeCamera native)
    {
        var settings = new FreeCameraSettings
        {
            Sensitivity = native.Sensitivity,
            WalkSpeed = native.WalkSpeed,
            RunSpeed = native.RunSpeed,
            ScrollFactor = native.ScrollFactor,
            Friction = native.Friction,
            RotationSpeed = native.RotationSpeed,
            LocalVerticalMovement = native.VerticalMovement == 1,
            GrabButton = native.GrabButton >= 0 ? (MouseButton)native.GrabButton : MouseButton.Right,
        };

        var keys = new int[NativeFreeCamera.KeyCount];
        for (var i = 0; i < keys.Length; i++) keys[i] = native.Keys[i];

        Key Read(int slot, Key otherwise) => keys[slot] >= 0 ? (Key)keys[slot] : otherwise;
        settings.Forward = Read(0, settings.Forward);
        settings.Back = Read(1, settings.Back);
        settings.Left = Read(2, settings.Left);
        settings.Right = Read(3, settings.Right);
        settings.Up = Read(4, settings.Up);
        settings.Down = Read(5, settings.Down);
        settings.Run = Read(6, settings.Run);
        settings.ToggleGrab = Read(7, settings.ToggleGrab);
        settings.SnapReverse = Read(8, settings.SnapReverse);
        settings.SnapTop = Read(9, settings.SnapTop);
        settings.SnapRight = Read(10, settings.SnapRight);
        settings.SnapFront = Read(11, settings.SnapFront);
        return settings;
    }

    internal static NativePanCamera ToNative(PanCameraSettings settings)
    {
        var native = new NativePanCamera
        {
            Enabled = settings.Enabled ? 1u : 0u,
            ZoomFactor = settings.ZoomFactor,
            MinZoom = settings.MinZoom,
            MaxZoom = settings.MaxZoom,
            ZoomSpeed = settings.ZoomSpeed,
            ZoomToCursor = settings.ZoomToCursor ? 1u : 0u,
            PanSpeed = settings.PanSpeed,
            RotationSpeed = settings.RotationSpeed,
            MousePan = settings.MousePan ? 1u : 0u,
            MousePanButton = (int)settings.MousePanButton,
        };

        Key?[] keys =
        [
            settings.ZoomIn, settings.ZoomOut, settings.Up, settings.Down, settings.Left, settings.Right,
            settings.RotateCounterclockwise, settings.RotateClockwise,
        ];
        for (var i = 0; i < NativePanCamera.KeyCount; i++) native.Keys[i] = keys[i] is { } key ? (int)key : -1;
        return native;
    }

    internal static PanCameraSettings FromNative(NativePanCamera native)
    {
        var keys = new int[NativePanCamera.KeyCount];
        for (var i = 0; i < keys.Length; i++) keys[i] = native.Keys[i];

        Key? Read(int slot) => keys[slot] >= 0 ? (Key)keys[slot] : null;

        return new PanCameraSettings
        {
            Enabled = native.Enabled != 0,
            ZoomFactor = native.ZoomFactor,
            MinZoom = native.MinZoom,
            MaxZoom = native.MaxZoom,
            ZoomSpeed = native.ZoomSpeed,
            ZoomToCursor = native.ZoomToCursor != 0,
            PanSpeed = native.PanSpeed,
            RotationSpeed = native.RotationSpeed,
            MousePan = native.MousePan != 0,
            MousePanButton = (MouseButton)native.MousePanButton,
            ZoomIn = Read(0),
            ZoomOut = Read(1),
            Up = Read(2),
            Down = Read(3),
            Left = Read(4),
            Right = Read(5),
            RotateCounterclockwise = Read(6),
            RotateClockwise = Read(7),
        };
    }
}
