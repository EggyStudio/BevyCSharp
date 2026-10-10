// Bevy's size_constraints example, examples/ui/layout/size_constraints.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates size constraints on a node, a white bar whose flex basis, width, minimum width and
// maximum width are each set from a row of Bevy's radio buttons, the checked one in each row lit.
internal static class SizeConstraints
{
    private static readonly Color ActiveBorder = Color.FromSrgb(250f / 255f, 235f / 255f, 215f / 255f);
    private static readonly Color InactiveBorder = Color.Black;
    private static readonly Color ActiveInner = Color.White;
    private static readonly Color InactiveInner = Color.FromSrgb(0f, 0f, 128f / 255f);
    private static readonly Color ActiveText = Color.Black;
    private static readonly Color HoveredText = Color.White;
    private static readonly Color UnhoveredText = Color.FromSrgb(0.5f, 0.5f, 0.5f);

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

        // Bevy's bar_scene, the bar white inside a black track inside yellow.
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
            RadioGroup(ecs, rows, label, kind, style);

        ecs.Observe<ValueChange<Entity>>(OnValueChangeConstraints);
    }, "size_constraints.Setup");

    // Bevy's radio_group_scene, a row of seven radio buttons for one field, Auto checked to begin
    // with.
    private static void RadioGroup(EcsWorld ecs, Entity parent, string field, ConstraintKind kind, UiTextSettings style)
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
        ecs.Insert<RadioGroupRef>(buttons);

        var values = new List<(string Text, RadioButtonValue Value)> { ("Auto", new RadioButtonValue { Auto = true }) };
        foreach (var percent in new[] { 0, 25, 50, 75, 100, 125 }) values.Add(($"{percent}%", new RadioButtonValue { Percent = percent }));
        foreach (var (text, value) in values) ecs.SetParent(RadioButton(ecs, kind, value, text, active: text == "Auto", style), buttons);
    }

    // Bevy's radio_button_scene, its label lightened while the pointer is over it unless it is
    // the row's checked one.
    private static Entity RadioButton(EcsWorld ecs, ConstraintKind kind, RadioButtonValue value, string text, bool active, UiTextSettings style)
    {
        var button = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Justify = UiJustify.Center, Border = Sides.All(Length.Px(2f)), Margin = Sides.Horizontal(Length.Px(2f)), BorderColor = active ? ActiveBorder : InactiveBorder });
        ecs.Insert<RadioButtonRef>(button);
        if (active) ecs.Insert<CheckedRef>(button);
        ecs.Add(button, new Constraint { Kind = kind });
        ecs.Add(button, value);

        var inner = Ui.SpawnNode(new UiSettings { Width = Length.Px(100f), Justify = UiJustify.Center, Color = active ? ActiveInner : InactiveInner });
        ecs.SetParent(inner, button);
        var label = Ui.SpawnText(text, new UiSettings { Color = active ? ActiveText : UnhoveredText }, new UiTextSettings { Font = style.Font, FontSize = style.FontSize, Justify = TextJustify.Center });
        ecs.SetParent(label, inner);

        ecs.Observe<Pointer<Over>>(button, on => Hover(on.Ecs, on.Entity, HoveredText));
        ecs.Observe<Pointer<Out>>(button, on => Hover(on.Ecs, on.Entity, UnhoveredText));
        return button;
    }

    // A radio button's label, its child's child.
    private static Entity LabelOf(EcsWorld ecs, Entity button) => ecs.ChildrenOf(ecs.ChildrenOf(button)[0])[0];

    private static void Hover(EcsWorld ecs, Entity button, Color color)
    {
        if (ecs.Get<CheckedRef>(button) is null) ecs.Wrap<TextColorRef>(LabelOf(ecs, button)).Value = color;
    }

    // A radio button lit as checked or dark as not, its border, its inside and its label.
    private static void Paint(EcsWorld ecs, Entity button, bool active)
    {
        var border = ecs.Wrap<BorderColorRef>(button);
        border.Top = border.Right = border.Bottom = border.Left = active ? ActiveBorder : InactiveBorder;
        ecs.Wrap<BackgroundColorRef>(ecs.ChildrenOf(button)[0]).Value = active ? ActiveInner : InactiveInner;
        ecs.Wrap<TextColorRef>(LabelOf(ecs, button)).Value = active ? ActiveText : UnhoveredText;
    }

    // Bevy's on_value_change_constraints, a radio button chosen in a row checked in place of the
    // row's last one and the bar's size it stands for set to its value.
    private static void OnValueChangeConstraints(On<ValueChange<Entity>> on)
    {
        var ecs = on.Ecs;
        var chosen = on.Event.Value;
        if (!ecs.Has<Constraint>(chosen) || ecs.Get<CheckedRef>(chosen) is not null) return;

        var kind = ecs.GetOrDefault<Constraint>(chosen).Kind;
        var value = ecs.GetOrDefault<RadioButtonValue>(chosen);
        foreach (var previous in ecs.EntitiesWith<Constraint>())
        {
            if (ecs.GetOrDefault<Constraint>(previous).Kind != kind || ecs.Get<CheckedRef>(previous) is not { } checkedOne) continue;

            checkedOne.Remove();
            Paint(ecs, previous, active: false);
        }

        ecs.Insert<CheckedRef>(chosen);
        Paint(ecs, chosen, active: true);

        foreach (var bar in ecs.EntitiesWith<Bar>())
        {
            var node = ecs.Wrap<NodeRef>(bar);
            switch (kind)
            {
                case ConstraintKind.FlexBasis: node.FlexBasis = value.ToVal(); break;
                case ConstraintKind.Width: node.Width = value.ToVal(); break;
                case ConstraintKind.MinWidth: node.MinWidth = value.ToVal(); break;
                case ConstraintKind.MaxWidth: node.MaxWidth = value.ToVal(); break;
            }
        }
    }
}

/// <summary>Which of the bar's sizes a row of radio buttons sets.</summary>
public enum ConstraintKind { FlexBasis, Width, MinWidth, MaxWidth }

/// <summary>The white bar whose sizes the radio buttons set.</summary>
[Behavior]
public partial struct Bar;

/// <summary>A radio button's value, Bevy's <c>RadioButtonValue</c> of a <c>Val</c>, here Auto or a percentage, the two the buttons hold.</summary>
[Behavior]
public partial struct RadioButtonValue
{
    /// <summary>Whether it is Auto.</summary>
    public bool Auto;

    /// <summary>The percentage, where it is not Auto.</summary>
    public float Percent;

    /// <summary>The value as Bevy's <c>Val</c>.</summary>
    public readonly Val ToVal() => Auto ? new Val.Auto() : new Val.Percent(Percent);
}

/// <summary>The size of the bar a radio button sets, as Bevy's <c>Constraint</c> enum keeps it on the button.</summary>
[Behavior]
public partial struct Constraint
{
    /// <summary>Which size.</summary>
    public ConstraintKind Kind;
}
