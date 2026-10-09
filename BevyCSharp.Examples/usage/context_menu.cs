// Bevy's context_menu example, examples/usage/context_menu.rs at v0.20.0, by Bevy's contributors
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
internal static class ContextMenuExample
{
    private static readonly (string Name, (float R, float G, float B, float A) Color)[] Items =
    [
        ("fuchsia", Color.FromSrgb8(255, 0, 255)),
        ("gray", Color.FromSrgb8(128, 128, 128)),
        ("maroon", Color.FromSrgb8(128, 0, 0)),
        ("purple", Color.FromSrgb8(128, 0, 128)),
        ("teal", Color.FromSrgb8(0, 128, 128)),
    ];

    // The background and its button, which Bevy observes presses on, and the interaction each had
    // last frame, which tells a new press from one held.
    private static Entity _background, _button;
    private static readonly Dictionary<Entity, UiInteraction> Last = [];

    // The frame a close was last asked on, since an item's press and the background's can both ask.
    private static ulong _closedFrame = ulong.MaxValue;

    public static void Build(App app)
    {
        app.Startup(Setup, "context_menu.Setup");
        app.Update(Pointer, "context_menu.Pointer");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Last.Clear();
        _closedFrame = ulong.MaxValue;
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

    // A press on the button opens the menu where the pointer is, and one on the background closes
    // any, the button's first, as Bevy's stops a press going further. An item takes its own.
    private static void Pointer(BehaviorContext ctx)
    {
        if (Pressed(_button))
        {
            OpenMenu(ctx, ctx.Input.MousePosition);
            return;
        }

        if (Pressed(_background)) CloseMenus(ctx);
    }

    /// <summary>Every open menu taken away, as Bevy's on_trigger_close_menus despawns them, once a frame.</summary>
    internal static void CloseMenus(BehaviorContext ctx)
    {
        if (ctx.Time.FrameCount == _closedFrame) return;
        _closedFrame = ctx.Time.FrameCount;
        foreach (var menu in ctx.Ecs.Query<ContextMenu>(markChanged: false)) ctx.Cmd.Despawn(menu.Entity);
    }

    private static void OpenMenu(BehaviorContext ctx, (float X, float Y) at)
    {
        var ecs = ctx.Ecs;
        CloseMenus(ctx);
        var menu = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(at.X),
            Top = Length.Px(at.Y),
            Direction = UiDirection.Column,
            Corners = Corners.All(Length.Px(4f)),
            Color = (0.1f, 0.1f, 0.1f, 1f),
        });
        ecs.Add(menu, new ContextMenu());

        foreach (var (name, color) in Items)
        {
            var item = Ui.SpawnNode(new UiSettings { Interactive = true, Padding = Sides.All(Length.Px(5f)) });
            ecs.Add(item, new ContextMenuItem { Color = new Color(color.R, color.G, color.B, color.A) });
            ecs.SetParent(item, menu);
            ecs.SetParent(Ui.SpawnText(name, new UiSettings(), 24f), item);
        }
    }
}

/// <summary>An open menu, taken away when a color is picked or anywhere else is pressed.</summary>
[Behavior]
public partial struct ContextMenu;

/// <summary>An item of the menu, and the color it sets the background to.</summary>
[Behavior]
public partial struct ContextMenuItem
{
    /// <summary>The color it sets.</summary>
    public Color Color;

    /// <summary>Whether a press on it picks it, which one held from opening the menu does not.</summary>
    public bool Armed;

    /// <summary>
    /// Its text red while the pointer is over it and white again when it leaves, and a new press
    /// sets the background to its color and closes the menu, as Bevy's observers of its pointer do.
    /// </summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void Pointer(BehaviorContext ctx)
    {
        var interaction = Ui.InteractionOf(ctx.Entity);
        foreach (var text in ctx.Ecs.ChildrenOf(ctx.Entity))
            ctx.Ecs.Wrap<TextColorRef>(text).Value = interaction == UiInteraction.None ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0f, 0f, 1f);

        if (interaction != UiInteraction.Pressed)
        {
            Armed = true;
            return;
        }

        if (!Armed) return;
        Render.SetClearColor((Color.R, Color.G, Color.B, Color.A));
        ContextMenuExample.CloseMenus(ctx);
    }
}
