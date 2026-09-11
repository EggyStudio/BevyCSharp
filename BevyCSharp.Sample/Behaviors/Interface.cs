using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// The game's own interface.
/// </summary>
/// <remarks>
/// <para>
/// The whole of what a game writes to have one: start the runtime once, and between
/// <see cref="ImGuiRuntime.Begin"/> and <see cref="ImGuiRuntime.End"/> call ImGui. There is no
/// document to load, no binding to declare and nothing to keep in step: what is on screen is what
/// this frame's calls said.
/// </para>
/// <para>
/// It needs a bridge with the interface compiled in (<c>build/build-native.sh --editor</c>) and
/// <c>Config.Gui</c> asked for, and says so rather than drawing nothing.
/// </para>
/// </remarks>
[Behavior]
public partial struct Interface
{
    private static int _score;
    private static bool _spinning = true;

    /// <summary>Starts the interface.</summary>
    [OnStartup]
    public static void Open(BehaviorContext ctx)
    {
        ImGuiRuntime.Start();
    }

    /// <summary>Draws it, once a frame.</summary>
    [OnUpdate]
    public static void Tick(BehaviorContext ctx)
    {
        if (!ImGuiRuntime.IsRunning) return;

        ImGuiRuntime.Begin(ctx);

        ImGui.SetNextWindowPos(new Vector2(16f, 16f), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.75f);

        if (ImGui.Begin("Sample", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings))
        {
            ImGui.Text($"{1f / MathF.Max(ctx.Time.Delta, 0.0001f):0} fps");
            ImGui.Separator();

            ImGui.Text($"Score {_score}");
            if (ImGui.Button("Add one")) _score++;

            ImGui.Checkbox("Spinning", ref _spinning);
        }

        ImGui.End();

        ImGuiRuntime.End();
    }
}
