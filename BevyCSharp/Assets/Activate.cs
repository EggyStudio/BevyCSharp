namespace Bevy;

/// <summary>A button or a menu item was activated, by a click, a tap or a key.</summary>
/// <param name="Entity">The button or the menu item.</param>
/// <remarks>
/// <para>
/// Bevy's <c>Activate</c>, triggered by Bevy's <c>Button</c> and <c>MenuItem</c> widgets, observed
/// at the widget or at every one:
/// </para>
/// <code>
/// ecs.Observe&lt;Activate&gt;(button, on => Save(on.Ecs));
/// </code>
/// <para>
/// It happens to the widget alone and does not go on to its parents. The first observer asks Bevy
/// to report it, and a build without the renderer, which has no widgets, reports nothing.
/// </para>
/// </remarks>
public readonly record struct Activate(Entity Entity) : IEntityEvent, IReportedEvent
{
    /// <inheritdoc/>
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.WatchWidget(WidgetEvents.Activate);
}
