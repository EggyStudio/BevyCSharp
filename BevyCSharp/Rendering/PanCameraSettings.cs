namespace Bevy;

/// <summary>
/// How Bevy's pan camera moves a 2D camera, which <see cref="Render.SetPanCamera"/> gives it.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's <c>PanCamera</c>, its defaults Bevy's own. The keys move and turn the view, the wheel and
/// the zoom keys zoom it, and dragging with <see cref="MousePanButton"/> moves it under the
/// pointer. A key left null does nothing.
/// </para>
/// <para>
/// The controller keeps the zoom it has come to in <see cref="ZoomFactor"/>, which
/// <see cref="Render.PanCameraOf"/> reads back. It reads the machine's clock, so a run on a set
/// clock (<see cref="Config.FrameSeconds"/>) moves the view by the time the frames took.
/// </para>
/// </remarks>
public sealed class PanCameraSettings
{
    /// <summary>Whether the controller moves the camera.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How far the view is zoomed, the scale the camera is drawn at.</summary>
    public float ZoomFactor { get; set; } = 1f;

    /// <summary>The least <see cref="ZoomFactor"/> goes to.</summary>
    public float MinZoom { get; set; } = 0.1f;

    /// <summary>The most <see cref="ZoomFactor"/> goes to.</summary>
    public float MaxZoom { get; set; } = 5f;

    /// <summary>How much a step of the wheel, or a frame of a zoom key, zooms the view.</summary>
    public float ZoomSpeed { get; set; } = 0.1f;

    /// <summary>Zoom about the pointer, where the view otherwise zooms about its middle.</summary>
    public bool ZoomToCursor { get; set; }

    /// <summary>How fast the keys move the view, in units a second.</summary>
    public float PanSpeed { get; set; } = 500f;

    /// <summary>How fast the keys turn the view, in radians a second.</summary>
    public float RotationSpeed { get; set; } = MathF.PI;

    /// <summary>Move the view by dragging it.</summary>
    public bool MousePan { get; set; } = true;

    /// <summary>
    /// The button that drags the view, <see cref="MouseButton.Left"/>, <see cref="MouseButton.Right"/>
    /// or <see cref="MouseButton.Middle"/>.
    /// </summary>
    public MouseButton MousePanButton { get; set; } = MouseButton.Left;

    /// <summary>Zooms the view in.</summary>
    public Key? ZoomIn { get; set; } = Key.Equal;

    /// <summary>Zooms the view out.</summary>
    public Key? ZoomOut { get; set; } = Key.Minus;

    /// <summary>Moves the view up.</summary>
    public Key? Up { get; set; } = Key.W;

    /// <summary>Moves the view down.</summary>
    public Key? Down { get; set; } = Key.S;

    /// <summary>Moves the view left.</summary>
    public Key? Left { get; set; } = Key.A;

    /// <summary>Moves the view right.</summary>
    public Key? Right { get; set; } = Key.D;

    /// <summary>Turns the view counterclockwise.</summary>
    public Key? RotateCounterclockwise { get; set; } = Key.Q;

    /// <summary>Turns the view clockwise.</summary>
    public Key? RotateClockwise { get; set; } = Key.E;
}
