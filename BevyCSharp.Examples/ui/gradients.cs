// Bevy's gradients example, examples/ui/styling/gradients.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows linear, radial and conic gradients on nodes of several shapes, each with a gradient
// border, three sets of color stops, the large linear ones turning, and a button that steps every
// gradient through the color spaces it can be blended in.
internal static class Gradients
{
    private static readonly InterpolationColorSpace[] Spaces = Enum.GetValues<InterpolationColorSpace>();

    // A node's gradient, kept beside it to be written again when its color space or its angle
    // changes.
    internal sealed class Painted(Entity node, string kind, float angle, (Color Color, float? Percent)[] stops)
    {
        public Entity Node { get; } = node;
        public string Kind { get; } = kind;
        public float Angle { get; set; } = angle;
        public (Color Color, float? Percent)[] Stops { get; } = stops;
    }

    internal static readonly Dictionary<Entity, Painted> Nodes = [];
    private static Entity _button;
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
            var label = Ui.SpawnText(Spaces[0].ToString(), new UiSettings(), 25f);
            ecs.SetParent(label, footer);
            ecs.Add(label, new CurrentColorSpaceLabel());

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
            var text = Ui.SpawnText("next color space", new UiSettings { Color = Color.FromSrgb(0.9f, 0.9f, 0.9f) });
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
                    foreach (var label in ecs.EntitiesWith<CurrentColorSpaceLabel>()) Ui.SetText(label, Spaces[_space].ToString());
                    foreach (var painted in Nodes.Values) Repaint(ecs, painted);
                }

                _last = interaction;
            }
        }, "gradients.Update");
    }

    // The node's gradient written again, as it is now.
    internal static void Repaint(EcsWorld ecs, Painted painted) =>
        ecs.Wrap<BackgroundGradientRef>(painted.Node).Value = [Single(painted, Spaces[_space])];

    private static void Paint(EcsWorld ecs, Entity parent, UiSettings settings, string kind, float angle, (Color, float?)[] stops, bool animated)
    {
        var node = Ui.SpawnNode(settings);
        ecs.SetParent(node, parent);
        var painted = new Painted(node, kind, angle, stops);
        Nodes[node] = painted;
        if (animated) ecs.Add(node, new AnimateMarker());
        ecs.Insert<BackgroundGradientRef>(node).Value = [Single(painted, Spaces[_space])];

        // Every border the same gradient, yellow to white to orange at three eighths of a turn.
        var border = new Painted(node, "Linear", 3f * MathF.Tau / 8f, [(C(255, 255, 0), null), (Color.White, null), (C(255, 165, 0), null)]);
        ecs.Insert<BorderGradientRef>(node).Value = [Single(border, InterpolationColorSpace.Oklaba)];
    }

    // The node's gradient, in a color space, its middle at the node's for the radial and conic ones.
    private static Gradient Single(Painted painted, InterpolationColorSpace space) => painted.Kind switch
    {
        "Linear" => new Gradient.Linear(space, painted.Angle, Stops(painted.Stops)),
        "Radial" => new Gradient.Radial(space, Vec2.Zero, new Val.Px(0f), new Val.Px(0f), new RadialGradientShape.ClosestSide(), Stops(painted.Stops)),
        _ => new Gradient.Conic(space, 0f, Vec2.Zero, new Val.Px(0f), new Val.Px(0f), [.. painted.Stops.Select(stop => new AngularColorStop(stop.Color, null, 0.5f))]),
    };

    private static ColorStop[] Stops((Color Color, float? Percent)[] stops) =>
        [.. stops.Select(stop => new ColorStop(stop.Color, stop.Percent is { } percent ? new Val.Percent(percent) : new Val.Auto(), 0.5f))];

    private static Color C(byte r, byte g, byte b) => Color.FromSrgb(r / 255f, g / 255f, b / 255f);
}

/// <summary>The label that names the color space the gradients are blended in.</summary>
[Behavior]
public partial struct CurrentColorSpaceLabel;

/// <summary>A large node whose gradient turns, where it is linear.</summary>
[Behavior]
public partial struct AnimateMarker
{
    /// <summary>A linear gradient turned half a radian a second, as Bevy's <c>update</c> turns it.</summary>
    [OnUpdate]
    public void Update(BehaviorContext ctx)
    {
        if (!Gradients.Nodes.TryGetValue(ctx.Entity, out var painted) || painted.Kind != "Linear") return;
        painted.Angle += 0.5f * ctx.Time.Delta;
        Gradients.Repaint(ctx.Ecs, painted);
    }
}
