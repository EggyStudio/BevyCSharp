namespace Bevy;

/// <summary>
/// How Bevy's free camera moves a camera, which <see cref="Render.SetFreeCamera"/> gives it.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's <c>FreeCamera</c>, its defaults Bevy's own. The keys move the camera along its own
/// forward and right and along the world's up, holding the grab button turns it with the mouse, and
/// the wheel changes how fast it moves. The keys on the number pad turn it to face along an axis.
/// </para>
/// <para>
/// Bevy's controller moves one camera, so with the free camera on two cameras it moves neither. It
/// reads the machine's clock, so a run on a set clock (<see cref="Config.FrameSeconds"/>) moves the
/// camera by the time the frames took.
/// </para>
/// </remarks>
public sealed class FreeCameraSettings
{
    /// <summary>How fast the mouse turns the camera.</summary>
    public float Sensitivity { get; set; } = 0.2f;

    /// <summary>How fast the camera moves, in units a second.</summary>
    public float WalkSpeed { get; set; } = 5f;

    /// <summary>How fast the camera moves while <see cref="Run"/> is held.</summary>
    public float RunSpeed { get; set; } = 15f;

    /// <summary>
    /// How much a step of the wheel changes both speeds, as the power of e it multiplies them by.
    /// </summary>
    /// <remarks>
    /// Bevy's own is about ln(1.05), five percent faster a step forward and that much slower a step
    /// back, the same on a wheel that reports one large step as on one that reports many small ones.
    /// </remarks>
    public float ScrollFactor { get; set; } = 0.04879016f;

    /// <summary>How fast the camera stops once no key moves it.</summary>
    public float Friction { get; set; } = 40f;

    /// <summary>How fast a snap to an axis turns the camera, in radians a second.</summary>
    public float RotationSpeed { get; set; } = MathF.PI / 16f * 60f;

    /// <summary>
    /// Move <see cref="Up"/> and <see cref="Down"/> along the camera's own up rather than the
    /// world's.
    /// </summary>
    public bool LocalVerticalMovement { get; set; }

    /// <summary>The button held to turn the camera with the mouse, which hides the cursor while held.</summary>
    public MouseButton GrabButton { get; set; } = MouseButton.Right;

    /// <summary>Moves the camera forward.</summary>
    public Key Forward { get; set; } = Key.W;

    /// <summary>Moves the camera back.</summary>
    public Key Back { get; set; } = Key.S;

    /// <summary>Moves the camera left.</summary>
    public Key Left { get; set; } = Key.A;

    /// <summary>Moves the camera right.</summary>
    public Key Right { get; set; } = Key.D;

    /// <summary>Moves the camera up.</summary>
    public Key Up { get; set; } = Key.E;

    /// <summary>Moves the camera down.</summary>
    public Key Down { get; set; } = Key.Q;

    /// <summary>Moves the camera at <see cref="RunSpeed"/> while held.</summary>
    public Key Run { get; set; } = Key.ShiftLeft;

    /// <summary>Grabs the cursor to turn the camera, or lets it go.</summary>
    public Key ToggleGrab { get; set; } = Key.M;

    /// <summary>Held with a snap key, turns the camera to face the other way along the axis.</summary>
    public Key SnapReverse { get; set; } = Key.ControlLeft;

    /// <summary>Turns the camera to look down from the top.</summary>
    public Key SnapTop { get; set; } = Key.Numpad7;

    /// <summary>Turns the camera to look along the world's right.</summary>
    public Key SnapRight { get; set; } = Key.Numpad3;

    /// <summary>Turns the camera to look along the world's forward.</summary>
    public Key SnapFront { get; set; } = Key.Numpad1;
}
