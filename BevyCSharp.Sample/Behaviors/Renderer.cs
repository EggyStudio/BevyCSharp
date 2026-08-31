using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Sets up the window and reports which graphics adapter the renderer actually picked.
/// </summary>
/// <remarks>
/// <para>
/// Every method here is inert in a headless run, so the sample's behavior scripts are identical
/// in both modes, which is the point. The engine decides whether there is a renderer; the
/// scripts do not branch on it.
/// </para>
/// </remarks>
[Behavior]
public partial struct Renderer
{
    /// <summary>Gives the window a camera and prints the adapter that was chosen.</summary>
    [OnStartup]
    public static void Describe(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        var adapter = App.DescribeAdapter();
        Console.WriteLine(adapter is null
            ? "[Renderer] the renderer has not reported an adapter"
            : $"[Renderer] adapter: {adapter}");
    }

    /// <summary>Closes the window on Escape.</summary>
    [OnUpdate]
    public static void QuitOnEscape(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.Escape)) ctx.Exit();
    }

    /// <summary>Whether the window is currently borderless fullscreen.</summary>
    private static bool _fullscreen;

    /// <summary>Whether the cursor is currently locked to the window.</summary>
    private static bool _cursorLocked;

    /// <summary>Drives the window from the keyboard: F11 fullscreen, Tab cursor lock.</summary>
    /// <remarks>
    /// Cursor lock is the one a first-person camera cannot do without, because it reads how far
    /// the mouse moved rather than where it is.
    /// </remarks>
    [OnUpdate]
    public static void ControlWindow(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        if (ctx.Input.KeyPressed(Key.F11))
        {
            _fullscreen = !_fullscreen;
            Window.SetMode(_fullscreen ? WindowMode.BorderlessFullscreen : WindowMode.Windowed);
        }

        if (ctx.Input.KeyPressed(Key.Tab))
        {
            _cursorLocked = !_cursorLocked;
            Window.SetCursor(_cursorLocked ? CursorGrab.Locked : CursorGrab.None, !_cursorLocked);
            Console.WriteLine($"[Renderer] cursor {(_cursorLocked ? "locked" : "free")}");
        }
    }

    /// <summary>Prints the frame rate every second while the window is open.</summary>
    [OnLast]
    [ToggleKey(Key.F1)]
    public static void PrintFps(BehaviorContext ctx)
    {
        if (ctx.Res<Config>().Headless) return;
        if (ctx.Time.FrameCount == 0 || ctx.Time.FrameCount % 60 != 0) return;

        Console.WriteLine(
            $"[Renderer] {ctx.Time.SmoothedFps,6:F1} fps   "
            + $"frame {ctx.Time.FrameCount,6}   "
            + $"spinners {ctx.Ecs.Count<Spinner>()}");
    }
}
