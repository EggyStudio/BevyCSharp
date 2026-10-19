using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Feathers, Bevy's styled widgets, spawned one control at a time under a node of the interface.
/// </summary>
/// <remarks>
/// <para>
/// Each control is Bevy's own, its headless widget from <c>bevy_ui_widgets</c> drawn in the colors
/// of Bevy's dark theme, and reports what is done with it as that widget does, so a button is
/// observed for <see cref="Activate"/>, a checkbox, a radio button or a toggle switch for
/// <see cref="ValueChange{T}"/> of a <c>bool</c>, and a slider for one of a <c>float</c>. A widget
/// changes nothing itself until it is told to, by the game from what it reports, or by Bevy's own
/// observer through <see cref="Ui.SelfUpdate"/>. Its state is Bevy's reflected components, such as
/// <c>CheckedRef</c>, <c>SliderValueRef</c> and <c>InteractionDisabledRef</c>.
/// </para>
/// <para>
/// The app asks for Feathers with <see cref="Config.Feathers"/>, and each call here throws without
/// it. A control is whole once Bevy has applied its scene, which happens as it is spawned, so what
/// the game adds under it afterwards comes after its own children. Bevy writes Feathers for tools
/// and editors rather than a game's own interface, which it draws with the plain widgets.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var row = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center });
/// Feathers.ThemeColor(row, FeathersToken.WindowBg);
/// var plus = Feathers.Button(row, "+1");
/// ctx.Ecs.Observe&lt;Activate&gt;(plus, _ => count++);
/// </code>
/// </example>
public static unsafe class Feathers
{
    private const int ButtonKind = 0, ToolButtonKind = 1, CheckboxKind = 2, RadioKind = 3, ToggleSwitchKind = 4;
    private const int SliderKind = 5, CaptionKind = 6, LabelKind = 7, LabelDimKind = 8, LabelSmallKind = 9;
    private const int NumberInputKind = 10, VirtualKeyboardKind = 11;

    // Each keyboard's keys as it was given them, by the keyboard, since Bevy reports a key by where
    // it stands and the game reads it as the text it gave.
    private static readonly Dictionary<Entity, string[][]> KeyTables = [];

    /// <summary>A button with its caption, Bevy's <c>FeathersButton</c>.</summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="caption">Its text, or empty for a button the game puts its own children in.</param>
    /// <param name="variant">How it looks.</param>
    /// <param name="corners">Which of its corners are rounded, for buttons that join into a group.</param>
    /// <exception cref="BevyNativeException">
    /// The app did not ask for <see cref="Config.Feathers"/>, the parent is gone, or this build has no
    /// renderer.
    /// </exception>
    public static Entity Button(Entity parent, string caption, ButtonVariant variant = ButtonVariant.Normal, RoundedCorners corners = RoundedCorners.All) =>
        Spawn(ButtonKind, parent, caption, new NativeFeathersProps { Variant = (int)variant, Corners = (int)corners });

    /// <summary>
    /// A square button for an icon or a symbol, Bevy's <c>FeathersToolButton</c>.
    /// </summary>
    /// <inheritdoc cref="Button" path="/param"/>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity ToolButton(Entity parent, string caption, ButtonVariant variant = ButtonVariant.Normal, RoundedCorners corners = RoundedCorners.All) =>
        Spawn(ToolButtonKind, parent, caption, new NativeFeathersProps { Variant = (int)variant, Corners = (int)corners });

    /// <summary>A checkbox with its caption beside it, Bevy's <c>FeathersCheckbox</c>.</summary>
    /// <remarks>
    /// Checked where it carries Bevy's <c>Checked</c>, which a click asks for through
    /// <see cref="ValueChange{T}"/> of a <c>bool</c> and <see cref="Ui.SelfUpdate"/> gives.
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="caption">Its text.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Checkbox(Entity parent, string caption) => Spawn(CheckboxKind, parent, caption, default);

    /// <summary>
    /// A radio button with its caption beside it, Bevy's <c>FeathersRadio</c>, one of those under a
    /// node carrying Bevy's <c>RadioGroup</c>.
    /// </summary>
    /// <remarks>
    /// The group reports the button clicked through <see cref="ValueChange{T}"/> of an
    /// <see cref="Entity"/>, and <see cref="Ui.SelfUpdate"/> on the group moves <c>Checked</c> to it.
    /// </remarks>
    /// <inheritdoc cref="Checkbox" path="/param"/>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Radio(Entity parent, string caption) => Spawn(RadioKind, parent, caption, default);

    /// <summary>A toggle switch, Bevy's <c>FeathersToggleSwitch</c>, a checkbox drawn as a switch.</summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity ToggleSwitch(Entity parent) => Spawn(ToggleSwitchKind, parent, "", default);

    /// <summary>
    /// A slider showing its value, Bevy's <c>FeathersSlider</c>, starting at its least value.
    /// </summary>
    /// <remarks>
    /// Its value is Bevy's <c>SliderValue</c>, its steps <c>SliderStep</c> and the digits it shows
    /// <c>SliderPrecision</c>, each written through its reflected wrapper. A drag reports the value
    /// asked for through <see cref="ValueChange{T}"/> of a <c>float</c>.
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="min">Its least value.</param>
    /// <param name="max">Its greatest value.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Slider(Entity parent, float min = 0f, float max = 1f) =>
        Spawn(SliderKind, parent, "", new NativeFeathersProps { Min = min, Max = max });

    /// <summary>
    /// Text in the theme's font and color, as a control's caption is, Bevy's <c>caption</c>.
    /// </summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="text">What it says, which <see cref="Ui.SetText"/> changes.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Caption(Entity parent, string text) => Spawn(CaptionKind, parent, text, default);

    /// <summary>A label beside a control, Bevy's <c>label</c>.</summary>
    /// <inheritdoc cref="Caption" path="/param"/>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Label(Entity parent, string text) => Spawn(LabelKind, parent, text, default);

    /// <summary>A label in the theme's dimmer text, Bevy's <c>label_dim</c>.</summary>
    /// <inheritdoc cref="Caption" path="/param"/>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity LabelDim(Entity parent, string text) => Spawn(LabelDimKind, parent, text, default);

    /// <summary>A smaller label, Bevy's <c>label_small</c>.</summary>
    /// <inheritdoc cref="Caption" path="/param"/>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity LabelSmall(Entity parent, string text) => Spawn(LabelSmallKind, parent, text, default);

    /// <summary>
    /// A number input, Bevy's <c>FeathersNumberInput</c>, a field the value is typed into or dragged
    /// across, with a letter at its left and a strip in a theme color along its edge where given.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Its value is written with <see cref="SetNumber(Entity, float)"/> and its kin, whose type is
    /// the one it holds and reports a change in, a <see cref="ValueChange{T}"/> of a <c>float</c>,
    /// a <c>double</c>, an <c>int</c> or a <c>long</c>, which it asks for as it is typed into or
    /// dragged, leaving the value to whoever observes it, as Bevy's example writes it back.
    /// </para>
    /// <para>
    /// <see cref="SetNumberLimit(Entity, float, float, bool)"/> bounds it,
    /// <see cref="SetNumberUnits"/> shows it in units, and Bevy's reflected
    /// <c>NumberInputPrecisionRef</c>, <c>NumberInputStepRef</c> and <c>NumberInputWrapRef</c> set
    /// the digits it shows, how far a step goes and whether it wraps past its hard limit.
    /// </para>
    /// <para>
    /// It writes its text as its value changes, so its settings are written before its value, or
    /// its text shows the value without its units or its limit's bar until the value next changes.
    /// </para>
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="letter">The letter at its left, as an axis's, or null for none.</param>
    /// <param name="sigil">
    /// The token the strip along its left edge is colored by, as <see cref="FeathersToken.TextInputXAxis"/>,
    /// or null for Bevy's own.
    /// </param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity NumberInput(Entity parent, string? letter = null, string? sigil = null) =>
        Spawn(NumberInputKind, parent, letter ?? "", default, sigil);

    /// <summary>
    /// A virtual keyboard, Bevy's <c>VirtualKeyboard</c>, a button a key in rows, which reports the
    /// key pressed as <see cref="VirtualKeyPressed"/> on the keyboard.
    /// </summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="keys">Its keys by row, each the text its button shows.</param>
    /// <exception cref="ArgumentException">A key is empty or holds a tab or a line break.</exception>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity VirtualKeyboard(Entity parent, string[][] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.SelectMany(row => row).Any(key => string.IsNullOrEmpty(key) || key.AsSpan().IndexOfAny('\t', '\n', '\r') >= 0))
        {
            throw new ArgumentException("a virtual key is empty or holds a tab or a line break", nameof(keys));
        }

        var copy = keys.Select(row => row.ToArray()).ToArray();
        var keyboard = Spawn(VirtualKeyboardKind, parent, string.Join('\n', copy.Select(row => string.Join('\t', row))), default);
        KeyTables[keyboard] = copy;
        return keyboard;
    }

    /// <summary>The text of a keyboard's key by where it stands, or empty for a key it was not given.</summary>
    internal static string KeyAt(Entity keyboard, int row, int column) =>
        KeyTables.TryGetValue(keyboard, out var keys) && row < keys.Length && column < keys[row].Length ? keys[row][column] : "";

    /// <summary>Writes a number input's value as a <see cref="float"/>, the type it then reports.</summary>
    /// <param name="input">A number input from <see cref="NumberInput"/>.</param>
    /// <param name="value">Its value.</param>
    /// <exception cref="BevyNativeException">The input is gone, or this build has no renderer.</exception>
    public static void SetNumber(Entity input, float value) => SetNumber(input, NumberKind.Float, value, 0);

    /// <summary>Writes a number input's value as a <see cref="double"/>, the type it then reports.</summary>
    /// <inheritdoc cref="SetNumber(Entity, float)" path="/param"/>
    /// <inheritdoc cref="SetNumber(Entity, float)" path="/exception"/>
    public static void SetNumber(Entity input, double value) => SetNumber(input, NumberKind.Double, value, 0);

    /// <summary>Writes a number input's value as an <see cref="int"/>, the type it then reports.</summary>
    /// <inheritdoc cref="SetNumber(Entity, float)" path="/param"/>
    /// <inheritdoc cref="SetNumber(Entity, float)" path="/exception"/>
    public static void SetNumber(Entity input, int value) => SetNumber(input, NumberKind.Int, value, value);

    /// <summary>Writes a number input's value as a <see cref="long"/>, the type it then reports.</summary>
    /// <inheritdoc cref="SetNumber(Entity, float)" path="/param"/>
    /// <inheritdoc cref="SetNumber(Entity, float)" path="/exception"/>
    public static void SetNumber(Entity input, long value) => SetNumber(input, NumberKind.Long, value, value);

    /// <summary>What a number input holds, or null where nothing has been written to it.</summary>
    /// <exception cref="BevyNativeException">The input is gone, or this build has no renderer.</exception>
    public static NumberInputValue? NumberOf(Entity input)
    {
        int kind;
        double value;
        long whole;
        var answer = Native.bcs_feathers_number_get(input.Bits, &kind, &value, &whole);
        if (answer == NativeStatus.NotPresent) return null;

        Native.Check(answer, $"reading the number {input} holds");
        return new NumberInputValue((NumberKind)kind, value, whole);
    }

    /// <summary>
    /// Bounds a number input's <see cref="float"/> value, hard, which neither typing nor dragging
    /// passes, or soft, which a drag stays within and typing can pass.
    /// </summary>
    /// <param name="input">A number input from <see cref="NumberInput"/>.</param>
    /// <param name="min">The least value.</param>
    /// <param name="max">The greatest value.</param>
    /// <param name="hard">Whether it is Bevy's <c>HardLimit</c> rather than its <c>SoftLimit</c>.</param>
    /// <exception cref="BevyNativeException">The input is gone, or this build has no renderer.</exception>
    public static void SetNumberLimit(Entity input, float min, float max, bool hard = false) => Limit(input, hard, NumberKind.Float, min, max, 0, 0);

    /// <summary>Bounds a number input's <see cref="double"/> value, hard or soft.</summary>
    /// <inheritdoc cref="SetNumberLimit(Entity, float, float, bool)" path="/param"/>
    /// <inheritdoc cref="SetNumberLimit(Entity, float, float, bool)" path="/exception"/>
    public static void SetNumberLimit(Entity input, double min, double max, bool hard = false) => Limit(input, hard, NumberKind.Double, min, max, 0, 0);

    /// <summary>Bounds a number input's <see cref="int"/> value, hard or soft.</summary>
    /// <inheritdoc cref="SetNumberLimit(Entity, float, float, bool)" path="/param"/>
    /// <inheritdoc cref="SetNumberLimit(Entity, float, float, bool)" path="/exception"/>
    public static void SetNumberLimit(Entity input, int min, int max, bool hard = false) => Limit(input, hard, NumberKind.Int, 0, 0, min, max);

    /// <summary>Bounds a number input's <see cref="long"/> value, hard or soft.</summary>
    /// <inheritdoc cref="SetNumberLimit(Entity, float, float, bool)" path="/param"/>
    /// <inheritdoc cref="SetNumberLimit(Entity, float, float, bool)" path="/exception"/>
    public static void SetNumberLimit(Entity input, long min, long max, bool hard = false) => Limit(input, hard, NumberKind.Long, 0, 0, min, max);

    /// <summary>Has a number input show its value in units, typed in any of their dimension.</summary>
    /// <param name="input">A number input from <see cref="NumberInput"/>.</param>
    /// <param name="units">The units.</param>
    /// <exception cref="BevyNativeException">The input is gone, or this build has no renderer.</exception>
    public static void SetNumberUnits(Entity input, NumberUnits units)
    {
        var id = units switch
        {
            NumberUnits.LengthMeters => "length_meters",
            NumberUnits.TimeSeconds => "time_seconds",
            NumberUnits.AngleDegrees => "angle_degrees",
            _ => "none",
        };
        var bytes = Encoding.UTF8.GetBytes(id);
        fixed (byte* text = bytes)
        {
            Native.Check(Native.bcs_feathers_number_units(input.Bits, text, bytes.Length), $"showing {input} in {units}");
        }
    }

    /// <summary>
    /// Colors a node of the game's own from the theme, by one of <see cref="FeathersToken"/>'s
    /// tokens, so it follows the theme as the controls do.
    /// </summary>
    /// <remarks>
    /// Bevy's <c>ThemeBackgroundColor</c>, <c>ThemeBorderColor</c> and, for its text,
    /// <c>InheritableThemeTextColor</c>, which its children's text takes, or <c>ThemeTextColor</c>
    /// for a span of text of its own.
    /// </remarks>
    /// <param name="node">The node to color.</param>
    /// <param name="token">The token, such as <see cref="FeathersToken.WindowBg"/>.</param>
    /// <param name="part">Which of its colors.</param>
    /// <exception cref="BevyNativeException">The node is gone, or this build has no renderer.</exception>
    public static void ThemeColor(Entity node, string token, ThemedPart part = ThemedPart.Background)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        var bytes = Encoding.UTF8.GetBytes(token);
        fixed (byte* text = bytes)
        {
            Native.Check(Native.bcs_feathers_theme_color(node.Bits, (int)part, text, bytes.Length), $"coloring {node} by {token}");
        }
    }

    private static void SetNumber(Entity input, NumberKind kind, double value, long whole) =>
        Native.Check(Native.bcs_feathers_number_set(input.Bits, (int)kind, value, whole), $"writing the number {input} holds");

    private static void Limit(Entity input, bool hard, NumberKind kind, double min, double max, long minWhole, long maxWhole) =>
        Native.Check(
            Native.bcs_feathers_number_limit(input.Bits, hard ? 1 : 0, (int)kind, min, max, minWhole, maxWhole),
            $"bounding the number {input} holds");

    private static Entity Spawn(int kind, Entity parent, string text, NativeFeathersProps props, string? token = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        var bytes = Encoding.UTF8.GetBytes(text);
        var tokenBytes = token is null ? [] : Encoding.UTF8.GetBytes(token);
        ulong spawned;
        fixed (byte* caption = bytes)
        fixed (byte* named = tokenBytes)
        {
            props.Token = token is null ? null : named;
            props.TokenLength = token is null ? 0 : tokenBytes.Length;
            Native.Check(
                Native.bcs_feathers_spawn(kind, parent.Bits, &props, caption, bytes.Length, &spawned),
                $"spawning a Feathers control under {parent}");
        }

        return new Entity(spawned);
    }
}
