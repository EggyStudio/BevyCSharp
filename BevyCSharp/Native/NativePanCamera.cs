using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>The pan camera's settings, the bridge's <c>BcsPanCamera</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativePanCamera
{
    /// <summary>How many keys <see cref="Keys"/> holds.</summary>
    public const int KeyCount = 8;

    /// <summary>Non-zero while the controller moves the camera.</summary>
    public uint Enabled;

    /// <summary>How far the view is zoomed.</summary>
    public float ZoomFactor;

    /// <summary>The least the zoom goes to.</summary>
    public float MinZoom;

    /// <summary>The most the zoom goes to.</summary>
    public float MaxZoom;

    /// <summary>How much a step of the wheel or a frame of a zoom key zooms.</summary>
    public float ZoomSpeed;

    /// <summary>Non-zero to zoom about the pointer.</summary>
    public uint ZoomToCursor;

    /// <summary>How fast the keys move the view.</summary>
    public float PanSpeed;

    /// <summary>How fast the keys turn the view.</summary>
    public float RotationSpeed;

    /// <summary>Non-zero to move the view by dragging it.</summary>
    public uint MousePan;

    /// <summary>The button that drags it, left, right or middle.</summary>
    public int MousePanButton;

    /// <summary>
    /// Zoom in, zoom out, up, down, left, right, turn counterclockwise and turn clockwise, each a
    /// <see cref="Key"/>, or -1 for none.
    /// </summary>
    public fixed int Keys[KeyCount];
}
