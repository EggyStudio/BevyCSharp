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
    private const int NumberInputKind = 10, VirtualKeyboardKind = 11, MenuKind = 12, MenuButtonKind = 13, MenuToolButtonKind = 14;
    private const int MenuPopupKind = 15, MenuItemKind = 16, MenuDividerKind = 17, SelectKind = 18, ListViewKind = 19;
    private const int ListRowKind = 20, DisclosureToggleKind = 21, TextInputContainerKind = 22, TextInputKind = 23;
    private const int ColorSwatchKind = 24, ColorPlaneKind = 25, ColorSliderKind = 26, ColorInputKind = 27, PaneKind = 28;
    private const int PaneHeaderKind = 29, PaneHeaderDividerKind = 30, PaneBodyKind = 31, SubpaneKind = 32, SubpaneHeaderKind = 33;
    private const int SubpaneBodyKind = 34, GroupKind = 35, GroupHeaderKind = 36, GroupBodyKind = 37, FlexSpacerKind = 38;
    private const int IconKind = 39, DialogKind = 40, FloatingDialogKind = 41, DialogHeaderKind = 42, DialogBodyKind = 43;
    private const int DialogFooterKind = 44, DialogCloseKind = 45;

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

    /// <summary>
    /// A menu, Bevy's <c>FeathersMenu</c>, which holds its button and its popup, spawned under it
    /// with <see cref="MenuButton"/> and <see cref="MenuPopup"/>.
    /// </summary>
    /// <remarks>
    /// The popup is spawned with the menu and shown as the button opens it, where Bevy's lazy menu
    /// builds its popup as it opens, which this does not offer. An item reports
    /// <see cref="Activate"/>.
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Menu(Entity parent) => Spawn(MenuKind, parent, "", default);

    /// <summary>A menu's button, Bevy's <c>FeathersMenuButton</c>, which opens its popup.</summary>
    /// <param name="menu">The menu it opens, from <see cref="Menu"/>.</param>
    /// <param name="caption">Its text.</param>
    /// <param name="arrow">Whether a chevron follows the text.</param>
    /// <param name="corners">Which of its corners are rounded.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity MenuButton(Entity menu, string caption, bool arrow = true, RoundedCorners corners = RoundedCorners.All) =>
        Spawn(MenuButtonKind, menu, caption, new NativeFeathersProps { Corners = (int)corners, Flag = arrow ? 1u : 0u });

    /// <summary>A menu's square button for a symbol, Bevy's <c>FeathersMenuToolButton</c>.</summary>
    /// <inheritdoc cref="MenuButton" path="/param"/>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity MenuToolButton(Entity menu, string caption, bool arrow = true, RoundedCorners corners = RoundedCorners.All) =>
        Spawn(MenuToolButtonKind, menu, caption, new NativeFeathersProps { Corners = (int)corners, Flag = arrow ? 1u : 0u });

    /// <summary>A menu's popup, Bevy's <c>FeathersMenuPopup</c>, its items spawned under it.</summary>
    /// <remarks>
    /// Placed below its button by Bevy's reflected <c>PopoverRef</c>, whose places the game can set
    /// otherwise, as above it or aligned to its button's right edge.
    /// </remarks>
    /// <param name="menu">The menu it belongs to, from <see cref="Menu"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity MenuPopup(Entity menu) => Spawn(MenuPopupKind, menu, "", default);

    /// <summary>A menu's item, Bevy's <c>FeathersMenuItem</c>, which reports <see cref="Activate"/>.</summary>
    /// <param name="popup">The popup it goes in, from <see cref="MenuPopup"/>.</param>
    /// <param name="caption">Its text.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity MenuItem(Entity popup, string caption) => Spawn(MenuItemKind, popup, caption, default);

    /// <summary>A line between a menu's items, Bevy's <c>FeathersMenuDivider</c>.</summary>
    /// <param name="popup">The popup it goes in, from <see cref="MenuPopup"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity MenuDivider(Entity popup) => Spawn(MenuDividerKind, popup, "", default);

    /// <summary>
    /// A select, Bevy's <c>FeathersSelect</c>, a button showing the option chosen that opens a list
    /// of its options.
    /// </summary>
    /// <remarks>
    /// Reports the option chosen as a <see cref="ValueChange{T}"/> of the row's
    /// <see cref="Entity"/>, which carries Bevy's reflected <c>OptionIndexRef</c>, the option's place
    /// in <paramref name="options"/>. It moves its choice itself.
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="options">Its options' text.</param>
    /// <param name="selected">The place of the option chosen, or -1 for none.</param>
    /// <param name="maxVisible">How many options the list shows before it scrolls.</param>
    /// <param name="corners">Which of its button's corners are rounded.</param>
    /// <exception cref="ArgumentException">An option holds a line break.</exception>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Select(Entity parent, string[] options, int selected = -1, int maxVisible = 8, RoundedCorners corners = RoundedCorners.All) =>
        Spawn(SelectKind, parent, Lines(options, nameof(options)), new NativeFeathersProps { Selected = selected, Count = maxVisible, Corners = (int)corners });

    /// <summary>
    /// A list view, Bevy's <c>FeathersListView</c>, rows in a column that scrolls past its height,
    /// which reports the row clicked as a <see cref="ValueChange{T}"/> of its <see cref="Entity"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Ui.SelfUpdate"/> with <see cref="UiWidgetKind.ListBox"/> moves Bevy's
    /// <c>Selected</c> to the row clicked. More rows are spawned with <see cref="ListRow"/> under
    /// <see cref="ContentsOf"/> the list.
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="rows">Its rows' text.</param>
    /// <param name="selected">The place of the row chosen, or -1 for none.</param>
    /// <exception cref="ArgumentException">A row holds a line break.</exception>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity ListView(Entity parent, string[] rows, int selected = -1) =>
        Spawn(ListViewKind, parent, Lines(rows, nameof(rows)), new NativeFeathersProps { Selected = selected });

    /// <summary>A row of a list view, Bevy's <c>FeathersListRow</c>, with its caption.</summary>
    /// <param name="list">The node its rows go under, <see cref="ContentsOf"/> the list.</param>
    /// <param name="caption">Its text.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity ListRow(Entity list, string caption) => Spawn(ListRowKind, list, caption, default);

    /// <summary>
    /// A toggle drawn as a chevron pointing right or down, Bevy's <c>FeathersDisclosureToggle</c>,
    /// a checkbox for showing what is folded under a heading.
    /// </summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity DisclosureToggle(Entity parent) => Spawn(DisclosureToggleKind, parent, "", default);

    /// <summary>
    /// The frame a text input is drawn in, Bevy's <c>FeathersTextInputContainer</c>, its input spawned
    /// under it with <see cref="TextInput"/>.
    /// </summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity TextInputContainer(Entity parent) => Spawn(TextInputContainerKind, parent, "", default);

    /// <summary>
    /// A text input, Bevy's <c>FeathersTextInput</c>, a field as <see cref="Ui.SetEditableText"/>
    /// makes one, in the theme's font and colors.
    /// </summary>
    /// <param name="container">Its frame, from <see cref="TextInputContainer"/>.</param>
    /// <param name="visibleWidth">How many characters wide it is, or zero for the room it is given.</param>
    /// <param name="maxCharacters">The most characters it takes, or zero for no bound.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity TextInput(Entity container, float visibleWidth = 0f, int maxCharacters = 0) =>
        Spawn(TextInputKind, container, "", new NativeFeathersProps { Extent = visibleWidth, Count = maxCharacters });

    /// <summary>
    /// A swatch showing a color, Bevy's <c>FeathersColorSwatch</c>, the color its reflected
    /// <c>ColorSwatchValueRef</c> holds.
    /// </summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="opaquePercentage">
    /// How much of it, from the left, shows the color without its alpha, the rest drawn over a
    /// checkered pattern.
    /// </param>
    /// <param name="corners">Which of its corners are rounded.</param>
    /// <param name="cornerRadius">How round they are, in pixels.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity ColorSwatch(Entity parent, float opaquePercentage = 0f, RoundedCorners corners = RoundedCorners.All, float cornerRadius = 5f) =>
        Spawn(ColorSwatchKind, parent, "", new NativeFeathersProps { Value = opaquePercentage, Corners = (int)corners, Extent = cornerRadius });

    /// <summary>
    /// A plane two parts of a color are picked on, Bevy's <c>FeathersColorPlane</c>, which reports
    /// the point picked as a <see cref="ValueChange{T}"/> of a <see cref="Vec2"/>.
    /// </summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="axes">The parts it sets, across and down.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity ColorPlane(Entity parent, ColorPlaneAxes axes) =>
        Spawn(ColorPlaneKind, parent, "", new NativeFeathersProps { Count = (int)axes });

    /// <summary>
    /// A slider of one part of a color, Bevy's <c>FeathersColorSlider</c>, which reports the value
    /// asked for as a <see cref="ValueChange{T}"/> of a <c>float</c>.
    /// </summary>
    /// <remarks>
    /// Its value is Bevy's reflected <c>SliderValueRef</c> and the color its track shows
    /// <c>SliderBaseColorRef</c>, which the game writes as the color changes.
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="channel">The part it sets.</param>
    /// <param name="value">Its value.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity ColorSlider(Entity parent, ColorChannel channel, float value = 0f) =>
        Spawn(ColorSliderKind, parent, "", new NativeFeathersProps { Count = (int)channel, Value = value });

    /// <summary>
    /// A color input, Bevy's <c>FeathersColorInput</c>, a swatch that opens a picker of a wheel,
    /// planes and sliders, and reports the color picked as a <see cref="ValueChange{T}"/> of a
    /// <see cref="Color"/>.
    /// </summary>
    /// <remarks>
    /// Its color is Bevy's reflected <c>ColorInputValueRef</c>, which <see cref="Ui.SelfUpdate"/>
    /// with <see cref="UiWidgetKind.ColorInput"/> keeps as the color picked.
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="opaquePercentage">How much of its swatch shows the color without its alpha.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity ColorInput(Entity parent, float opaquePercentage = 0f) =>
        Spawn(ColorInputKind, parent, "", new NativeFeathersProps { Value = opaquePercentage });

    /// <summary>An editor's pane, Bevy's <c>pane</c>, holding its header and its body.</summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Pane(Entity parent) => Spawn(PaneKind, parent, "", default);

    /// <summary>A pane's header, Bevy's <c>pane_header</c>, a row of its tool buttons.</summary>
    /// <param name="pane">The pane, from <see cref="Pane"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity PaneHeader(Entity pane) => Spawn(PaneHeaderKind, pane, "", default);

    /// <summary>A line between a pane header's buttons, Bevy's <c>pane_header_divider</c>.</summary>
    /// <param name="header">The header, from <see cref="PaneHeader"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity PaneHeaderDivider(Entity header) => Spawn(PaneHeaderDividerKind, header, "", default);

    /// <summary>A pane's body, Bevy's <c>pane_body</c>.</summary>
    /// <param name="pane">The pane, from <see cref="Pane"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity PaneBody(Entity pane) => Spawn(PaneBodyKind, pane, "", default);

    /// <summary>A pane within a pane, Bevy's <c>subpane</c>, holding its header and its body.</summary>
    /// <param name="parent">The node it goes under, usually a pane's body.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Subpane(Entity parent) => Spawn(SubpaneKind, parent, "", default);

    /// <summary>A subpane's header, Bevy's <c>subpane_header</c>.</summary>
    /// <param name="subpane">The subpane, from <see cref="Subpane"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity SubpaneHeader(Entity subpane) => Spawn(SubpaneHeaderKind, subpane, "", default);

    /// <summary>A subpane's body, Bevy's <c>subpane_body</c>.</summary>
    /// <param name="subpane">The subpane, from <see cref="Subpane"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity SubpaneBody(Entity subpane) => Spawn(SubpaneBodyKind, subpane, "", default);

    /// <summary>A group of properties, Bevy's <c>group</c>, holding its header and its body.</summary>
    /// <param name="parent">The node it goes under, usually a pane's or a subpane's body.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Group(Entity parent) => Spawn(GroupKind, parent, "", default);

    /// <summary>A group's header, Bevy's <c>group_header</c>.</summary>
    /// <param name="group">The group, from <see cref="Group"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity GroupHeader(Entity group) => Spawn(GroupHeaderKind, group, "", default);

    /// <summary>A group's body, Bevy's <c>group_body</c>.</summary>
    /// <param name="group">The group, from <see cref="Group"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity GroupBody(Entity group) => Spawn(GroupBodyKind, group, "", default);

    /// <summary>A node that takes the room left in its row, Bevy's <c>flex_spacer</c>.</summary>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity FlexSpacer(Entity parent) => Spawn(FlexSpacerKind, parent, "", default);

    /// <summary>
    /// An icon fourteen pixels tall, Bevy's <c>icon</c>, tinted as the text around it is, or Bevy's
    /// <c>icon_untinted</c>.
    /// </summary>
    /// <param name="parent">The node it goes under, as a tool button.</param>
    /// <param name="image">The image's path, one of <see cref="FeathersIcon"/> or the game's own.</param>
    /// <param name="tinted">Whether it takes the color of the text around it.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Icon(Entity parent, string image, bool tinted = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(image);
        return Spawn(IconKind, parent, image, new NativeFeathersProps { Flag = tinted ? 0u : 1u });
    }

    /// <summary>
    /// A modal dialog over the window, Bevy's <c>FeathersDialog</c>, its contents spawned under
    /// <see cref="ContentsOf"/> it, as <see cref="DialogHeader"/>, <see cref="DialogBody"/> and
    /// <see cref="DialogFooter"/>.
    /// </summary>
    /// <remarks>
    /// It covers the window and takes the pointer from what is under it until it is despawned, which
    /// the game does as it is asked to close, observing <see cref="RequestClose"/> on it.
    /// </remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="width">Its width in pixels, or zero for its contents' width.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity Dialog(Entity parent, float width = 0f) =>
        Spawn(DialogKind, parent, "", new NativeFeathersProps { Extent = width });

    /// <summary>
    /// A dialog floating over the window, dragged by its title, Bevy's
    /// <c>FeathersFloatingDialog</c>, its contents spawned under <see cref="ContentsOf"/> it.
    /// </summary>
    /// <remarks>Its close button despawns it, reporting <see cref="RequestClose"/> as it does.</remarks>
    /// <param name="parent">The node it goes under, or <see cref="Entity.None"/> for the root.</param>
    /// <param name="title">Its title.</param>
    /// <param name="width">Its width in pixels, or zero for its contents' width.</param>
    /// <param name="left">Its left edge, in pixels.</param>
    /// <param name="top">Its top edge, in pixels.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity FloatingDialog(Entity parent, string title, float width = 0f, float left = 120f, float top = 120f) =>
        Spawn(FloatingDialogKind, parent, title, new NativeFeathersProps { Extent = width, Left = left, Top = top });

    /// <summary>A dialog's header, Bevy's <c>FeathersDialogHeader</c>, its title and close button in a row.</summary>
    /// <param name="dialog">The node its contents go under, <see cref="ContentsOf"/> the dialog.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity DialogHeader(Entity dialog) => Spawn(DialogHeaderKind, dialog, "", default);

    /// <summary>A dialog's body, Bevy's <c>FeathersDialogBody</c>.</summary>
    /// <inheritdoc cref="DialogHeader" path="/param"/>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity DialogBody(Entity dialog) => Spawn(DialogBodyKind, dialog, "", default);

    /// <summary>A dialog's footer, Bevy's <c>FeathersDialogFooter</c>, its buttons in a row.</summary>
    /// <inheritdoc cref="DialogHeader" path="/param"/>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity DialogFooter(Entity dialog) => Spawn(DialogFooterKind, dialog, "", default);

    /// <summary>A dialog's close button, Bevy's <c>FeathersDialogClose</c>, which reports <see cref="RequestClose"/>.</summary>
    /// <param name="header">The header it goes in, from <see cref="DialogHeader"/>.</param>
    /// <inheritdoc cref="Button" path="/exception"/>
    public static Entity DialogClose(Entity header) => Spawn(DialogCloseKind, header, "", default);

    /// <summary>
    /// The node a control's contents go under, where Bevy's own props would hold them, a dialog's
    /// panel, a floating dialog's body, or the part of a list view or a select that scrolls.
    /// </summary>
    /// <exception cref="BevyNativeException">
    /// The control is gone, has no such node, or this build has no renderer.
    /// </exception>
    public static Entity ContentsOf(Entity control)
    {
        ulong contents;
        Native.Check(Native.bcs_feathers_contents(control.Bits, &contents), $"finding where the contents of {control} go");
        return new Entity(contents);
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
    /// Replaces the theme with one that colors each of the tokens given and nothing else, as Bevy's
    /// examples' basic theme does, Bevy's <c>ThemeProps::new_non_contextual</c>.
    /// </summary>
    /// <remarks>
    /// A token the theme is not given draws in no color, so a theme of the game's own names every
    /// part of the controls it shows. The controls take the new colors in the frame after.
    /// <see cref="SetDarkTheme"/> puts Bevy's dark theme back.
    /// </remarks>
    /// <param name="colors">Each token, one of <see cref="FeathersToken"/>'s, with its color.</param>
    /// <exception cref="ArgumentException">A token is empty or holds a line break.</exception>
    /// <exception cref="BevyNativeException">
    /// The app did not ask for <see cref="Config.Feathers"/>, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// Feathers.SetTheme(
    ///     (FeathersToken.TextMain, Color.White),
    ///     (FeathersToken.RadioBg, Color.White),
    ///     (FeathersToken.RadioMark, Color.Black));
    /// </code>
    /// </example>
    public static void SetTheme(params ReadOnlySpan<(string Token, Color Color)> colors)
    {
        if (colors.IsEmpty) throw new ArgumentException("a theme names at least one token", nameof(colors));

        var names = new string[colors.Length];
        var linear = new float[colors.Length * 4];
        for (var i = 0; i < colors.Length; i++)
        {
            names[i] = colors[i].Token;
            (linear[i * 4], linear[i * 4 + 1], linear[i * 4 + 2], linear[i * 4 + 3]) = colors[i].Color;
        }

        var bytes = Encoding.UTF8.GetBytes(Lines(names, nameof(colors)));
        if (names.Any(string.IsNullOrEmpty)) throw new ArgumentException("a token is empty", nameof(colors));
        fixed (byte* text = bytes)
        fixed (float* values = linear)
        {
            Native.Check(Native.bcs_feathers_set_theme(text, bytes.Length, values, names.Length), "setting the theme");
        }
    }

    /// <summary>Puts Bevy's dark theme back, the one the app begins with.</summary>
    /// <exception cref="BevyNativeException">
    /// The app did not ask for <see cref="Config.Feathers"/>, or this build has no renderer.
    /// </exception>
    public static void SetDarkTheme() => Native.Check(Native.bcs_feathers_set_theme(null, 0, null, 0), "setting the dark theme");

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

    private static string Lines(string[] items, string name)
    {
        ArgumentNullException.ThrowIfNull(items, name);
        if (items.Any(item => item is null || item.AsSpan().IndexOfAny('\n', '\r') >= 0))
        {
            throw new ArgumentException("an item is null or holds a line break", name);
        }

        return string.Join('\n', items);
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
