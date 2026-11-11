using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>What the free camera holds as it runs, the bridge's <c>BcsFreeCameraState</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeFreeCameraState
{
    /// <summary>Non-zero while the controller moves the camera.</summary>
    public uint Enabled;

    /// <summary>The pitch it has turned the camera to.</summary>
    public float Pitch;

    /// <summary>The yaw it has turned the camera to.</summary>
    public float Yaw;

    /// <summary>What the wheel has multiplied the speeds by.</summary>
    public float SpeedMultiplier;

    /// <summary>How fast the camera moves along its right.</summary>
    public float VelocityX;

    /// <summary>How fast the camera moves along its up.</summary>
    public float VelocityY;

    /// <summary>How fast the camera moves along its forward.</summary>
    public float VelocityZ;
}
