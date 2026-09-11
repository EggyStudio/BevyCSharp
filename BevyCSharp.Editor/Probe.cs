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
                if (script.Contains("style")) EditorShell.OpenTab = 1;
                break;

            case 140:
                if (script.Contains("click")) Click(0);
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

        Console.WriteLine(EditorSelection.Any
            ? $"[probe] selected {ctx.Ecs.NameOf(EditorSelection.Current) ?? "?"}"
            : "[probe] nothing selected");

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

        if (script.Contains("console"))
        {
            Console.WriteLine($"[probe] the log holds {ConsoleLog.All().Length} lines");
        }

        _ = Vector2.Zero;
    }
}
