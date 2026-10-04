using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates Display and Visibility on interface nodes, four nested boxes on the left whose
// display and visibility are switched by the buttons in the matching boxes on the right, a node
// with no display taking no room and a hidden one keeping its room.
internal static class DisplayAndVisibility
{

    private static readonly Color HiddenColor = Color.FromSrgb(1f, 0.7f, 0.7f);
    private static readonly string[] Palette = ["27496D", "466B7A", "669DB3", "ADCBE3"];

    // A button switching one property of one box on the left, and the text that says its value.
    private sealed class Switch(Entity button, Entity label, Entity target, bool display)
    {
        public Entity Button { get; } = button;
        public Entity Label { get; } = label;
        public Entity Target { get; } = target;
        public bool Display { get; } = display;
        public string Text { get; set; } = display ? "Display::Flex" : "Visibility::Inherited";
        public UiInteraction Last { get; set; }
    }

    private static readonly List<Switch> Switches = [];

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Switches.Clear();
            Render2d.SpawnCamera2d();
            var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
            var style = new UiTextSettings { Font = font };
            var palette = Palette.Select(Hex).ToArray();

            var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Direction = UiDirection.Column, Align = UiAlign.Center, Justify = UiJustify.SpaceEvenly, Color = (0f, 0f, 0f, 1f) });
            ecs.SetParent(Ui.SpawnText("Use the panel on the right to change the Display and Visibility properties for the respective nodes of the panel on the left", new UiSettings { Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(10f)) }, new UiTextSettings { Font = font, Justify = TextJustify.Center }), root);

            var panels = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f) });
            ecs.SetParent(panels, root);
            var leftHalf = Ui.SpawnNode(new UiSettings { Width = Length.Percent(50f), Height = Length.Px(520f), Justify = UiJustify.Center });
            var rightHalf = Ui.SpawnNode(new UiSettings { Width = Length.Percent(50f), Justify = UiJustify.Center });
            ecs.SetParent(leftHalf, panels);
            ecs.SetParent(rightHalf, panels);

            // Left, four boxes each inside the last, each beside a spacer that holds its height.
            var whiteLeft = Ui.SpawnNode(new UiSettings { Padding = Sides.All(Length.Px(10f)), Color = (1f, 1f, 1f, 1f) });
            ecs.SetParent(whiteLeft, leftHalf);
            var blackLeft = Ui.SpawnNode(new UiSettings { Color = (0f, 0f, 0f, 1f) });
            ecs.SetParent(blackLeft, whiteLeft);
            var targets = new Entity[4];
            var parent = blackLeft;
            for (var i = 0; i < 4; i++)
            {
                if (i < 3)
                {
                    targets[i] = Ui.SpawnNode(new UiSettings { Height = i == 0 ? Length.Auto : Length.Px(500f - 100f * i), Align = UiAlign.FlexEnd, Justify = UiJustify.FlexEnd, Color = palette[i] });
                    ecs.SetParent(targets[i], parent);
                    ecs.SetParent(Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(500f - 100f * i) }), targets[i]);
                }
                else
                {
                    targets[i] = Ui.SpawnNode(new UiSettings { Width = Length.Px(200f), Height = Length.Px(200f), Color = palette[i] });
                    ecs.SetParent(targets[i], parent);
                }

                parent = targets[i];
            }

            OutlineOf(ecs, targets[0]);

            // Right, boxes of the same colors, each holding the buttons for its match on the left.
            var whiteRight = Ui.SpawnNode(new UiSettings { Padding = Sides.All(Length.Px(10f)), Color = (1f, 1f, 1f, 1f) });
            ecs.SetParent(whiteRight, rightHalf);
            parent = whiteRight;
            for (var i = 0; i < 4; i++)
            {
                var size = Length.Px(500f - 100f * i);
                var box = Ui.SpawnNode(new UiSettings
                {
                    Width = size,
                    Height = size,
                    Direction = UiDirection.Column,
                    Align = i == 3 ? UiAlign.FlexStart : UiAlign.FlexEnd,
                    Justify = UiJustify.SpaceBetween,
                    Padding = new Sides(Length.Px(5f), Length.Px(5f), Length.Zero, Length.Zero),
                    Color = palette[i],
                });
                ecs.SetParent(box, parent);
                if (i == 0) OutlineOf(ecs, box);

                Button(ecs, box, style, targets[i], display: true);
                Button(ecs, box, style, targets[i], display: false);
                if (i == 3) ecs.SetParent(Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(100f) }), box);
                parent = box;
            }

            // The key under both panels.
            var key = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Align = UiAlign.Start, Justify = UiJustify.Start, ColumnGap = Length.Px(10f) });
            ecs.SetParent(key, root);
            ecs.SetParent(Ui.SpawnText("Display::None\nVisibility::Hidden\nVisibility::Inherited", new UiSettings { Color = (HiddenColor.R, HiddenColor.G, HiddenColor.B, 1f) }, new UiTextSettings { Font = font, Justify = TextJustify.Center }), key);
            ecs.SetParent(Ui.SpawnText("-\n-\n-", new UiSettings { Color = Scene.Srgb8(169, 169, 169) }, new UiTextSettings { Font = font, Justify = TextJustify.Center }), key);
            ecs.SetParent(Ui.SpawnText("The UI Node and its descendants will not be visible and will not be allotted any space in the UI layout.\nThe UI Node will not be visible but will still occupy space in the UI layout.\nThe UI node will inherit the visibility property of its parent. If it has no parent it will be visible.", new UiSettings(), style), key);
        }, "display_and_visibility.Setup");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            foreach (var item in Switches)
            {
                var interaction = Ui.InteractionOf(item.Button);
                if (interaction == item.Last) continue;
                item.Last = interaction;

                if (interaction == UiInteraction.Pressed)
                {
                    if (item.Display)
                    {
                        var none = item.Text == "Display::Flex";
                        ecs.Wrap<NodeRef>(item.Target).Display = none ? NodeRef.DisplayVariant.None : NodeRef.DisplayVariant.Flex;
                        item.Text = none ? "Display::None" : "Display::Flex";
                    }
                    else
                    {
                        // Inherited, then Visible, then Hidden, and round again.
                        (var visibility, item.Text) = item.Text switch
                        {
                            "Visibility::Inherited" => (Visibility.Visible, "Visibility::Visible"),
                            "Visibility::Visible" => (Visibility.Hidden, "Visibility::Hidden"),
                            _ => (Visibility.Inherited, "Visibility::Inherited"),
                        };
                        ecs.Set(item.Target, visibility);
                    }

                    Ui.SetText(item.Label, item.Text);
                }

                // Hovered, the button darkens and its text turns yellow, and otherwise the text is
                // pink for a value that hides the box.
                var hovered = interaction == UiInteraction.Hovered;
                ecs.Wrap<BackgroundColorRef>(item.Button).Value = new Color(0f, 0f, 0f, hovered ? 0.6f : 0.5f);
                ecs.Wrap<TextColorRef>(item.Label).Value = hovered ? Color.FromSrgb(1f, 1f, 0f) : Hides(item.Text) ? HiddenColor : Color.White;
            }
        }, "display_and_visibility.Buttons");
    }

    private static void Button(EcsWorld ecs, Entity parent, UiTextSettings style, Entity target, bool display)
    {
        var button = Ui.SpawnNode(new UiSettings { Interactive = true, AlignSelf = UiAlignSelf.FlexStart, Padding = new Sides(Length.Px(5f), Length.Px(1f), Length.Px(5f), Length.Px(1f)), Color = (0f, 0f, 0f, 0.5f) });
        ecs.SetParent(button, parent);
        var label = Ui.SpawnText(display ? "Display::Flex" : "Visibility::Inherited", new UiSettings(), new UiTextSettings { Font = style.Font, Justify = TextJustify.Center });
        ecs.SetParent(label, button);
        Switches.Add(new Switch(button, label, target, display));
    }

    private static bool Hides(string text) => text.Contains("None", StringComparison.Ordinal) || text.Contains("Hidden", StringComparison.Ordinal);

    private static void OutlineOf(EcsWorld ecs, Entity node)
    {
        var outline = ecs.Insert<OutlineRef>(node);
        (outline.Width, outline.Offset, outline.Color) = (new Val.Px(4f), new Val.Px(10f), Color.FromSrgb(0f, 139f / 255f, 139f / 255f));
    }

    private static (float R, float G, float B, float A) Hex(string hex) =>
        Scene.Srgb8(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex[2..4], 16), Convert.ToByte(hex[4..], 16));
}
