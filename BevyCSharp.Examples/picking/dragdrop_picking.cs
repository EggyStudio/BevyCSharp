// Bevy's dragdrop_picking example, examples/picking/dragdrop_picking.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Pointers;

// Demonstrates drag and drop with picking events. A button is dragged from the left onto a green
// square, a pale circle following the pointer over the square as a preview, and each drop leaves a
// circle where it fell.
internal static class DragdropPicking
{
    private const float AreaSize = 500f;
    private const float ButtonWidth = 150f, ButtonHeight = 50f;
    private const float ElementSize = 25f;

    private static AssetHandle _circle;

    public static void Build(App app)
    {
        app.Startup(Setup, "dragdrop_picking.Setup");
    }

    // Meshes are picked, as Bevy's adds MeshPickingPlugin.
    public static void Configure(Config config) => config.MeshPicking = true;

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        _circle = Render.CreateMesh(MeshShape.Circle, ElementSize);

        // The button at the left, its text picked through, which turns orange while it is dragged.
        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Start });
        Unpickable(ecs, root);

        var button = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(ButtonWidth),
            Height = Length.Px(ButtonHeight),
            Margin = Sides.All(Length.Px(10f)),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = (1f, 0f, 0f, 1f),
        });
        ecs.Add(button, new DraggableButton());
        ecs.SetParent(button, root);
        var label = Ui.SpawnText("Drag from me", new UiSettings { Color = (1f, 1f, 1f, 1f) });
        Unpickable(ecs, label);
        ecs.SetParent(label, button);

        ecs.Observe<Pointer<DragStart>>(button, on =>
        {
            on.Ecs.Wrap<BackgroundColorRef>(Single<DraggableButton>(on.Ecs)).Value = Color.FromSrgb(1f, 0.5f, 0f);
            on.Propagate(false);
        });
        ecs.Observe<Pointer<DragEnd>>(button, on =>
        {
            on.Ecs.Wrap<BackgroundColorRef>(Single<DraggableButton>(on.Ecs)).Value = Color.FromSrgb(1f, 0f, 0f);
            on.Propagate(false);
        });

        // The square things are dropped on, with its words picked through.
        var area = ecs.Spawn();
        ecs.Add(area, Transform.Identity);
        ecs.Add(area, new DropArea());
        Render2d.SetMesh(ecs, area, Render.CreateMesh(MeshShape.Rectangle, AreaSize, AreaSize));
        Render2d.SetMaterial(ecs, area, Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.FromSrgb(0.1f, 0.4f, 0.1f) }));

        var words = ecs.Spawn();
        ecs.Add(words, Transform.At(0f, 0f, 1f));
        ecs.Insert<Text2dRef>(words).Value = "Drop here";
        ecs.Insert<TextFontRef>(words).FontSize = new FontSize.Px(50f);
        ecs.Insert<TextColorRef>(words).Value = Color.FromSrgb(0f, 0f, 0f);
        Unpickable(ecs, words);
        ecs.SetParent(words, area);

        ecs.Observe<Pointer<DragEnter>>(area, OnDragEnter);
        ecs.Observe<Pointer<DragOver>>(area, OnDragOver);
        ecs.Observe<Pointer<DragDrop>>(area, OnDragDrop);
        ecs.Observe<Pointer<DragLeave>>(area, OnDragLeave);
    }

    // The preview appears where the button is dragged onto the square.
    private static void OnDragEnter(On<Pointer<DragEnter>> on)
    {
        if (on.Event.Event.Dragged != Single<DraggableButton>(on.Ecs) || on.Event.Event.Hit.Position is not { } at) return;

        on.Ecs.Add(Circle(on.Ecs, at + new Vec3(0f, 0f, 2f), Color.FromSrgb(1f, 1f, 0.6f, 0.5f)), new GhostPreview());
        on.Propagate(false);
    }

    // And follows the pointer over it.
    private static void OnDragOver(On<Pointer<DragOver>> on)
    {
        var ghost = Single<GhostPreview>(on.Ecs);
        if (on.Event.Event.Dragged != Single<DraggableButton>(on.Ecs) || on.Event.Event.Hit.Position is not { } at || ghost == Entity.None) return;

        on.Ecs.Set(ghost, Transform.At(at.X, at.Y, at.Z));
        on.Propagate(false);
    }

    // A drop leaves a circle where it fell, in place of the preview.
    private static void OnDragDrop(On<Pointer<DragDrop>> on)
    {
        if (on.Event.Event.Dropped != Single<DraggableButton>(on.Ecs)) return;

        foreach (var ghost in on.Ecs.EntitiesWith<GhostPreview>()) on.Ecs.Despawn(ghost);
        if (on.Event.Event.Hit.Position is not { } at) return;

        on.Ecs.Add(Circle(on.Ecs, at + new Vec3(0f, 0f, 2f), Color.FromSrgb(1f, 1f, 0.6f)), new DroppedElement());
        on.Propagate(false);
    }

    // And the preview goes when the button is dragged off the square.
    private static void OnDragLeave(On<Pointer<DragLeave>> on)
    {
        if (on.Event.Event.Dragged != Single<DraggableButton>(on.Ecs)) return;

        foreach (var ghost in on.Ecs.EntitiesWith<GhostPreview>()) on.Ecs.Despawn(ghost);
        on.Propagate(false);
    }

    // A circle of a color, picked through, as both the preview and what is dropped are.
    private static Entity Circle(EcsWorld ecs, Vec3 at, Color color)
    {
        var circle = ecs.Spawn();
        ecs.Add(circle, Transform.At(at.X, at.Y, at.Z));
        Render2d.SetMesh(ecs, circle, _circle);
        // Blended where the color is less than opaque, as Bevy's ColorMaterial made from a color is.
        Render2d.SetMaterial(ecs, circle, Render2d.CreateMaterial(new ColorMaterialSettings
        {
            Color = color,
            AlphaMode = color.A < 1f ? AlphaMode2d.Blend : AlphaMode2d.Opaque,
        }));
        Unpickable(ecs, circle);
        return circle;
    }

    // The one entity carrying a marker, as Bevy's Single finds it, or none.
    private static Entity Single<T>(EcsWorld ecs) where T : unmanaged =>
        ecs.EntitiesWith<T>() is [var only] ? only : Entity.None;

    // Bevy's Pickable::IGNORE, neither blocking what is under it nor hovered itself.
    private static void Unpickable(EcsWorld ecs, Entity entity)
    {
        var pickable = ecs.Insert<PickableRef>(entity);
        (pickable.ShouldBlockLower, pickable.IsHoverable) = (false, false);
    }
}

/// <summary>The square things are dropped on.</summary>
[Behavior]
public partial struct DropArea;

/// <summary>The button dragged from.</summary>
[Behavior]
public partial struct DraggableButton;

/// <summary>
/// The pale circle following the pointer over the square while the button is dragged.
/// </summary>
[Behavior]
public partial struct GhostPreview;

/// <summary>A circle left where the button was dropped.</summary>
[Behavior]
public partial struct DroppedElement;
