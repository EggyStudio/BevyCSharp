using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// An event Bevy reports once the registry asks it to, a change to a component or something a
/// pointer did.
/// </summary>
internal interface IReportedEvent
{
    /// <summary>Asks Bevy to report this kind of event.</summary>
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
    private readonly HashSet<(int Kind, int Component, Type Event)> _watched = [];
    private readonly HashSet<int> _watchedPointers = [];
    private readonly HashSet<int> _watchedWidgets = [];
    private bool _watchedFocusedKeys;
    private bool _watchedClipEvents;
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

        // A change to a component, or what a pointer did, is Bevy's to report, so watching one
        // starts the report, as watching an event placed on a clip starts the clips' reports.
        if (default(TEvent) is IReportedEvent reported) reported.Watch(this);
        if (default(TEvent) is IAnimationEvent) WatchClipEvents();

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

    /// <summary>Runs the observers of an event at an entity it happened at, without taking it further.</summary>
    /// <remarks>
    /// For an event that is no <see cref="IEntityEvent"/> and still happens somewhere, as an event
    /// placed on a clip happens at its player or target, Bevy's <c>AnimationEventTrigger</c>.
    /// </remarks>
    internal void TriggerAt<TEvent>(TEvent value, Entity at)
    {
        var on = new On<TEvent>(_world, value, at);
        Run(_global, typeof(TEvent), on);
        if (at != Entity.None) Run(_onEntity, (typeof(TEvent), at), on);
    }

    /// <summary>Asks the bridge to report the events this app's clips reach, once.</summary>
    /// <remarks>A bridge without the renderer has no animation, answers so, and reports nothing.</remarks>
    internal void WatchClipEvents()
    {
        if (_watchedClipEvents) return;
        _watchedClipEvents = true;

        var status = Native.bcs_observe_clip_events(ComponentRegistry.AppHandle, &ClipEventReported, GCHandle.ToIntPtr(_self));
        if (status != NativeStatus.Unsupported) Native.Check(status, "observing the events placed on clips");
    }

    /// <summary>Where the bridge reports a clip reaching an event placed on it, with the world on loan.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void ClipEventReported(uint number, ulong entity, IntPtr user)
    {
        try
        {
            if (GCHandle.FromIntPtr(user).Target is not ObserverRegistry registry) return;
            ClipEvents.Fire(registry, number, new Entity(entity));
        }
        catch (Exception ex)
        {
            // An exception crossing back into Rust is undefined, so it ends here, said.
            EngineLog.Error(null, "observer", $"[BevyCSharp] An observer threw: {ex}", ex);
        }
    }

    /// <summary>Asks the bridge to report one kind of change to <typeparamref name="T"/>, once.</summary>
    /// <remarks>
    /// <para>
    /// <typeparamref name="T"/> is a C# component, whose bytes as the change left them are handed
    /// on, or a wrapper over one of Bevy's, found by its type path and handed on as a wrapper over
    /// the entity, since Bevy's bytes are Rust's own and only its reflection reads them.
    /// </para>
    /// <para>
    /// Two types can name one component, a mirror such as <see cref="Transform"/> and its wrapper
    /// <c>TransformRef</c>, so each is reported in turn from the one observer the bridge keeps for
    /// the component, rather than the first to ask being the only one heard.
    /// </para>
    /// </remarks>
    internal void Watch<T, TEvent>(int kind, Func<Entity, T, TEvent> make) where T : struct
    {
        var wrapper = (object)default(T) as IReflectedWrapper<T>;
        var component = wrapper is null ? ComponentType<T>.Id : NativeComponents.Resolve(wrapper.ComponentPath, 0);
        if (!_watched.Add((kind, component, typeof(TEvent)))) return;

        Action<Entity, ReadOnlySpan<byte>> report = wrapper is null
            ? (entity, bytes) =>
            {
                var value = bytes.Length >= Unsafe.SizeOf<T>() ? MemoryMarshal.Read<T>(bytes) : default;
                Trigger(make(entity, value));
            }
            : (entity, _) => Trigger(make(entity, wrapper.Over(_world.Resource<EcsWorld>(), entity)));

        if (_reports.TryGetValue((kind, component), out var reports))
        {
            _reports[(kind, component)] = reports + report;
            return;
        }

        _reports[(kind, component)] = report;

        ulong observer;
        Native.Check(
            Native.bcs_observe_component(ComponentRegistry.AppHandle, kind, component, &Reported, GCHandle.ToIntPtr(_self), &observer),
            $"observing {typeof(T).Name}");
    }

    /// <summary>Asks the bridge to report one kind of thing a pointer does, once.</summary>
    /// <remarks>
    /// A bridge without picking answers that it has none, and nothing is ever reported, which is
    /// right for a build that draws nothing a pointer could be over.
    /// </remarks>
    internal void WatchPointer(int kind)
    {
        if (!_watchedPointers.Add(kind)) return;

        ulong observer;
        var status = Native.bcs_observe_pointer(ComponentRegistry.AppHandle, kind, &PointerReported, GCHandle.ToIntPtr(_self), &observer);
        if (status != NativeStatus.Unsupported) Native.Check(status, "observing what a pointer does");
    }

    /// <summary>Asks the bridge to report one kind of thing Bevy's widgets report, once.</summary>
    /// <remarks>
    /// A bridge without the renderer has no widgets, answers so, and nothing is ever reported.
    /// </remarks>
    internal void WatchWidget(int kind)
    {
        if (!_watchedWidgets.Add(kind)) return;

        ulong observer;
        var status = Native.bcs_observe_widget(ComponentRegistry.AppHandle, kind, &WidgetReported, GCHandle.ToIntPtr(_self), &observer);
        if (status != NativeStatus.Unsupported) Native.Check(status, "observing what a widget reports");
    }

    /// <summary>Asks the bridge to report each key that reaches the focused entity, once.</summary>
    /// <remarks>A bridge without the renderer has no input focus, answers so, and reports nothing.</remarks>
    internal void WatchFocusedKeys()
    {
        if (_watchedFocusedKeys) return;
        _watchedFocusedKeys = true;

        ulong observer;
        var status = Native.bcs_observe_focused_keys(ComponentRegistry.AppHandle, &FocusedKeyReported, GCHandle.ToIntPtr(_self), &observer);
        if (status != NativeStatus.Unsupported) Native.Check(status, "observing keys handed to the focused entity");
    }

    /// <summary>Where the bridge reports a key that reached the focused entity, with the world on loan.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void FocusedKeyReported(NativeFocusedKey* reported, IntPtr user)
    {
        try
        {
            if (GCHandle.FromIntPtr(user).Target is not ObserverRegistry registry || reported == null) return;

            var native = *reported;
            var logical = Encoding.UTF8.GetString(native.Logical, Math.Min((int)native.LogicalLength, NativeFocusedKey.TextCapacity));
            var key = new KeyboardInput(
                native.Key >= 0 && native.Key < KeyTable.Count ? (Key)native.Key : null,
                native.LogicalKind switch
                {
                    1 => LogicalKey.Character(logical),
                    2 => LogicalKey.Dead(logical),
                    _ => LogicalKey.Named(logical),
                },
                native.State == 1 ? ButtonState.Pressed : ButtonState.Released,
                native.HasText != 0 ? Encoding.UTF8.GetString(native.Text, Math.Min((int)native.TextLength, NativeFocusedKey.TextCapacity)) : null,
                native.Repeat != 0);

            registry.Trigger(new FocusedInput<KeyboardInput>(new Entity(native.Entity), key));
        }
        catch (Exception ex)
        {
            // An exception crossing back into Rust is undefined, so it ends here, said.
            EngineLog.Error(null, "observer", $"[BevyCSharp] An observer threw: {ex}", ex);
        }
    }

    /// <summary>Where the bridge reports what a widget reported, with the world on loan.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void WidgetReported(NativeWidgetEvent* reported, IntPtr user)
    {
        try
        {
            if (GCHandle.FromIntPtr(user).Target is not ObserverRegistry registry || reported == null) return;
            WidgetEvents.Trigger(registry, *reported);
        }
        catch (Exception ex)
        {
            // An exception crossing back into Rust is undefined, so it ends here, said.
            EngineLog.Error(null, "observer", $"[BevyCSharp] An observer threw: {ex}", ex);
        }
    }

    /// <summary>Where the bridge reports what a pointer did, with the world on loan.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void PointerReported(NativePointerEvent* reported, IntPtr user)
    {
        try
        {
            if (GCHandle.FromIntPtr(user).Target is not ObserverRegistry registry || reported == null) return;
            PointerEvents.Trigger(registry, *reported);
        }
        catch (Exception ex)
        {
            // An exception crossing back into Rust is undefined, so it ends here, said.
            EngineLog.Error(null, "observer", $"[BevyCSharp] An observer threw: {ex}", ex);
        }
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
