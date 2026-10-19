// Bevy's feathers_number_input example, examples/ui/widgets/feathers_number_input.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Feathers' number input in each of its settings, a field a setting with its name above it, which
// are bare, with a soft limit a drag stays within, a hard limit typing cannot pass, both, its
// digits, its step, a double, an int, a letter with a colored strip, disabled, wrapping past its
// hard limit, and in meters, seconds and degrees. Each writes back the value it is asked for, as
// Bevy's does.
internal static class FeathersNumberInput
{
    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Start,
            Justify = UiJustify.Start,
            Direction = UiDirection.Column,
            Wrap = UiWrap.Wrap,
            Padding = Sides.All(Length.Px(8f)),
            RowGap = Length.Px(8f),
        });
        ecs.Insert<TabGroupRef>(root);
        Feathers.ThemeColor(root, FeathersToken.WindowBg);

        Field(ecs, root, "none (bare)", 1f, _ => { });
        Field(ecs, root, "soft limit", 2f, input => Feathers.SetNumberLimit(input, 0f, 10f));
        Field(ecs, root, "hard limit", 3f, input => Feathers.SetNumberLimit(input, -100f, 100f, hard: true));
        Field(ecs, root, "soft + hard", 4f, input =>
        {
            Feathers.SetNumberLimit(input, 0f, 10f);
            Feathers.SetNumberLimit(input, -100f, 100f, hard: true);
        });
        Field(ecs, root, "precision(0)", 5f, input => ecs.Insert<NumberInputPrecisionRef>(input).Value = 0);
        Field(ecs, root, "precision(2)", 6f, input => ecs.Insert<NumberInputPrecisionRef>(input).Value = 2);
        Field(ecs, root, "precision(4)", 7f, input => ecs.Insert<NumberInputPrecisionRef>(input).Value = 4);
        Field(ecs, root, "step(1.0)", 8f, input => ecs.Insert<NumberInputStepRef>(input).Value = 1.0);
        Field(ecs, root, "f64: soft limit", 1.0, input => Feathers.SetNumberLimit(input, 0.0, 10.0));
        Field(ecs, root, "f64: soft limit + precision(2)", 1.0, input =>
        {
            Feathers.SetNumberLimit(input, 0.0, 10.0);
            ecs.Insert<NumberInputPrecisionRef>(input).Value = 2;
        });
        Field(ecs, root, "i32: bare", 1, _ => { });
        Field(ecs, root, "i32: soft limit", 1, input => Feathers.SetNumberLimit(input, 0, 10));
        Field(ecs, root, "precision(2) + sigil", 6f, input => ecs.Insert<NumberInputPrecisionRef>(input).Value = 2,
            letter: "X", sigil: FeathersToken.TextInputXAxis, rowGap: false);
        Field(ecs, root, "soft limit + disabled", 2f, input =>
        {
            ecs.Insert<InteractionDisabledRef>(input);
            Feathers.SetNumberLimit(input, 0f, 10f);
        });
        Field(ecs, root, "hard limit + wrap", 0f, input =>
        {
            Feathers.SetNumberLimit(input, -180f, 180f, hard: true);
            ecs.Insert<NumberInputWrapRef>(input).Value = NumberInputWrapRef.ValueVariant.Wrap;
        });
        Field(ecs, root, "in meters", 2f, input => Feathers.SetNumberUnits(input, NumberUnits.LengthMeters));
        Field(ecs, root, "in seconds", 2f, input => Feathers.SetNumberUnits(input, NumberUnits.TimeSeconds));
        Field(ecs, root, "in degrees", MathF.PI, input => Feathers.SetNumberUnits(input, NumberUnits.AngleDegrees));
    }, "feathers_number_input.Setup");

    private static void Field(EcsWorld ecs, Entity root, string label, float value, Action<Entity> options, string? letter = null, string? sigil = null, bool rowGap = true)
    {
        var input = Input(ecs, root, label, options, letter, sigil, rowGap, input => Feathers.SetNumber(input, value));
        ecs.Observe<ValueChange<float>>(input, on => Feathers.SetNumber(on.Event.Source, on.Event.Value));
    }

    private static void Field(EcsWorld ecs, Entity root, string label, double value, Action<Entity> options)
    {
        var input = Input(ecs, root, label, options, null, null, true, input => Feathers.SetNumber(input, value));
        ecs.Observe<ValueChange<double>>(input, on => Feathers.SetNumber(on.Event.Source, on.Event.Value));
    }

    private static void Field(EcsWorld ecs, Entity root, string label, int value, Action<Entity> options)
    {
        var input = Input(ecs, root, label, options, null, null, true, input => Feathers.SetNumber(input, value));
        ecs.Observe<ValueChange<int>>(input, on => Feathers.SetNumber(on.Event.Source, on.Event.Value));
    }

    // A column with the setting's name over a row holding the input and a label of a dash, Bevy's
    // "Output", which nothing writes.
    private static Entity Input(EcsWorld ecs, Entity root, string label, Action<Entity> options, string? letter, string? sigil, bool rowGap, Action<Entity> value)
    {
        var column = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            Align = UiAlign.Start,
            Width = Length.Px(200f),
            RowGap = rowGap ? Length.Px(4f) : Length.Zero,
        });
        ecs.SetParent(column, root);
        Feathers.Label(column, label);

        var row = Ui.SpawnNode(new UiSettings
        {
            Align = UiAlign.Center,
            AlignSelf = UiAlignSelf.Stretch,
            Justify = UiJustify.SpaceBetween,
        });
        ecs.SetParent(row, column);

        // Its settings before its value, since it writes its text as its value changes.
        var input = Feathers.NumberInput(row, letter, sigil);
        options(input);
        value(input);
        var node = ecs.Wrap<NodeRef>(input);
        (node.FlexGrow, node.MaxWidth) = (1f, new Val.Px(120f));

        Feathers.Label(row, "-");
        return input;
    }
}
