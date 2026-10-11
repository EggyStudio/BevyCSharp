// Bevy's ghost_nodes example, examples/ui/layout/ghost_nodes.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Ghost nodes, which the layout passes through as though their children were their parent's. A
// line of text under a ghost root is laid out as a root of its own, and in the middle three
// buttons sit in one row, though two of them are a ghost node's children and the third is beside
// the ghost. Each counter is on the button's parent, so the two under the ghost count together and
// the third counts alone.
internal static class GhostNodes
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        // Bevy's ghost_root, a ghost with a node under it holding a line of text.
        var ghostRoot = ecs.Spawn();
        ecs.Insert<GhostNodeRef>(ghostRoot);
        var holder = Ui.SpawnNode(new UiSettings());
        ecs.SetParent(holder, ghostRoot);
        ecs.SetParent(Label(ecs, "This text node is rendered under a ghost root"), holder);

        // Bevy's normal_root, the row of three buttons in the middle of the window.
        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
        var row = Ui.SpawnNode(new UiSettings());
        ecs.Add(row, new GhostCounter());
        ecs.SetParent(row, root);

        var ghost = ecs.Spawn();
        ecs.Insert<GhostNodeRef>(ghost);
        ecs.Add(ghost, new GhostCounter());
        ecs.SetParent(ghost, row);
        ecs.SetParent(Button(ecs), ghost);
        ecs.SetParent(Button(ecs), ghost);
        ecs.SetParent(Button(ecs), row);
    }, "ghost_nodes.Scene");

    // Bevy's button, a round one counting its parent's clicks, which each of its siblings' labels shows.
    private static Entity Button(EcsWorld ecs)
    {
        var button = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(150f),
            Height = Length.Px(65f),
            Border = Sides.All(Length.Px(5f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Corners = Corners.All(Length.Px(1_000_000f)),
            BorderColor = (0f, 0f, 0f, 1f),
            Color = Color.FromSrgb(0.15f, 0.15f, 0.15f),
        });
        ecs.Insert<ButtonRef>(button);
        ecs.SetParent(Label(ecs, "0"), button);

        ecs.Observe<Activate>(button, on =>
        {
            var parent = on.Ecs.ParentOf(on.Entity);
            if (parent == Entity.None || !on.Ecs.Has<GhostCounter>(parent)) return;

            ref var counter = ref on.Ecs.GetRef<GhostCounter>(parent);
            counter.Value++;
            var value = counter.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

            // Every label of every sibling, the button's own among them, as Bevy's walks them.
            foreach (var sibling in on.Ecs.ChildrenOf(parent))
                foreach (var label in on.Ecs.ChildrenOf(sibling))
                    if (on.Ecs.Get<TextRef>(label) is { } text) text.Value = value;
        });
        return button;
    }

    // Bevy's label, light text in Fira Sans Bold.
    private static Entity Label(EcsWorld ecs, string text)
    {
        var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);
        return Ui.SpawnText(text, new UiSettings { Color = (light.R, light.G, light.B, 1f) },
            new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 33f });
    }
}

/// <summary>How many times a button under this node was pressed.</summary>
[Behavior]
public partial struct GhostCounter
{
    /// <summary>The count so far.</summary>
    public int Value;
}
