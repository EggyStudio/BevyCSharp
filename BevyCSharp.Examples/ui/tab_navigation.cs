// Bevy's tab_navigation example, examples/ui/widgets/tab_navigation.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows Tab and Shift+Tab moving the input focus through four groups of buttons, in the order each
// group's tab indices give, the focused button outlined. A click on a button focuses it and a click
// elsewhere lets the focus go.
//
// Bevy observes the pointer's clicks on the buttons and the page. Here each is an interactive node
// whose press is read as it begins, which is the same click.
internal static class TabNavigation
{
    private static readonly Color Normal = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    private static readonly Color Hovered = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    private static readonly Color Pressed = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    private static readonly Dictionary<Entity, UiInteraction> Buttons = [];
    private static Entity _page, _focused;
    private static UiInteraction _pageWas;

    public static void Build(App app)
    {
        app.Startup(Setup, "tab_navigation.Setup");
        app.Update(ButtonSystem, "tab_navigation.ButtonSystem");
        app.Update(FocusSystem, "tab_navigation.FocusSystem");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Buttons.Clear();
        (_focused, _pageWas) = (Entity.None, UiInteraction.None);
        Render2d.SpawnCamera2d();

        _page = Ui.SpawnNode(new UiSettings
        {
            Interactive = true,
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Direction = UiDirection.Column,
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            RowGap = Length.Px(6f),
        });

        foreach (var (label, order, modal, indices) in new (string, int, bool, int[])[]
        {
            // The same index throughout, so the buttons are visited in the order they are children.
            ("TabGroup 0", 0, false, [0, 0, 0, 0]),
            // Reversed, so from right to left.
            ("TabGroup 2", 2, false, [3, 2, 1, 0]),
            // In the order they stand, so from left to right.
            ("TabGroup 1", 1, false, [0, 1, 2, 3]),
            // Modal, so Tab stays within it, in an order of its own.
            ("Modal TabGroup", 0, true, [0, 3, 1, 2]),
        })
        {
            ecs.SetParent(Ui.SpawnText(label, new UiSettings()), _page);

            var group = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(6f), Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(10f)) });
            var tabGroup = ecs.Insert<TabGroupRef>(group);
            (tabGroup.Order, tabGroup.Modal) = (order, modal);
            ecs.SetParent(group, _page);

            foreach (var index in indices)
            {
                var button = Ui.SpawnNode(new UiSettings
                {
                    Interactive = true,
                    Width = Length.Px(200f),
                    Height = Length.Px(65f),
                    Border = Sides.All(Length.Px(5f)),
                    Justify = UiJustify.Center,
                    Align = UiAlign.Center,
                    BorderColor = (0f, 0f, 0f, 1f),
                    Color = (Normal.R, Normal.G, Normal.B, 1f),
                });
                ecs.Insert<TabIndexRef>(button).Value = index;
                ecs.SetParent(Ui.SpawnText($"TabIndex {index}", new UiSettings { Color = Scene.Srgb(0.9f, 0.9f, 0.9f) }, new UiTextSettings { FontSize = 20f }), button);
                ecs.SetParent(button, group);
                Buttons[button] = UiInteraction.None;
            }
        }
    }

    // Each button colored as the pointer moves over and presses it, and a press giving it the
    // focus. A press on the page itself, away from every button, lets the focus go.
    private static void ButtonSystem(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var focus = ecs.Resource<InputFocusRef>() ?? ecs.InsertResource<InputFocusRef>();

        foreach (var (button, was) in Buttons.ToArray())
        {
            var now = Ui.InteractionOf(button);
            if (now == was) continue;
            Buttons[button] = now;

            var (color, border) = now switch
            {
                UiInteraction.Pressed => (Pressed, new Color(1f, 0f, 0f, 1f)),
                UiInteraction.Hovered => (Hovered, Color.White),
                _ => (Normal, Color.Black),
            };
            ecs.Wrap<BackgroundColorRef>(button).Value = color;
            var edge = ecs.Wrap<BorderColorRef>(button);
            edge.Top = edge.Right = edge.Bottom = edge.Left = border;

            if (now == UiInteraction.Pressed) focus.CurrentFocus = button;
        }

        var page = Ui.InteractionOf(_page);
        if (page == UiInteraction.Pressed && _pageWas != UiInteraction.Pressed) focus.CurrentFocus = null;
        _pageWas = page;
    }

    // The focused button outlined in white, wherever the focus came from.
    private static void FocusSystem(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var focused = ecs.Resource<InputFocusRef>()?.CurrentFocus ?? Entity.None;
        if (focused == _focused) return;

        if (_focused != Entity.None && Buttons.ContainsKey(_focused)) ecs.Get<OutlineRef>(_focused)?.Remove();
        if (Buttons.ContainsKey(focused))
        {
            var outline = ecs.Insert<OutlineRef>(focused);
            (outline.Color, outline.Width, outline.Offset) = (Color.White, new Val.Px(2f), new Val.Px(2f));
        }
        _focused = focused;
    }
}
