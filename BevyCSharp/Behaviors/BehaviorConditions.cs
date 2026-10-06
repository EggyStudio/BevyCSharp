namespace Bevy;

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

    /// <summary>
    /// Passes while <typeparamref name="TState"/> holds <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What <c>[InState]</c> emits. The state is read per system run rather than cached, because
    /// a transition applied this frame has to take effect this frame.
    /// </para>
    /// <para>
    /// A state that was never added reads as "not in it", so the system does not run. Throwing
    /// would be the louder answer, but this is evaluated once per system per frame, and the
    /// report would repeat for as long as the app ran. It is written to standard error once
    /// instead, for the state rather than for each system scoped to it, naming the first.
    /// </para>
    /// <para>
    /// A sub-state reads the same way while its parent holds another value, and a computed state
    /// while its source holds a value it does not exist under. That is not worth reporting, because
    /// it is the whole point of either rather than a mistake.
    /// </para>
    /// <para>
    /// The state's slot is claimed here, so a state declared with <see cref="InitialStateAttribute"/>
    /// is added when the app runs even when no system enters or leaves it.
    /// </para>
    /// </remarks>
    public static Func<World, bool> InState<TState>(TState value) where TState : struct, Enum
    {
        var wanted = StateRegistry.ToInt(value);
        var reported = false;

        // Claimed as a transition's systems claim theirs, so a state declared on its enum is one an
        // app adds as it starts when nothing but a condition like this one names it.
        StateRegistry.SlotForRegistration<TState>();

        return _ =>
        {
            if (StateRegistry.TryCurrentRaw<TState>(out var current)) return current == wanted;

            if (!reported && !StateRegistry.ComesAndGoes<TState>() && (StateRegistry.ReportUnentered || !StateRegistry.IsDeclared<TState>()))
            {
                reported = true;
                if (!StateRegistry.FirstReportOf<TState>()) return false;

                // A state declared on its enum and still not added was declared by an assembly
                // loaded once the app was running, such as a script the editor loads, which no
                // AddState call in a program can be told to fix.
                var state = typeof(TState).Name;
                Console.Error.WriteLine(StateRegistry.IsDeclared<TState>()
                    ? $"[BevyCSharp] A system is scoped to {state}.{value}, and {state} is declared with "
                      + "[InitialState] but was loaded after the app started, so it was not added, and no "
                      + $"system scoped to {state} will run until an app is started with it."
                    : $"[BevyCSharp] A system is scoped to {state}.{value}, but no state of type {state} "
                      + $"was added, so it and every other system scoped to {state} will never run. Call "
                      + $"app.AddState({state}.<initial>) before running the app.");
            }

            return false;
        };
    }

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
    /// <param name="key">The key whose press flips the system.</param>
    /// <param name="modifiers">Modifiers that must be held; combine them with <c>|</c>.</param>
    /// <param name="defaultEnabled">Whether the system starts enabled.</param>
    public static Func<World, bool> KeyToggle<TTag>(
        Key key,
        KeyModifier modifiers = KeyModifier.None,
        bool defaultEnabled = true) where TTag : notnull =>
        KeyToggle(typeof(TTag).FullName!, key, modifiers, defaultEnabled);

    /// <summary>
    /// A keyboard toggle keyed by an explicit id. Generated code uses this overload; prefer the
    /// generic one in hand-written code.
    /// </summary>
    /// <remarks>
    /// The flip happens on the frame <paramref name="key"/> goes down while the modifiers are
    /// already held. That is the same shape as Bevy's own <c>input_toggle_active</c>, plus the
    /// modifier check Bevy does not offer.
    /// </remarks>
    public static Func<World, bool> KeyToggle(
        string systemId,
        Key key,
        KeyModifier modifiers = KeyModifier.None,
        bool defaultEnabled = true)
    {
        return world =>
        {
            var registry = world.GetOrInsertResource(static () => new SystemToggleRegistry());

            if (world.TryGetResource<Input>(out var input)
                && input.KeyPressed(key)
                && ModifiersHeld(input, modifiers))
            {
                registry.Flip(systemId, defaultEnabled);
            }

            return registry.Get(systemId, defaultEnabled);
        };
    }

    /// <summary>
    /// True when every modifier set in <paramref name="modifiers"/> is currently held.
    /// </summary>
    /// <remarks>
    /// Flags are ANDed, so <c>Ctrl | Shift</c> requires both. Each flag is satisfied by either side
    /// of the keyboard, as a shortcut normally is; to pin one side, name that key directly instead.
    /// </remarks>
    public static bool ModifiersHeld(Input input, KeyModifier modifiers)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (modifiers == KeyModifier.None) return true;

        foreach (var (flag, keys) in ModifierKeys)
            if ((modifiers & flag) != 0 && !input.AnyKeyDown(keys))
                return false;

        return true;
    }

    /// <summary>The keys that satisfy each modifier flag, either side counting.</summary>
    private static readonly (KeyModifier Flag, Key[] Keys)[] ModifierKeys =
    [
        (KeyModifier.Ctrl, [Key.ControlLeft, Key.ControlRight]),
        (KeyModifier.Shift, [Key.ShiftLeft, Key.ShiftRight]),
        (KeyModifier.Alt, [Key.AltLeft, Key.AltRight]),
        (KeyModifier.Super, [Key.SuperLeft, Key.SuperRight]),
    ];
}
