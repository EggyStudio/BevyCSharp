// Bevy's ui_transform example, examples/ui/ui_transform.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows nodes moved, turned and scaled after layout, a panel of four buttons that the side buttons
// turn and grow or shrink and the arrow keys slide, its own buttons turned to face its edges.
internal static class UiTransformExample
{
    internal static readonly Color Normal = Color.White;
    internal static readonly Color Hovered = Color.FromSrgb(1f, 1f, 0f);
    internal static readonly Color Pressed = Color.FromSrgb(1f, 0f, 0f);

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = (0f, 0f, 0f, 1f) });
        var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Justify = UiJustify.SpaceEvenly, ColumnGap = Length.Px(25f), RowGap = Length.Px(25f), Color = (0f, 0f, 0f, 1f) });
        ecs.SetParent(row, root);

        Controls(ecs, row, ("<--", -MathF.PI / 8f, 0f), ("-", 0f, -0.25f));

        var target = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Justify = UiJustify.SpaceBetween, Align = UiAlign.Center, Width = Length.Px(300f), Height = Length.Px(300f), Color = Color.FromSrgb8(64, 64, 64) });
        ecs.Insert<UiTransformRef>(target);
        ecs.SetParent(target, row);
        ecs.Add(target, new TargetNode());

        Edge(ecs, target, "Top", 0f);
        var middle = Ui.SpawnNode(new UiSettings { AlignSelf = UiAlignSelf.Stretch, Justify = UiJustify.SpaceBetween, Align = UiAlign.Center });
        ecs.SetParent(middle, target);
        Edge(ecs, middle, "Left", -MathF.PI / 2f);
        var logo = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Height = Length.Px(100f) });
        Ui.SetImage(logo, new UiImageSettings { Image = AssetServer.Load(AssetKind.Image, "branding/icon.png"), Mode = UiImageMode.Stretch });
        ecs.SetParent(logo, middle);
        Edge(ecs, middle, "Right", MathF.PI / 2f);
        Edge(ecs, target, "Bottom", MathF.PI);

        Controls(ecs, row, ("-->", MathF.PI / 8f, 0f), ("+", 0f, 0.25f));
    }, "ui_transform.Setup");

    // A column of two small buttons beside the panel, drawn over it when it grows, each turning or
    // scaling the panel.
    private static void Controls(EcsWorld ecs, Entity row, params (string Label, float Turn, float Scale)[] buttons)
    {
        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Justify = UiJustify.Center, RowGap = Length.Px(10f), ColumnGap = Length.Px(10f), Padding = Sides.All(Length.Px(10f)), Color = (0f, 0f, 0f, 1f) });
        ecs.Insert<GlobalZIndexRef>(column).Value = 1;
        ecs.SetParent(column, row);
        foreach (var (label, turn, scale) in buttons)
        {
            var button = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(50f), Height = Length.Px(50f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = (1f, 1f, 1f, 1f) });
            ecs.Insert<ButtonRef>(button);
            ecs.Insert<HoveredRef>(button);
            ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = (0f, 0f, 0f, 1f) }), button);
            ecs.SetParent(button, column);
            ecs.Add(button, new TransformButton());
            if (turn != 0f) ecs.Add(button, new RotateButton { Angle = turn });
            if (scale != 0f) ecs.Add(button, new ScaleButton { Step = scale });
        }
    }

    // One of the panel's own buttons, turned to face the edge it sits on.
    private static void Edge(EcsWorld ecs, Entity parent, string label, float angle)
    {
        var button = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(80f), Height = Length.Px(80f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = (1f, 1f, 1f, 1f) });
        ecs.Insert<ButtonRef>(button);
        ecs.Insert<HoveredRef>(button);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings { Color = (0f, 0f, 0f, 1f) }), button);
        ecs.Insert<UiTransformRef>(button);
        Turn(ecs, button, angle);
        ecs.SetParent(button, parent);
        ecs.Add(button, new TransformButton());
    }

    internal static void Turn(EcsWorld ecs, Entity node, float angle)
    {
        var transform = ecs.Wrap<UiTransformRef>(node);
        (transform.RotationCos, transform.RotationSin) = (MathF.Cos(angle), MathF.Sin(angle));
    }
}

/// <summary>A button turning the panel by an angle, Bevy's <c>RotateButton</c> of a <c>Rot2</c>.</summary>
[Behavior]
public partial struct RotateButton
{
    /// <summary>The turn, in radians.</summary>
    public float Angle;
}

/// <summary>A button growing or shrinking the panel.</summary>
[Behavior]
public partial struct ScaleButton
{
    /// <summary>What it adds to the panel's scale.</summary>
    public float Step;
}

/// <summary>The panel the buttons turn and scale and the arrow keys slide.</summary>
[Behavior]
public partial struct TargetNode
{
    /// <summary>
    /// Each held arrow slides the panel fifty pixels a second its way, no farther than 150 either
    /// way, as Bevy's <c>translation_system</c> does.
    /// </summary>
    [OnUpdate]
    public void TranslationSystem(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var transform = ctx.Ecs.Wrap<UiTransformRef>(ctx.Entity);
        foreach (var (key, dx, dy) in new[] { (Key.ArrowLeft, -1f, 0f), (Key.ArrowRight, 1f, 0f), (Key.ArrowUp, 0f, -1f), (Key.ArrowDown, 0f, 1f) })
        {
            if (!input.KeyDown(key) || transform.TranslationX is not Val.Px x || transform.TranslationY is not Val.Px y) continue;
            var step = 50f * ctx.Time.Delta;
            (transform.TranslationX, transform.TranslationY) = (new Val.Px(Math.Clamp(x.Value + dx * step, -150f, 150f)), new Val.Px(Math.Clamp(y.Value + dy * step, -150f, 150f)));
        }
    }
}

/// <summary>A button of the example, one of Bevy's widgets, marked for the example's query.</summary>
[Behavior]
public partial struct TransformButton
{
    /// <summary>
    /// Colored by whether it is pressed or hovered as either changes, and as it is pressed turning
    /// or scaling the panel by what the button carries, the scale kept between a quarter and three.
    /// </summary>
    /// <remarks>
    /// Bevy's acts on a change of <c>Hovered</c> or <c>Pressed</c>, which no C# type names for a
    /// filter. The bridge's <c>Interaction</c> on an interactive node changes with both, so it
    /// stands in as the filter, and the state is read from <c>Hovered</c> and <c>Pressed</c>.
    /// </remarks>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void ButtonSystem(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var pressed = ecs.Get<PressedRef>(ctx.Entity) is not null;
        var hovered = ecs.Get<HoveredRef>(ctx.Entity)?.Value == true;
        ecs.Wrap<BackgroundColorRef>(ctx.Entity).Value = pressed ? UiTransformExample.Pressed : hovered ? UiTransformExample.Hovered : UiTransformExample.Normal;
        if (!pressed) return;

        foreach (var target in ecs.EntitiesWith<TargetNode>())
        {
            var transform = ecs.Wrap<UiTransformRef>(target);
            if (ecs.Has<RotateButton>(ctx.Entity))
                UiTransformExample.Turn(ecs, target, MathF.Atan2(transform.RotationSin, transform.RotationCos) + ecs.GetOrDefault<RotateButton>(ctx.Entity).Angle);
            if (ecs.Has<ScaleButton>(ctx.Entity))
            {
                var step = ecs.GetOrDefault<ScaleButton>(ctx.Entity).Step;
                var scale = transform.Scale;
                transform.Scale = new Vec2(Math.Clamp(scale.X + step, 0.25f, 3f), Math.Clamp(scale.Y + step, 0.25f, 3f));
            }
        }
    }
}
