// Bevy's fixed_node example, examples/ui/layout/fixed_node.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// A yellow square laid out as a root node though a blue one that centers its children holds it,
// so it sits in the window's corner, and the blue turns red while the pointer is over the square,
// the hover reaching the parent as it passes up.
internal static class FixedNodeExample
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var blue = new Color(0f, 0f, 1f);
        var red = new Color(1f, 0f, 0f);

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            Color = blue,
        });

        // Neither hovered itself nor hiding what is under it, so it hears of the square alone.
        var pickable = ecs.Insert<PickableRef>(root);
        (pickable.IsHoverable, pickable.ShouldBlockLower) = (false, false);

        var square = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(100f), Color = new Color(1f, 1f, 0f) });
        ecs.Insert<FixedNodeRef>(square);
        ecs.SetParent(square, root);

        ecs.Observe<Pointer<Over>>(root, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = red);
        ecs.Observe<Pointer<Leave>>(root, on => on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = blue);
    }, "fixed_node.Setup");
}
