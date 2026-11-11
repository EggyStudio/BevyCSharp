namespace Bevy;

/// <summary>Input handed to the entity with the input focus.</summary>
/// <typeparam name="TInput">The input, a <see cref="KeyboardInput"/>, the one kind this bridge reports.</typeparam>
/// <param name="FocusedEntity">The entity with the focus, which the input reached first.</param>
/// <param name="Input">The input.</param>
/// <remarks>
/// <para>
/// Bevy's <c>FocusedInput&lt;KeyboardInput&gt;</c>, each key handed to the focused text field, button
/// or menu, which takes its keys from it. It goes on up the entity's parents, so a row holding a
/// field hears the field's keys, until an observer calls <see cref="On{TEvent}.Propagate"/> with
/// false:
/// </para>
/// <code>
/// ecs.Observe&lt;FocusedInput&lt;KeyboardInput&gt;&gt;(field, on =>
/// {
///     if (on.Event.Input.State == ButtonState.Pressed &amp;&amp; on.Event.Input.LogicalKey == LogicalKey.Enter)
///         Submit(on.Ecs, on.Event.FocusedEntity);
/// });
/// </code>
/// <para>
/// Bevy takes it on to the window once it has passed the root, and stops it where a text field has
/// taken a key, while the C# observers of an entity's parents hear every key that reached it, up
/// to the root. A run with no window is handed its keys as well, which Bevy itself leaves out, so
/// a test or a script types into an offscreen run's fields. A build without the renderer has no
/// input focus and reports nothing.
/// </para>
/// </remarks>
public readonly record struct FocusedInput<TInput>(Entity FocusedEntity, TInput Input) : IPropagatingEvent, IReportedEvent
    where TInput : struct
{
    /// <inheritdoc/>
    Entity IEntityEvent.Entity => FocusedEntity;

    /// <inheritdoc/>
    void IReportedEvent.Watch(ObserverRegistry registry)
    {
        if (typeof(TInput) != typeof(KeyboardInput))
            throw new NotSupportedException(
                $"Only keys are handed to the focused entity here, as FocusedInput<KeyboardInput>, not {typeof(TInput).Name}.");
        registry.WatchFocusedKeys();
    }
}
