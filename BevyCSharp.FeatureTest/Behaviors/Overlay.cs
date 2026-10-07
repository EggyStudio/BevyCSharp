using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.FeatureTest.Behaviors;

/// <summary>
/// The debug overlay F3 shows, the frame time, where the view is and which way it faces, the
/// entities alive and what the program holds.
/// </summary>
/// <remarks>
/// <para>
/// F3 shows it and hides it as the key is let go, and not where another key was pressed while it
/// was held, so F3 held with another key is a chord of its own, as Minecraft's is. What the program
/// holds is the <c>memory</c> command's answer, asked twice a second rather than every frame, since
/// it reads the bridge's counts and every asset kind, and shown a name and a number a line, bytes
/// in megabytes.
/// </para>
/// </remarks>
public static class Overlay
{
    private static bool _chord;
    private static double _asked = double.NegativeInfinity;
    private static string[] _held = [];

    /// <summary>Whether the overlay is shown, which F3 and the panel's debug page set.</summary>
    public static bool Shown => Settings.Current.Overlay;

    /// <summary>Shows or hides it on F3, and draws it while it is shown.</summary>
    public static void Draw(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);
        var input = ctx.Input;

        if (input.KeyDown(Key.F3) && input.AnyKeyPressed() && !input.KeyPressed(Key.F3)) _chord = true;
        if (input.KeyReleased(Key.F3))
        {
            if (!_chord && !ImGuiRuntime.Typing)
            {
                Settings.Change(s => s with { Overlay = !s.Overlay });
                Console.WriteLine($"[Overlay] {(Shown ? "shown" : "hidden")} on frame {ctx.Time.FrameCount}");
            }
            _chord = false;
        }

        if (!Shown) return;

        if (ctx.Time.ElapsedSeconds - _asked >= 0.5)
        {
            _asked = ctx.Time.ElapsedSeconds;
            _held = Held(ctx);
        }

        var window = ImGuiRuntime.Size;
        ImGui.SetNextWindowPos(new Vector2(window.X - 16f, 16f), ImGuiCond.Always, new Vector2(1f, 0f));
        ImGui.SetNextWindowBgAlpha(0.6f);

        const ImGuiWindowFlags Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoFocusOnAppearing;

        if (ImGui.Begin("Overlay##feature-test", Flags))
        {
            var delta = ctx.Time.Delta;
            ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture,
                $"frame {delta * 1000f:0.0} ms, {ctx.Time.SmoothedFps:0} fps, frame {ctx.Time.FrameCount}"));

            if (Scene.Camera is { } camera && ctx.Ecs.TryGet<Transform>(camera, out var place))
            {
                var at = place.Translation;
                var forward = place.Rotation * new Vec3(0f, 0f, -1f);
                var heading = (MathF.Atan2(forward.X, -forward.Z) * 180f / MathF.PI + 360f) % 360f;
                var pitch = MathF.Asin(Math.Clamp(forward.Y, -1f, 1f)) * 180f / MathF.PI;

                ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"at {at.X:0.0} {at.Y:0.0} {at.Z:0.0}"));
                ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"facing {Compass(heading)} {heading:0}°, pitch {pitch:0}°"));
            }

            ImGui.TextUnformatted(string.Create(CultureInfo.InvariantCulture, $"entities {ctx.Ecs.All().Length}"));
            ImGui.Separator();
            foreach (var line in _held) ImGui.TextUnformatted(line);
        }

        ImGui.End();
    }

    /// <summary>
    /// The eight points of the compass a heading in degrees is nearest, north being minus Z.
    /// </summary>
    internal static string Compass(float heading) =>
        new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" }[(int)MathF.Round(heading / 45f) % 8];

    /// <summary>
    /// The memory command's answer, a name and a number a line, bytes in megabytes.
    /// </summary>
    internal static string[] Held(BehaviorContext ctx)
    {
        string answer;
        using (ConsoleHost.Lend(ctx.World)) answer = ConsoleCommands.Run("memory") ?? string.Empty;

        var words = answer.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        for (var i = 0; i + 1 < words.Length; i += 2)
        {
            if (!long.TryParse(words[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number)) continue;

            var name = words[i];
            var bytes = name is "managed" or "heap" or "process" or "nativeBytes";
            lines.Add(bytes
                ? string.Create(CultureInfo.InvariantCulture, $"{name} {number / (1024.0 * 1024.0):0.0} MB")
                : string.Create(CultureInfo.InvariantCulture, $"{name} {number}"));
        }

        return [.. lines];
    }
}
