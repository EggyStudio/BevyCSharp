namespace Bevy;

/// <summary>A menu was asked to open, to close, or to give the focus back to its owner.</summary>
/// <param name="Source">The menu item or the menu's popup that asked.</param>
/// <param name="Action">What it asked for.</param>
/// <param name="Navigation">For a menu asked to open, which of its items takes the focus.</param>
/// <remarks>
/// <para>
/// Bevy's <c>MenuEvent</c>, triggered by Bevy's <c>MenuButton</c>, <c>MenuItem</c> and
/// <c>MenuPopup</c> widgets. A menu does not open or close itself. The game spawns its popup as a
/// child of the menu's owner when asked to open and despawns it when asked to close, so the event
/// goes on up the parents, from an item to its popup and to the owner, where the game observes it:
/// </para>
/// <code>
/// ecs.Observe&lt;MenuEvent&gt;(owner, on =>
/// {
///     if (on.Event.Action == MenuAction.Toggle) ToggleMenu(on.Ecs, on.Entity);
/// });
/// </code>
/// </remarks>
public readonly record struct MenuEvent(Entity Source, MenuAction Action, NavAction Navigation) : IPropagatingEvent, IReportedEvent
{
    /// <inheritdoc/>
    Entity IEntityEvent.Entity => Source;

    /// <inheritdoc/>
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.WatchWidget(WidgetEvents.Menu);
}
