using System.Numerics;
using Bevy;
using BevyCSharp.Editor.Behaviors;
using BevyCSharp.Editor.Framework;
using ImGuiNET;

namespace BevyCSharp.Editor;

/// <summary>
/// Drives the editor from the outside, so that what it does can be checked without a person.
/// </summary>
/// <remarks>
/// <para>
/// What <c>BCS_PROBE</c> names is run at fixed frames: something is put into the world, something
/// is clicked, something is typed, and what the interface ended up saying is printed. Together with
/// <c>BCS_SHOT</c> that makes a still of a known state rather than of whatever the editor happened
/// to be doing.
/// </para>
/// <para>
/// The clicks go into ImGui's own event queue, which is where a real pointer's report ends up, so
/// everything they set off behaves exactly as it would.
/// </para>
/// </remarks>
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
                if (script.Contains("select")) Select(ctx);
                break;

            case 120:
                if (script.Contains("dock")) EditorShell.Docked = !EditorShell.Docked;
                if (script.Contains("wide")) EditorShell.PanelWidth = 760f;
                if (script.Contains("narrow")) EditorShell.PanelWidth = 340f;
                if (script.Contains("tab")) EditorShell.OpenTab = 0;
                if (script.Contains("assets")) EditorShell.OpenTab = 1;
                if (script.Contains("style")) EditorShell.OpenTab = 2;
                break;

            case 140:
                if (script.Contains("click")) Click(0);

                // Nothing chosen, so that what a click on the scene picks is the click's doing and
                // not what was already there.
                if (script.Contains("pick")) EditorSelection.Clear();

                // The tool a frame before the hand reaches for a handle, because the handles are
                // drawn by the frame that knows which tool is in force.
                if (script.Contains("drag")) EditorTools.Current = EditorTool.Move;
                if (script.Contains("nogrid")) ViewportGizmos.ShowGrid = false;
                if (script.Contains("gridup")) ViewportGizmos.GridHeight = 4f;
                if (script.Contains("native")) EditorShell.Wear(EditorTheme.Native);

                if (script.Contains("shift"))
                {
                    foreach (var entity in ctx.Ecs.All())
                    {
                        if (ctx.Ecs.NameOf(entity) != "Cube") continue;

                        var was = ctx.Ecs.GetOrDefault<Transform>(entity);
                        ctx.Ecs.Set(entity, was with { Translation = new Vec3(3f, 0f, 0f) });
                    }
                }

                break;

            case 145:
                // Pressed on one frame and released on the next, because that is what a click is:
                // the engine decides an object was clicked by matching a release to the press that
                // landed on it, and both in one frame is one event, not two.
                if (script.Contains("pick")) Press(0);

                if (script.Contains("drag")) Press(0);

                break;

            case 146:
                if (script.Contains("pick")) Release(0);

                // Moved while held, over several frames, because a drag is a run of positions and
                // a handle that is grabbed and let go at once has moved nothing.
                if (script.Contains("drag")) Move(1);

                break;

            case 147 when script.Contains("scroll"):
                // Over the details panel and rolled down, so a capture can see what is past the
                // bottom of it without a hand.
                SyntheticInput.MoveTo(1450f, 700f);
                SyntheticInput.Wheel(script.Contains("deep") ? -60f : -12f);
                break;

            case 147:
            case 148:
                if (script.Contains("drag")) Move(1);
                break;

            case 149:
                if (script.Contains("drag")) Release(1);
                break;

            case 150:
                if (Environment.GetEnvironmentVariable("BCS_PROBE_TYPE") is { Length: > 0 } typed)
                {
                    SyntheticInput.Type(typed);
                    SyntheticInput.Key(ImGuiKey.Enter);

                    Console.WriteLine($"[probe] typed {typed}");
                }

                break;

            case 155:
                if (script.Contains("click")) Click(1);
                break;

            case 170:
                Report(script, ctx);
                break;

            // Well after the shot, which is read back off the GPU over several frames: quitting on
            // the frame it was asked for loses the file.
            case 200:
                ctx.Exit();
                break;
        }
    }

    /// <summary>Puts a component with one of everything on the cube, and selects it.</summary>
    private static void Select(BehaviorContext ctx)
    {
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
            return;
        }
    }

    /// <summary>
    /// Clicks where BCS_PROBE_CLICK says, in logical pixels.
    /// </summary>
    /// <remarks>
    /// Points rather than names, because an immediate mode interface has no elements to look up: a
    /// widget is a call that happened, and where it landed is what the layout decided. Several
    /// points separated by semicolons are clicked one after another, a few frames apart.
    /// </remarks>
    private static void Click(int step)
    {
        var wanted = (Environment.GetEnvironmentVariable("BCS_PROBE_CLICK") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (step >= wanted.Length) return;

        var parts = wanted[step].Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length != 2
            || !float.TryParse(parts[0], out var x)
            || !float.TryParse(parts[1], out var y))
        {
            Console.WriteLine($"[probe] {wanted[step]} is not a point");
            return;
        }

        SyntheticInput.Press(x, y);
        SyntheticInput.Release(x, y);

        Console.WriteLine($"[probe] clicked {x:0},{y:0}");
    }

    /// <summary>Puts the pointer on a point and holds the button.</summary>
    private static void Press(int step)
    {
        if (Point(step) is not { } at) return;

        SyntheticInput.Press(at.X, at.Y);
        Console.WriteLine($"[probe] pressed {at.X:0},{at.Y:0}");
    }

    /// <summary>Moves the pointer without changing what is held.</summary>
    private static void Move(int step)
    {
        if (Point(step) is not { } at) return;

        SyntheticInput.MoveTo(at.X, at.Y);
    }

    /// <summary>And lets go of it, which is what makes the click.</summary>
    private static void Release(int step)
    {
        if (Point(step) is not { } at) return;

        SyntheticInput.Release(at.X, at.Y);
    }

    /// <summary>What BCS_PROBE_CLICK names, as a point.</summary>
    private static (float X, float Y)? Point(int step)
    {
        var wanted = (Environment.GetEnvironmentVariable("BCS_PROBE_CLICK") ?? string.Empty)
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (step >= wanted.Length) return null;

        var parts = wanted[step].Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length != 2
            || !float.TryParse(parts[0], out var x)
            || !float.TryParse(parts[1], out var y))
        {
            return null;
        }

        return (x, y);
    }

    /// <summary>Prints where the interface put things, and what the world made of it.</summary>
    private static void Report(string script, BehaviorContext ctx)
    {
        var scene = EditorShell.Scene;
        var panel = EditorShell.Panel;

        Console.WriteLine(
            $"[probe] docked={EditorShell.Docked} panel={panel.X:0},{panel.Y:0}"
            + $" {panel.Width:0}x{panel.Height:0}");

        Console.WriteLine(
            $"[probe] scene={scene.X:0},{scene.Y:0} {scene.Width:0}x{scene.Height:0}"
            + $" split={(EditorShell.Stacked ? "above" : "beside")}"
            + $" tab={EditorShell.OpenTab}");

        Console.WriteLine($"[probe] menu open: {EditorShell.MenuOpen}");

        var style = ImGui.GetStyle();

        Console.WriteLine(
            $"[probe] theme={EditorTheme.Current.Name} frame-rounding={style.FrameRounding:0.##}"
            + $" child={style.ChildRounding:0.##} window={style.WindowRounding:0.##}"
            + $" frame-padding={style.FramePadding.Y:0.##}");


        // What the orientation gizmo reads, so an arm pointing the wrong way can be checked
        // against numbers rather than against a screenshot.
        if (!EditorSelection.Camera.IsNone)
        {
            var view = ctx.Ecs.GetOrDefault<GlobalTransform>(EditorSelection.Camera);

            Console.WriteLine(
                $"[probe] eye {view.Translation.X:0.##},{view.Translation.Y:0.##},{view.Translation.Z:0.##}"
                + $" grid={ViewportGizmos.GridHeight:0.##} shown={ViewportGizmos.ShowGrid}");

            Console.WriteLine(
                $"[probe] camera right={view.XAxis.X:0.###},{view.XAxis.Y:0.###},{view.XAxis.Z:0.###}"
                + $" up={view.YAxis.X:0.###},{view.YAxis.Y:0.###},{view.YAxis.Z:0.###}"
                + $" back={view.ZAxis.X:0.###},{view.ZAxis.Y:0.###},{view.ZAxis.Z:0.###}");
        }

        Console.WriteLine(EditorSelection.Any
            ? $"[probe] selected {ctx.Ecs.NameOf(EditorSelection.Current) ?? "?"}"
            : "[probe] nothing selected");

        if (EditorSelection.Any)
        {
            var tags = new List<string>();

            foreach (var id in ctx.Ecs.ComponentsOf(EditorSelection.Current))
            {
                if (ComponentSchemas.For(id) is not { Fields.Count: 0 } schema) continue;

                tags.Add(schema.Name);
            }

            Console.WriteLine($"[probe] tags {string.Join(", ", tags)}");

            foreach (var one in EditorSelection.All)
            {
                var has = Render.TryGetBounds(one, out var low, out var high);

                Console.WriteLine(
                    $"[probe] bounds {ctx.Ecs.NameOf(one) ?? "?"} {has}"
                    + $" {low.X:0.##},{low.Y:0.##},{low.Z:0.##}"
                    + $" to {high.X:0.##},{high.Y:0.##},{high.Z:0.##}");
            }
        }

        if (EditorSelection.Any && ctx.Ecs.TryGet<Transform>(EditorSelection.Current, out var where))
        {
            Console.WriteLine(
                $"[probe] at {where.Translation.X:0.###},{where.Translation.Y:0.###},"
                + $"{where.Translation.Z:0.###}");
        }

        if (EditorSelection.Any)
        {
            foreach (var id in ctx.Ecs.ComponentsOf(EditorSelection.Current))
            {
                if (ComponentSchemas.For(id) is not { Name: "Showcase" } schema) continue;

                foreach (var field in schema.Fields)
                {
                    if (field.Name is not ("Speed" or "Enabled")) continue;

                    Console.WriteLine(
                        $"[probe] Showcase.{field.Name} = "
                        + $"{field.Read(ctx.Ecs, EditorSelection.Current)}");
                }
            }
        }

        if (script.Contains("camera")
            && EditorSelection.Camera is { IsNone: false } eye
            && ctx.Ecs.TryGet<GlobalTransform>(eye, out var basis))
        {
            Console.WriteLine(
                $"[probe] camera x={basis.XAxis.X:0.##},{basis.XAxis.Y:0.##},{basis.XAxis.Z:0.##}"
                + $" y={basis.YAxis.X:0.##},{basis.YAxis.Y:0.##},{basis.YAxis.Z:0.##}"
                + $" z={basis.ZAxis.X:0.##},{basis.ZAxis.Y:0.##},{basis.ZAxis.Z:0.##}");
        }

        if (script.Contains("console"))
        {
            Console.WriteLine($"[probe] the log holds {ConsoleLog.All().Length} lines");
        }

        _ = Vector2.Zero;
    }
}
