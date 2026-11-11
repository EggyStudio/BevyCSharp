namespace Bevy;

public sealed partial class EcsWorld
{
    private ObserverRegistry? _observers;

    /// <summary>The world this belongs to, which observers are handed, set by the app that made it.</summary>
    internal World? Owner { get; set; }

    private ObserverRegistry Observers => _observers ??= new ObserverRegistry(
        Owner ?? throw new InvalidOperationException("Observers need the app's world, and this EcsWorld was made outside an app."));

    /// <summary>Runs <paramref name="observer"/> each time a <typeparamref name="TEvent"/> is triggered.</summary>
    /// <remarks>
    /// <para>
    /// Bevy's <c>add_observer</c>. <typeparamref name="TEvent"/> is either an event a game declares,
    /// triggered with <see cref="Trigger{TEvent}(TEvent)"/>, or a change Bevy reports to a
    /// component, <see cref="Add{T}"/>, <see cref="Insert{T}"/>, <see cref="Discard{T}"/>,
    /// <see cref="Remove{T}"/> or <see cref="Despawn{T}"/>, of a C# component or of one of Bevy's
    /// through its wrapper, as <c>Add&lt;PressedRef&gt;</c>.
    /// </para>
    /// <para>
    /// An event of a game's runs its observers at once, from inside the call that triggered it. A
    /// change to a component runs them once the change has been made and before the call that made
    /// it returns, or as the commands are applied for one queued on <see cref="BehaviorContext.Cmd"/>,
    /// with the whole world to work in. A removal is handed the value that went. Observing an entity event this way sees it at every entity it
    /// reaches. Disposing the returned handle stops the observer.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// ecs.Observe&lt;Remove&lt;Mine&gt;&gt;(on =&gt; index.Forget(on.Event.Value.Position));
    /// ecs.Observe&lt;Explode&gt;(on =&gt; Console.WriteLine($"Boom! {on.Entity} exploded."));
    /// </code>
    /// </example>
    public IDisposable Observe<TEvent>(Action<On<TEvent>> observer) => Observers.Observe(null, observer);

    /// <summary>Runs <paramref name="observer"/> each time a <typeparamref name="TEvent"/> reaches <paramref name="entity"/>.</summary>
    /// <remarks>
    /// Bevy's <c>observe</c> on an entity, for an <see cref="IEntityEvent"/> that happened to it, or
    /// reached it propagating from a child, or a change to one of its components. The observer stays
    /// until its handle is disposed, and an entity that is gone is never reached again.
    /// </remarks>
    public IDisposable Observe<TEvent>(Entity entity, Action<On<TEvent>> observer) => Observers.Observe(entity, observer);

    /// <summary>Lets go of the observers as the app that made this is disposed.</summary>
    internal void ForgetObservers()
    {
        _observers?.Dispose();
        _observers = null;
    }

    /// <summary>Runs the observers of <paramref name="value"/> now.</summary>
    /// <remarks>
    /// Bevy's <c>trigger</c>. An <see cref="IEntityEvent"/> runs the observers watching every such
    /// event and then the ones of its entity, and an <see cref="IPropagatingEvent"/> then goes on up the
    /// entity's parents until an observer stops it. Any other event runs the ones watching it.
    /// </remarks>
    public void Trigger<TEvent>(TEvent value) => Observers.Trigger(value);
}
