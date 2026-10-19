namespace Bevy;

/// <summary>
/// The tokens Feathers' theme colors a part of a control by, Bevy's <c>bevy_feathers::tokens</c>, for
/// <see cref="Feathers.ThemeColor"/> to color a node of the game's own as the theme colors that part.
/// </summary>
/// <remarks>
/// Each is the name Bevy gives the token, and the theme maps it to a color, so a node colored by
/// one follows the theme as it changes. The names and their notes are Bevy's own.
/// </remarks>
public static class FeathersToken
{
    /// <summary>Window background.</summary>
    public const string WindowBg = "feathers.window.bg";

    /// <summary>Focus ring.</summary>
    public const string FocusRing = "feathers.focus.ring";

    /// <summary>Regular text.</summary>
    public const string TextMain = "feathers.text.main";

    /// <summary>Dim text.</summary>
    public const string TextDim = "feathers.text.dim";

    /// <summary>Regular button background.</summary>
    public const string ButtonBg = "feathers.button.bg";

    /// <summary>Regular button background (hovered).</summary>
    public const string ButtonBgHover = "feathers.button.bg.hover";

    /// <summary>Regular button background (disabled).</summary>
    public const string ButtonBgDisabled = "feathers.button.bg.disabled";

    /// <summary>Regular button background (pressed).</summary>
    public const string ButtonBgPressed = "feathers.button.bg.pressed";

    /// <summary>Regular button text.</summary>
    public const string ButtonText = "feathers.button.txt";

    /// <summary>Regular button text (disabled).</summary>
    public const string ButtonTextDisabled = "feathers.button.txt.disabled";

    /// <summary>Primary button background.</summary>
    public const string ButtonPrimaryBg = "feathers.button.primary.bg";

    /// <summary>Primary button background (hovered).</summary>
    public const string ButtonPrimaryBgHover = "feathers.button.primary.bg.hover";

    /// <summary>Primary button background (disabled).</summary>
    public const string ButtonPrimaryBgDisabled = "feathers.button.primary.bg.disabled";

    /// <summary>Primary button background (pressed).</summary>
    public const string ButtonPrimaryBgPressed = "feathers.button.primary.bg.pressed";

    /// <summary>Primary button text.</summary>
    public const string ButtonPrimaryText = "feathers.button.primary.txt";

    /// <summary>Primary button text (disabled).</summary>
    public const string ButtonPrimaryTextDisabled = "feathers.button.primary.txt.disabled";

    /// <summary>Plain button background.</summary>
    public const string ButtonPlainBg = "feathers.button.plain.bg";

    /// <summary>Plain button background (hovered).</summary>
    public const string ButtonPlainBgHover = "feathers.button.plain.bg.hover";

    /// <summary>Plain button background (disabled).</summary>
    public const string ButtonPlainBgDisabled = "feathers.button.plain.bg.disabled";

    /// <summary>Plain button background (pressed).</summary>
    public const string ButtonPlainBgPressed = "feathers.button.plain.bg.pressed";

    /// <summary>Background for slider.</summary>
    public const string SliderBg = "feathers.slider.bg";

    /// <summary>Background for slider (hovered).</summary>
    public const string SliderBgHover = "feathers.slider.bg.hover";

    /// <summary>Background for slider (pressed).</summary>
    public const string SliderBgPressed = "feathers.slider.bg.pressed";

    /// <summary>Background for slider (disabled).</summary>
    public const string SliderBgDisabled = "feathers.slider.bg.disabled";

    /// <summary>Fill color for slider.</summary>
    public const string SliderBar = "feathers.slider.bar";

    /// <summary>Fill color for slider (hovered).</summary>
    public const string SliderBarHover = "feathers.slider.bar.hover";

    /// <summary>Fill color for slider (pressed).</summary>
    public const string SliderBarPressed = "feathers.slider.bar.pressed";

    /// <summary>Fill color for slider (disabled).</summary>
    public const string SliderBarDisabled = "feathers.slider.bar.disabled";

    /// <summary>Background for slider text.</summary>
    public const string SliderText = "feathers.slider.text";

    /// <summary>Background for slider text (disabled).</summary>
    public const string SliderTextDisabled = "feathers.slider.text.disabled";

    /// <summary>Background for scrollbar.</summary>
    public const string ScrollbarBg = "feathers.scrollbar.bg";

    /// <summary>Background for scrollbar moving bar.</summary>
    public const string ScrollbarThumb = "feathers.scrollbar.thumb";

    /// <summary>Background for scrollbar moving bar (hovered).</summary>
    public const string ScrollbarThumbHover = "feathers.scrollbar.thumb.hover";

    /// <summary>Checkbox background around the checkmark.</summary>
    public const string CheckboxBg = "feathers.checkbox.bg";

    /// <summary>Checkbox background around the checkmark (hovered).</summary>
    public const string CheckboxBgHover = "feathers.checkbox.bg.hover";

    /// <summary>Checkbox background around the checkmark (pressed).</summary>
    public const string CheckboxBgPressed = "feathers.checkbox.bg.pressed";

    /// <summary>Checkbox border around the checkmark (disabled).</summary>
    public const string CheckboxBgDisabled = "feathers.checkbox.bg.disabled";

    /// <summary>Checkbox background around the checkmark (checked).</summary>
    public const string CheckboxBgChecked = "feathers.checkbox.bg.checked";

    /// <summary>Checkbox background around the checkmark (checked+hover).</summary>
    public const string CheckboxBgCheckedHover = "feathers.checkbox.bg.checked.hover";

    /// <summary>Checkbox background around the checkmark (checked+pressed).</summary>
    public const string CheckboxBgCheckedPressed = "feathers.checkbox.bg.checked.pressed";

    /// <summary>Checkbox border around the checkmark (checked+disabled).</summary>
    public const string CheckboxBgCheckedDisabled = "feathers.checkbox.bg.checked.disabled";

    /// <summary>Checkbox border around the checkmark.</summary>
    public const string CheckboxBorder = "feathers.checkbox.border";

    /// <summary>Checkbox border around the checkmark (hovered).</summary>
    public const string CheckboxBorderHover = "feathers.checkbox.border.hover";

    /// <summary>Checkbox border around the checkmark (pressed).</summary>
    public const string CheckboxBorderPressed = "feathers.checkbox.border.pressed";

    /// <summary>Checkbox border around the checkmark (disabled).</summary>
    public const string CheckboxBorderDisabled = "feathers.checkbox.border.disabled";

    /// <summary>Checkbox border around the checkmark (checked).</summary>
    public const string CheckboxBorderChecked = "feathers.checkbox.border.checked";

    /// <summary>Checkbox border around the checkmark (checked+hovered).</summary>
    public const string CheckboxBorderCheckedHover = "feathers.checkbox.border.checked.hover";

    /// <summary>Checkbox border around the checkmark (checked+pressed).</summary>
    public const string CheckboxBorderCheckedPressed = "feathers.checkbox.border.checked.pressed";

    /// <summary>Checkbox border around the checkmark (checked+disabled).</summary>
    public const string CheckboxBorderCheckedDisabled = "feathers.checkbox.border.checked.disabled";

    /// <summary>Checkbox check mark.</summary>
    public const string CheckboxMark = "feathers.checkbox.mark";

    /// <summary>Checkbox check mark (disabled).</summary>
    public const string CheckboxMarkDisabled = "feathers.checkbox.mark.disabled";

    /// <summary>Checkbox label text.</summary>
    public const string CheckboxText = "feathers.checkbox.text";

    /// <summary>Checkbox label text (disabled).</summary>
    public const string CheckboxTextDisabled = "feathers.checkbox.text.disabled";

    /// <summary>Background of the radio button.</summary>
    public const string RadioBg = "feathers.radio.bg";

    /// <summary>Background of the radio button (hovered).</summary>
    public const string RadioBgHover = "feathers.radio.bg.hover";

    /// <summary>Background of the radio button (pressed).</summary>
    public const string RadioBgPressed = "feathers.radio.bg.pressed";

    /// <summary>Background of the radio button (disabled).</summary>
    public const string RadioBgDisabled = "feathers.radio.bg.disabled";

    /// <summary>Background of the radio button (checked).</summary>
    public const string RadioBgChecked = "feathers.radio.bg.checked";

    /// <summary>Background of the radio button (checked+hovered).</summary>
    public const string RadioBgCheckedHover = "feathers.radio.bg.checked.hover";

    /// <summary>Background of the radio button (checked+pressed).</summary>
    public const string RadioBgCheckedPressed = "feathers.radio.bg.checked.pressed";

    /// <summary>Background of the radio button (checked+disabled).</summary>
    public const string RadioBgCheckedDisabled = "feathers.radio.bg.checked.disabled";

    /// <summary>Border around the radio button.</summary>
    public const string RadioBorder = "feathers.radio.border";

    /// <summary>Border around the radio button (hovered).</summary>
    public const string RadioBorderHover = "feathers.radio.border.hover";

    /// <summary>Border around the radio button (pressed).</summary>
    public const string RadioBorderPressed = "feathers.radio.border.pressed";

    /// <summary>Border around the radio button (disabled).</summary>
    public const string RadioBorderDisabled = "feathers.radio.border.disabled";

    /// <summary>Border around the radio button (checked).</summary>
    public const string RadioBorderChecked = "feathers.radio.border.checked";

    /// <summary>Border around the radio button (checked+hovered).</summary>
    public const string RadioBorderCheckedHover = "feathers.radio.border.checked.hover";

    /// <summary>Border around the radio button (checked+pressed).</summary>
    public const string RadioBorderCheckedPressed = "feathers.radio.border.checked.pressed";

    /// <summary>Border around the radio button (checked+disabled).</summary>
    public const string RadioBorderCheckedDisabled = "feathers.radio.border.checked.disabled";

    /// <summary>Radio check mark.</summary>
    public const string RadioMark = "feathers.radio.mark";

    /// <summary>Radio check mark (hovered).</summary>
    public const string RadioMarkHover = "feathers.radio.mark.hover";

    /// <summary>Radio check mark (pressed).</summary>
    public const string RadioMarkPressed = "feathers.radio.mark.pressed";

    /// <summary>Radio check mark (disabled).</summary>
    public const string RadioMarkDisabled = "feathers.radio.mark.disabled";

    /// <summary>Radio label text.</summary>
    public const string RadioText = "feathers.radio.text";

    /// <summary>Radio label text (disabled).</summary>
    public const string RadioTextDisabled = "feathers.radio.text.disabled";

    /// <summary>Switch background around the switch.</summary>
    public const string SwitchBg = "feathers.switch.bg";

    /// <summary>Switch background around the switch (hovered).</summary>
    public const string SwitchBgHover = "feathers.switch.bg.hover";

    /// <summary>Switch background around the switch (pressed).</summary>
    public const string SwitchBgPressed = "feathers.switch.bg.pressed";

    /// <summary>Switch background around the switch (disabled).</summary>
    public const string SwitchBgDisabled = "feathers.switch.bg.disabled";

    /// <summary>Switch background around the switch (checked).</summary>
    public const string SwitchBgChecked = "feathers.switch.bg.checked";

    /// <summary>Switch background around the switch (checked+hover).</summary>
    public const string SwitchBgCheckedHover = "feathers.switch.bg.checked.hover";

    /// <summary>Switch background around the switch (checked+pressed).</summary>
    public const string SwitchBgCheckedPressed = "feathers.switch.bg.checked.pressed";

    /// <summary>Switch background around the switch (checked+disabled).</summary>
    public const string SwitchBgCheckedDisabled = "feathers.switch.bg.checked.disabled";

    /// <summary>Switch border around the switch.</summary>
    public const string SwitchBorder = "feathers.switch.border";

    /// <summary>Switch border around the switch (hovered).</summary>
    public const string SwitchBorderHover = "feathers.switch.border.hover";

    /// <summary>Switch border around the switch (pressed).</summary>
    public const string SwitchBorderPressed = "feathers.switch.border.hover.pressed";

    /// <summary>Switch border around the switch (disabled).</summary>
    public const string SwitchBorderDisabled = "feathers.switch.border.disabled";

    /// <summary>Switch border around the switch (checked).</summary>
    public const string SwitchBorderChecked = "feathers.switch.border.checked";

    /// <summary>Switch border around the switch (checked+hovered).</summary>
    public const string SwitchBorderCheckedHover = "feathers.switch.border.checked.hover";

    /// <summary>Switch border around the switch (checked+pressed).</summary>
    public const string SwitchBorderCheckedPressed = "feathers.switch.border.checked.pressed";

    /// <summary>Switch border around the switch (checked+disabled).</summary>
    public const string SwitchBorderCheckedDisabled = "feathers.switch.border.checked.disabled";

    /// <summary>Switch slide background.</summary>
    public const string SwitchSlideBg = "feathers.switch.slide.bg";

    /// <summary>Switch slide background (hovered).</summary>
    public const string SwitchSlideBgHover = "feathers.switch.slide.bg.hover";

    /// <summary>Switch slide background (pressed).</summary>
    public const string SwitchSlideBgPressed = "feathers.switch.slide.bg.pressed";

    /// <summary>Switch slide background (disabled).</summary>
    public const string SwitchSlideBgDisabled = "feathers.switch.slide.bg.disabled";

    /// <summary>Switch slide background (checked).</summary>
    public const string SwitchSlideBgChecked = "feathers.switch.slide.bg.checked";

    /// <summary>Switch slide background (checked+hovered).</summary>
    public const string SwitchSlideBgCheckedHover = "feathers.switch.slide.bg.checked.hover";

    /// <summary>Switch slide background (checked+pressed).</summary>
    public const string SwitchSlideBgCheckedPressed = "feathers.switch.slide.bg.checked.pressed";

    /// <summary>Switch slide background (checked+disabled).</summary>
    public const string SwitchSlideBgCheckedDisabled = "feathers.switch.slide.bg.checked.disabled";

    /// <summary>Switch slide border.</summary>
    public const string SwitchSlideBorder = "feathers.switch.slide.border";

    /// <summary>Switch slide border (hovered).</summary>
    public const string SwitchSlideBorderHover = "feathers.switch.slide.border.hover";

    /// <summary>Switch slide border (pressed).</summary>
    public const string SwitchSlideBorderPressed = "feathers.switch.slide.border.pressed";

    /// <summary>Switch slide border (disabled).</summary>
    public const string SwitchSlideBorderDisabled = "feathers.switch.slide.border.disabled";

    /// <summary>Switch slide border (checked).</summary>
    public const string SwitchSlideBorderChecked = "feathers.switch.slide.border.checked";

    /// <summary>Switch slide border (checked+hovered).</summary>
    public const string SwitchSlideBorderCheckedHover = "feathers.switch.slide.border.checked.hover";

    /// <summary>Switch slide border (checked+pressed).</summary>
    public const string SwitchSlideBorderCheckedPressed = "feathers.switch.slide.border.checked.pressed";

    /// <summary>Switch slide border (checked+disabled).</summary>
    public const string SwitchSlideBorderCheckedDisabled = "feathers.switch.slide.border.checked.disabled";

    /// <summary>Color plane frame background.</summary>
    public const string ColorPlaneBg = "feathers.colorplane.bg";

    /// <summary>Menu background.</summary>
    public const string MenuBg = "feathers.menu.bg";

    /// <summary>Menu border.</summary>
    public const string MenuBorder = "feathers.menu.border";

    /// <summary>Menu item.</summary>
    public const string MenuitemBg = "feathers.menuitem.bg";

    /// <summary>Menu item hovered.</summary>
    public const string MenuitemBgHover = "feathers.menuitem.bg.hover";

    /// <summary>Menu item pressed.</summary>
    public const string MenuitemBgPressed = "feathers.menuitem.bg.pressed";

    /// <summary>Menu item focused.</summary>
    public const string MenuitemBgFocused = "feathers.menuitem.bg.focused";

    /// <summary>Menu item text.</summary>
    public const string MenuitemText = "feathers.menuitem.text";

    /// <summary>Menu item text (disabled).</summary>
    public const string MenuitemTextDisabled = "feathers.menuitem.text.disabled";

    /// <summary>Background for text input.</summary>
    public const string TextInputBg = "feathers.textinput.bg";

    /// <summary>Text color for text input.</summary>
    public const string TextInputText = "feathers.textinput.text";

    /// <summary>Text color for text input (disabled).</summary>
    public const string TextInputTextDisabled = "feathers.textinput.text.disabled";

    /// <summary>Cursor color for text input.</summary>
    public const string TextInputCursor = "feathers.textinput.cursor";

    /// <summary>Selection color for text input.</summary>
    public const string TextInputSelection = "feathers.textinput.selection";

    /// <summary>Selection color for unfocused text input.</summary>
    public const string TextInputSelectionUnfocused = "feathers.textinput.selection.unfocused";

    /// <summary>Background color for label text.</summary>
    public const string TextInputLabelBg = "feathers.textinput.label.bg";

    /// <summary>Sigil color for X.</summary>
    public const string TextInputXAxis = "feathers.textinput.axis.x";

    /// <summary>Sigil color for Y.</summary>
    public const string TextInputYAxis = "feathers.textinput.axis.y";

    /// <summary>Sigil color for Z.</summary>
    public const string TextInputZAxis = "feathers.textinput.axis.z";

    /// <summary>Sigil color for W.</summary>
    public const string TextInputWAxis = "feathers.textinput.axis.w";

    /// <summary>Pane header background.</summary>
    public const string PaneHeaderBg = "feathers.pane.header.bg";

    /// <summary>Pane header border.</summary>
    public const string PaneHeaderBorder = "feathers.pane.header.border";

    /// <summary>Pane header text color.</summary>
    public const string PaneHeaderText = "feathers.pane.header.text";

    /// <summary>Pane header divider color.</summary>
    public const string PaneHeaderDivider = "feathers.pane.header.divider";

    /// <summary>Pane body background.</summary>
    public const string PaneBodyBg = "feathers.pane.body.bg";

    /// <summary>Subpane background.</summary>
    public const string SubpaneHeaderBg = "feathers.subpane.header.bg";

    /// <summary>Subpane header border.</summary>
    public const string SubpaneHeaderBorder = "feathers.subpane.header.border";

    /// <summary>Subpane header text color.</summary>
    public const string SubpaneHeaderText = "feathers.subpane.header.text";

    /// <summary>Subpane body background.</summary>
    public const string SubpaneBodyBg = "feathers.subpane.body.bg";

    /// <summary>Subpane body border.</summary>
    public const string SubpaneBodyBorder = "feathers.subpane.body.border";

    /// <summary>Group background.</summary>
    public const string GroupBg = "feathers.group.bg";

    /// <summary>Group header border.</summary>
    public const string GroupBorder = "feathers.group.border";

    /// <summary>Group header text color.</summary>
    public const string GroupHeaderText = "feathers.group.header.text";

    /// <summary>Listview row background.</summary>
    public const string ListrowBg = "feathers.listrow.bg";

    /// <summary>Listview row background (hovered).</summary>
    public const string ListrowBgHover = "feathers.listrow.bg.hover";

    /// <summary>Listview row background (selected).</summary>
    public const string ListrowBgSelected = "feathers.listrow.bg.selected";

    /// <summary>Listview row text.</summary>
    public const string ListrowText = "feathers.listrow.text";

    /// <summary>Listview row text (disabled).</summary>
    public const string ListrowTextDisabled = "feathers.listrow.text.disabled";

    /// <summary>Dialog background.</summary>
    public const string DialogBg = "feathers.dialog.bg";

    /// <summary>Dialog border.</summary>
    public const string DialogBorder = "feathers.dialog.border";

    /// <summary>Dialog header background.</summary>
    public const string DialogHeaderBg = "feathers.dialog.header.bg";

    /// <summary>Dialog text.</summary>
    public const string DialogText = "feathers.dialog.text";

    /// <summary>Dialog header text.</summary>
    public const string DialogHeaderText = "feathers.dialog.header.text";
}
