// Bevy's multiple_text_inputs example, examples/ui/text/multiple_text_inputs.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Arranges three text fields in a grid of three rows. Each field has its value beside it, kept in
// step as it is typed into, and the text it held when Enter was last pressed in it, which also
// clears it and moves the focus on to the next field. A row whose field has not the focus is drawn
// dimmer.
internal static class MultipleTextInputs
{
    private static readonly Color Slate300 = Color.FromSrgb8(203, 213, 225);
    private static readonly Color DarkGrey = Color.FromSrgb8(169, 169, 169);
    private static readonly Color DarkSlateBlue = Color.FromSrgb8(72, 61, 139);
    private static readonly Color DarkSlateGray = Color.FromSrgb8(47, 79, 79);
    private static readonly Color White = Color.FromSrgb(1f, 1f, 1f);

    // What each field held when last read, so its value is written only as it changes, as Bevy's
    // system runs only for a field that changed.
    private static readonly Dictionary<Entity, string> Shown = [];

    // The focus as it was last drawn, so the rows are redrawn only as it moves, as Bevy's are.
    private static Entity? _focusDrawn;
    private static bool _drawn;

    public static void Build(App app)
    {
        app.Startup(Setup, "multiple_text_inputs.Setup");
        app.Update(SynchronizeOutputText, "multiple_text_inputs.SynchronizeOutputText");
        app.Update(SubmitText, "multiple_text_inputs.SubmitText");
        app.Update(UpdateRowBorderColors, "multiple_text_inputs.UpdateRowBorderColors");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Shown.Clear();
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
        UiGrid.Set(root, new GridSettings { Columns = [Track.Px(320f).Repeated(3)], Rows = [Track.Auto.Repeated(6)] });
        ecs.Insert<TabGroupRef>(root);

        // Spans the three columns, centered within them.
        Entity Across(string text, Sides margin)
        {
            var line = Ui.SpawnText(text, new UiSettings { Margin = margin, Color = White }, Font(24f));
            UiGrid.Place(line, new GridPlacement { ColumnSpan = 3 });
            ecs.Wrap<NodeRef>(line).JustifySelf = NodeRef.JustifySelfVariant.Center;
            ecs.SetParent(line, root);
            return line;
        }

        Across("Multiple Text Inputs Example", new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(16f)));

        foreach (var label in new[] { "EditableText", "value", "submission" })
        {
            var heading = Ui.SpawnText(label, new UiSettings { Margin = new Sides(Length.Zero, Length.Zero, Length.Zero, Length.Px(-4f)) }, Font(14f));
            ecs.Wrap<NodeRef>(heading).JustifySelf = NodeRef.JustifySelfVariant.Center;
            ecs.SetParent(heading, root);
        }

        for (var row = 0; row < 3; row++)
        {
            var input = Ui.SpawnNode(new UiSettings
            {
                Border = Sides.All(Length.Px(4f)),
                Padding = Sides.All(Length.Px(4f)),
                Color = DarkGrey,
                BorderColor = Slate300,
            });
            Ui.SetEditableText(input, new UiEditableTextSettings { Text = $"Initial text {row}" });
            var inputFont = ecs.Wrap<TextFontRef>(input);
            (inputFont.Font, inputFont.FontSize) = (new FontSource.Handle(font), new FontSize.Px(24f));
            ecs.Wrap<TextLayoutRef>(input).Linebreak = TextLayoutRef.LinebreakVariant.NoWrap;
            ecs.Insert<TabIndexRef>(input).Value = row;
            ecs.Add(input, new TextInputRow { Row = row });
            if (row == 0) ecs.Insert<AutoFocusRef>(input);
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
        }

        Across("Press Enter to submit", new Sides(Length.Zero, Length.Px(16f), Length.Zero, Length.Zero));
    }

    // Bevy's synchronize_output_text, each field's value written beside it as it changes.
    private static void SynchronizeOutputText(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var input in Inputs(ecs))
        {
            var text = Ui.EditableTextOf(input) ?? string.Empty;
            if (Shown.TryGetValue(input, out var shown) && shown == text) continue;
            Shown[input] = text;

            var row = ecs.GetOrDefault<TextInputRow>(input).Row;
            foreach (var output in ecs.EntitiesWith<ValueOutput>())
            {
                if (ecs.GetOrDefault<TextInputRow>(output).Row == row) Ui.SetText(output, text);
            }
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

/// <summary>Which of the three rows a field, a box or a text belongs to.</summary>
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
