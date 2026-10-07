using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The program's interface, the admin panel, the debug overlay and the console, drawn with Dear
/// ImGui in one frame of it a frame.
/// </summary>
/// <remarks>
/// <para>
/// The whole of what a game writes to have one. Start the runtime once, and between
/// <see cref="ImGuiRuntime.Begin"/> and <see cref="ImGuiRuntime.End"/> call ImGui. There is no
/// document to load, no binding to declare and nothing to keep in step. The screen shows what this
/// frame's calls said, so each piece draws itself only while it is open.
/// </para>
/// <para>
/// It needs a bridge with the interface compiled in (<c>build/build-native.sh --editor</c>) and
/// <c>Config.Gui</c> asked for, and says so rather than drawing nothing. F1 opens the panel, F3 the
/// overlay, and the key under Escape the console.
/// </para>
/// </remarks>
[Behavior]
public partial struct Interface
{
    /// <summary>Starts the interface.</summary>
    [OnStartup]
    public static void Open(BehaviorContext ctx)
    {
        ImGuiRuntime.Start();
        if (ImGuiRuntime.IsRunning)
            Console.WriteLine("[Interface] F1 opens the panel, F3 the overlay, the key under Escape the console");
    }

    /// <summary>Draws it, once a frame.</summary>
    [OnUpdate]
    public static void Tick(BehaviorContext ctx)
    {
        if (!ImGuiRuntime.IsRunning) return;

        ImGuiRuntime.Begin(ctx);
        Panel.Draw(ctx);
        Overlay.Draw(ctx);
        ImGuiConsole.Draw(ctx);
        ImGuiRuntime.End();
    }
}
