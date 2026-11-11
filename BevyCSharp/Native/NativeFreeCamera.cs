using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>The free camera's settings, the bridge's <c>BcsFreeCamera</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeFreeCamera
{
    /// <summary>How many keys <see cref="Keys"/> holds.</summary>
    public const int KeyCount = 12;

    /// <summary>How fast the mouse turns the camera.</summary>
    public float Sensitivity;

    /// <summary>How fast it moves, in units a second.</summary>
    public float WalkSpeed;

    /// <summary>How fast it moves with the run key held.</summary>
    public float RunSpeed;

    /// <summary>How much a step of the wheel changes both speeds.</summary>
    public float ScrollFactor;

    /// <summary>How fast the camera stops once no key moves it.</summary>
    public float Friction;

    /// <summary>How fast a snap to an axis turns it, in radians a second.</summary>
    public float RotationSpeed;

    /// <summary>Zero to move up and down along the world's up, one along the camera's.</summary>
    public int VerticalMovement;

    /// <summary>The mouse button held to turn the camera.</summary>
    public int GrabButton;

    /// <summary>
    /// Forward, back, left, right, up, down, run, toggle grab, snap reverse, snap top, snap right
    /// and snap front, each a <see cref="Key"/>, or -1 for a key outside the table.
    /// </summary>
    public fixed int Keys[KeyCount];
}
