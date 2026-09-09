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

        // Every report the interface makes, while the frames of interest are running.
        EditorShell.Watching = script.Contains("tick")
            && ctx.Time.FrameCount is >= 148 and <= 172;

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

            // A tick and a drag, reported frame by frame: what the panel holds, and what the world
            // says, which is the only way to tell which half of the round trip is losing the edit.
            case >= 140 and <= 175 when script.Contains("tick"):
                if (EditorShell.Find<Panels.DataPanel>() is not { } watched) break;

                var flag = -1;
                var bar = -1;

                for (var i = 0; i < Panels.DataPanel.Rows; i++)
                {
                    if (watched.Names[i].Trim() == "Enabled") flag = i;
                    if (watched.Names[i].Trim() == "Speed") bar = i;
                }

                var schema = ComponentSchemas.For("BevyCSharp.Editor.Behaviors.Showcase");
                var subject = EditorSelection.Current;

                if (ctx.Time.FrameCount == 150 && flag >= 0
                    && Xui.TryRect(Xui.Element($"dc-{flag}"), out var hit))
                {
                    SyntheticInput.MoveTo(hit.X + (hit.Width / 2f), hit.Y + (hit.Height / 2f));
                    SyntheticInput.Press(hit.X + (hit.Width / 2f), hit.Y + (hit.Height / 2f));
                }

                if (ctx.Time.FrameCount == 151 && flag >= 0
                    && Xui.TryRect(Xui.Element($"dc-{flag}"), out var up))
                {
                    SyntheticInput.Release(up.X + (up.Width / 2f), up.Y + (up.Height / 2f));
                }

                if (ctx.Time.FrameCount == 149 && flag >= 0 && bar >= 0)
                {
                    Console.Error.WriteLine(
                        $"[probe] element dc-{flag} is {Xui.Element($"dc-{flag}").Bits}"
                        + $", dsl-{bar} is {Xui.Element($"dsl-{bar}").Bits}");
                }

                if (ctx.Time.FrameCount is >= 149 and <= 156 && flag >= 0)
                {
                    Console.Error.WriteLine(
                        $"[probe] {ctx.Time.FrameCount} tick row {flag}"
                        + $" panel {watched.Flags[flag]}"
                        + $" world {schema?.Read(ctx.Ecs, subject, "Enabled")}");
                }

                if (ctx.Time.FrameCount == 165 && bar >= 0
                    && Xui.TryRect(Xui.Element($"dsl-{bar}"), out var slid))
                {
                    SyntheticInput.MoveTo(slid.X + 10f, slid.Y + (slid.Height / 2f));
                    SyntheticInput.Press(slid.X + 10f, slid.Y + (slid.Height / 2f));
                }

                if (ctx.Time.FrameCount == 167 && bar >= 0
                    && Xui.TryRect(Xui.Element($"dsl-{bar}"), out var moved))
                {
                    SyntheticInput.MoveTo(
                        moved.X + (moved.Width * 0.75f), moved.Y + (moved.Height / 2f));
                }

                if (ctx.Time.FrameCount == 169 && bar >= 0
                    && Xui.TryRect(Xui.Element($"dsl-{bar}"), out var done))
                {
                    SyntheticInput.Release(
                        done.X + (done.Width * 0.75f), done.Y + (done.Height / 2f));
                }

                if (ctx.Time.FrameCount is >= 164 and <= 175 && bar >= 0)
                {
                    Console.Error.WriteLine(
                        $"[probe] {ctx.Time.FrameCount} bar row {bar}"
                        + $" panel {watched.Bars[bar]:F1}"
                        + $" widget {Xui.GetNumber(Xui.Element($"dsl-{bar}")):F1}"
                        + $" world {schema?.Read(ctx.Ecs, subject, "Speed")}");
                }

                break;

            // The asset browser, which draws a picture of what it can.
            case 140 when script.Contains("tiles"):
                if (EditorTabs.Find("Assets") is { } files) EditorTabs.Open(files);
                EditorAssets.Enter("icons/ui");
                break;

            // The console a key drops into the middle of the window, with something typed into it.
            case 140 when script.Contains("console"):
                Panels.QuickConsolePanel.Toggle();
                break;

            case 150 when script.Contains("console"):
                Console.WriteLine("[scene] a line the console shows");
                Console.Error.WriteLine("[scene] and one it shows in red");
                break;

            case >= 160 and <= 190 when script.Contains("console"):
                if (EditorShell.Find<Panels.QuickConsolePanel>() is not { } quick) break;

                switch (ctx.Time.FrameCount)
                {
                    case 160:
                        quick.Typed = "help";
                        break;

                    case 165:
                        quick.Run(quick.Typed);
                        break;

                    case 170:
                        quick.Typed = "entiti";
                        break;

                    case 175:
                        quick.Complete();
                        break;

                    case 180:
                        quick.Run(quick.Typed);
                        break;
                }

                break;

            // The console tab along the bottom, with the same log in it.
            case 140 when script.Contains("band"):
                if (EditorTabs.Find("Console") is { } tab) EditorTabs.Open(tab);
                break;

            case 145 when script.Contains("wide"):
                foreach (var many in new[] { 40, 80, 120, 160, 200 })
                {
                    Console.WriteLine($"{many}:" + new string('x', many));
                }

                break;

            case 150 when script.Contains("band"):
                Console.WriteLine("[scene] an ordinary line");
                Console.Error.WriteLine("[scene] something that went wrong");
                break;

            case 160 when script.Contains("band"):
                if (EditorShell.Find<Panels.ConsolePanel>() is { } band)
                {
                    band.Typed = "components";
                    band.Run(band.Typed);
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

            case 199 when script.Contains("picker"):
                foreach (var id in new[] { "color", "c-name", "data", "dname-14", "dv-14" })
                {
                    var found = Xui.Element(id);
                    if (found.IsNone) continue;

                    Console.Error.WriteLine(
                        $"[probe] {id} stack {Xui.StackOf(found)}");

                    foreach (var child in ctx.Ecs.ChildrenOf(found))
                    {
                        Console.Error.WriteLine(
                            $"[probe]   child {child.Bits} stack {Xui.StackOf(child)}");
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
