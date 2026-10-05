// Bevy's context_menu example, examples/usage/context_menu.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Usage;

// A context menu, opened by a press on the button where the pointer is, of five colors to set the
// background to. Its items turn red under the pointer, and a press anywhere off the menu closes it.
//
// Bevy observes its pointer events on each node and lets one stop the press from reaching the
// background behind. Here each node's interaction is read as it changes, and a press is the
// button's or an item's before it is the background's.
internal static class ContextMenu
{
    private static readonly (string Name, (float R, float G, float B, float A) Color)[] Items =
    [
        ("fuchsia", Scene.Srgb8(255, 0, 255)),
        ("gray", Scene.Srgb8(128, 128, 128)),
        ("maroon", Scene.Srgb8(128, 0, 0)),
        ("purple", Scene.Srgb8(128, 0, 128)),
        ("teal", Scene.Srgb8(0, 128, 128)),
    ];

    private static Entity _background, _button, _menu;
    private static readonly List<(Entity Node, Entity Text, int Item)> MenuItems = [];
    private static readonly Dictionary<Entity, UiInteraction> Last = [];

    public static void Build(App app)
    {
        app.Startup(Setup, "context_menu.Setup");
        app.Update(Pointer, "context_menu.Pointer");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _menu = Entity.None;
        MenuItems.Clear();
        Last.Clear();
        Render2d.SpawnCamera2d();

        // The background takes the presses nothing in front of it does, behind everything.
        _background = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
        ecs.Insert<ZIndexRef>(_background).Value = -10;

        _button = Ui.SpawnNode(new UiSettings
        {
            Interactive = true,
            Width = Length.Px(250f),
            Height = Length.Px(65f),
            Border = Sides.All(Length.Px(5f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Corners = Corners.All(Length.Px(float.MaxValue)),
            Color = (0f, 0f, 0f, 1f),
        });
        ecs.SetParent(_button, _background);
        var label = Ui.SpawnText("Context Menu", new UiSettings(), new UiTextSettings { FontSize = 28f });
        ecs.Insert<TextShadowRef>(label);
        ecs.SetParent(label, _button);
    }

    private static bool Pressed(Entity node)
    {
        var interaction = Ui.InteractionOf(node);
        var changed = !Last.TryGetValue(node, out var last) || last != interaction;
        Last[node] = interaction;
        return changed && interaction == UiInteraction.Pressed;
    }

    private static void Pointer(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Each item red while the pointer is over it, and white again when it leaves.
        foreach (var (node, text, _) in MenuItems)
        {
            var hovered = Ui.InteractionOf(node) != UiInteraction.None;
            ecs.Wrap<TextColorRef>(text).Value = hovered ? new Color(1f, 0f, 0f, 1f) : new Color(1f, 1f, 1f, 1f);
        }

        // The menu's items first, then the button, then the background, so a press is taken by
        // the nearest of them, as Bevy's stops it going further.
        foreach (var (node, _, item) in MenuItems)
        {
            if (!Pressed(node)) continue;
            Render.SetClearColor(Items[item].Color);
            CloseMenus(ecs);
            return;
        }

        if (Pressed(_button))
        {
            OpenMenu(ecs, ctx.Input.MousePosition);
            return;
        }

        if (Pressed(_background)) CloseMenus(ecs);
    }

    private static void CloseMenus(EcsWorld ecs)
    {
        if (_menu != Entity.None) ecs.Despawn(_menu);
        _menu = Entity.None;
        foreach (var (node, _, _) in MenuItems) Last.Remove(node);
        MenuItems.Clear();
    }

    private static void OpenMenu(EcsWorld ecs, (float X, float Y) at)
    {
        CloseMenus(ecs);
        _menu = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(at.X),
            Top = Length.Px(at.Y),
            Direction = UiDirection.Column,
            Corners = Corners.All(Length.Px(4f)),
            Color = (0.1f, 0.1f, 0.1f, 1f),
        });

        for (var i = 0; i < Items.Length; i++)
        {
            var item = Ui.SpawnNode(new UiSettings { Interactive = true, Padding = Sides.All(Length.Px(5f)) });
            ecs.SetParent(item, _menu);
            var text = Ui.SpawnText(Items[i].Name, new UiSettings(), 24f);
            ecs.SetParent(text, item);
            MenuItems.Add((item, text, i));

            // A press that opened the menu is still held, so each item starts out as it is.
            Last[item] = Ui.InteractionOf(item);
        }
    }
}
