using Bevy;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The program's interface, the admin panel, the mode's label and the console, drawn with Dear
/// ImGui in one frame of it a frame, beside the engine's own debug overlay.
/// </summary>
/// <remarks>
/// <para>
/// The whole of what a game writes to have one. Start the runtime once, and between
/// <see cref="ImGuiRuntime.Begin"/> and <see cref="ImGuiRuntime.End"/> call ImGui. There is no
/// document to load, no binding to declare and nothing to keep in step. The screen shows what this
/// frame's calls said, so each piece draws itself only while it is open.
/// </para>
/// <para>
/// It needs a bridge that draws and <c>Config.Gui</c> asked for, and says so rather than drawing
/// nothing. F4 opens the panel and the key under Escape the console. The mode's label is the HUD,
/// drawn inside the hidden scope (<see cref="Ui.BeginHidden"/>), so the engine's F1 hides it while
/// the panel and the console, screens over the game, stay. The engine's F3 shows its debug
/// overlay, which the panel's debug page turns on and off too.
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
            Console.WriteLine("[Interface] F4 opens the panel, F3 the overlay, F1 hides the HUD, the key under Escape the console");
    }

    /// <summary>Draws it, once a frame.</summary>
    [OnUpdate]
    public static void Tick(BehaviorContext ctx)
    {
        if (!ImGuiRuntime.IsRunning) return;

        ImGuiRuntime.Begin(ctx);
        Panel.Draw(ctx);
        Field.Draw();
        if (Ui.BeginHidden()) Player.DrawMode(ctx);
        Ui.EndHidden();
        ImGuiConsole.Draw(ctx);
        ImGuiRuntime.End();
    }
}
