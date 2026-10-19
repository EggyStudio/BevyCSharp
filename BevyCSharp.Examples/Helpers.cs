// Bevy's examples/helpers/radio.rs, examples/helpers/theme.rs and examples/helpers/number_input_f32.rs
// at v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#. Bevy's examples
// take them in by path, a row of Feathers' radio buttons for a setting, the plain theme they are
// drawn in and a labeled number input, and the examples here that Bevy's take them into share this
// file, which build/examples-on-package.sh lists for it.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples;

/// <summary>
/// Bevy's <c>helpers/radio.rs</c>, an option menu of Feathers' radio buttons at the bottom left of
/// the window.
/// </summary>
internal static class Radio
{
    // Each radio button's option value, Bevy's RadioButtonOptionValue<T> component, by the button.
    private static readonly Dictionary<Entity, object> OptionValues = [];

    /// <summary>
    /// The outer node of the interface, Bevy's <c>main_ui_node_scene</c>, a column at the window's
    /// bottom left the rows go in.
    /// </summary>
    public static Entity MainUiNode() => Ui.SpawnNode(new UiSettings
    {
        Direction = UiDirection.Column,
        Absolute = true,
        RowGap = Length.Px(6f),
        Left = Length.Px(10f),
        Bottom = Length.Px(10f),
    });

    /// <summary>
    /// A row of radio buttons that sets one option, Bevy's <c>feathers_option_buttons</c>, its title
    /// and then each option, the one at <paramref name="selectedOption"/> checked.
    /// </summary>
    /// <remarks>
    /// The row is a radio group, which reports the button clicked as a <c>ValueChange</c> of its
    /// <see cref="Entity"/>, whose value <see cref="OptionValue{T}"/> reads, as Bevy's examples query
    /// the button's <c>RadioButtonOptionValue</c>. Bevy's examples have the group keep its own state
    /// through <c>radio_self_update</c>, which <see cref="Ui.SelfUpdate"/> attaches.
    /// </remarks>
    public static Entity FeathersOptionButtons<T>(EcsWorld ecs, Entity parent, string title, (T Value, string Name)[] options, int selectedOption)
        where T : notnull
    {
        var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, ColumnGap = Length.Px(5f) });
        ecs.SetParent(row, parent);
        ecs.Insert<RadioGroupRef>(row);
        Feathers.Label(row, title);

        for (var i = 0; i < options.Length; i++)
        {
            var button = Feathers.Radio(row, options[i].Name);
            ecs.Insert<HoveredRef>(button);
            if (i == selectedOption) ecs.Insert<CheckedRef>(button);
            OptionValues[button] = options[i].Value;
        }

        return row;
    }

    /// <summary>
    /// The option value of a radio button from <see cref="FeathersOptionButtons{T}"/>, Bevy's
    /// <c>RadioButtonOptionValue</c>, where it is a <typeparamref name="T"/>.
    /// </summary>
    public static bool OptionValue<T>(Entity button, out T value)
    {
        if (OptionValues.TryGetValue(button, out var held) && held is T option)
        {
            value = option;
            return true;
        }

        value = default!;
        return false;
    }
}

/// <summary>
/// Bevy's <c>helpers/theme.rs</c>, the plain theme its examples draw their radio buttons, number
/// inputs, checkboxes, buttons, selects and sliders in.
/// </summary>
internal static class Theme
{
    // Feathers' palette, Bevy's bevy_feathers::palette, as its examples' theme takes it.
    private static readonly Color Transparent = Color.From(ColorSpace.Oklch, new Vec4(0f, 0f, 0f, 0f));
    private static readonly Color PaletteBlack = Color.From(ColorSpace.Oklch, new Vec4(0f, 0f, 0f, 1f));
    private static readonly Color Gray0 = Color.From(ColorSpace.Oklch, new Vec4(0.2414f, 0.0095f, 285.67f, 1f));
    private static readonly Color Gray1 = Color.From(ColorSpace.Oklch, new Vec4(0.2866f, 0.0072f, 285.93f, 1f));
    private static readonly Color Gray2 = Color.From(ColorSpace.Oklch, new Vec4(0.3373f, 0.0071f, 274.77f, 1f));

    // Bevy's palettes::basic::GRAY.
    private static readonly Color BasicGray = Color.FromSrgb(0.5019608f, 0.5019608f, 0.5019608f);

    // Feathers' accent, made lighter as Bevy's Oklch color is, its lightness raised.
    private static Color Accent(float lighter = 0f) => Color.From(ColorSpace.Oklch, new Vec4(MathF.Min(0.542f + lighter, 1f), 0.1594f, 255.4f, 1f));

    /// <summary>
    /// The basic example theme, Bevy's <c>basic_example_theme</c>, white radio buttons and black
    /// fields with the text in <paramref name="textColor"/>, set as Bevy's examples set it.
    /// </summary>
    public static void BasicExampleTheme(Color textColor) => Feathers.SetTheme(
        // Radio Button tokens
        (FeathersToken.RadioText, textColor),
        (FeathersToken.RadioMark, Color.Black),
        (FeathersToken.RadioMarkHover, Color.Black),
        (FeathersToken.RadioMarkPressed, Color.Black),
        (FeathersToken.RadioBg, Color.White),
        (FeathersToken.RadioBgHover, BasicGray),
        (FeathersToken.RadioBgPressed, BasicGray),
        (FeathersToken.RadioBgChecked, Color.White),
        (FeathersToken.RadioBgCheckedHover, Color.Black),
        (FeathersToken.RadioBgCheckedPressed, Color.Black),
        (FeathersToken.RadioBorder, Color.Black),
        (FeathersToken.RadioBorderHover, BasicGray),
        (FeathersToken.RadioBorderPressed, Color.Black),
        (FeathersToken.RadioBorderChecked, Color.Black),
        (FeathersToken.RadioBorderCheckedHover, Color.Black),
        (FeathersToken.RadioBorderCheckedPressed, Color.Black),

        // Number Input tokens
        (FeathersToken.TextInputText, textColor),
        (FeathersToken.TextInputBg, Color.Black),
        (FeathersToken.TextInputLabelBg, Color.Black),
        (FeathersToken.TextInputCursor, Accent(0.2f)),
        (FeathersToken.TextInputSelection, Accent()),
        (FeathersToken.TextInputSelectionUnfocused, Transparent),

        // Checkbox tokens
        (FeathersToken.CheckboxText, textColor),
        (FeathersToken.CheckboxBg, Color.Black),
        (FeathersToken.CheckboxMark, Color.White),
        (FeathersToken.CheckboxBorder, Color.Black),
        (FeathersToken.CheckboxBorderHover, Gray2),
        (FeathersToken.CheckboxBorderCheckedHover, Gray0),
        (FeathersToken.CheckboxBorderPressed, Gray1),
        (FeathersToken.CheckboxBorderCheckedPressed, Gray1),
        (FeathersToken.CheckboxBorderChecked, Color.Black),
        (FeathersToken.CheckboxBgHover, Gray0),
        (FeathersToken.CheckboxBgChecked, Color.Black),
        (FeathersToken.CheckboxBgCheckedHover, Color.Black),
        (FeathersToken.CheckboxBgCheckedPressed, Gray1),
        (FeathersToken.CheckboxBgPressed, Color.Black),

        // Feathers Button / Select
        (FeathersToken.ButtonBg, Transparent),
        (FeathersToken.ButtonBgHover, Transparent),
        (FeathersToken.ButtonBgPressed, Transparent),
        (FeathersToken.MenuBg, Gray0),
        (FeathersToken.MenuBorder, Gray0),
        (FeathersToken.MenuitemBgHover, Transparent),
        (FeathersToken.MenuitemBgPressed, Transparent),
        (FeathersToken.MenuitemBgFocused, Transparent),
        (FeathersToken.ListrowBg, Transparent),
        (FeathersToken.ListrowBgHover, PaletteBlack),
        (FeathersToken.ListrowBgSelected, PaletteBlack),
        (FeathersToken.ButtonText, textColor),
        (FeathersToken.ListrowText, textColor),

        // Slider tokens. Bevy's map sets them once among the number input's as well, white and
        // black, and these, set after, take their place.
        (FeathersToken.SliderBg, PaletteBlack),
        (FeathersToken.SliderBgHover, Gray0),
        (FeathersToken.SliderBgPressed, Gray1),
        (FeathersToken.SliderBar, Accent()),
        (FeathersToken.SliderBarHover, Accent(0.05f)),
        (FeathersToken.SliderBarPressed, Accent(0.1f)),
        (FeathersToken.SliderText, textColor),

        // Main text color
        (FeathersToken.TextMain, textColor));
}

/// <summary>
/// Bevy's <c>helpers/number_input_f32.rs</c>, a labeled Feathers number input holding a
/// <see cref="float"/>.
/// </summary>
internal static class NumberInputs
{
    /// <summary>
    /// A row of a label and a number input holding a <see cref="float"/>, Bevy's
    /// <c>number_input_f32</c>, which typing cannot take past <paramref name="min"/> or
    /// <paramref name="max"/>.
    /// </summary>
    /// <remarks>
    /// The input reports the value asked for as a <c>ValueChange</c> of a <see cref="float"/>, and
    /// an example with more than one tells them apart by <paramref name="numberInputIdentifier"/>, a
    /// component put on the input, as Bevy's examples query the marker they pass. The input's
    /// settings are written before its value, since it writes its text as its value changes.
    /// </remarks>
    /// <returns>The row, whose last child is the input.</returns>
    public static Entity NumberInputF32<T>(EcsWorld ecs, Entity parent, string name, T? numberInputIdentifier, float value, int precision, float min, float max)
        where T : unmanaged
    {
        var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, ColumnGap = Length.Px(5f) });
        ecs.SetParent(row, parent);

        var title = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Width = Length.Px(150f) });
        ecs.SetParent(title, row);
        Feathers.Label(title, name);

        var input = Feathers.NumberInput(row);
        if (numberInputIdentifier is { } identifier) ecs.Add(input, identifier);
        ecs.Insert<NumberInputPrecisionRef>(input).Value = precision;
        Feathers.SetNumberLimit(input, min, max, hard: true);
        Feathers.SetNumber(input, value);
        var node = ecs.Wrap<NodeRef>(input);
        (node.AlignItems, node.FlexGrow, node.MinWidth) = (NodeRef.AlignItemsVariant.Center, 1f, new Val.Px(50f));
        return row;
    }
}
