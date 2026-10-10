// Bevy's multiple_text_inputs example, examples/ui/text/multiple_text_inputs.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

using Justify = Bevy.Reflected.TextLayoutRef.JustifyVariant;

namespace BevyCSharp.Examples.Interface;

// Arranges text fields in a grid of four columns, one for each way a line is justified and one for
// each way a field takes changes, typed into, read only or shown alone. Each field has its value
// beside it, kept in step as it reports its edits, and the text it held when Enter was last pressed
// in it, which also clears it and moves the focus on to the next field. A row whose field has not
// the focus is drawn dimmer.
internal static class MultipleTextInputs
{
    private static readonly Color Slate300 = Color.FromSrgb8(203, 213, 225);
    private static readonly Color DarkGrey = Color.FromSrgb8(169, 169, 169);
    private static readonly Color DarkSlateBlue = Color.FromSrgb8(72, 61, 139);
    private static readonly Color DarkSlateGray = Color.FromSrgb8(47, 79, 79);
    private static readonly Color White = Color.FromSrgb(1f, 1f, 1f);

    // The focus as it was last drawn, so the rows are redrawn only as it moves, as Bevy's are.
    private static Entity? _focusDrawn;
    private static bool _drawn;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Setup(ctx);
            ctx.Ecs.Observe<TextEditChange>(SynchronizeOutputText);
        }, "multiple_text_inputs.Setup");
        app.Update(SubmitText, "multiple_text_inputs.SubmitText");
        app.Update(UpdateRowBorderColors, "multiple_text_inputs.UpdateRowBorderColors");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_focusDrawn, _drawn) = (null, false);
        Render2d.SpawnCamera2d();

        var font = AssetServer.Load(AssetKind.Font, "fonts/FiraMono-Medium.ttf");
        UiTextSettings Font(float size, TextWrap wrap = TextWrap.WordBoundary) => new() { Font = font, FontSize = size, Wrap = wrap };

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Display = UiDisplay.Grid,
            Justify = UiJustify.Center,
            AlignContent = UiJustify.Center,
            RowGap = Length.Px(8f),
            ColumnGap = Length.Px(8f),
        });
        UiGrid.Set(root, new GridSettings { Columns = [Track.Px(160f), Track.Px(320f).Repeated(3)] });
        ecs.Insert<TabGroupRef>(root);

        // Spans the four columns, centered within them.
        void Across(string text, Sides margin)
        {
            var line = Ui.SpawnText(text, new UiSettings { Margin = margin, Color = White }, Font(24f));
            UiGrid.Place(line, new GridPlacement { ColumnSpan = 4 });
            ecs.Wrap<NodeRef>(line).JustifySelf = NodeRef.JustifySelfVariant.Center;
            ecs.SetParent(line, root);
        }

        void Headings(string first)
        {
            foreach (var label in new[] { first, "EditableText", "value", "submission" })
            {
                var heading = Ui.SpawnText(label, new UiSettings { Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(-4f)) }, Font(14f));
                ecs.Wrap<NodeRef>(heading).JustifySelf = NodeRef.JustifySelfVariant.Center;
                ecs.SetParent(heading, root);
            }
        }

        // A row of the grid: what sets its field apart, the field, its value and its submission.
        Entity Row(int row, string name)
        {
            var label = Ui.SpawnNode(new UiSettings { Border = Sides.All(Length.Px(4f)), Justify = UiJustify.Center, Align = UiAlign.Center, BorderColor = White });
            ecs.SetParent(Ui.SpawnText(name, new UiSettings(), Font(24f)), label);
            ecs.SetParent(label, root);

            var input = Ui.SpawnNode(new UiSettings
            {
                Border = Sides.All(Length.Px(4f)),
                Padding = Sides.All(Length.Px(4f)),
                Color = DarkGrey,
                BorderColor = Slate300,
            });
            ecs.SetParent(input, root);

            // The value, kept in step with the field, and the submission, each clipped to the
            // content of its box.
            foreach (var submission in new[] { false, true })
            {
                var box = Ui.SpawnNode(new UiSettings
                {
                    Border = Sides.All(Length.Px(4f)),
                    Padding = Sides.All(Length.Px(4f)),
                    OverflowX = UiOverflow.Clip,
                    ClipBox = UiClipBox.Content,
                    Color = DarkSlateBlue,
                    BorderColor = White,
                });
                ecs.SetParent(box, root);

                var output = Ui.SpawnText(string.Empty, new UiSettings(), Font(24f, TextWrap.NoWrap));
                ecs.Add(output, new TextInputRow { Row = row });
                if (submission)
                {
                    ecs.Add(output, new SubmitOutput());
                }
                else
                {
                    ecs.Insert<BackgroundColorRef>(output).Value = DarkSlateGray;
                    var border = ecs.Insert<BorderColorRef>(output);
                    (border.Top, border.Right, border.Bottom, border.Left) = (White, White, White, White);
                    ecs.Add(output, new ValueOutput());
                }
                ecs.SetParent(output, box);
            }

            return input;
        }

        // Its field set in its font at its size, on one line, at a place in the tab order.
        void Field(Entity input, int row, TextReadWriteMode mode, Justify justify)
        {
            Ui.SetEditableText(input, new UiEditableTextSettings { Text = $"Initial text {row % 100}", Mode = mode });
            var inputFont = ecs.Wrap<TextFontRef>(input);
            (inputFont.Font, inputFont.FontSize) = (new FontSource.Handle(font), new FontSize.Px(24f));
            var layout = ecs.Wrap<TextLayoutRef>(input);
            (layout.Linebreak, layout.Justify) = (TextLayoutRef.LinebreakVariant.NoWrap, justify);
            ecs.Insert<TabIndexRef>(input).Value = row;
            ecs.Add(input, new TextInputRow { Row = row });
            if (row % 100 == 0) ecs.Insert<AutoFocusRef>(input);
        }

        Across("Multiple Text Inputs Example", new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(16f)));

        // A field for each of the ways a line is justified.
        Headings("Justify");
        Justify[] justifications = [Justify.Left, Justify.Center, Justify.Right, Justify.Justified, Justify.Start, Justify.End];
        for (var row = 0; row < justifications.Length; row++)
            Field(Row(row, $"{justifications[row]}"), row, TextReadWriteMode.Editable, justifications[row]);

        // And one for each of the ways a field takes changes, after the others in the tab order.
        Headings("ReadWrite");
        TextReadWriteMode[] modes = [TextReadWriteMode.Editable, TextReadWriteMode.ReadOnly, TextReadWriteMode.Static];
        for (var row = 0; row < modes.Length; row++)
            Field(Row(row + 100, $"{modes[row]}"), row + 100, modes[row], Justify.Left);

        Across("Press Enter to submit", new Sides(Length.Zero, Length.Px(16f), Length.Zero, Length.Zero));
    }

    // Bevy's synchronize_output_text, each field's value written beside it as it reports an edit.
    private static void SynchronizeOutputText(On<TextEditChange> on)
    {
        var ecs = on.Ecs;
        var input = on.Event.Entity;
        if (!ecs.Has<TextInputRow>(input) || Ui.EditableTextOf(input) is not { } text) return;

        var row = ecs.GetOrDefault<TextInputRow>(input).Row;
        foreach (var output in ecs.EntitiesWith<ValueOutput>())
        {
            if (ecs.GetOrDefault<TextInputRow>(output).Row == row) Ui.SetText(output, text);
        }
    }

    // Bevy's submit_text. Enter writes the focused field's value beside it as its submission,
    // clears it, and moves the focus on to the next field.
    private static void SubmitText(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (!ctx.Input.KeyPressed(LogicalKey.Enter)
            || ecs.Resource<InputFocusRef>()?.CurrentFocus is not { } focused
            || !IsField(focused)
            || !ecs.Has<TextInputRow>(focused))
            return;

        var row = ecs.GetOrDefault<TextInputRow>(focused).Row;
        foreach (var output in ecs.EntitiesWith<SubmitOutput>())
        {
            if (ecs.GetOrDefault<TextInputRow>(output).Row != row) continue;
            Ui.SetText(output, Ui.EditableTextOf(focused) ?? string.Empty);
            break;
        }
        Ui.SetEditableValue(focused, string.Empty);

        if (Ui.Navigate(NavAction.Next) is { } next) Ui.Focus(next);
    }

    // Bevy's update_row_border_colors, the row of the focused field drawn as it was made and the
    // others dimmer, as the focus moves.
    private static void UpdateRowBorderColors(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var focus = ecs.Resource<InputFocusRef>()?.CurrentFocus;
        if (_drawn && focus == _focusDrawn) return;
        (_focusDrawn, _drawn) = (focus, true);

        int? focusedRow = focus is { } entity && IsField(entity) && ecs.Has<TextInputRow>(entity)
            ? ecs.GetOrDefault<TextInputRow>(entity).Row
            : null;

        // Every node of a row with a border color, the field and its value's text, whose border is
        // no width and so is not seen, as in Bevy's.
        foreach (var node in ecs.EntitiesWith<TextInputRow>())
        {
            if (ecs.Has<SubmitOutput>(node)) continue;

            var color = IsField(node) ? Slate300 : White;
            if (ecs.GetOrDefault<TextInputRow>(node).Row != focusedRow) color = Darker(color, 0.75f);
            var border = ecs.Wrap<BorderColorRef>(node);
            (border.Top, border.Right, border.Bottom, border.Left) = (color, color, color, color);
        }
    }

    private static IEnumerable<Entity> Inputs(EcsWorld ecs) =>
        ecs.EntitiesWith<TextInputRow>().Where(IsField);

    // Whether a node is a text field, which only a field has text of to read.
    private static bool IsField(Entity node) => Ui.EditableTextOf(node) is not null;

    // Bevy's Color::darker, which lowers a linear color's luminance by the amount by mixing it
    // toward black.
    private static Color Darker(Color color, float amount)
    {
        var luminance = 0.2126f * color.R + 0.7152f * color.G + 0.0722f * color.B;
        if (luminance <= 0f) return color;
        var t = (luminance - Math.Max(luminance - amount, 0f)) / luminance;
        return new Color(color.R * (1f - t), color.G * (1f - t), color.B * (1f - t), color.A);
    }
}

/// <summary>
/// Which row a field, a box or a text belongs to, the read-write rows numbered from a hundred.
/// </summary>
[Behavior]
public partial struct TextInputRow
{
    /// <summary>The row, from the top.</summary>
    public int Row;
}

/// <summary>
/// The text that holds a field's value as it is typed, Bevy's <c>TextOutput</c> under another name
/// since text_input's has it.
/// </summary>
[Behavior]
public partial struct ValueOutput;

/// <summary>The text that holds what a field held when Enter was last pressed in it.</summary>
[Behavior]
public partial struct SubmitOutput;
