namespace Bevy;

/// <summary>A dialog was asked to close, by its close button or a button of the game's.</summary>
/// <param name="Source">The control that asked, a dialog's close button among them.</param>
/// <remarks>
/// <para>
/// Bevy's <c>RequestClose</c>. A modal dialog does not close itself, so the event goes on up the
/// parents, from the button to the dialog, where the game observes it and despawns the dialog. A
/// floating dialog despawns itself as it is asked:
/// </para>
/// <code>
/// ecs.Observe&lt;RequestClose&gt;(dialog, on => on.Ecs.Despawn(dialog));
/// </code>
/// </remarks>
public readonly record struct RequestClose(Entity Source) : IPropagatingEvent, IReportedEvent
{
    /// <inheritdoc/>
    Entity IEntityEvent.Entity => Source;

    /// <inheritdoc/>
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.WatchWidget(WidgetEvents.Close);
}
