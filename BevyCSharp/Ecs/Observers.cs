using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>An event that happens to one entity, so observers of that entity are told of it.</summary>
/// <remarks>
/// Bevy's <c>EntityEvent</c>. Triggered with <see cref="EcsWorld.Trigger{TEvent}(TEvent)"/>, it reaches
/// the observers watching every such event and then the ones watching <see cref="Entity"/>.
/// </remarks>
public interface IEntityEvent
{
    /// <summary>The entity it happened to.</summary>
    Entity Entity { get; }
}

/// <summary>
/// An entity event that goes on up to the entity's parent, and its parent's, after the entity's own
/// observers, until one of them stops it or it reaches the root.
/// </summary>
/// <remarks>
/// Bevy's <c>#[entity_event(propagate, auto_propagate)]</c>, for an event such as an attack on a
/// piece of armor, which the armor may block before the body it hangs from feels it.
/// <see cref="On{TEvent}.Propagate"/> stops it.
/// </remarks>
public interface IPropagatingEvent : IEntityEvent;

/// <summary>An event as an observer is handed it, with the world it happened in.</summary>
/// <typeparam name="TEvent">The event.</typeparam>
/// <remarks>
/// Bevy's <c>On</c>. The event is handed by reference, so an observer that changes it, as armor
/// lessens an attack's damage, passes the changed event to the observers after it and to the
/// entities it propagates to.
/// </remarks>
public sealed class On<TEvent>
{
    private TEvent _event;

    internal On(World world, TEvent value, Entity entity)
    {
        World = world;
        _event = value;
        Entity = entity;
    }

    /// <summary>The event, to read or to change for the observers after this one.</summary>
    public ref TEvent Event => ref _event;

    /// <summary>
    /// The entity the event is at now, which is the entity it happened to or, as it propagates, the
    /// ancestor it has reached. <see cref="Entity.None"/> for an event that happened to no entity.
    /// </summary>
    public Entity Entity { get; internal set; }

    /// <summary>The world it happened in.</summary>
    public World World { get; }

    /// <summary>The world's entities and components.</summary>
    public EcsWorld Ecs => World.Resource<EcsWorld>();

    /// <summary>A context over the world, as a system has, with the entity the event is at.</summary>
    public BehaviorContext Context => new(World) { Entity = Entity };

    /// <summary>Whether the event goes on to the entity's parent after this entity's observers.</summary>
    internal bool Propagating { get; private set; } = true;

    /// <summary>Lets the event go on up to the parent, or, given false, stops it here.</summary>
    /// <remarks>Read only for an <see cref="IPropagatingEvent"/>, and only after every observer of this entity has run.</remarks>
    public void Propagate(bool propagate) => Propagating = propagate;
}

/// <summary>A component was added to an entity that did not have one.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity it was added to.</param>
/// <param name="Value">Its value once added.</param>
/// <remarks>Bevy's <c>On&lt;Add, T&gt;</c>, observed with <see cref="EcsWorld.Observe{TEvent}(Action{On{TEvent}})"/>.</remarks>
public readonly record struct Add<T>(Entity Entity, T Value) : IEntityEvent, ILifecycleEvent where T : unmanaged
{
    void ILifecycleEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Add<T>>(0, (entity, value) => new Add<T>(entity, value));
}

/// <summary>A component was put on an entity, whether or not it had one before.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity it was put on.</param>
/// <param name="Value">Its value once put there.</param>
/// <remarks>Bevy's <c>On&lt;Insert, T&gt;</c>, which follows an <see cref="Add{T}"/> where there was one.</remarks>
public readonly record struct Insert<T>(Entity Entity, T Value) : IEntityEvent, ILifecycleEvent where T : unmanaged
{
    void ILifecycleEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Insert<T>>(1, (entity, value) => new Insert<T>(entity, value));
}

/// <summary>A component's value was given up, by being replaced, removed or despawned.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity that held it.</param>
/// <param name="Value">The value given up.</param>
/// <remarks>Bevy's <c>On&lt;Discard, T&gt;</c>, for keeping an index of values in step as they change.</remarks>
public readonly record struct Discard<T>(Entity Entity, T Value) : IEntityEvent, ILifecycleEvent where T : unmanaged
{
    void ILifecycleEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Discard<T>>(2, (entity, value) => new Discard<T>(entity, value));
}

/// <summary>A component left an entity, by removal or despawn.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity it left.</param>
/// <param name="Value">The value it had.</param>
/// <remarks>Bevy's <c>On&lt;Remove, T&gt;</c>. The observer runs once the component has gone, and is handed the value it had.</remarks>
public readonly record struct Remove<T>(Entity Entity, T Value) : IEntityEvent, ILifecycleEvent where T : unmanaged
{
    void ILifecycleEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Remove<T>>(3, (entity, value) => new Remove<T>(entity, value));
}

/// <summary>An entity carrying a component was despawned.</summary>
/// <typeparam name="T">The component.</typeparam>
/// <param name="Entity">The entity, no longer alive by the time the observer runs.</param>
/// <param name="Value">The value the component had.</param>
/// <remarks>Bevy's <c>On&lt;Despawn, T&gt;</c>.</remarks>
public readonly record struct Despawn<T>(Entity Entity, T Value) : IEntityEvent, ILifecycleEvent where T : unmanaged
{
    void ILifecycleEvent.Watch(ObserverRegistry registry) => registry.Watch<T, Despawn<T>>(4, (entity, value) => new Despawn<T>(entity, value));
}

/// <summary>A change to a component that Bevy reports and the registry asks it to.</summary>
internal interface ILifecycleEvent
{
    /// <summary>Asks Bevy to report this kind of change to this component.</summary>
    void Watch(ObserverRegistry registry);
}

/// <summary>
/// Every observer of one world, and the triggering that runs them.
/// </summary>
/// <remarks>
/// <para>
/// An event a game declares is triggered from C# and runs its observers at once, in managed code,
/// since nothing but C# triggers it. A change to a C# component is Bevy's to report, so the first
/// observer of one asks the bridge for a Bevy observer of that component, which calls back with
/// the entity and the component's bytes once the world is whole again, and the change is triggered
/// here from that.
/// </para>
/// <para>
/// A handler list is copied before it runs, so an observer that adds or drops observers changes
/// what the next trigger runs and not the one in progress.
/// </para>
/// </remarks>
internal sealed unsafe class ObserverRegistry : IDisposable
{
    private readonly World _world;
    private readonly Dictionary<Type, List<Delegate>> _global = [];
    private readonly Dictionary<(Type, Entity), List<Delegate>> _onEntity = [];
    private readonly HashSet<(int Kind, int Component)> _watched = [];
    private readonly Dictionary<(int Kind, int Component), Action<Entity, ReadOnlySpan<byte>>> _reports = [];
    private GCHandle _self;

    internal ObserverRegistry(World world)
    {
        _world = world;
        _self = GCHandle.Alloc(this, GCHandleType.Normal);
    }

    /// <summary>Adds an observer of every <typeparamref name="TEvent"/>, or of those at one entity.</summary>
    internal IDisposable Observe<TEvent>(Entity? entity, Action<On<TEvent>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        // A change to a component is Bevy's to report, so watching one starts the report.
        if (default(TEvent) is ILifecycleEvent lifecycle) lifecycle.Watch(this);

        var list = entity is { } at
            ? GetOrAdd(_onEntity, (typeof(TEvent), at))
            : GetOrAdd(_global, typeof(TEvent));
        list.Add(handler);
        return new Subscription(() => list.Remove(handler));
    }

    /// <summary>Runs the observers of an event, at its entity and up its parents where it propagates.</summary>
    internal void Trigger<TEvent>(TEvent value)
    {
        var at = value is IEntityEvent entityEvent ? entityEvent.Entity : Entity.None;
        var on = new On<TEvent>(_world, value, at);

        if (at == Entity.None)
        {
            Run(_global, typeof(TEvent), on);
            return;
        }

        var ecs = _world.Resource<EcsWorld>();
        while (true)
        {
            // The ones watching every such event first, then the entity's own, in Bevy's order.
            on.Entity = at;
            Run(_global, typeof(TEvent), on);
            Run(_onEntity, (typeof(TEvent), at), on);

            if (value is not IPropagatingEvent || !on.Propagating || !ecs.IsAlive(at)) return;
            var parent = ecs.ParentOf(at);
            if (parent == Entity.None) return;
            at = parent;
        }
    }

    /// <summary>Asks the bridge to report one kind of change to <typeparamref name="T"/>, once.</summary>
    internal void Watch<T, TEvent>(int kind, Func<Entity, T, TEvent> make) where T : unmanaged
    {
        var component = EcsWorld.ComponentId<T>();
        if (!_watched.Add((kind, component))) return;

        _reports[(kind, component)] = (entity, bytes) =>
        {
            var value = bytes.Length >= Unsafe.SizeOf<T>() ? MemoryMarshal.Read<T>(bytes) : default;
            Trigger(make(entity, value));
        };

        ulong observer;
        Native.Check(
            Native.bcs_observe_component(ComponentRegistry.AppHandle, kind, component, &Reported, GCHandle.ToIntPtr(_self), &observer),
            $"observing {typeof(T).Name}");
    }

    /// <summary>Where the bridge reports a change, with the world on loan.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void Reported(int kind, int component, ulong entity, byte* data, nuint length, IntPtr user)
    {
        try
        {
            if (GCHandle.FromIntPtr(user).Target is not ObserverRegistry registry) return;
            if (!registry._reports.TryGetValue((kind, component), out var report)) return;
            report(new Entity(entity), new ReadOnlySpan<byte>(data, (int)length));
        }
        catch (Exception ex)
        {
            // An exception crossing back into Rust is undefined, so it ends here, said.
            EngineLog.Error(null, "observer", $"[BevyCSharp] An observer threw: {ex}", ex);
        }
    }

    private void Run<TKey, TEvent>(Dictionary<TKey, List<Delegate>> table, TKey key, On<TEvent> on) where TKey : notnull
    {
        if (!table.TryGetValue(key, out var list) || list.Count == 0) return;
        foreach (var handler in list.ToArray()) ((Action<On<TEvent>>)handler)(on);
    }

    private static List<Delegate> GetOrAdd<TKey>(Dictionary<TKey, List<Delegate>> table, TKey key) where TKey : notnull
    {
        if (!table.TryGetValue(key, out var list)) table[key] = list = [];
        return list;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_self.IsAllocated) _self.Free();
    }

    private sealed class Subscription(Action remove) : IDisposable
    {
        private Action? _remove = remove;

        public void Dispose()
        {
            _remove?.Invoke();
            _remove = null;
        }
    }
}
