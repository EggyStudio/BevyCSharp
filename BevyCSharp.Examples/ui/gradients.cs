using System.Text.Json.Nodes;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows linear, radial and conic gradients on nodes of several shapes, each with a gradient
// border, three sets of color stops, the large linear ones turning, and a button that steps every
// gradient through the color spaces it can be blended in.
internal static class Gradients
{
    // A gradient is a list of gradients, each holding a list of stops, which a wrapper does not
    // type, so both are written as JSON.
    private const string Background = "bevy_ui::gradients::BackgroundGradient";
    private const string Border = "bevy_ui::gradients::BorderGradient";

    private static readonly string[] Spaces = ["Oklaba", "Oklcha", "OklchaLong", "Srgba", "LinearRgba", "Hsla", "HslaLong", "Hsva", "HsvaLong"];

    // A node's gradient, kept to be written again when its color space or its angle changes.
    private sealed class Painted(Entity node, string kind, float angle, (Color Color, float? Percent)[] stops, bool animated)
    {
        public Entity Node { get; } = node;
        public string Kind { get; } = kind;
        public float Angle { get; set; } = angle;
        public (Color Color, float? Percent)[] Stops { get; } = stops;
        public bool Animated { get; } = animated;
    }

    private static readonly List<Painted> Nodes = [];
    private static Entity _button, _label;
    private static int _space;
    private static UiInteraction _last;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Nodes.Clear();
            (_space, _last) = (0, UiInteraction.None);
            Render2d.SpawnCamera2d();

            var (red, blue, lime, orange, yellow, green, indigo, violet) = (C(255, 0, 0), C(0, 0, 255), C(0, 255, 0), C(255, 165, 0), C(255, 255, 0), C(0, 128, 0), C(75, 0, 130), C(238, 130, 238));
            var root = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, RowGap = Length.Px(20f), Margin = Sides.All(Length.Px(20f)) });

            var seventh = 100f / 7f;
            foreach (var (border, stops) in new (float, (Color, float?)[])[]
            {
                (4f, [(Color.White, 15f), (Color.Black, 85f)]),
                (4f, [(red, null), (blue, null), (lime, null)]),
                (0f, [(red, null), (red, seventh), (orange, seventh), (orange, 2 * seventh), (yellow, 2 * seventh), (yellow, 3 * seventh), (green, 3 * seventh), (green, 4 * seventh), (blue, 4 * seventh), (blue, 5 * seventh), (indigo, 5 * seventh), (indigo, 6 * seventh), (violet, 6 * seventh), (violet, null)]),
            })
            {
                var row = Ui.SpawnNode(new UiSettings());
                ecs.SetParent(row, root);

                // Eight turns of the gradient on each of three shapes.
                var shapes = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, RowGap = Length.Px(5f) });
                ecs.SetParent(shapes, row);
                foreach (var (w, h) in new[] { (70f, 70f), (35f, 70f), (70f, 35f) })
                {
                    var line = Ui.SpawnNode(new UiSettings { ColumnGap = Length.Px(10f) });
                    ecs.SetParent(line, shapes);
                    for (var i = 0; i < 8; i++)
                        Paint(ecs, line, new UiSettings { Width = Length.Px(w), Height = Length.Px(h), Border = Sides.All(Length.Px(border)), Corners = Corners.All(Length.Px(20f)) }, "Linear", i * MathF.Tau / 8f, stops, false);
                }

                // A large one of each kind, the linear one turning.
                var large = Ui.SpawnNode(new UiSettings());
                ecs.SetParent(large, row);
                foreach (var kind in new[] { "Linear", "Radial", "Conic" })
                    Paint(ecs, large, new UiSettings { AspectRatio = 1f, Height = Length.Percent(100f), Border = Sides.All(Length.Px(border)), Margin = new Sides(Length.Px(20f), Length.Zero, Length.Zero, Length.Zero), Corners = Corners.All(Length.Px(20f)) }, kind, 0f, stops, true);
            }

            var footer = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, RowGap = Length.Px(10f), Align = UiAlign.Center });
            ecs.SetParent(footer, root);
            _label = Ui.SpawnText(Spaces[0], new UiSettings(), 25f);
            ecs.SetParent(_label, footer);

            _button = Ui.SpawnNode(new UiSettings
            {
                Interactive = true,
                Border = Sides.All(Length.Px(2f)),
                Padding = new Sides(Length.Px(8f), Length.Px(4f), Length.Px(8f), Length.Px(4f)),
                Justify = UiJustify.Center,
                Align = UiAlign.Center,
                Corners = Corners.All(Length.Px(1_000_000f)),
                BorderColor = (1f, 1f, 1f, 1f),
                Color = (0f, 0f, 0f, 1f),
            });
            var text = Ui.SpawnText("next color space", new UiSettings { Color = Scene.Srgb(0.9f, 0.9f, 0.9f) });
            ecs.Insert<TextShadowRef>(text);
            ecs.SetParent(text, _button);
            ecs.SetParent(_button, footer);
        }, "gradients.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;

            // The button's border is red under the pointer, and a click moves every gradient on to
            // the next color space.
            var interaction = Ui.InteractionOf(_button);
            if (interaction != _last)
            {
                var color = interaction == UiInteraction.None ? Color.White : new Color(1f, 0f, 0f);
                var edge = ecs.Wrap<BorderColorRef>(_button);
                edge.Top = edge.Right = edge.Bottom = edge.Left = color;
                if (_last == UiInteraction.Pressed && interaction == UiInteraction.Hovered)
                {
                    _space = (_space + 1) % Spaces.Length;
                    Ui.SetText(_label, Spaces[_space]);
                    foreach (var painted in Nodes) ecs.SetReflected(painted.Node, Background, string.Empty, Gradient(painted).ToJsonString());
                }

                _last = interaction;
            }

            foreach (var painted in Nodes.Where(painted => painted.Animated && painted.Kind == "Linear"))
            {
                painted.Angle += 0.5f * ctx.Time.Delta;
                ecs.SetReflected(painted.Node, Background, string.Empty, Gradient(painted).ToJsonString());
            }
        }, "gradients.Update");
    }

    private static void Paint(EcsWorld ecs, Entity parent, UiSettings settings, string kind, float angle, (Color, float?)[] stops, bool animated)
    {
        var node = Ui.SpawnNode(settings);
        ecs.SetParent(node, parent);
        var painted = new Painted(node, kind, angle, stops, animated);
        Nodes.Add(painted);
        ecs.InsertReflected(node, Background, Gradient(painted).ToJsonString());

        // Every border the same gradient, yellow to white to orange at three eighths of a turn.
        var border = new Painted(node, "Linear", 3f * MathF.Tau / 8f, [(C(255, 255, 0), null), (Color.White, null), (C(255, 165, 0), null)], false);
        ecs.InsertReflected(node, Border, new JsonArray(Single(border, "Oklaba")).ToJsonString());
    }

    // The node's gradient as the list Bevy's BackgroundGradient holds, in the current color space.
    private static JsonArray Gradient(Painted painted) => new(Single(painted, Spaces[_space]));

    private static JsonObject Single(Painted painted, string space) => painted.Kind switch
    {
        "Linear" => new JsonObject { ["Linear"] = new JsonObject { ["color_space"] = space, ["angle"] = painted.Angle, ["stops"] = Stops(painted.Stops) } },
        "Radial" => new JsonObject { ["Radial"] = new JsonObject { ["color_space"] = space, ["position"] = Center(), ["shape"] = "ClosestSide", ["stops"] = Stops(painted.Stops) } },
        _ => new JsonObject
        {
            ["Conic"] = new JsonObject
            {
                ["color_space"] = space,
                ["start"] = 0f,
                ["position"] = Center(),
                ["stops"] = new JsonArray([.. painted.Stops.Select(stop => (JsonNode)new JsonObject { ["color"] = Srgba(stop.Color), ["angle"] = null, ["hint"] = 0.5f })]),
            },
        },
    };

    private static JsonArray Stops((Color Color, float? Percent)[] stops) => new([.. stops.Select(stop => (JsonNode)new JsonObject
    {
        ["color"] = Srgba(stop.Color),
        ["point"] = stop.Percent is { } percent ? new JsonObject { ["Percent"] = percent } : "Auto",
        ["hint"] = 0.5f,
    })]);

    private static JsonObject Center() => new() { ["anchor"] = new JsonArray(0f, 0f), ["x"] = new JsonObject { ["Px"] = 0f }, ["y"] = new JsonObject { ["Px"] = 0f } };

    // Bevy's palette colors are sRGB, made linear by C and given as its LinearRgba, the same color.
    private static JsonObject Srgba(Color linear) => new()
    {
        ["LinearRgba"] = new JsonObject { ["red"] = linear.R, ["green"] = linear.G, ["blue"] = linear.B, ["alpha"] = linear.A },
    };

    private static Color C(byte r, byte g, byte b) => Color.FromSrgb(r / 255f, g / 255f, b / 255f);
}
