namespace Bevy;

/// <summary>A system: a function invoked once per stage run with the world.</summary>
/// <param name="world">The world the system operates on.</param>
public delegate void SystemFn(World world);

/// <summary>
/// A system plus the metadata the engine needs to schedule it.
/// </summary>
/// <remarks>
/// <para>
/// Every C# system reaches Bevy as an <em>exclusive</em> system, one that takes the whole
/// world. That is the only sound option while managed code can spawn, despawn and insert at
/// any moment, so Bevy serialises C# systems against each other rather than running them in
/// parallel. The parallelism that matters is still there: a behavior's per-entity loop is
/// fanned out across worker threads by the generated code, which is where the entity counts
/// actually are.
/// </para>
/// <para>
/// <see cref="Read{T}"/> and <see cref="Write{T}"/> therefore record intent rather than drive
/// scheduling today. They are honoured by <see cref="ConflictsWith"/>, which the diagnostics
/// overlay uses to explain ordering, and they are what a future non-exclusive fast path would
/// key off.
/// </para>
/// </remarks>
public sealed class SystemDescriptor
{
    private readonly HashSet<Type> _reads = [];
    private readonly HashSet<Type> _writes = [];

    /// <summary>A human-readable name, used in diagnostics and hot-reload bookkeeping.</summary>
    public string Name { get; }

    /// <summary>The function to invoke.</summary>
    public SystemFn System { get; }

    /// <summary>An optional predicate; the system is skipped for the frame when it is false.</summary>
    public Func<World, bool>? RunCondition { get; private set; }

    /// <summary>Provenance tag, used to remove a generation of systems on hot-reload.</summary>
    public string? Source { get; set; }

    /// <summary>Resource types this system reads.</summary>
    public IReadOnlyCollection<Type> Reads => _reads;

    /// <summary>Resource types this system writes.</summary>
    public IReadOnlyCollection<Type> Writes => _writes;

    /// <summary>True when the system declared any access at all.</summary>
    public bool HasExplicitAccess => _reads.Count > 0 || _writes.Count > 0;

    /// <summary>Wraps a system function.</summary>
    /// <param name="system">The function to invoke.</param>
    /// <param name="name">A display name; inferred from the method when omitted.</param>
    public SystemDescriptor(SystemFn system, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(system);
        System = system;
        Name = name ?? InferName(system);
    }

    private static string InferName(SystemFn system)
    {
        var method = system.Method;
        return $"{method.DeclaringType?.Name ?? "?"}.{method.Name}";
    }

    /// <summary>Attaches a run condition.</summary>
    public SystemDescriptor RunIf(Func<World, bool> condition)
    {
        RunCondition = condition;
        return this;
    }

    /// <summary>Declares a read of resource type <typeparamref name="T"/>.</summary>
    public SystemDescriptor Read<T>() where T : notnull
    {
        _reads.Add(typeof(T));
        return this;
    }

    /// <summary>Declares a write of resource type <typeparamref name="T"/>.</summary>
    public SystemDescriptor Write<T>() where T : notnull
    {
        _writes.Add(typeof(T));
        return this;
    }

    /// <summary>
    /// True when this system's declared access overlaps <paramref name="other"/>'s in a way
    /// that would prevent the two running concurrently.
    /// </summary>
    /// <remarks>
    /// Systems that declared no access at all are treated as broad writers, so an unannotated
    /// system never gets assumed to be safe alongside an annotated one.
    /// </remarks>
    public bool ConflictsWith(SystemDescriptor other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (!HasExplicitAccess || !other.HasExplicitAccess) return true;

        foreach (var type in _writes)
            if (other._writes.Contains(type) || other._reads.Contains(type))
                return true;

        foreach (var type in _reads)
            if (other._writes.Contains(type))
                return true;

        return false;
    }

    /// <summary>Runs the system, honouring <see cref="RunCondition"/>.</summary>
    /// <returns><see langword="true"/> if the system ran; <see langword="false"/> if skipped.</returns>
    public bool Invoke(World world)
    {
        if (RunCondition is { } condition && !condition(world)) return false;
        System(world);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Name;
}
