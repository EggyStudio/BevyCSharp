namespace Bevy;

/// <summary>
/// Per-system on/off state for <see cref="ToggleKeyAttribute"/>, keyed by system name.
/// </summary>
/// <remarks>
/// Stored as a world resource so the state survives across frames and can be inspected or
/// driven from elsewhere - a debug menu, a save file, a test.
/// </remarks>
public sealed class SystemToggleRegistry
{
    private readonly Dictionary<string, bool> _states = [];

    /// <summary>The state for <paramref name="id"/>, or <paramref name="defaultEnabled"/> if unset.</summary>
    public bool Get(string id, bool defaultEnabled = true) =>
        _states.TryGetValue(id, out var value) ? value : defaultEnabled;

    /// <summary>Sets the state for <paramref name="id"/>.</summary>
    public void Set(string id, bool enabled) => _states[id] = enabled;

    /// <summary>Flips the state for <paramref name="id"/>.</summary>
    public void Flip(string id, bool defaultEnabled = true) =>
        _states[id] = !Get(id, defaultEnabled);

    /// <summary>Every recorded toggle, for diagnostics.</summary>
    public IReadOnlyDictionary<string, bool> States => _states;
}

/// <summary>
/// Ready-made run conditions for <see cref="SystemDescriptor.RunIf"/> and
/// <see cref="RunIfAttribute"/>.
/// </summary>
public static class BehaviorConditions
{
    /// <summary>Passes while a resource of type <typeparamref name="T"/> exists.</summary>
    public static Func<World, bool> HasResource<T>() where T : notnull =>
        static world => world.ContainsResource<T>();

    /// <summary>Passes while a resource exists and satisfies <paramref name="predicate"/>.</summary>
    public static Func<World, bool> ResourceIs<T>(Func<T, bool> predicate) where T : notnull
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return world => world.TryGetResource<T>(out var resource) && predicate(resource);
    }

    /// <summary>Passes while at least one entity carries <typeparamref name="T"/>.</summary>
    public static Func<World, bool> AnyWithComponent<T>() where T : unmanaged =>
        static world => world.Resource<EcsWorld>().Count<T>() > 0;

    /// <summary>Passes only on the first frame.</summary>
    public static Func<World, bool> RunOnce()
    {
        var done = false;
        return _ =>
        {
            if (done) return false;
            done = true;
            return true;
        };
    }

    /// <summary>
    /// A keyboard toggle keyed by <typeparamref name="TTag"/>, usually the behavior struct.
    /// </summary>
    public static Func<World, bool> KeyToggle<TTag>(
        Key key,
        KeyModifier modifier = KeyModifier.None,
        bool defaultEnabled = true) where TTag : notnull =>
        KeyToggle(typeof(TTag).FullName!, key, modifier, defaultEnabled);

    /// <summary>
    /// A keyboard toggle keyed by an explicit id. Generated code uses this overload; prefer the
    /// generic one in hand-written code.
    /// </summary>
    public static Func<World, bool> KeyToggle(
        string systemId,
        Key key,
        KeyModifier modifier = KeyModifier.None,
        bool defaultEnabled = true)
    {
        return world =>
        {
            var registry = world.GetOrInsertResource(static () => new SystemToggleRegistry());

            if (world.TryGetResource<Input>(out var input)
                && input.KeyPressed(key)
                && ModifiersHeld(input, modifier))
            {
                registry.Flip(systemId, defaultEnabled);
            }

            return registry.Get(systemId, defaultEnabled);
        };
    }

    /// <summary>True when every modifier in <paramref name="modifier"/> is currently held.</summary>
    public static bool ModifiersHeld(Input input, KeyModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (modifier == KeyModifier.None) return true;

        if (modifier.HasFlag(KeyModifier.Ctrl)
            && !input.KeyDown(Key.ControlLeft) && !input.KeyDown(Key.ControlRight)) return false;
        if (modifier.HasFlag(KeyModifier.Shift)
            && !input.KeyDown(Key.ShiftLeft) && !input.KeyDown(Key.ShiftRight)) return false;
        if (modifier.HasFlag(KeyModifier.Alt)
            && !input.KeyDown(Key.AltLeft) && !input.KeyDown(Key.AltRight)) return false;
        if (modifier.HasFlag(KeyModifier.Super)
            && !input.KeyDown(Key.SuperLeft) && !input.KeyDown(Key.SuperRight)) return false;

        return true;
    }
}

/// <summary>
/// Tags every system registered inside the scope with a provenance string.
/// </summary>
/// <remarks>
/// This is what makes hot-reload swappable: a reloaded generation of behaviors registers under
/// its own tag, and <see cref="App.RemoveSystemsBySource"/> retires the previous one without
/// touching systems that came from anywhere else.
/// </remarks>
public sealed class SystemRegistrationSourceScope : IDisposable
{
    [ThreadStatic] private static string? _current;

    private readonly string? _previous;

    /// <summary>The tag in force on this thread, if any.</summary>
    public static string? Current => _current;

    /// <summary>Applies <paramref name="source"/> until disposed.</summary>
    public SystemRegistrationSourceScope(string source)
    {
        _previous = _current;
        _current = source;
    }

    /// <inheritdoc/>
    public void Dispose() => _current = _previous;
}
