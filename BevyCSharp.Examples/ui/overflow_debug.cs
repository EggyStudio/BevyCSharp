using Bevy;

namespace BevyCSharp.Examples.Interface;

// Shows clipping against nodes that move, grow and turn, a logo and a word in six frames, O cycling
// what the frames clip, S their size, and Space starting and stopping the motion.
internal static class OverflowDebug
{
    private const string Node = "bevy_ui::ui_node::Node";
    private const string Transform = "bevy_ui::ui_transform::UiTransform";
    private const string TextSpan = "bevy_text::text::TextSpan";
    private const float Size = 150f;
    private const float LoopLength = 4f;

    private enum Motion { Move, Scale, Rotate }

    private static readonly List<Entity> Containers = [];
    private static readonly List<(Entity Node, Motion Motion)> Moving = [];
    private static readonly (string X, string Y)[] Overflows = [("Clip", "Clip"), ("Visible", "Visible"), ("Visible", "Clip"), ("Clip", "Visible")];

    private static Entity _setting;
    private static int _overflow, _size;
    private static bool _playing;
    private static float _pausedAt, _pausedTotal, _t;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Containers.Clear();
            Moving.Clear();
            (_overflow, _size, _playing, _pausedAt, _pausedTotal, _t) = (0, 0, false, 0f, 0f, 0f);
            Render2d.SpawnCamera2d();

            var instructions = Ui.SpawnText("Next Overflow Setting (O)\nNext Container Size (S)\nToggle Animation (space)\n\n", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
            _setting = Ui.SpawnTextSpan(instructions, Describe(), new UiTextSettings(), (1f, 1f, 1f, 1f));

            var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center });
            var grid = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, RowGap = Length.Px(80f), ColumnGap = Length.Px(80f) });
            UiGrid.Set(grid, new GridSettings { Columns = [Track.Px(Size).Repeated(3)], Rows = [Track.Px(Size).Repeated(2)] });
            ecs.SetParent(grid, middle);

            var logo = AssetServer.Load(AssetKind.Image, "branding/bevy_logo_dark_big.png");
            var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
            foreach (var motion in new[] { Motion.Move, Motion.Scale, Motion.Rotate })
            {
                var image = Ui.SpawnNode(new UiSettings { Height = Length.Px(100f), Absolute = true, Top = Length.Px(-50f), Left = Length.Px(-200f) });
                Ui.SetImage(image, logo);
                Container(ecs, grid, motion, image);
            }

            foreach (var motion in new[] { Motion.Move, Motion.Scale, Motion.Rotate })
                Container(ecs, grid, motion, Ui.SpawnText("Bevy", new UiSettings(), new UiTextSettings { Font = font, FontSize = 100f }));
        }, "overflow_debug.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var input = ctx.Input;

            if (input.KeyPressed(Key.O))
            {
                _overflow = (_overflow + 1) % Overflows.Length;
                foreach (var container in Containers)
                {
                    ecs.SetVariant(container, Node, ".overflow.x", Overflows[_overflow].X);
                    ecs.SetVariant(container, Node, ".overflow.y", Overflows[_overflow].Y);
                }

                ecs.SetReflected(_setting, TextSpan, ".0", System.Text.Json.JsonSerializer.Serialize(Describe()));
            }

            // The frames take turns being full, short and narrow.
            if (input.KeyPressed(Key.S))
            {
                _size = (_size + 1) % 3;
                foreach (var container in Containers)
                {
                    ecs.SetReflected(container, Node, ".width", _size == 2 ? "{\"Percent\":30.0}" : "{\"Percent\":100.0}");
                    ecs.SetReflected(container, Node, ".height", _size == 1 ? "{\"Percent\":30.0}" : "{\"Percent\":100.0}");
                }
            }

            var elapsed = ctx.Time.Elapsed;
            if (input.KeyPressed(Key.Space))
            {
                _playing = !_playing;
                if (_playing) _pausedTotal += elapsed - _pausedAt;
                else _pausedAt = elapsed;
            }

            if (_playing) _t = (elapsed - _pausedTotal) % LoopLength / LoopLength;

            foreach (var (node, motion) in Moving)
            {
                var a = _t * MathF.Tau;
                switch (motion)
                {
                    case Motion.Move:
                        ecs.SetReflected(node, Transform, ".translation.x", FormattableString.Invariant($"{{\"Percent\":{MathF.Sin(a - MathF.PI / 2f) * 50f}}}"));
                        ecs.SetReflected(node, Transform, ".translation.y", FormattableString.Invariant($"{{\"Percent\":{-MathF.Cos(a - MathF.PI / 2f) * 50f}}}"));
                        break;
                    case Motion.Scale:
                        ecs.SetReflected(node, Transform, ".scale", FormattableString.Invariant($"[{1f + 0.5f * MathF.Max(MathF.Cos(a), 0f)},{1f + 0.5f * MathF.Max(MathF.Cos(a + MathF.PI), 0f)}]"));
                        break;
                    default:
                        var angle = MathF.Cos(a) * 45f;
                        ecs.SetReflected(node, Transform, ".rotation.cos", FormattableString.Invariant($"{MathF.Cos(angle)}"));
                        ecs.SetReflected(node, Transform, ".rotation.sin", FormattableString.Invariant($"{MathF.Sin(angle)}"));
                        break;
                }
            }
        }, "overflow_debug.Update");
    }

    // A clipping frame with a node inside it that moves, grows or turns, holding the content.
    private static void Container(EcsWorld ecs, Entity grid, Motion motion, Entity content)
    {
        var container = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            OverflowX = UiOverflow.Clip,
            OverflowY = UiOverflow.Clip,
            Color = Scene.Srgb(0.25f, 0.25f, 0.25f),
        });
        ecs.SetParent(container, grid);
        Containers.Add(container);

        var inner = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Justify = UiJustify.Center });
        ecs.InsertReflected(inner, Transform);
        ecs.SetParent(inner, container);
        ecs.SetParent(content, inner);
        Moving.Add((inner, motion));
    }

    // The setting as Bevy's debug output prints it.
    private static string Describe() => $"Overflow {{ x: {Overflows[_overflow].X}, y: {Overflows[_overflow].Y} }}";
}
