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

    private static Entity Spawn(int kind, Entity parent, string text, NativeFeathersProps props)
    {
        ArgumentNullException.ThrowIfNull(text);

        var bytes = Encoding.UTF8.GetBytes(text);
        ulong spawned;
        fixed (byte* caption = bytes)
        {
            Native.Check(
                Native.bcs_feathers_spawn(kind, parent.Bits, &props, caption, bytes.Length, &spawned),
                $"spawning a Feathers control under {parent}");
        }

        return new Entity(spawned);
    }
}
