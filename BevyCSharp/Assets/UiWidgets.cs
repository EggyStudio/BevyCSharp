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

public static unsafe partial class Ui
{
    /// <summary>Makes one of Bevy's widgets keep its own state as the player works it.</summary>
    /// <remarks>
    /// <para>
    /// Bevy's widgets are its own components, <c>Slider</c>, <c>Checkbox</c>, <c>RadioGroup</c> and
    /// the rest, put on a node through their wrappers (<c>Bevy.Reflected.SliderRef</c> and its
    /// kin). A widget does not change itself. It reports a change as an event and leaves the change
    /// to whoever listens, and Bevy's examples attach Bevy's own listener that writes it back,
    /// which this attaches. The state is then read from the widget's components, a
    /// slider's through <c>SliderValueRef</c> and a checkbox's or a radio button's by whether it
    /// carries <c>CheckedRef</c>.
    /// </para>
    /// <para>
    /// A game that checks a change before taking it, refusing a value or asking first, writes the
    /// widget's components itself instead, from what it reads of the pointer.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SelfUpdate(Entity widget, UiWidgetKind kind)
    {
        var status = Native.bcs_ui_widget_self_update(widget.Bits, (int)kind);
        if (status == NativeStatus.Unsupported) throw Render.NoRenderer("Setting up a widget");
        Native.Check(status, $"making {widget} keep its own state");
    }
}
