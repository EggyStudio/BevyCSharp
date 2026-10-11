namespace Bevy;

/// <summary>
/// How often the app updates in one of the two moments winit tells apart, its window focused or
/// not, as Bevy's <c>UpdateMode</c>.
/// </summary>
/// <remarks>
/// <para>
/// Continuous updates as fast as the loop can, which suits a game while it is played. Reactive
/// waits, updating once <see cref="Wait"/> has passed since the last update or sooner where an
/// event of a kind it reacts to arrives, which a tool or a game in the background saves power by.
/// <see cref="ReactiveLowPower"/> leaves the devices' own events out, so a mouse moved elsewhere
/// on the desktop does not wake an app whose window it is not over.
/// </para>
/// <para>
/// Independent of vertical sync, which <see cref="Config.Vsync"/> sets, and which bounds a
/// continuous app by the display's rate where it is on.
/// </para>
/// </remarks>
public readonly record struct UpdateMode
{
    /// <summary>Whether the app updates as fast as it can, with nothing waited for.</summary>
    public bool IsContinuous { get; init; }

    /// <summary>How long a reactive app waits at most from one update to the next.</summary>
    public TimeSpan Wait { get; init; }

    /// <summary>Whether a reactive app wakes for the devices' own events, as a mouse moved anywhere.</summary>
    public bool ReactToDeviceEvents { get; init; }

    /// <summary>Whether a reactive app wakes for the app's own events, as <see cref="Window.WakeUp"/> sends.</summary>
    public bool ReactToUserEvents { get; init; }

    /// <summary>Whether a reactive app wakes for its window's events, as a key pressed or the pointer moved over it.</summary>
    public bool ReactToWindowEvents { get; init; }

    /// <summary>Updates as fast as the loop can.</summary>
    public static UpdateMode Continuous => new() { IsContinuous = true };

    /// <summary>Updates after <paramref name="wait"/>, or sooner for any event.</summary>
    /// <param name="wait">The longest wait from one update to the next.</param>
    public static UpdateMode Reactive(TimeSpan wait) =>
        new() { Wait = wait, ReactToDeviceEvents = true, ReactToUserEvents = true, ReactToWindowEvents = true };

    /// <summary>
    /// Updates after <paramref name="wait"/>, or sooner for the window's events and the app's own,
    /// but not for the devices' own, as a mouse moved where the window is not.
    /// </summary>
    /// <param name="wait">The longest wait from one update to the next.</param>
    public static UpdateMode ReactiveLowPower(TimeSpan wait) =>
        new() { Wait = wait, ReactToUserEvents = true, ReactToWindowEvents = true };
}
