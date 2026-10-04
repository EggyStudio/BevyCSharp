using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples;

/// <summary>
/// The row of radio buttons many of Bevy's examples are driven by, drawn as Bevy's
/// <c>helpers/widgets.rs</c> draws it, with a title and then the options side by side, the chosen
/// one white with black text and the rest the other way round.
/// </summary>
/// <remarks>
/// Bevy's helper sends a message for each press and leaves the colors to a system of the example's.
/// Here the buttons are kept in a list and asked each frame, and <see cref="Select"/> recolors them,
/// since an example here has its own frame loop and no message type to declare.
/// </remarks>
internal sealed class RadioButtons<T> where T : notnull
{
    private readonly List<(Entity Button, Entity Label, T Value)> _buttons = [];

    /// <summary>
    /// The column the rows go in, at the bottom left of the window, as Bevy's <c>main_ui_node</c>.
    /// </summary>
    public static Entity Column() => Ui.SpawnNode(new UiSettings
    {
        Absolute = true,
        Direction = UiDirection.Column,
        RowGap = Length.Px(6f),
        Left = Length.Px(10f),
        Bottom = Length.Px(10f),
    });

    /// <summary>A row of buttons under <paramref name="column"/>, titled, with one chosen.</summary>
    public RadioButtons(EcsWorld ecs, Entity column, string title, (T Value, string Name)[] options, T selected)
    {
        var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center });
        ecs.SetParent(row, column);
        ecs.SetParent(Ui.SpawnText(title, new UiSettings { Width = Length.Px(150f) }, 18f), row);

        for (var i = 0; i < options.Length; i++)
        {
            var (first, last) = (i == 0, i == options.Length - 1);
            var rounded = Length.Px(6f);
            var button = Ui.SpawnNode(new UiSettings
            {
                Interactive = true,
                Color = (0f, 0f, 0f, 1f),
                Border = new Sides(first ? Length.Px(1f) : Length.Zero, Length.Px(1f), Length.Px(1f), Length.Px(1f)),
                BorderColor = (1f, 1f, 1f, 1f),
                Justify = UiJustify.Center,
                Align = UiAlign.Center,
                Padding = new Sides(Length.Px(12f), Length.Px(6f), Length.Px(12f), Length.Px(6f)),
                Corners = new Corners(first ? rounded : Length.Zero, last ? rounded : Length.Zero, last ? rounded : Length.Zero, first ? rounded : Length.Zero),
            });
            ecs.SetParent(button, row);

            var label = Ui.SpawnText(options[i].Name, new UiSettings(), 18f);
            ecs.SetParent(label, button);
            _buttons.Add((button, label, options[i].Value));
        }

        Select(ecs, selected);
    }

    /// <summary>The option pressed this frame, if one was.</summary>
    public bool Pressed(out T value)
    {
        foreach (var (button, _, option) in _buttons)
        {
            if (Ui.InteractionOf(button) != UiInteraction.Pressed) continue;
            value = option;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>Shows <paramref name="selected"/> as the chosen option.</summary>
    public void Select(EcsWorld ecs, T selected)
    {
        foreach (var (button, label, option) in _buttons)
        {
            var chosen = EqualityComparer<T>.Default.Equals(option, selected);
            ecs.Wrap<BackgroundColorRef>(button).Value = chosen ? Color.White : Color.Black;
            ecs.Wrap<TextColorRef>(label).Value = chosen ? Color.Black : Color.White;
        }
    }
}
