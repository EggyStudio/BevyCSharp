// Bevy's size_constraints example, examples/ui/layout/size_constraints.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates size constraints on a node, a white bar whose flex basis, width, minimum width and
// maximum width are each set from a row of buttons, the chosen one in each row lit.
internal static class SizeConstraints
{
    internal static readonly Color ActiveBorder = Color.FromSrgb(250f / 255f, 235f / 255f, 215f / 255f);
    internal static readonly Color InactiveBorder = Color.Black;
    internal static readonly Color ActiveInner = Color.White;
    internal static readonly Color InactiveInner = Color.FromSrgb(0f, 0f, 128f / 255f);
    internal static readonly Color ActiveText = Color.Black;
    internal static readonly Color HoveredText = Color.White;
    internal static readonly Color UnhoveredText = Color.FromSrgb(0.5f, 0.5f, 0.5f);

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        var style = new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 33f };
        var light = Color.FromSrgb(0.9f, 0.9f, 0.9f);

        var root = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center, Color = (0f, 0f, 0f, 1f) });
        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Center, Justify = UiJustify.Center });
        ecs.SetParent(column, root);
        ecs.SetParent(Ui.SpawnText("Size Constraints Example", new UiSettings { Color = light, Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(25f)) }, style), column);

        // The bar, white inside a black track inside yellow.
        var yellow = Color.FromSrgb8(255, 255, 0);
        var frame = Ui.SpawnNode(new UiSettings { Basis = Length.Percent(100f), AlignSelf = UiAlignSelf.Stretch, Padding = Sides.All(Length.Px(10f)), Color = yellow });
        ecs.SetParent(frame, column);
        var track = Ui.SpawnNode(new UiSettings { Align = UiAlign.Stretch, Width = Length.Percent(100f), Height = Length.Px(100f), Padding = Sides.All(Length.Px(4f)), Color = (0f, 0f, 0f, 1f) });
        ecs.SetParent(track, frame);
        var bar = Ui.SpawnNode(new UiSettings { Color = (1f, 1f, 1f, 1f) });
        ecs.SetParent(bar, track);
        ecs.Add(bar, new Bar());

        var rows = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.Stretch, Padding = Sides.All(Length.Px(10f)), Margin = new Sides(Length.Zero, Length.Px(50f), Length.Zero, Length.Zero), Color = yellow });
        ecs.SetParent(rows, column);
        foreach (var (label, kind) in new[] { ("min_size", ConstraintKind.MinWidth), ("flex_basis", ConstraintKind.FlexBasis), ("size", ConstraintKind.Width), ("max_size", ConstraintKind.MaxWidth) })
            Row(ecs, rows, label, kind, style);
    }, "size_constraints.Setup");

    // A row of seven choices for one field, Auto lit to begin with.
    private static void Row(EcsWorld ecs, Entity parent, string field, ConstraintKind kind, UiTextSettings style)
    {
        var outer = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Padding = Sides.All(Length.Px(2f)), Align = UiAlign.Stretch, Color = (0f, 0f, 0f, 1f) });
        ecs.SetParent(outer, parent);
        var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Justify = UiJustify.End, Padding = Sides.All(Length.Px(2f)) });
        ecs.SetParent(row, outer);

        var name = Ui.SpawnNode(new UiSettings { MinWidth = Length.Px(200f), MaxWidth = Length.Px(200f), Justify = UiJustify.Center, Align = UiAlign.Center });
        ecs.SetParent(name, row);
        ecs.SetParent(Ui.SpawnText(field, new UiSettings { Color = Color.FromSrgb(0.9f, 0.9f, 0.9f) }, style), name);

        var buttons = Ui.SpawnNode(new UiSettings());
        ecs.SetParent(buttons, row);
        var values = new List<(string Text, ButtonValue Value)> { ("Auto", new ButtonValue { Auto = true }) };
        foreach (var percent in new[] { 0, 25, 50, 75, 100, 125 }) values.Add(($"{percent}%", new ButtonValue { Percent = percent }));

        foreach (var (text, value) in values)
        {
            var active = text == "Auto";
            var button = Ui.SpawnNode(new UiSettings { Interactive = true, Align = UiAlign.Center, Justify = UiJustify.Center, Border = Sides.All(Length.Px(2f)), Margin = Sides.Horizontal(Length.Px(2f)), BorderColor = active ? ActiveBorder : InactiveBorder });
            ecs.SetParent(button, buttons);
            ecs.Add(button, new Constraint { Kind = kind });
            ecs.Add(button, value);

            var inner = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Justify = UiJustify.Center, Color = active ? ActiveInner : InactiveInner });
            ecs.SetParent(inner, button);
            var labelText = Ui.SpawnText(text, new UiSettings { Color = active ? ActiveText : UnhoveredText }, new UiTextSettings { Font = style.Font, FontSize = style.FontSize, Justify = TextJustify.Center });
            ecs.SetParent(labelText, inner);
        }
    }

    // A button's label, its child's child, as Bevy's systems find it.
    internal static Entity LabelOf(EcsWorld ecs, Entity button) => ecs.ChildrenOf(ecs.ChildrenOf(button)[0])[0];

    // Every button of the pressed one's row recolored, the pressed one lit and the rest dark, a
    // hovered one's label white, as Bevy's update_radio_buttons_colors does on the button's
    // ButtonActivated message, which here is the call.
    internal static void Activate(BehaviorContext ctx, Entity pressed, ConstraintKind kind)
    {
        var ecs = ctx.Ecs;
        foreach (var button in ecs.EntitiesWith<Constraint>())
        {
            if (ecs.GetOrDefault<Constraint>(button).Kind != kind) continue;

            var active = button == pressed;
            var border = ecs.Wrap<BorderColorRef>(button);
            border.Top = border.Right = border.Bottom = border.Left = active ? ActiveBorder : InactiveBorder;
            ecs.Wrap<BackgroundColorRef>(ecs.ChildrenOf(button)[0]).Value = active ? ActiveInner : InactiveInner;
            ecs.Wrap<TextColorRef>(LabelOf(ecs, button)).Value = active ? ActiveText : Ui.InteractionOf(button) == UiInteraction.Hovered ? HoveredText : UnhoveredText;
        }
    }
}

/// <summary>Which of the bar's sizes a row of buttons sets.</summary>
public enum ConstraintKind { FlexBasis, Width, MinWidth, MaxWidth }

/// <summary>The white bar whose sizes the buttons set.</summary>
[Behavior]
public partial struct Bar;

/// <summary>A button's value, Bevy's <c>ButtonValue</c> of a <c>Val</c>, here Auto or a percentage, the two the buttons hold.</summary>
[Behavior]
public partial struct ButtonValue
{
    /// <summary>Whether it is Auto.</summary>
    public bool Auto;

    /// <summary>The percentage, where it is not Auto.</summary>
    public float Percent;

    /// <summary>The value as Bevy's <c>Val</c>.</summary>
    public readonly Val ToVal() => Auto ? new Val.Auto() : new Val.Percent(Percent);
}

/// <summary>The size of the bar a button sets, as Bevy's <c>Constraint</c> enum keeps it on the button.</summary>
[Behavior]
public partial struct Constraint
{
    /// <summary>Which size.</summary>
    public ConstraintKind Kind;

    /// <summary>
    /// Pressed, the bar's size set to the button's value and its row recolored, and hovered or let
    /// go, its label lightened or dimmed unless it is the row's choice, as its interaction changes.
    /// </summary>
    [OnUpdate]
    [Changed(typeof(Interaction))]
    public void UpdateButtons(BehaviorContext ctx, in ButtonValue value)
    {
        var ecs = ctx.Ecs;
        var interaction = Ui.InteractionOf(ctx.Entity);
        if (interaction == UiInteraction.Pressed)
        {
            foreach (var bar in ecs.EntitiesWith<Bar>())
            {
                var node = ecs.Wrap<NodeRef>(bar);
                switch (Kind)
                {
                    case ConstraintKind.FlexBasis: node.FlexBasis = value.ToVal(); break;
                    case ConstraintKind.Width: node.Width = value.ToVal(); break;
                    case ConstraintKind.MinWidth: node.MinWidth = value.ToVal(); break;
                    case ConstraintKind.MaxWidth: node.MaxWidth = value.ToVal(); break;
                }
            }

            SizeConstraints.Activate(ctx, ctx.Entity, Kind);
            return;
        }

        var label = ecs.Wrap<TextColorRef>(SizeConstraints.LabelOf(ecs, ctx.Entity));
        if (label.Value != SizeConstraints.ActiveText) label.Value = interaction == UiInteraction.Hovered ? SizeConstraints.HoveredText : SizeConstraints.UnhoveredText;
    }
}
