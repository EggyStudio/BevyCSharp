using Bevy;

namespace BevyCSharp.Examples;

/// <summary>One of Bevy's examples written here, by Bevy's name.</summary>
/// <param name="Name">Bevy's name for it, which is also its file's.</param>
/// <param name="Build">What it adds to the app, as Bevy's <c>main</c> adds its systems.</param>
/// <param name="Configure">What it changes about the app before it runs, where it changes anything.</param>
/// <param name="Prints">
/// For an example with nothing to draw, how many frames it runs headless, and what it prints in
/// them is its capture. Zero for one that draws.
/// </param>
/// <param name="Returned">What runs once the app has stopped and the program is back in its own code.</param>
/// <param name="Drive">
/// Input given to an example that waits for it, a few keys or buttons at set frames, added when it
/// is run with <c>--drive</c> as a capture runs it, so one that prints what it is given has
/// something to print.
/// </param>
/// <remarks>
/// Bevy's examples of the ECS mostly print to the console, some in a window left empty, and one
/// that prints is run here with no window at all, so it needs no renderer and its output is
/// the same on any machine.
/// </remarks>
internal sealed record Example(
    string Name,
    Action<App> Build,
    Action<Config>? Configure = null,
    uint Prints = 0,
    Action? Returned = null,
    Action<App>? Drive = null);

/// <summary>What drives an example for its capture, beside what the package says for it.</summary>
internal static class Scene
{
    /// <summary>
    /// The size the example was opened at, in pixels, for one that divides it between cameras,
    /// since an offscreen run has no window to ask.
    /// </summary>
    public static (uint Width, uint Height) Size { get; set; } = (1280, 720);

    /// <summary>
    /// Runs each step on its frame, counted from the first update, for input pretended where a
    /// person would give it.
    /// </summary>
    public static App Script(this App app, params (int Frame, Action Step)[] steps)
    {
        var frame = 0;
        return app.Update(_ =>
        {
            frame++;
            foreach (var (at, step) in steps)
                if (at == frame) step();
        }, "Example.Script");
    }
}

/// <summary>
/// Flies its camera as Bevy's <c>FreeCamera</c> does: WASD across, E and Q up and down, Shift to
/// run, and the mouse turning it while its left button is held.
/// </summary>
/// <remarks>
/// Bevy grabs the cursor while the button is held, which an example here leaves to the platform,
/// and an offscreen run, with no pointer, turns it not at all.
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
    public void Fly(BehaviorContext ctx, ref Transform transform)
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

        transform.Rotation = Quat.FromRotationY(_yaw) * Quat.FromRotationX(_pitch);

        var forward = transform.Rotation * -Vec3.UnitZ;
        var right = transform.Rotation * Vec3.UnitX;
        var move = Vec3.Zero;
        if (input.KeyDown(Key.W)) move += forward;
        if (input.KeyDown(Key.S)) move -= forward;
        if (input.KeyDown(Key.D)) move += right;
        if (input.KeyDown(Key.A)) move -= right;
        if (input.KeyDown(Key.E)) move += Vec3.UnitY;
        if (input.KeyDown(Key.Q)) move -= Vec3.UnitY;

        var speed = Speed * (input.KeyDown(Key.ShiftLeft) ? 3f : 1f);
        transform.Translation += move * (speed * ctx.Time.Delta);
    }
}
