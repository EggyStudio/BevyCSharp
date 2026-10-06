// Bevy's multiline_text_input example, examples/ui/text/multiline_text_input.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using System.Globalization;
using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates a single, minimal multiline text field, eight lines tall and wrapping at a word or
// anywhere, which Ctrl+Enter prints, beside two fields that set how many lines it shows and the
// size of its font, each taken as Enter is pressed in it.
internal static class MultilineTextInput
{
    private static readonly Color DarkSlateGray = Color.FromSrgb8(47, 79, 79);
    private static readonly Color Slate300 = Color.FromSrgb8(203, 213, 225);

    private static AssetHandle _font;
    private static Entity _multiline;

    public static void Build(App app) => app.Startup(Setup, "multiline_text_input.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _font = AssetServer.Load(AssetKind.Font, "fonts/FiraMono-Medium.ttf");
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
        });

        var column = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Align = UiAlign.End, RowGap = Length.Px(10f) });
        ecs.Insert<TabGroupRef>(column);
        ecs.SetParent(column, root);

        _multiline = Field(ecs, new UiSettings { Width = Length.Px(450f), Padding = Sides.All(Length.Px(8f)) },
            new UiEditableTextSettings { VisibleLines = 8f, AllowNewlines = true });
        ecs.Wrap<TextLayoutRef>(_multiline).Linebreak = TextLayoutRef.LinebreakVariant.WordOrCharacter;
        ecs.Insert<TabIndexRef>(_multiline).Value = 0;
        ecs.Insert<AutoFocusRef>(_multiline);
        ecs.SetParent(_multiline, column);

        // Ctrl+Enter prints what it holds, where Enter alone starts a new line.
        ecs.Observe<FocusedInput<KeyboardInput>>(_multiline, on =>
        {
            var input = on.Event.Input;
            if (!(input.State == ButtonState.Pressed && input.LogicalKey == LogicalKey.Enter && on.Context.Input.KeyDown(LogicalKey.Control))) return;
            if (Ui.EditableTextOf(on.Event.FocusedEntity) is { } output) Console.WriteLine(output);
        });

        // How many lines it shows, from one to ten, which a field of the row's sets as Enter is
        // pressed in it. A field's lines are among its settings, so it is set again with what it
        // holds.
        Row(ecs, column, "visible lines:", "8", "0123456789.", 1, text =>
        {
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var lines)) return;
            Ui.SetEditableText(_multiline, new UiEditableTextSettings
            {
                Text = Ui.EditableTextOf(_multiline) ?? string.Empty,
                VisibleLines = Math.Clamp(lines, 1f, 10f),
                AllowNewlines = true,
            });
        });

        // The size of its font, from five pixels to fifty.
        Row(ecs, column, "font size:", "30", "0123456789", 2, text =>
        {
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var size)) return;
            ecs.Wrap<TextFontRef>(_multiline).FontSize = new FontSize.Px(Math.Clamp(size, 5f, 50f));
        });
    }

    // A label and a field a hundred pixels wide, its text at its right, all selected as it gains the
    // focus, and an observer on the row taking what the field holds as Enter is pressed in it.
    private static void Row(EcsWorld ecs, Entity column, string label, string text, string allowed, int index, Action<string> take)
    {
        var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, ColumnGap = Length.Px(10f) });
        ecs.SetParent(row, column);
        ecs.SetParent(Ui.SpawnText(label, new UiSettings(), new UiTextSettings { Font = _font, FontSize = 30f }), row);

        var field = Field(ecs, new UiSettings { Width = Length.Px(100f) }, new UiEditableTextSettings { Text = text, Allowed = allowed });
        ecs.Wrap<TextLayoutRef>(field).Justify = TextLayoutRef.JustifyVariant.End;
        ecs.Insert<SelectAllOnFocusRef>(field);
        ecs.Insert<TabIndexRef>(field).Value = index;
        ecs.SetParent(field, row);

        ecs.Observe<FocusedInput<KeyboardInput>>(row, on =>
        {
            var input = on.Event.Input;
            if (!(input.State == ButtonState.Pressed && input.LogicalKey == LogicalKey.Enter) || on.Event.FocusedEntity != field) return;
            if (Ui.EditableTextOf(field) is { } value) take(value);
        });
    }

    // A field in the example's font and colors, bordered two pixels wide.
    private static Entity Field(EcsWorld ecs, UiSettings node, UiEditableTextSettings settings)
    {
        node.Border = Sides.All(Length.Px(2f));
        node.BorderColor = Slate300;
        node.Color = DarkSlateGray;
        var field = Ui.SpawnNode(node);
        Ui.SetEditableText(field, settings);

        var font = ecs.Wrap<TextFontRef>(field);
        font.Font = new FontSource.Handle(_font);
        font.FontSize = new FontSize.Px(30f);
        return field;
    }
}
