// Bevy's text_input example, examples/ui/text/text_input.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows two text fields, Tab moving between them, and Enter writing what the focused one holds
// below them and clearing it.
internal static class TextInput
{
    public static void Build(App app) => app.Startup(Setup, "text_input.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Align = UiAlign.Center,
            Direction = UiDirection.Column,
            Padding = Sides.All(Length.Px(20f)),
            RowGap = Length.Px(16f),
            Margin = Sides.All(Length.Auto),
        });

        var instructions = Ui.SpawnText("Enter to submit text\nTab to switch inputs", new UiSettings(),
            new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 25f });
        ecs.SetParent(instructions, root);

        // A row of the two fields, which Tab moves between in the order of their indices, and
        // which is focused as it opens.
        var row = Ui.SpawnNode(new UiSettings { ColumnGap = Length.Px(16f) });
        ecs.Insert<AutoFocusRef>(row);
        ecs.Insert<TabGroupRef>(row);
        ecs.SetParent(row, root);
        foreach (var (name, index) in new[] { ("Left", 0), ("Right", 1) })
        {
            var field = Ui.SpawnNode(new UiSettings { Border = Sides.All(Length.Px(2f)), BorderColor = Color.FromSrgb8(203, 213, 225), Color = Color.FromSrgb8(169, 169, 169) });
            Ui.SetEditableText(field, new UiEditableTextSettings { VisibleWidth = 10f });
            ecs.Wrap<TextLayoutRef>(field).Linebreak = TextLayoutRef.LinebreakVariant.NoWrap;
            ecs.Wrap<TextFontRef>(field).FontSize = new FontSize.Px(24f);
            ecs.Insert<TabIndexRef>(field).Value = index;
            ecs.SetParent(field, row);
            ecs.SetName(field, name);
        }

        // What was submitted, a line tall while it is empty, wrapping at a word or, where a word
        // is too long, anywhere.
        var output = Ui.SpawnText(string.Empty,
            new UiSettings { Width = Length.Px(400f), Border = Sides.All(Length.Px(2f)), Padding = Sides.All(Length.Px(8f)), BorderColor = Color.FromSrgb8(203, 213, 225) },
            new UiTextSettings { FontSize = 24f });
        ecs.Wrap<TextLayoutRef>(output).Linebreak = TextLayoutRef.LinebreakVariant.WordOrCharacter;
        ecs.Add(output, new TextOutput());
        ecs.SetParent(output, root);
    }
}

/// <summary>The text that shows what was submitted.</summary>
[Behavior]
public partial struct TextOutput
{
    /// <summary>
    /// Enter writes the focused field's name and what it holds here, and clears the field, a
    /// field being the focused entity with editable text and a name.
    /// </summary>
    [OnUpdate]
    public void TextSubmission(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Enter)) return;
        if (ctx.Ecs.Resource<InputFocusRef>()?.CurrentFocus is not { } focused || ctx.Ecs.NameOf(focused) is not { } name) return;

        Ui.SetText(ctx.Entity, $"{name}: {Ui.EditableTextOf(focused)}");
        Ui.SetEditableValue(focused, string.Empty);
    }
}
