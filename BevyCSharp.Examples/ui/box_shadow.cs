using System.Text.Json.Nodes;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows box shadows on a node, with a panel of buttons that change the node's shape and the
// shadow's offset, blur, spread, count and samples, a button held down repeating.
internal static class BoxShadowExample
{
    // Bevy's BoxShadow is a list of shadows, which a wrapper does not type, so it is written as JSON.
    private const string Shadow = "bevy_ui::ui_node::BoxShadow";

    private static readonly Color Normal = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    private static readonly Color Hovered = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    private static readonly Color Pressed = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    // Width, height and corner radius of the five shapes, a radius of a million being round.
    private static readonly (string Name, float W, float H, float Radius)[] Shapes =
    [
        ("1", 164f, 164f, 0f), ("2", 164f, 164f, 41f), ("3", 164f, 164f, 1_000_000f), ("4", 240f, 80f, 32f), ("5", 80f, 240f, 32f),
    ];

    private sealed class Settings
    {
        public int Shape;
        public float X = 20f, Y = 20f, Blur = 10f, Spread = 15f;
        public int Count = 1;
        public uint Samples = 6;
    }

    private static Settings _settings = new();
    private static readonly List<(Entity Button, string Action, UiInteraction Last)> Buttons = [];
    private static readonly Dictionary<string, Entity> Values = [];
    private static Entity _camera, _node;
    private static string? _held;
    private static double _pressedAt, _lastRepeat;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_settings, _held) = (new Settings(), null);
            Buttons.Clear();
            Values.Clear();
            var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

            _camera = Render2d.SpawnCamera2d();
            ecs.Insert<BoxShadowSamplesRef>(_camera).Value = (uint)_settings.Samples;

            var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = Scene.Srgb8(128, 128, 128) });
            _node = Ui.SpawnNode(new UiSettings { Border = Sides.All(Length.Px(1f)), Align = UiAlign.Center, Justify = UiJustify.Center, BorderColor = (1f, 1f, 1f, 1f), Color = Scene.Srgb(0.21f, 0.21f, 0.21f) });
            ecs.InsertReflected(_node, Shadow, "[]");
            ecs.SetParent(_node, middle);

            var panel = Ui.SpawnNode(new UiSettings
            {
                Direction = UiDirection.Column,
                Absolute = true,
                Left = Length.Px(24f),
                Bottom = Length.Px(24f),
                Width = Length.Px(270f),
                Padding = Sides.All(Length.Px(16f)),
                Corners = Corners.All(Length.Px(12f)),
                Color = Scene.Srgb(0.12f, 0.12f, 0.12f, 0.85f),
                BorderColor = (1f, 1f, 1f, 0.15f),
            });
            ecs.Insert<ZIndexRef>(panel).Value = 10;

            Row(ecs, panel, font, "Shape", "shape", true);
            Row(ecs, panel, font, "X Offset", "x", false);
            Row(ecs, panel, font, "Y Offset", "y", false);
            Row(ecs, panel, font, "Blur", "blur", false);
            Row(ecs, panel, font, "Spread", "spread", false);
            Row(ecs, panel, font, "Count", "count", false);
            Row(ecs, panel, font, "Samples", "samples", false);

            var resetRow = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Align = UiAlign.Center, Height = Length.Px(36f), Margin = new Sides(Length.Zero, Length.Px(12f), Length.Zero, Length.Zero) });
            ecs.SetParent(resetRow, panel);
            var reset = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(90f), Height = Length.Px(32f), Justify = UiJustify.Center, Align = UiAlign.Center, Corners = Corners.All(Length.Px(8f)), Color = (Normal.R, Normal.G, Normal.B, 1f) });
            ecs.SetParent(Ui.SpawnText("Reset", new UiSettings(), new UiTextSettings { Font = font, FontSize = 16f }), reset);
            ecs.SetParent(reset, resetRow);
            Buttons.Add((reset, "reset", UiInteraction.None));

            Apply(ecs);
        }, "box_shadow.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var now = ctx.Time.ElapsedSeconds;
            var changed = false;

            for (var i = 0; i < Buttons.Count; i++)
            {
                var (button, action, last) = Buttons[i];
                var interaction = Ui.InteractionOf(button);
                if (interaction == last) continue;
                Buttons[i] = (button, action, interaction);

                ecs.Wrap<BackgroundColorRef>(button).Value = interaction switch { UiInteraction.Pressed => Pressed, UiInteraction.Hovered => Hovered, _ => Normal };
                if (interaction == UiInteraction.Pressed)
                {
                    Act(action);
                    (_held, _pressedAt, _lastRepeat, changed) = (action, now, now, true);
                }
                else if (_held == action)
                {
                    _held = null;
                }
            }

            // Held, a button acts again after 0.15 seconds and then every 0.08.
            if (_held is { } held && now - _pressedAt > 0.15 && now - _lastRepeat > 0.08)
            {
                Act(held);
                (_lastRepeat, changed) = (now, true);
            }

            if (changed) Apply(ecs);
        }, "box_shadow.Buttons");
    }

    private static void Act(string action)
    {
        var s = _settings;
        switch (action)
        {
            case "x+": s.X++; break;
            case "x-": s.X--; break;
            case "y+": s.Y++; break;
            case "y-": s.Y--; break;
            case "blur+": s.Blur = MathF.Max(s.Blur + 1f, 0f); break;
            case "blur-": s.Blur = MathF.Max(s.Blur - 1f, 0f); break;
            case "spread+": s.Spread++; break;
            case "spread-": s.Spread--; break;
            case "count+": s.Count = Math.Min(s.Count + 1, 3); break;
            case "count-": s.Count = Math.Max(s.Count - 1, 1); break;
            case "shape-": s.Shape = (s.Shape + Shapes.Length - 1) % Shapes.Length; break;
            case "shape+": s.Shape = (s.Shape + 1) % Shapes.Length; break;
            case "samples+": s.Samples++; break;
            case "samples-": s.Samples = Math.Max(s.Samples - 1, 1); break;
            case "reset": _settings = new Settings(); break;
        }
    }

    // The node's shape, its shadows, the camera's samples and the values shown, from the settings.
    private static void Apply(EcsWorld ecs)
    {
        var s = _settings;
        var (name, w, h, radius) = Shapes[s.Shape];
        var node = ecs.Wrap<NodeRef>(_node);
        (node.Width, node.Height) = (new Val.Px(w), new Val.Px(h));
        node.BorderRadiusTopLeft = node.BorderRadiusTopRight = node.BorderRadiusBottomRight = node.BorderRadiusBottomLeft = new Val.Px(radius);

        // One black shadow, or blue and yellow on opposite sides, and a red one turned a quarter.
        var shadows = new JsonArray();
        if (s.Count == 1) shadows.Add(Style((0f, 0f, 0f), s.X, s.Y));
        if (s.Count >= 2)
        {
            shadows.Add(Style((0f, 0f, 1f), s.X, s.Y));
            shadows.Add(Style((1f, 1f, 0f), -s.X, -s.Y));
        }

        if (s.Count == 3) shadows.Add(Style((1f, 0f, 0f), s.Y, -s.X));
        ecs.SetReflected(_node, Shadow, string.Empty, shadows.ToJsonString());
        ecs.Wrap<BoxShadowSamplesRef>(_camera).Value = (uint)s.Samples;

        Show(ecs, "shape", name);
        Show(ecs, "x", FormattableString.Invariant($"{s.X:0.0}"));
        Show(ecs, "y", FormattableString.Invariant($"{s.Y:0.0}"));
        Show(ecs, "blur", FormattableString.Invariant($"{s.Blur:0.0}"));
        Show(ecs, "spread", FormattableString.Invariant($"{s.Spread:0.0}"));
        Show(ecs, "count", $"{s.Count}");
        Show(ecs, "samples", $"{s.Samples}");

        JsonObject Style((float R, float G, float B) color, float x, float y) => new()
        {
            ["color"] = new JsonObject { ["Srgba"] = new JsonObject { ["red"] = color.R, ["green"] = color.G, ["blue"] = color.B, ["alpha"] = 0.8f } },
            ["x_offset"] = new JsonObject { ["Px"] = x },
            ["y_offset"] = new JsonObject { ["Px"] = y },
            ["spread_radius"] = new JsonObject { ["Px"] = s.Spread },
            ["blur_radius"] = new JsonObject { ["Px"] = s.Blur },
        };
    }

    private static void Show(EcsWorld ecs, string key, string text) => Ui.SetText(Values[key], text);

    // A labeled row with a button either side of its value.
    private static void Row(EcsWorld ecs, Entity panel, AssetHandle font, string label, string key, bool shape)
    {
        var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Align = UiAlign.Center, Height = Length.Px(32f) });
        ecs.SetParent(row, panel);

        var name = Ui.SpawnNode(new UiSettings { Width = Length.Px(80f), Justify = UiJustify.FlexEnd, Align = UiAlign.Center });
        ecs.SetParent(Ui.SpawnText(label, new UiSettings(), new UiTextSettings { Font = font, FontSize = 16f }), name);
        ecs.SetParent(name, row);

        Button(ecs, row, font, shape ? "<" : "-", key + "-", Length.Px(8f));
        var value = Ui.SpawnNode(new UiSettings { Width = Length.Px(48f), Height = Length.Px(28f), Margin = Sides.Horizontal(Length.Px(8f)), Justify = UiJustify.Center, Align = UiAlign.Center, Corners = Corners.All(Length.Px(6f)) });
        Values[key] = Ui.SpawnText(string.Empty, new UiSettings(), new UiTextSettings { Font = font, FontSize = 16f });
        ecs.SetParent(Values[key], value);
        ecs.SetParent(value, row);
        Button(ecs, row, font, shape ? ">" : "+", key + "+", Length.Zero);
    }

    private static void Button(EcsWorld ecs, Entity row, AssetHandle font, string text, string action, Length left)
    {
        // Bevy's start white and take the normal color the first frame their interaction is seen.
        var button = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(28f), Height = Length.Px(28f), Margin = new Sides(left, Length.Zero, Length.Zero, Length.Zero), Justify = UiJustify.Center, Align = UiAlign.Center, Corners = Corners.All(Length.Px(6f)), Color = (Normal.R, Normal.G, Normal.B, 1f) });
        ecs.SetParent(Ui.SpawnText(text, new UiSettings(), new UiTextSettings { Font = font, FontSize = 18f }), button);
        ecs.SetParent(button, row);
        Buttons.Add((button, action, UiInteraction.None));
    }
}
