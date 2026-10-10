namespace Bevy;

/// <summary>A widget that edits a value was asked to take a new one.</summary>
/// <typeparam name="T">
/// The value, a <see cref="float"/> for a slider, a <see cref="bool"/> for a checkbox or a radio
/// button, an <see cref="Bevy.Entity"/> for a radio group, the button chosen, and an
/// <see cref="Bevy.Entity"/>? for a tab list, the tab chosen, null for none.
/// </typeparam>
/// <param name="Source">The widget.</param>
/// <param name="Value">The value asked for.</param>
/// <param name="IsFinal">
/// Whether the interaction that asked is over, the button let go or the drag ended, rather than
/// going on.
/// </param>
/// <remarks>
/// <para>
/// Bevy's <c>ValueChange&lt;T&gt;</c>. A widget does not change itself. It says what it was asked
/// to become, and whoever observes this decides, writing the value back to the widget, refusing it,
/// or keeping it somewhere of its own that the widget is then set from:
/// </para>
/// <code>
/// ecs.Observe&lt;ValueChange&lt;float&gt;&gt;(slider, on => volume = on.Event.Value);
/// </code>
/// <para>
/// <see cref="Ui.SelfUpdate"/> attaches Bevy's own observer that writes the value straight back,
/// for a widget nothing else needs to decide about. It happens to the widget alone and does not go
/// on to its parents. A value of another type throws as it is observed, since no widget of Bevy's
/// reports one.
/// </para>
/// </remarks>
public readonly record struct ValueChange<T>(Entity Source, T Value, bool IsFinal) : IEntityEvent, IReportedEvent
{
    /// <inheritdoc/>
    Entity IEntityEvent.Entity => Source;

    /// <inheritdoc/>
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.WatchWidget(WidgetEvents.KindOfValue<T>());
}
