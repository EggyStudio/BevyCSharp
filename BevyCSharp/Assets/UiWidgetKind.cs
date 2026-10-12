using Bevy.Interop;

namespace Bevy;

/// <summary>Which of Bevy's widgets keeps its own state, for <see cref="Ui.SelfUpdate"/>.</summary>
public enum UiWidgetKind
{
    /// <summary>A slider, its <c>SliderValue</c> following a drag, a click on its track or a key.</summary>
    Slider = 0,

    /// <summary>A checkbox, its <c>Checked</c> marker coming and going as it is clicked.</summary>
    Checkbox = 1,

    /// <summary>A radio group, the button clicked marked <c>Checked</c> and the others not.</summary>
    RadioGroup = 2,

    /// <summary>
    /// A tab list, its <c>SelectedTab</c> following the tab clicked, or the tab the focus moves to
    /// where the list activates tabs as they are focused.
    /// </summary>
    TabList = 3,
}
