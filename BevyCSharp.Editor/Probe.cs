using Bevy;
using BevyCSharp.Editor.Behaviors;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor;

/// <summary>TEMPORARY: sets up a state to photograph.</summary>
[Behavior]
public partial struct Probe
{
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

                    ctx.Ecs.Add(entity, new Showcase
                    {
                        Speed = 7.5f,
                        Mass = 1.25f,
                        Count = 3,
                        Tint = new Vec3(0.85f, 0.35f, 0.15f),
                        Offset = new Vec3(0f, 1f, 0f),
                        Mode = ShowcaseMode.Running,
                        Parts = ShowcaseParts.Head | ShowcaseParts.Tail,
                        Ticks = 12,
                    });

                    EditorSelection.Select(entity);
                    break;
                }

                break;

            case 120 when script.Contains("info"):
                EditorShell.Show(new Panels.InfoPanel());
                break;

            case >= 150 and <= 170 when script.Contains("fold"):
                if (EditorShell.Find<Panels.DataPanel>() is not { } panel) break;

                for (var i = 0; i < Panels.DataPanel.Rows; i++)
                {
                    var name = panel.Names[i].Trim();
                    if (name is "Transform" or "Visibility") panel.Fold(i);
                }

                break;

            // Opens a menu squarely over the panel on the left, which the shell would otherwise
            // step out of the way of.
            case 150 when script.Contains("over"):
                EditorShell.ShowMenu("Spawn", 300f, 18f);
                break;

            case >= 160 and <= 200 when script.Contains("over"):
                if (Xui.Element("menu") is { IsNone: false } menu) Xui.SetLayer(menu, 5000);


                if (ctx.Time.FrameCount == 199)
                {
                    var ids = new List<string> { "menu", "menu-title", "mrow-0", "barleft" };
                    for (var i = 0; i < 4; i++)
                    {
                        ids.Add($"tb-{i}");
                        ids.Add($"tbimg-{i}");
                    }

                    // Every node under a toolbar button, since what is drawn may not be the
                    // element the id names.
                    foreach (var id in new[] { "tb-1", "tbimg-1", "menu" })
                    {
                        var top = Xui.Element(id);
                        if (top.IsNone) continue;

                        Console.Error.WriteLine($"[probe] {id} = {top.Bits} stack {Xui.StackOf(top)}");

                        foreach (var child in ctx.Ecs.ChildrenOf(top))
                        {
                            Console.Error.WriteLine(
                                $"[probe]   child {child.Bits} stack {Xui.StackOf(child)}");
                        }
                    }

                    foreach (var id in ids)
                    {
                        var el = Xui.Element(id);
                        Console.Error.WriteLine(
                            $"[probe] {id} stack {(el.IsNone ? -1 : Xui.StackOf(el))}"
                            + $" copies {Xui.Count(id)}");
                    }
                }

                break;

            case >= 200 and <= 208 when script.Contains("scroll"):
                if (Xui.TryRect(Xui.Element("data"), out var box))
                {
                    SyntheticInput.MoveTo(box.X + (box.Width / 2f), box.Y + (box.Height / 2f));
                    SyntheticInput.Wheel(-3f);
                }

                break;
        }
    }
}
