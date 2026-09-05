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

            case >= 170 and <= 190 when script.Contains("three"):
                Xui.SetText(Xui.Element("dvb-0"), "22");
                Xui.SetText(Xui.Element("dvc-0"), "33");
                break;

            case 194 when script.Contains("rows"):
                foreach (var open in EditorShell.Open)
                {
                    Console.WriteLine(
                        $"[probe] open {open.GetType().Name} showing {EditorShell.IsShowing(open)}");
                }

                break;

            case 190 when script.Contains("hide"):
                if (EditorShell.Find<DataPanel>() is { } data) EditorShell.Conceal(data);
                break;

            case 193 when script.Contains("rows"):
                for (var i = 0; i < 14; i++)
                {
                    foreach (var id in new[] { $"drow-{i}", $"dnum0-{i}", $"dv-{i}", $"dc-{i}" })
                    {
                        var el = Xui.Element(id);
                        if (el.IsNone || !Xui.TryRect(el, out var box)) continue;
                        if (box.Height < 1f) continue;

                        Console.WriteLine(
                            $"[probe] {id} {box.X:F0},{box.Y:F0} {box.Width:F0}x{box.Height:F0} "
                            + $"visible {Xui.IsVisible(el)}");
                    }
                }

                break;

            case 150 when script.Contains("info"):
                EditorShell.Show(new InfoPanel());
                break;

            case 150 when script.Contains("settings"):
                EditorShell.Show(new SettingsPanel());
                break;

            case 150 when script.Contains("level"):
                EditorShell.ShowMenu("Panels", 300f, 100f);
                break;

            case 150 when script.Contains("menu"):
                EditorShell.ShowMenu(string.Empty, 300f, 100f);
                break;

            case 150 when script.Contains("place"):
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

            case >= 163 and <= 175 when script.Contains("place"):
                if (Xui.Element("info") is { IsNone: false } e && Xui.TryRect(e, out var r))
                {
                    Console.WriteLine(
                        $"[probe] f{ctx.Time.FrameCount} info {r.X:F0},{r.Y:F0} {r.Width:F0}x{r.Height:F0}"
                        + $" root {e.Bits} gen {Xui.Generation} vis {Xui.IsVisible(e)}");
                }

                break;
        }
    }
}
