// Bevy's ui_transform example, examples/ui/ui_transform.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows nodes moved, turned and scaled after layout, a panel of four buttons that the side buttons
// turn and grow or shrink and the arrow keys slide, its own buttons turned to face its edges.
internal static class UiTransformExample
{

    private static readonly Color Normal = Color.White;
    private static readonly Color Hovered = Color.FromSrgb(1f, 1f, 0f);
    private static readonly Color Pressed = Color.FromSrgb(1f, 0f, 0f);

    // What each button does when pressed, a turn in radians or a change of scale.
    private static readonly List<(Entity Button, float Turn, float Scale)> Buttons = [];
    private static readonly Dictionary<Entity, UiInteraction> Last = [];
    private static Entity _target;
    private static float _angle, _scale, _x, _y;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Buttons.Clear();
            Last.Clear();
            (_angle, _scale, _x, _y) = (0f, 1f, 0f, 0f);
            Render2d.SpawnCamera2d();

            var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = (0f, 0f, 0f, 1f) });
            var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Justify = UiJustify.SpaceEvenly, ColumnGap = Length.Px(25f), RowGap = Length.Px(25f), Color = (0f, 0f, 0f, 1f) });
            ecs.SetParent(row, root);

            Controls(ecs, row, ("<--", -MathF.PI / 8f, 0f), ("-", 0f, -0.25f));

            _target = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Justify = UiJustify.SpaceBetween, Align = UiAlign.Center, Width = Length.Px(300f), Height = Length.Px(300f), Color = Scene.Srgb8(64, 64, 64) });
            ecs.Insert<UiTransformRef>(_target);
            ecs.SetParent(_target, row);

            Edge(ecs, _target, "Top", 0f);
            var middle = Ui.SpawnNode(new UiSettings { AlignSelf = UiAlignSelf.Stretch, Justify = UiJustify.SpaceBetween, Align = UiAlign.Center });
            ecs.SetParent(middle, _target);
            Edge(ecs, middle, "Left", -MathF.PI / 2f);
            var logo = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(100f) });
            Ui.SetImage(logo, new UiImageSettings { Image = AssetServer.Load(AssetKind.Image, "branding/icon.png"), Mode = UiImageMode.Stretch });
            ecs.SetParent(logo, middle);
            Edge(ecs, middle, "Right", MathF.PI / 2f);
            Edge(ecs, _target, "Bottom", MathF.PI);

            Controls(ecs, row, ("-->", MathF.PI / 8f, 0f), ("+", 0f, 0.25f));
        }, "ui_transform.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var changed = false;
            foreach (var (button, turn, scale) in Buttons)
            {
                var interaction = Ui.InteractionOf(button);
                if (Last.TryGetValue(button, out var last) && last == interaction) continue;
                Last[button] = interaction;

                ecs.Wrap<BackgroundColorRef>(button).Value = interaction switch { UiInteraction.Pressed => Pressed, UiInteraction.Hovered => Hovered, _ => Normal };
                if (interaction != UiInteraction.Pressed) continue;
                _angle += turn;
                _scale = Math.Clamp(_scale + scale, 0.25f, 3f);
                changed = true;
            }

            // The arrow keys slide the panel fifty pixels a second, no farther than 150 either way.
            var input = ctx.Input;
            var step = 50f * ctx.Time.Delta;
            var (dx, dy) = ((input.KeyDown(Key.ArrowRight) ? 1 : 0) - (input.KeyDown(Key.ArrowLeft) ? 1 : 0), (input.KeyDown(Key.ArrowDown) ? 1 : 0) - (input.KeyDown(Key.ArrowUp) ? 1 : 0));
            if (dx != 0 || dy != 0)
            {
                (_x, _y) = (Math.Clamp(_x + dx * step, -150f, 150f), Math.Clamp(_y + dy * step, -150f, 150f));
                changed = true;
            }

            if (!changed) return;
            Turn(ecs, _target, _angle);
            var target = ecs.Wrap<UiTransformRef>(_target);
            target.Scale = new Vec2(_scale, _scale);
            (target.TranslationX, target.TranslationY) = (new Val.Px(_x), new Val.Px(_y));
        }, "ui_transform.ButtonsAndTranslation");
    }

    // A column of two small buttons beside the panel, drawn over it when it grows.
    private static void Controls(EcsWorld ecs, Entity row, params (string Label, float Turn, float Scale)[] buttons)
    {
        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Justify = UiJustify.Center, RowGap = Length.Px(10f), ColumnGap = Length.Px(10f), Padding = Sides.All(Length.Px(10f)), Color = (0f, 0f, 0f, 1f) });
        ecs.Insert<GlobalZIndexRef>(column).Value = 1;
        ecs.SetParent(column, row);
        foreach (var (label, turn, scale) in buttons)
        {
            var button = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(50f), Height = Length.Px(50f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = (1f, 1f, 1f, 1f) });
            ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = (0f, 0f, 0f, 1f) }), button);
            ecs.SetParent(button, column);
            Buttons.Add((button, turn, scale));
        }
    }

    // One of the panel's own buttons, turned to face the edge it sits on.
    private static void Edge(EcsWorld ecs, Entity parent, string label, float angle)
    {
        var button = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(80f), Height = Length.Px(80f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = (1f, 1f, 1f, 1f) });
        ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = (0f, 0f, 0f, 1f) }), button);
        ecs.Insert<UiTransformRef>(button);
        Turn(ecs, button, angle);
        ecs.SetParent(button, parent);
    }

    private static void Turn(EcsWorld ecs, Entity node, float angle)
    {
        var transform = ecs.Wrap<UiTransformRef>(node);
        (transform.RotationCos, transform.RotationSin) = (MathF.Cos(angle), MathF.Sin(angle));
    }
}
