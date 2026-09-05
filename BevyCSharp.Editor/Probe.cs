using Bevy;
using BevyCSharp.Editor.Framework;
using BevyCSharp.Editor.Panels;

namespace BevyCSharp.Editor;

/// <summary>TEMPORARY: opens a panel and reports where it is on each of the first frames.</summary>
[Behavior]
public partial struct Probe
{
    private static (float X, float Y) _grab;

    /// <summary>Runs whatever BCS_PROBE names.</summary>
    [OnUpdate]
    public static void Run(BehaviorContext ctx)
    {
        if (Environment.GetEnvironmentVariable("BCS_PROBE") is not { Length: > 0 } script) return;

        switch (ctx.Time.FrameCount)
        {
            case 100:
                foreach (var entity in ctx.Ecs.All())
                {
                    if (ctx.Ecs.NameOf(entity) != "Cube") continue;

                    EditorSelection.Select(entity);
                    break;
                }

                break;

            case 150 when script.Contains("settings"):
                EditorShell.Show(new SettingsPanel());
                break;

            case 150:
                Xui.TryRect(Xui.Element("tr-0"), out var button);
                _grab = (button.X + (button.Width / 2f), button.Y + (button.Height / 2f));
                SyntheticInput.MoveTo(_grab.X, _grab.Y);
                break;

            case 152:
                SyntheticInput.Press(_grab.X, _grab.Y);
                break;

            case 162:
                SyntheticInput.Release(_grab.X, _grab.Y);
                break;

            case >= 163 and <= 172 when !script.Contains("settings"):
                if (Xui.Element("info") is { IsNone: false } e && Xui.TryRect(e, out var r))
                {
                    Console.WriteLine(
                        $"[probe] f{ctx.Time.FrameCount} info {r.X:F0},{r.Y:F0} {r.Width:F0}x{r.Height:F0}");
                }

                break;
        }
    }
}
