namespace Bevy;

/// <summary>A key of a Feathers virtual keyboard was pressed.</summary>
/// <param name="Keyboard">The keyboard, from <see cref="Feathers.VirtualKeyboard"/>.</param>
/// <param name="Key">What the key says, as the keyboard was given it.</param>
/// <param name="Row">The key's row, from the top.</param>
/// <param name="Column">Its place in the row, from the left.</param>
/// <remarks>
/// Bevy's <c>VirtualKeyPressed</c>, triggered on the keyboard as a key is let go, which does
/// nothing more, so a game observing it decides what the key does, typing into a field or
/// moving its cursor:
/// <code>
/// ecs.Observe&lt;VirtualKeyPressed&gt;(keyboard, on => Type(on.Event.Key));
/// </code>
/// </remarks>
public readonly record struct VirtualKeyPressed(Entity Keyboard, string Key, int Row, int Column) : IEntityEvent, IReportedEvent
{
    /// <inheritdoc/>
    Entity IEntityEvent.Entity => Keyboard;

    /// <inheritdoc/>
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.WatchWidget(WidgetEvents.VirtualKey);
}
