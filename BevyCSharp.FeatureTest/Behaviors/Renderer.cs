using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// Sets up the window and reports which graphics adapter the renderer actually picked.
/// </summary>
/// <remarks>
/// <para>
/// Every method here is inert in a headless run, so the program's behavior scripts are identical
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

        for (var i = 0; i < Window.MonitorCount(); i++)
        {
            var monitor = Window.Monitor(i);
            var name = Window.MonitorName(i);

            var modes = Window.MonitorModes(i);

            Console.WriteLine(
                $"[Renderer] monitor {i}: {(name.Length > 0 ? name : "unnamed")} "
                + $"{monitor.Width}x{monitor.Height} at {monitor.RefreshHz:F0} Hz, "
                + $"{modes.Length} video mode{(modes.Length == 1 ? "" : "s")}");
        }
    }

    /// <summary>Whether the cursor is currently locked to the window.</summary>
    private static bool _cursorLocked;

    /// <summary>Whether Tab has locked the cursor, which the player's view turns with.</summary>
    internal static bool CursorLocked => _cursorLocked;

    /// <summary>Reports what the window says about itself.</summary>
    /// <remarks>
    /// These come from Bevy rather than from another script, and arrive on the same bus, so this
    /// reads them exactly as it would read a message the program sent itself.
    /// </remarks>
    [OnUpdate]
    public static void ReportWindow(BehaviorContext ctx)
    {
        foreach (var resized in ctx.Read<WindowResized>())
            Console.WriteLine($"[Renderer] resized to {resized.Width:F0}x{resized.Height:F0}");

        foreach (var focus in ctx.Read<WindowFocusChanged>())
            Console.WriteLine($"[Renderer] {(focus.Focused ? "focused" : "unfocused")}");

        foreach (var scale in ctx.Read<WindowScaleFactorChanged>())
            Console.WriteLine($"[Renderer] display scale is {scale.ScaleFactor:F2}");

        foreach (var hovered in ctx.Read<FileHovered>())
            Console.WriteLine($"[Renderer] hovering {hovered.Path}");

        foreach (var _ in ctx.Read<FileHoverCanceled>())
            Console.WriteLine("[Renderer] the drag left without dropping");

        foreach (var dropped in ctx.Read<FileDropped>())
            Console.WriteLine($"[Renderer] dropped {dropped.Path}");
    }

    /// <summary>Drives the window from the keyboard: F11 fullscreen, Tab cursor lock.</summary>
    /// <remarks>
    /// Cursor lock is the one a first-person camera cannot do without, because it reads how far
    /// the mouse moved rather than where it is.
    /// </remarks>
    [OnUpdate]
    public static void ControlWindow(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        // Through the settings, so the panel's window row and the next start agree with the key.
        if (ctx.Input.KeyPressed(Key.F11))
        {
            Settings.Change(settings => settings with
            {
                Window = settings.Window == WindowMode.Windowed ? WindowMode.BorderlessFullscreen : WindowMode.Windowed,
            });
        }

        // Not while a line is being typed, where the key completes a command.
        if (ctx.Input.KeyPressed(Key.Tab) && !ImGuiRuntime.Typing)
        {
            _cursorLocked = !_cursorLocked;
            Window.SetCursor(_cursorLocked ? CursorGrab.Locked : CursorGrab.None, !_cursorLocked);
            Console.WriteLine($"[Renderer] cursor {(_cursorLocked ? "locked" : "free")}");
        }
    }
}
