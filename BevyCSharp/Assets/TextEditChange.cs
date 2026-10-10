namespace Bevy;

/// <summary>
/// A text field's edits for a frame were made, its text changed or its cursor moved.
/// </summary>
/// <param name="Entity">The field.</param>
/// <remarks>
/// <para>
/// Bevy's <c>TextEditChange</c>, triggered by a field made with <see cref="Ui.SetEditableText"/>
/// once what was typed, pasted or moved in a frame is applied, observed at the field or at every
/// one, with the text read back through <see cref="Ui.EditableTextOf"/>:
/// </para>
/// <code>
/// ecs.Observe&lt;TextEditChange&gt;(field, on => Ui.SetText(echo, Ui.EditableTextOf(on.Event.Entity) ?? ""));
/// </code>
/// <para>
/// A cursor moved without a character changing triggers it too, so a game comparing the text with
/// what it last read sees whether anything was written. It happens to the field alone and does not
/// go on to its parents. The first observer asks Bevy to report it, and a build without the
/// renderer, which has no fields, reports nothing.
/// </para>
/// </remarks>
public readonly record struct TextEditChange(Entity Entity) : IEntityEvent, IReportedEvent
{
    /// <inheritdoc/>
    void IReportedEvent.Watch(ObserverRegistry registry) => registry.WatchWidget(WidgetEvents.TextEdit);
}
