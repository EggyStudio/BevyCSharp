// Bevy's overflow_transform example, examples/ui/scroll_and_overflow/overflow_transform.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// A node of an image and a line of text, turned and growing and shrinking, clipped by three nested
// square nodes that each turn at their own speed, so what shows of it is cut by every turned edge
// above it.
internal static class OverflowTransform
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
        });

        var outer = Layer(ecs, root, 400f, 0.35f, 0.18f, Color.FromSrgb(0.12f, 0.17f, 0.22f));
        var middle = Layer(ecs, outer, 350f, -0.5f, -0.4f, Color.FromSrgb(0.24f, 0.18f, 0.32f));
        var inner = Layer(ecs, middle, 300f, 0.65f, 0.55f, Color.FromSrgb(0.15f, 0.30f, 0.25f));

        var content = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            Absolute = true,
            Margin = Sides.All(Length.Auto),
            Corners = Corners.All(Length.Percent(20f)),
            Align = UiAlign.Center,
            Justify = UiJustify.SpaceAround,
            Border = Sides.All(Length.Px(5f)),
            Padding = Sides.All(Length.Px(5f)),
            Color = Color.FromSrgb8(0, 0, 128),
            BorderColor = Color.FromSrgb8(173, 216, 230),
        });
        ecs.Insert<BoxShadowRef>(content).Value =
        [
            new ShadowStyle(Darker(Color.FromSrgb(0.15f, 0.30f, 0.25f), 0.05f), new Val.Px(30f), new Val.Px(30f), new Val.Px(0f), new Val.Px(4f)),
        ];
        OverflowTransformLayer.Turn(ecs, content, MathF.PI / 4f);
        ecs.Add(content, new InnerNode());
        ecs.SetParent(content, inner);

        var logo = Ui.SpawnNode(new UiSettings { Width = Length.Px(400f) });
        Ui.SetImage(logo, new UiImageSettings { Image = AssetServer.Load(AssetKind.Image, "branding/bevy_logo_dark_big.png") });
        ecs.SetParent(logo, content);

        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
        ecs.SetParent(Ui.SpawnText("transform + overflow", new UiSettings(), new UiTextSettings { Font = font, FontSize = 34f }), content);
    }, "overflow_transform.Setup");

    // A square node clipping what is inside it, turned and turning at its own speed, as Bevy's
    // rotating_node bundle is.
    private static Entity Layer(EcsWorld ecs, Entity parent, float size, float rotation, float speed, Color color)
    {
        var node = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(size),
            Height = Length.Px(size),
            OverflowX = UiOverflow.Clip,
            OverflowY = UiOverflow.Clip,
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            Border = Sides.All(Length.Px(4f)),
            Color = color,
            BorderColor = (1f, 1f, 1f, 0.7f),
        });
        OverflowTransformLayer.Turn(ecs, node, rotation);
        ecs.Add(node, new OverflowTransformLayer { BaseRotation = rotation, Speed = speed });
        ecs.SetParent(node, parent);
        return node;
    }

    // Bevy's darker on a color, which moves it toward black in linear light until its luminance is
    // lower by the amount.
    private static Color Darker(Color linear, float amount)
    {
        var luminance = linear.R * 0.2126f + linear.G * 0.7152f + linear.B * 0.0722f;
        var scale = luminance <= 0f ? 0f : Math.Clamp(luminance - amount, 0f, 1f) / luminance;
        return new Color(linear.R * scale, linear.G * scale, linear.B * scale, linear.A);
    }
}

/// <summary>
/// A node clipping what is inside it, turned at a base angle plus its speed times the time, Bevy's
/// example's <c>RotatingClipLayer</c>.
/// </summary>
[Behavior]
public partial struct OverflowTransformLayer
{
    /// <summary>The angle it starts at, in radians.</summary>
    public float BaseRotation;

    /// <summary>How fast it turns, in radians a second.</summary>
    public float Speed;

    /// <summary>Turns it to its angle at this time, as Bevy's <c>rotate_nodes</c> does.</summary>
    [OnUpdate]
    public void RotateNodes(BehaviorContext ctx) =>
        Turn(ctx.Ecs, ctx.Entity, BaseRotation + ctx.Time.Elapsed * Speed);

    /// <summary>Turns a node to an angle, in radians, after layout.</summary>
    internal static void Turn(EcsWorld ecs, Entity node, float angle)
    {
        var transform = ecs.Get<UiTransformRef>(node) ?? ecs.Insert<UiTransformRef>(node);
        (transform.RotationCos, transform.RotationSin) = (MathF.Cos(angle), MathF.Sin(angle));
    }
}

/// <summary>The node of an image and text inside the layers, Bevy's example's <c>InnerNode</c>.</summary>
[Behavior]
public partial struct InnerNode
{
    /// <summary>Grows and shrinks it with time, as Bevy's <c>scale_inner</c> does.</summary>
    [OnUpdate]
    public void ScaleInner(BehaviorContext ctx)
    {
        var scale = 1f + 0.75f * MathF.Sin(0.4f * ctx.Time.Elapsed);
        ctx.Ecs.Wrap<UiTransformRef>(ctx.Entity).Scale = new Vec2(scale);
    }
}
