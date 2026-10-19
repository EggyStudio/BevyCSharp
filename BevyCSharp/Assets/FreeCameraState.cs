namespace Bevy;

/// <summary>
/// What Bevy's free camera holds about a camera as it moves it, read with
/// <see cref="Render.FreeCameraStateOf"/> and written with <see cref="Render.SetFreeCameraState"/>.
/// </summary>
/// <remarks>
/// Bevy's <c>FreeCameraState</c> but for a snap to an axis in progress, which goes on as it was.
/// </remarks>
public struct FreeCameraState
{
    /// <summary>Whether the controller moves the camera. Turned off, it lets the cursor go.</summary>
    public bool Enabled { get; set; }

    /// <summary>The pitch the controller has turned the camera to, in radians.</summary>
    public float Pitch { get; set; }

    /// <summary>The yaw the controller has turned the camera to, in radians.</summary>
    public float Yaw { get; set; }

    /// <summary>What the wheel has multiplied the camera's speeds by.</summary>
    public float SpeedMultiplier { get; set; }

    /// <summary>How fast the camera moves, along its own right, up and forward.</summary>
    public Vec3 Velocity { get; set; }
}
