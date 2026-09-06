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
                        Blend = 0.4f,
                        Fill = 62f,
                        Corner = new Vec3(1f, 0.5f, -2f),
                        Radius = 3f,
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

            // A menu opened from a row of the inspector, which should sit under that row rather
            // than step out from under the panel.
            case 150 when script.Contains("rowmenu"):
                if (EditorShell.Find<Panels.DataPanel>() is { } rows)
                {
                    for (var i = 0; i < Panels.DataPanel.Rows; i++)
                    {
                        if (rows.Names[i].Trim() != "Speed") continue;

                        rows.RowMenu(i);
                        break;
                    }
                }

                break;

            // What is drawn over what, as numbers rather than as a guess at a screenshot.
            case 199 when script.Contains("rowmenu"):
                foreach (var id in new[] { "menu", "mrow-0", "data", "drow-4", "dv-4", "dsw-15" })
                {
                    Console.Error.WriteLine($"[probe] {id} copies {Xui.Count(id)}");
                }

                foreach (var id in new[] { "menu", "mrow-0", "data", "drow-4", "dv-4", "dsw-15" })
                {
                    var found = Xui.Element(id);
                    if (found.IsNone)
                    {
                        Console.Error.WriteLine($"[probe] {id} missing");
                        continue;
                    }

                    Console.Error.WriteLine(
                        $"[probe] {id} entity {found.Bits} stack {Xui.StackOf(found)}");

                    foreach (var child in ctx.Ecs.ChildrenOf(found))
                    {
                        Console.Error.WriteLine(
                            $"[probe]   child {child.Bits} stack {Xui.StackOf(child)}");
                    }
                }

                break;

            // The picker a patch of color opens, over the row it was opened from.
            case 150 when script.Contains("picker"):
                if (EditorShell.Find<Panels.DataPanel>() is { } colored)
                {
                    for (var i = 0; i < Panels.DataPanel.Rows; i++)
                    {
                        if (colored.Names[i].Trim() != "Tint") continue;

                        colored.PressSwatch(i);
                        break;
                    }
                }

                break;

            // The pointer left over a row that has something to say, so the hint is on screen.
            case >= 150 and <= 210 when script.Contains("hint"):
                if (Xui.Element("dname-9") is { IsNone: false } named
                    && Xui.TryRect(named, out var over))
                {
                    SyntheticInput.MoveTo(over.X + 20f, over.Y + (over.Height / 2f));
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
