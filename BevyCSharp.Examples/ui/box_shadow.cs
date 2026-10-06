// Bevy's box_shadow example, examples/ui/styling/box_shadow.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using System.Text.Json.Nodes;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows box shadows on a node, with a panel of buttons that change the node's shape and the
// shadow's offset, blur, spread, count and samples, a button held down repeating.
internal static class BoxShadowExample
{
    // Bevy's BoxShadow is a list of shadows, which a wrapper does not type, so it is written as JSON.
    private const string Shadow = "bevy_ui::ui_node::BoxShadow";

    internal static readonly Color Normal = Color.FromSrgb(0.15f, 0.15f, 0.15f);
    internal static readonly Color Hovered = Color.FromSrgb(0.25f, 0.25f, 0.25f);
    internal static readonly Color Pressed = Color.FromSrgb(0.35f, 0.75f, 0.35f);

    // Width, height and corner radius of the five shapes, a radius of a million being round.
    private static readonly (string Name, float W, float H, float Radius)[] Shapes =
    [
        ("1", 164f, 164f, 0f), ("2", 164f, 164f, 41f), ("3", 164f, 164f, 1_000_000f), ("4", 240f, 80f, 32f), ("5", 80f, 240f, 32f),
    ];

    // Bevy's ShadowSettings and ShapeSettings resources, in one.
    private sealed class Settings
    {
        public int Shape;
        public float X = 20f, Y = 20f, Blur = 10f, Spread = 15f;
        public int Count = 1;
        public uint Samples = 6;
    }

    private static Settings _settings = new();
    private static Entity _camera;

    // Bevy's HeldButton resource, the button held and when it was pressed and last acted.
    internal static SettingsButtonKind? Held;
    internal static double PressedAt, LastRepeat;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_settings, Held) = (new Settings(), null);
            var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");

            _camera = Render2d.SpawnCamera2d();
            ecs.Insert<BoxShadowSamplesRef>(_camera).Value = (uint)_settings.Samples;

            var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center, Color = Color.FromSrgb8(128, 128, 128) });
            var node = Ui.SpawnNode(new UiSettings { Border = Sides.All(Length.Px(1f)), Align = UiAlign.Center, Justify = UiJustify.Center, BorderColor = (1f, 1f, 1f, 1f), Color = Color.FromSrgb(0.21f, 0.21f, 0.21f) });
            ecs.InsertReflected(node, Shadow, "[]");
            ecs.SetParent(node, middle);
            ecs.Add(node, new ShadowNode());

            var panel = Ui.SpawnNode(new UiSettings
            {
                Direction = UiDirection.Column,
                Absolute = true,
                Left = Length.Px(24f),
                Bottom = Length.Px(24f),
                Width = Length.Px(270f),
                Padding = Sides.All(Length.Px(16f)),
                Corners = Corners.All(Length.Px(12f)),
                Color = Color.FromSrgb(0.12f, 0.12f, 0.12f, 0.85f),
                BorderColor = (1f, 1f, 1f, 0.15f),
            });
            ecs.Insert<ZIndexRef>(panel).Value = 10;

            Row(ecs, panel, font, "Shape", SettingTypeKind.Shape, SettingsButtonKind.ShapePrev, SettingsButtonKind.ShapeNext);
            Row(ecs, panel, font, "X Offset", SettingTypeKind.XOffset, SettingsButtonKind.XOffsetDec, SettingsButtonKind.XOffsetInc);
            Row(ecs, panel, font, "Y Offset", SettingTypeKind.YOffset, SettingsButtonKind.YOffsetDec, SettingsButtonKind.YOffsetInc);
            Row(ecs, panel, font, "Blur", SettingTypeKind.Blur, SettingsButtonKind.BlurDec, SettingsButtonKind.BlurInc);
            Row(ecs, panel, font, "Spread", SettingTypeKind.Spread, SettingsButtonKind.SpreadDec, SettingsButtonKind.SpreadInc);
            Row(ecs, panel, font, "Count", SettingTypeKind.Count, SettingsButtonKind.CountDec, SettingsButtonKind.CountInc);
            Row(ecs, panel, font, "Samples", SettingTypeKind.Samples, SettingsButtonKind.SamplesDec, SettingsButtonKind.SamplesInc);

            var resetRow = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Align = UiAlign.Center, Height = Length.Px(36f), Margin = new Sides(Length.Zero, Length.Px(12f), Length.Zero, Length.Zero) });
            ecs.SetParent(resetRow, panel);
            var reset = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(90f), Height = Length.Px(32f), Justify = UiJustify.Center, Align = UiAlign.Center, Corners = Corners.All(Length.Px(8f)), Color = (Normal.R, Normal.G, Normal.B, 1f) });
            ecs.SetParent(Ui.SpawnText("Reset", new UiSettings(), new UiTextSettings { Font = font, FontSize = 16f }), reset);
            ecs.SetParent(reset, resetRow);
            ecs.Add(reset, new SettingsButton { Kind = SettingsButtonKind.Reset });

            Apply(ecs);
        }, "box_shadow.Setup");

        app.Update(ButtonRepeatSystem, "box_shadow.ButtonRepeatSystem");
    }

    // Held, a button acts again after 0.15 seconds and then every 0.08.
    private static void ButtonRepeatSystem(BehaviorContext ctx)
    {
        var now = ctx.Time.ElapsedSeconds;
        if (Held is not { } held || now - PressedAt <= 0.15 || now - LastRepeat <= 0.08) return;
        Act(ctx.Ecs, held);
        LastRepeat = now;
    }

    // What a button does to the settings, and the node, the camera and the values shown after it,
    // as Bevy's update systems follow a change to its settings.
    internal static void Act(EcsWorld ecs, SettingsButtonKind action)
    {
        var s = _settings;
        switch (action)
        {
            case SettingsButtonKind.XOffsetInc: s.X++; break;
            case SettingsButtonKind.XOffsetDec: s.X--; break;
            case SettingsButtonKind.YOffsetInc: s.Y++; break;
            case SettingsButtonKind.YOffsetDec: s.Y--; break;
            case SettingsButtonKind.BlurInc: s.Blur = MathF.Max(s.Blur + 1f, 0f); break;
            case SettingsButtonKind.BlurDec: s.Blur = MathF.Max(s.Blur - 1f, 0f); break;
            case SettingsButtonKind.SpreadInc: s.Spread++; break;
            case SettingsButtonKind.SpreadDec: s.Spread--; break;
            case SettingsButtonKind.CountInc: s.Count = Math.Min(s.Count + 1, 3); break;
            case SettingsButtonKind.CountDec: s.Count = Math.Max(s.Count - 1, 1); break;
            case SettingsButtonKind.ShapePrev: s.Shape = (s.Shape + Shapes.Length - 1) % Shapes.Length; break;
            case SettingsButtonKind.ShapeNext: s.Shape = (s.Shape + 1) % Shapes.Length; break;
            case SettingsButtonKind.SamplesInc: s.Samples++; break;
            case SettingsButtonKind.SamplesDec: s.Samples = Math.Max(s.Samples - 1, 1); break;
            case SettingsButtonKind.Reset: _settings = new Settings(); break;
        }

        Apply(ecs);
    }

    // The node's shape, its shadows, the camera's samples and the values shown, from the settings.
    private static void Apply(EcsWorld ecs)
    {
        var s = _settings;
        var (name, w, h, radius) = Shapes[s.Shape];

        // One black shadow, or blue and yellow on opposite sides, and a red one turned a quarter.
        var shadows = new JsonArray();
        if (s.Count == 1) shadows.Add(Style((0f, 0f, 0f), s.X, s.Y));
        if (s.Count >= 2)
        {
            shadows.Add(Style((0f, 0f, 1f), s.X, s.Y));
            shadows.Add(Style((1f, 1f, 0f), -s.X, -s.Y));
        }

        if (s.Count == 3) shadows.Add(Style((1f, 0f, 0f), s.Y, -s.X));

        foreach (var shadowNode in ecs.EntitiesWith<ShadowNode>())
        {
            var node = ecs.Wrap<NodeRef>(shadowNode);
            (node.Width, node.Height) = (new Val.Px(w), new Val.Px(h));
            node.BorderRadiusTopLeft = node.BorderRadiusTopRight = node.BorderRadiusBottomRight = node.BorderRadiusBottomLeft = new Val.Px(radius);
            ecs.SetReflected(shadowNode, Shadow, string.Empty, shadows.ToJsonString());
        }

        ecs.Wrap<BoxShadowSamplesRef>(_camera).Value = s.Samples;

        foreach (var label in ecs.EntitiesWith<SettingType>())
        {
            Ui.SetText(label, ecs.GetOrDefault<SettingType>(label).Kind switch
            {
                SettingTypeKind.Shape => name,
                SettingTypeKind.XOffset => FormattableString.Invariant($"{s.X:0.0}"),
                SettingTypeKind.YOffset => FormattableString.Invariant($"{s.Y:0.0}"),
                SettingTypeKind.Blur => FormattableString.Invariant($"{s.Blur:0.0}"),
                SettingTypeKind.Spread => FormattableString.Invariant($"{s.Spread:0.0}"),
                SettingTypeKind.Count => $"{s.Count}",
                _ => $"{s.Samples}",
            });
        }

        JsonObject Style((float R, float G, float B) color, float x, float y) => new()
        {
            ["color"] = new JsonObject { ["Srgba"] = new JsonObject { ["red"] = color.R, ["green"] = color.G, ["blue"] = color.B, ["alpha"] = 0.8f } },
            ["x_offset"] = new JsonObject { ["Px"] = x },
            ["y_offset"] = new JsonObject { ["Px"] = y },
            ["spread_radius"] = new JsonObject { ["Px"] = s.Spread },
            ["blur_radius"] = new JsonObject { ["Px"] = s.Blur },
        };
    }

    // A labeled row with a button either side of its value.
    private static void Row(EcsWorld ecs, Entity panel, AssetHandle font, string label, SettingTypeKind kind, SettingsButtonKind less, SettingsButtonKind more)
    {
        var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Align = UiAlign.Center, Height = Length.Px(32f) });
        ecs.SetParent(row, panel);

        var name = Ui.SpawnNode(new UiSettings { Width = Length.Px(80f), Justify = UiJustify.FlexEnd, Align = UiAlign.Center });
        ecs.SetParent(Ui.SpawnText(label, new UiSettings(), new UiTextSettings { Font = font, FontSize = 16f }), name);
        ecs.SetParent(name, row);

        var shape = kind == SettingTypeKind.Shape;
        Button(ecs, row, font, shape ? "<" : "-", less, Length.Px(8f));
        var value = Ui.SpawnNode(new UiSettings { Width = Length.Px(48f), Height = Length.Px(28f), Margin = Sides.Horizontal(Length.Px(8f)), Justify = UiJustify.Center, Align = UiAlign.Center, Corners = Corners.All(Length.Px(6f)) });
        var text = Ui.SpawnText(string.Empty, new UiSettings(), new UiTextSettings { Font = font, FontSize = 16f });
        ecs.Add(text, new SettingType { Kind = kind });
        ecs.SetParent(text, value);
        ecs.SetParent(value, row);
        Button(ecs, row, font, shape ? ">" : "+", more, Length.Zero);
    }

    private static void Button(EcsWorld ecs, Entity row, AssetHandle font, string text, SettingsButtonKind action, Length left)
    {
        // Bevy's start white and take the normal color the first frame their interaction is seen.
        var button = Ui.SpawnNode(new UiSettings { Interactive = true, Width = Length.Px(28f), Height = Length.Px(28f), Margin = new Sides(left, Length.Zero, Length.Zero, Length.Zero), Justify = UiJustify.Center, Align = UiAlign.Center, Corners = Corners.All(Length.Px(6f)), Color = (Normal.R, Normal.G, Normal.B, 1f) });
        ecs.SetParent(Ui.SpawnText(text, new UiSettings(), new UiTextSettings { Font = font, FontSize = 18f }), button);
        ecs.SetParent(button, row);
        ecs.Add(button, new SettingsButton { Kind = action });
    }
}

/// <summary>What a button of the panel does, as Bevy's <c>SettingsButton</c> enum names it.</summary>
public enum SettingsButtonKind { XOffsetInc, XOffsetDec, YOffsetInc, YOffsetDec, BlurInc, BlurDec, SpreadInc, SpreadDec, CountInc, CountDec, ShapePrev, ShapeNext, Reset, SamplesInc, SamplesDec }

/// <summary>Which setting a value shown is, as Bevy's <c>SettingType</c> enum names it.</summary>
public enum SettingTypeKind { XOffset, YOffset, Blur, Spread, Count, Shape, Samples }

/// <summary>The node the shadows are cast from.</summary>
[Behavior]
public partial struct ShadowNode;

/// <summary>A value shown on the panel, and which setting it is.</summary>
[Behavior]
public partial struct SettingType
{
    /// <summary>The setting.</summary>
    public SettingTypeKind Kind;
}

/// <summary>A button of the panel, and what it does.</summary>
[Behavior]
public partial struct SettingsButton
{
    /// <summary>What it does.</summary>
    public SettingsButtonKind Kind;

    /// <summary>
    /// Pressed, it acts and is held, to act again while it stays down, and let go it is held no
    /// longer, as its interaction changes.
    /// </summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void ButtonSystem(BehaviorContext ctx)
    {
        if (Ui.InteractionOf(ctx.Entity) == UiInteraction.Pressed)
        {
            BoxShadowExample.Act(ctx.Ecs, Kind);
            var now = ctx.Time.ElapsedSeconds;
            (BoxShadowExample.Held, BoxShadowExample.PressedAt, BoxShadowExample.LastRepeat) = (Kind, now, now);
        }
        else if (BoxShadowExample.Held == Kind)
        {
            BoxShadowExample.Held = null;
        }
    }

    /// <summary>Colored by its interaction as it changes.</summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void ButtonColorSystem(BehaviorContext ctx) =>
        ctx.Ecs.Wrap<BackgroundColorRef>(ctx.Entity).Value = Ui.InteractionOf(ctx.Entity) switch
        {
            UiInteraction.Pressed => BoxShadowExample.Pressed,
            UiInteraction.Hovered => BoxShadowExample.Hovered,
            _ => BoxShadowExample.Normal,
        };
}
