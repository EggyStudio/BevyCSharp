using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows two text fields, Tab moving between them, and Enter writing what the focused one holds
// below them and clearing it.
internal static class TextInput
{
    private static readonly Dictionary<Entity, string> Names = [];
    private static Entity _output;

    public static void Build(App app)
    {
        app.Startup(Setup, "text_input.Setup");
        app.Update(TextSubmission, "text_input.TextSubmission");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Names.Clear();
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
            var field = Ui.SpawnNode(new UiSettings { Border = Sides.All(Length.Px(2f)), BorderColor = Scene.Srgb8(203, 213, 225), Color = Scene.Srgb8(169, 169, 169) });
            Ui.SetEditableText(field, new UiEditableTextSettings { VisibleWidth = 10f });
            ecs.Wrap<TextLayoutRef>(field).Linebreak = TextLayoutRef.LinebreakVariant.NoWrap;
            ecs.Wrap<TextFontRef>(field).FontSize = new FontSize.Px(24f);
            ecs.Insert<TabIndexRef>(field).Value = index;
            ecs.SetParent(field, row);
            Names[field] = name;
        }

        // What was submitted, a line tall while it is empty, wrapping at a word or, where a word
        // is too long, anywhere.
        _output = Ui.SpawnText(string.Empty,
            new UiSettings { Width = Length.Px(400f), Border = Sides.All(Length.Px(2f)), Padding = Sides.All(Length.Px(8f)), BorderColor = Scene.Srgb8(203, 213, 225) },
            new UiTextSettings { FontSize = 24f });
        ecs.Wrap<TextLayoutRef>(_output).Linebreak = TextLayoutRef.LinebreakVariant.WordOrCharacter;
        ecs.SetParent(_output, root);
    }

    private static void TextSubmission(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Enter)) return;
        if (ctx.Ecs.Resource<InputFocusRef>()?.CurrentFocus is not { } focused || !Names.TryGetValue(focused, out var name)) return;

        Ui.SetText(_output, $"{name}: {Ui.EditableTextOf(focused)}");
        Ui.SetEditableValue(focused, string.Empty);
    }
}
