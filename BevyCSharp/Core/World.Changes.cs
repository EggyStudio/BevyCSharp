using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Bevy;

public sealed partial class World
{
    // When each resource was added and last changed, and by which call where that is kept.
    private readonly ConcurrentDictionary<Type, (long Added, long Changed, string? By)> _ticks = new();

    // A system's run is a tick of its own, as in Bevy, counted here. The running system's tick and
    // the tick of its run before, which a change is weighed against, the last run being earlier
    // than every change outside a system.
    private long _nextTick;
    private long _tick;
    private long _lastRun = -1;

    /// <summary>
    /// Whether a call's place is kept for <see cref="ChangedBy{T}"/> and
    /// <see cref="EcsWorld.ChangedBy{T}"/>, as <see cref="Config.TrackChangeLocations"/> asks.
    /// </summary>
    public bool TrackChangeLocations { get; internal set; }

    /// <summary>
    /// Whether the resource of type <typeparamref name="T"/> was changed since the running system
    /// last ran, as Bevy's <c>Res::is_changed</c> answers, inserting it among the changes.
    /// </summary>
    /// <remarks>
    /// A system's first run sees every resource there as changed, as in Bevy, and a system does not
    /// see its own changes on its next run. Outside a system it says whether the resource has ever
    /// changed.
    /// </remarks>
    public bool IsChanged<T>() where T : notnull =>
        _ticks.TryGetValue(typeof(T), out var ticks) && ticks.Changed > _lastRun;

    /// <summary>
    /// Whether the resource of type <typeparamref name="T"/> was inserted since the running system
    /// last ran, as Bevy's <c>Res::is_added</c> answers.
    /// </summary>
    public bool IsAdded<T>() where T : notnull =>
        _ticks.TryGetValue(typeof(T), out var ticks) && ticks.Added > _lastRun;

    /// <summary>
    /// The file and line of the call that last changed the resource of type
    /// <typeparamref name="T"/>, as Bevy's <c>changed_by</c> gives it, or <see langword="null"/>
    /// where <see cref="TrackChangeLocations"/> is off or the resource was never changed.
    /// </summary>
    public string? ChangedBy<T>() where T : notnull => _ticks.TryGetValue(typeof(T), out var ticks) ? ticks.By : null;

    /// <summary>
    /// Replaces the resource of type <typeparamref name="T"/>, marked changed, and added as well
    /// where there was none.
    /// </summary>
    public void SetResource<T>(T value, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) where T : notnull
    {
        var added = !_resources.ContainsKey(typeof(T));
        _resources[typeof(T)] = value;
        Marked(typeof(T), added, file, line);
    }

    /// <summary>
    /// Replaces the resource of type <typeparamref name="T"/> only where it differs from the one
    /// held, as Bevy's <c>set_if_neq</c> does, and answers whether it did.
    /// </summary>
    /// <remarks>
    /// The two are compared by <see cref="EqualityComparer{T}.Default"/>, so a record's fields, or a
    /// class's own <see cref="object.Equals(object?)"/>, and by reference otherwise.
    /// </remarks>
    public bool SetResourceIfNeq<T>(T value, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0) where T : notnull
    {
        if (TryGetResource<T>(out var held) && EqualityComparer<T>.Default.Equals(held, value)) return false;
        SetResource(value, file, line);
        return true;
    }

    /// <summary>
    /// Marks the resource of type <typeparamref name="T"/> changed, after a field of the object it
    /// holds was written, which the world cannot see, as Bevy's <c>ResMut</c> marks it when written
    /// through.
    /// </summary>
    /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
    public void MarkChanged<T>([CallerFilePath] string file = "", [CallerLineNumber] int line = 0) where T : notnull
    {
        if (!_resources.ContainsKey(typeof(T)))
            throw new InvalidOperationException($"Resource '{typeof(T).Name}' is not registered, so there is nothing to mark changed.");
        Marked(typeof(T), added: false, file, line);
    }

    /// <summary>The place a call was made from, its file's name and its line, as a change names it.</summary>
    internal static string Place(string file, int line) => $"{Path.GetFileName(file)}:{line}";

    /// <summary>Starts a system's run, a tick of its own, weighed against the run before it.</summary>
    /// <returns>The outer run's ticks, which <see cref="LeaveSystem"/> puts back, since a system can run another from a schedule it runs.</returns>
    internal (long Tick, long LastRun) EnterSystem(long lastRun, out long tick)
    {
        var outer = (_tick, _lastRun);
        tick = _tick = ++_nextTick;
        _lastRun = lastRun;
        return outer;
    }

    /// <summary>Ends a system's run, the outer run's ticks put back.</summary>
    internal void LeaveSystem((long Tick, long LastRun) outer) => (_tick, _lastRun) = outer;

    private T Added<T>(Type type, T value) where T : notnull
    {
        Marked(type, added: true, null, 0);
        return value;
    }

    private void Marked(Type type, bool added, string? file, int line)
    {
        var by = TrackChangeLocations && file is not null ? Place(file, line) : null;
        _ticks.AddOrUpdate(type, (_tick, _tick, by), (_, ticks) => (added ? _tick : ticks.Added, _tick, by));
    }
}
