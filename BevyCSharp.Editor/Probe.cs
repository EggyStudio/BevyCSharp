using Bevy;
using BevyCSharp.Editor.Behaviors;
using BevyCSharp.Editor.Framework;

namespace BevyCSharp.Editor;

/// <summary>
/// Drives the editor from the outside, so that what it does can be checked without a person.
/// </summary>
/// <remarks>
/// <para>
/// What <c>BCS_PROBE</c> names is run at fixed frames: something is put into the world, something
/// is clicked, something is typed, and what the page ended up saying is printed. Together with
/// <c>BCS_SHOT</c> that makes a still of a known state rather than of whatever the editor happened
/// to be doing.
/// </para>
/// <para>
/// It asks the page questions rather than asking a panel: an element by selector, the box it was
/// laid out in, the text it holds, what a field holds. That is the same interface a test has,
/// which is the point.
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

            case 130:
                // The console, driven the way a person drives it: dropped in, typed into, and the
                // answer read off the page.
                if (script.Contains("console"))
                {
                    ConsolePage.Toggle();
                    Console.WriteLine($"[probe] console open: {ConsolePage.IsOpen}");
                }

                break;

            case 140:
                if (script.Contains("click")) Click(0);

                if (script.Contains("console"))
                {
                    SyntheticInput.Type("help");
                    SyntheticInput.Key("Enter");
                }

                break;

            case 150:
                // Typed into whatever the click left holding the keyboard, and finished with, so
                // that a value going in and coming back is one run rather than a person's hands.
                if (Environment.GetEnvironmentVariable("BCS_PROBE_TYPE") is { Length: > 0 } typed)
                {
                    for (var back = 0; back < 8; back++) SyntheticInput.Key("Backspace");

                    SyntheticInput.Type(typed);
                    SyntheticInput.Key("Enter");

                    Console.WriteLine($"[probe] typed {typed}");
                }

                break;

            case 155:
                if (script.Contains("click")) Click(1);
                break;

            case 160:
                if (script.Contains("click")) Click(2);
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

    /// <summary>Clicks what BCS_PROBE_CLICK selects, in the middle of it.</summary>
    /// <remarks>
    /// Several selectors separated by commas are clicked one after another, a few frames apart,
    /// which is how a menu is opened and then something in it picked.
    /// </remarks>
    private static void Click(int step)
    {
        var wanted = (Environment.GetEnvironmentVariable("BCS_PROBE_CLICK") ?? "#tool-move")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (step >= wanted.Length) return;

        var selector = wanted[step];
        var element = Dom.Select(selector);

        if (!element.Exists || !Dom.TryRect(element, out var rect))
        {
            Console.WriteLine($"[probe] nothing matched {selector}");
            return;
        }

        var x = rect.Left + (rect.Width * 0.5f);
        var y = rect.Top + (rect.Height * 0.5f);

        SyntheticInput.Press(x, y);
        SyntheticInput.Release(x, y);

        Console.WriteLine($"[probe] clicked {selector} at {x:0},{y:0}");
    }

    /// <summary>Prints where the parts of the page ended up, and what they say.</summary>
    private static void Report(string script, BehaviorContext ctx)
    {
        foreach (var id in new[] { "toolbar", "hierarchy", "viewport", "inspector", "status-frame" })
        {
            var element = Dom.Element(id);

            if (!element.Exists)
            {
                Console.WriteLine($"[probe] {id}: missing");
                continue;
            }

            var laid = Dom.TryRect(element, out var rect)
                ? $"{rect.Left:0},{rect.Top:0} {rect.Width:0}x{rect.Height:0}"
                : "not laid out";

            Console.WriteLine($"[probe] {id}: {laid}");
        }

        if (Environment.GetEnvironmentVariable("BCS_PROBE_CLICK") is { Length: > 0 } clicked)
        {
            var selector = clicked.Split(',')[0].Trim();
            var element = Dom.Select(selector);

            Console.WriteLine(element.Exists
                ? $"[probe] {selector}: on={Dom.Attribute(element, "data-on")}"
                    + $" value={Dom.GetValue(element)}"
                : $"[probe] {selector} is gone");

            var over = Dom.Select("#overlays > *");

            Console.WriteLine(over.Exists && Dom.TryRect(over, out var box)
                ? $"[probe] overlay: {box.Left:0},{box.Top:0} {box.Width:0}x{box.Height:0}"
                : "[probe] overlay: nothing");
        }

        // What the component made of what was typed, which is the half a field cannot show: a
        // field holding 12.5 may be a value that was written or a value that never left the box.
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
            var lines = Dom.Select("#console-lines");

            Console.WriteLine(lines.Exists
                ? $"[probe] console says {Dom.GetText(lines).Length} characters"
                : "[probe] the console has no lines");
        }

        if (script.Contains("text"))
        {
            foreach (var selector in new[] { "#status-selection", ".tree-item", ".group > header" })
            {
                var element = Dom.Select(selector);
                var text = element.Exists ? Dom.GetText(element) : "(none)";

                Console.WriteLine($"[probe] {selector}: {text}");
            }
        }
    }
}
