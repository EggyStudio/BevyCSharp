// Bevy's virtual_keyboard example, examples/ui/widgets/virtual_keyboard.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Feathers' virtual keyboard, in a navy panel at the bottom of the window, typing into a text field
// at the top, its keys the digits, the letters, some punctuation, space, enter, backspace and the
// cursor's moves.
internal static class VirtualKeyboardExample
{
    private static readonly string[][] Keys =
    [
        ["1", "2", "3", "4", "5", "6", "7", "8", "9", "0", ".", ","],
        ["Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P"],
        ["A", "S", "D", "F", "G", "H", "J", "K", "L", "'"],
        ["Z", "X", "C", "V", "B", "N", "M", "-", "/"],
        ["space", "enter", "backspace"],
        ["left", "right", "up", "down", "home", "end"],
    ];

    private static Entity _field;

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        TextInput(ecs);
        Keyboard(ecs);
    }, "virtual_keyboard.Setup");

    private static void OnVirtualKeyPressed(EcsWorld ecs, string key)
    {
        Console.WriteLine($"Virtual keyboard key pressed: {key}");
        TextEdit? edit = key switch
        {
            "space" => TextEdit.Insert(" "),
            "enter" => TextEdit.Insert("\n"),
            "backspace" => TextEdit.Backspace,
            "left" => TextEdit.Left(),
            "right" => TextEdit.Right(),
            "up" => TextEdit.Up(),
            "down" => TextEdit.Down(),
            "home" => TextEdit.LineStart(),
            "end" => TextEdit.LineEnd(),
            _ when key.Length == 1 => TextEdit.Insert(key),
            _ => null,
        };
        if (edit is not { } queued) return;

        Ui.EditText(_field, queued);
        Ui.Focus(_field, FocusCause.Navigated);
    }

    private static void Keyboard(EcsWorld ecs)
    {
        var bottom = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Bottom = Length.Px(0f),
            Direction = UiDirection.Column,
            Absolute = true,
            Align = UiAlign.Center,
            Justify = UiJustify.End,
        });

        var panel = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            Border = Sides.All(Length.Px(5f)),
            RowGap = Length.Px(5f),
            Padding = Sides.All(Length.Px(5f)),
            Align = UiAlign.Center,
            Margin = Sides.All(Length.Px(25f)),
            Corners = Corners.All(Length.Px(10f)),
            Color = Color.FromSrgb8(0, 0, 128),
            BorderColor = Color.White,
        });
        ecs.SetParent(panel, bottom);

        ecs.SetParent(Ui.SpawnText("virtual keyboard", new UiSettings()), panel);

        var keyboard = Feathers.VirtualKeyboard(panel, Keys);
        ecs.Observe<VirtualKeyPressed>(keyboard, on => OnVirtualKeyPressed(on.Ecs, on.Event.Key));
    }

    private static void TextInput(EcsWorld ecs)
    {
        var top = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Padding = Sides.All(Length.Px(25f)),
            Absolute = true,
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
        });
        ecs.Insert<TabGroupRef>(top);

        _field = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(80f),
            Border = Sides.All(Length.Px(5f)),
            Padding = Sides.All(Length.Px(5f)),
            Corners = Corners.All(Length.Px(10f)),
            BorderColor = Color.White,
        });
        ecs.SetParent(_field, top);
        Ui.SetEditableText(_field, new UiEditableTextSettings { VisibleLines = 5f, AllowNewlines = true });
        ecs.Wrap<TextLayoutRef>(_field).Linebreak = TextLayoutRef.LinebreakVariant.NoWrap;
        Ui.SetTextCursor(_field, new UiTextCursorSettings { Color = Color.White });
        ecs.Wrap<TextFontRef>(_field).FontSize = new FontSize.Px(25f);
        ecs.Insert<TabIndexRef>(_field).Value = 0;
        ecs.Insert<AutoFocusRef>(_field);
    }
}
