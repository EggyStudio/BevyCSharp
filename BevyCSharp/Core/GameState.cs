using System.Globalization;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Declares an enum to be a sub-state of another, existing only while that one holds a value.
/// </summary>
/// <remarks>
/// <para>
/// A pause that only means anything during a run is a sub-state of the run: leaving the run should
/// take the pause with it rather than leave a state nothing is looking at. While the parent holds
/// any other value the sub-state does not exist at all, and <see cref="App.TryState{TState}"/>
/// says so rather than answering with a default.
/// </para>
/// <para>
/// On the type rather than on the call that adds it, because which state a sub-state belongs to is
/// a property of the type in Bevy too, and because a behavior's <c>[OnEnter]</c> systems are
/// registered before an app has had the chance to add anything. A relationship declared here is
/// known whichever happens first.
/// </para>
/// <para>
/// A state carries a fixed number of sub-states, because Bevy names the parent as an associated
/// type and the types exist when the bridge is built. <see cref="StateRegistry.SubsPerSlot"/>
/// reports how many, and one past that is refused rather than half-worked.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public enum Screen { Menu, Playing }
///
/// [SubStateOf(typeof(Screen), Screen.Playing)]
/// public enum Pause { Off, On }
/// </code>
/// </example>
/// <param name="parent">The state enum this one lives inside.</param>
/// <param name="whileIn">The value of that state which this one exists under.</param>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class SubStateOfAttribute(Type parent, object whileIn) : Attribute
{
    /// <summary>The state enum this one lives inside.</summary>
    public Type Parent { get; } = parent;

    /// <summary>The value of that state which this one exists under.</summary>
    public object WhileIn { get; } = whileIn;
}

/// <summary>
/// Declares an enum as a state worked out from another rather than set.
/// </summary>
/// <remarks>
/// <para>
/// The third shape of state. A plain state is set, a sub-state exists while its parent holds one
/// value, and a computed state takes a value of its own from whatever its source holds. What that
/// is for is a fact that follows from another fact, such as whether the interface is up, which is
/// true on three screens and false on the rest. Writing it as a plain state leaves two facts to
/// keep in step, and one of them eventually lies.
/// </para>
/// <para>
/// The table is stated at <c>app.AddComputedState</c> rather than here, because it is a list of
/// pairs and an attribute carrying one would be a worse way to read the same thing. A source value
/// the table says nothing about means the computed state does not exist at all, so a system scoped
/// to it does not run and <c>App.TryState</c> answers false.
/// </para>
/// <para>
/// Setting one is always refused, because there is nothing to set. It changes when its source
/// does.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public enum Screen { Menu, Playing, Paused, Cutscene }
///
/// [ComputedFrom(typeof(Screen))]
/// public enum Hud { Hidden, Shown }
///
/// app.AddComputedState((Screen.Playing, Hud.Shown), (Screen.Paused, Hud.Shown));
/// </code>
/// </example>
/// <param name="source">The state enum this one is worked out from.</param>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class ComputedFromAttribute(Type source) : Attribute
{
    /// <summary>The state enum this one is worked out from.</summary>
    public Type Source { get; } = source;
}

/// <summary>
/// Maps C# enums onto Bevy's app states.
/// </summary>
/// <remarks>
/// <para>
/// A Bevy state is a Rust type, and C# cannot define one, so the bridge provides a fixed number of
/// state slots that each hold an integer and let the managed side decide what the numbers mean. An
/// enum claims a slot the first time it is added, which keeps two unrelated state machines apart.
/// Bevy keys its state resource and its transitions on the type, so two slots really are two
/// independent state machines.
/// </para>
/// <para>
/// Slots are per app, so a second <see cref="App"/> starts the assignment over.
/// </para>
/// </remarks>
public static unsafe class StateRegistry
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, int> Slots = [];
    private static int _generation = -1;
    private static int _next;

    /// <summary>How many independent state machines this bridge supports.</summary>
    public static int SlotCount => Native.bcs_state_slots();

    /// <summary>How many sub-states one state can carry.</summary>
    /// <remarks>
    /// Fixed by the bridge, because Bevy names a sub-state's parent as an associated type and the
    /// types exist when the crate is built. The sub-states of the state in slot <c>n</c> are the
    /// block of slots starting at <see cref="SlotCount"/> plus <c>n</c> times this.
    /// </remarks>
    public static int SubsPerSlot => Native.bcs_state_subs_per_slot();

    /// <summary>How many computed states one state can carry.</summary>
    /// <remarks>Fixed by the bridge for the same reason <see cref="SubsPerSlot"/> is.</remarks>
    public static int ComputedPerSlot => Native.bcs_state_computed_per_slot();

    /// <summary>How many sub-states exist in total, which is where computed slots start.</summary>
    internal static int SubCount => Native.bcs_state_sub_count();

    /// <summary>The slot <typeparamref name="TState"/> was added under.</summary>
    /// <exception cref="InvalidOperationException">It was never added.</exception>
    internal static int SlotOf<TState>() where TState : struct, Enum
    {
        lock (Gate)
        {
            Reset();
            if (Slots.TryGetValue(typeof(TState), out var slot)) return slot;

            throw new InvalidOperationException(
                $"No state of type {typeof(TState).Name} has been added. Call "
                + $"app.AddState({typeof(TState).Name}.<initial>) before the app runs, so Bevy "
                + "has somewhere to keep it and something to transition from.");
        }
    }

    /// <summary>The slot <typeparamref name="TState"/> holds, if it was ever added.</summary>
    internal static bool TryGetSlot<TState>(out int slot) where TState : struct, Enum
    {
        lock (Gate)
        {
            Reset();
            return Slots.TryGetValue(typeof(TState), out slot);
        }
    }

    /// <summary>
    /// The slot <typeparamref name="TState"/> holds, claiming one if it has none yet.
    /// </summary>
    /// <remarks>
    /// Registration cannot demand that the state already exists. A behavior's transition systems
    /// are registered when behaviors are discovered, which is before an app has had the chance to
    /// add its states, and requiring one order over the other would be a trap. Claiming here
    /// means a later <see cref="App.AddState{TState}"/> finds the same slot whichever came first.
    /// If it never comes, the transition never fires.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Every slot is taken.</exception>
    internal static int SlotForRegistration<TState>() where TState : struct, Enum => Claim<TState>();

    /// <summary>Claims a slot for <typeparamref name="TState"/>, or returns the one it holds.</summary>
    /// <exception cref="InvalidOperationException">Every slot is taken.</exception>
    internal static int Claim<TState>() where TState : struct, Enum => Claim(typeof(TState));

    /// <summary>
    /// The same, for a type known only at runtime.
    /// </summary>
    /// <remarks>
    /// Which is how a sub-state reaches its parent. The relationship is written as an attribute,
    /// so the parent arrives as a <see cref="Type"/> rather than as a type argument.
    /// </remarks>
    internal static int Claim(Type state)
    {
        lock (Gate)
        {
            Reset();
            if (Slots.TryGetValue(state, out var existing)) return existing;

            // A sub-state takes one of the slots set aside for its parent's rather than one of its
            // own, so the pairing the bridge is built around holds.
            if (Describe(state) is { } sub)
            {
                var parent = Claim(sub.Parent);
                var room = SubsPerSlot;
                var first = SlotCount + (parent * room);

                for (var offset = 0; offset < room; offset++)
                {
                    if (Slots.ContainsValue(first + offset)) continue;

                    Slots[state] = first + offset;
                    return first + offset;
                }

                throw new InvalidOperationException(
                    $"{sub.Parent.Name} already carries {room} sub-states, so {state.Name} "
                    + "cannot be another. Bevy names a sub-state's parent as a type, so how many "
                    + "a state can carry is fixed when the bridge is built.");
            }

            // A computed state takes one of the slots set aside for its source, past every
            // sub-state, which is how one number addresses all three shapes of state.
            if (DescribeComputed(state) is { } computed)
            {
                var source = Claim(computed.Source);
                var room = ComputedPerSlot;
                var first = SlotCount + SubCount + (source * room);

                for (var offset = 0; offset < room; offset++)
                {
                    if (Slots.ContainsValue(first + offset)) continue;

                    Slots[state] = first + offset;
                    return first + offset;
                }

                throw new InvalidOperationException(
                    $"{computed.Source.Name} already has {room} states computed from it, so "
                    + $"{state.Name} cannot be another. Bevy names a computed state's source as a "
                    + "type, so how many one state can carry is fixed when the bridge is built.");
            }

            var count = SlotCount;
            if (_next >= count)
                throw new InvalidOperationException(
                    $"All {count} state slots are in use, so {state.Name} cannot have "
                    + "one. Independent state machines are rarer than they look, because a pause that "
                    + "only matters while playing is a value of the state it belongs to, not a "
                    + "second machine beside it.");

            var next = _next++;
            Slots[state] = next;
            return next;
        }
    }

    /// <summary>
    /// What an enum says about being a sub-state, or null when it says nothing.
    /// </summary>
    /// <remarks>
    /// Refuses a parent that is not an enum, and one that is itself a sub-state. The first is a
    /// mistake the compiler cannot catch, because the attribute takes a <see cref="Type"/>; the
    /// second is a chain of sub-states, which Bevy allows and this bridge's fixed pairing does
    /// not.
    /// </remarks>
    internal static SubStateOfAttribute? Describe(Type state)
    {
        var sub = (SubStateOfAttribute?)Attribute.GetCustomAttribute(
            state, typeof(SubStateOfAttribute));

        if (sub is null) return null;

        if (!sub.Parent.IsEnum)
            throw new InvalidOperationException(
                $"{state.Name} names {sub.Parent.Name} as its parent state, which is not an "
                + "enum. A state is an enum, so a sub-state's parent is one too.");

        if (Attribute.IsDefined(sub.Parent, typeof(SubStateOfAttribute)))
            throw new InvalidOperationException(
                $"{state.Name} is a sub-state of {sub.Parent.Name}, which is itself a sub-state. "
                + "The bridge pairs one sub-state with one state, so a chain of them has nowhere "
                + "to live.");

        return sub;
    }

    /// <summary>
    /// What an enum says about being computed from another, or null when it says nothing.
    /// </summary>
    /// <remarks>
    /// Refuses a source that is not an enum, and one that is itself computed. The first is a
    /// mistake the compiler cannot catch, because the attribute takes a <see cref="Type"/>; the
    /// second is a chain, which needs a slot layout this bridge does not have.
    /// </remarks>
    internal static ComputedFromAttribute? DescribeComputed(Type state)
    {
        var computed = (ComputedFromAttribute?)Attribute.GetCustomAttribute(
            state, typeof(ComputedFromAttribute));

        if (computed is null) return null;

        if (!computed.Source.IsEnum)
            throw new InvalidOperationException(
                $"{state.Name} names {computed.Source.Name} as its source, which is not an enum. "
                + "A state is an enum, so what one is computed from is one too.");

        if (Attribute.IsDefined(computed.Source, typeof(ComputedFromAttribute)))
            throw new InvalidOperationException(
                $"{state.Name} is computed from {computed.Source.Name}, which is itself computed. "
                + "The bridge sets a computed state's source aside when it is built, so a chain "
                + "of them has nowhere to live.");

        return computed;
    }

    /// <summary>Drops every assignment when a new app is created.</summary>
    private static void Reset()
    {
        if (_generation == ComponentRegistry.Generation) return;

        Slots.Clear();
        _next = 0;
        _generation = ComponentRegistry.Generation;
    }

    /// <summary>The current value of <typeparamref name="TState"/>.</summary>
    /// <remarks>
    /// Reads the live value, so it reflects a transition only once Bevy has applied it, not the
    /// moment it was requested. Needs a world loan, so it is only valid inside a system.
    /// </remarks>
    /// <exception cref="BevyNativeException">No world is loaned, or the state is missing.</exception>
    public static TState Current<TState>() where TState : struct, Enum
    {
        int value;
        Native.Check(
            Native.bcs_state_get(SlotOf<TState>(), &value),
            $"reading state {typeof(TState).Name}");

        return Unsafe.As<int, TState>(ref value);
    }

    /// <summary>The current value of <typeparamref name="TState"/>, left as the raw number.</summary>
    /// <remarks>
    /// What a run condition compares against. Converting to the enum and back again would be two
    /// conversions per system per frame to answer a question about two integers.
    /// </remarks>
    internal static int CurrentRaw<TState>() where TState : struct, Enum
    {
        int value;
        Native.Check(
            Native.bcs_state_get(SlotOf<TState>(), &value),
            $"reading state {typeof(TState).Name}");

        return value;
    }

    /// <summary>
    /// The raw current value, reporting whether <typeparamref name="TState"/> exists at all.
    /// </summary>
    /// <remarks>
    /// For a caller that runs every frame and cannot afford to throw once per frame if the state
    /// was never added.
    /// </remarks>
    internal static bool TryCurrentRaw<TState>(out int value) where TState : struct, Enum
    {
        value = 0;
        if (!TryGetSlot<TState>(out var slot)) return false;

        int read;
        var status = Native.bcs_state_get(slot, &read);

        // Not there is an answer rather than a failure. A sub-state whose parent is elsewhere does
        // not exist at all, and a state that claimed a slot when a behavior registered but was
        // never added has none either. Both mean the system is not in that state, which answers the
        // caller.
        if (status == NativeStatus.NotPresent) return false;

        Native.Check(status, $"reading state {typeof(TState).Name}");

        value = read;
        return true;
    }

    /// <summary>Whether this enum is declared as a sub-state of another.</summary>
    internal static bool IsSub<TState>() where TState : struct, Enum =>
        Attribute.IsDefined(typeof(TState), typeof(SubStateOfAttribute));

    /// <summary>
    /// The current value of <typeparamref name="TState"/>, reporting whether it exists at all.
    /// </summary>
    /// <remarks>
    /// What a sub-state needs. While its parent holds another value there is no state, and the
    /// honest answer to "which value is it in" is that it is in none. A plain state that was never
    /// added answers the same way.
    /// </remarks>
    /// <param name="value">The value, when this returns true.</param>
    /// <returns>Whether the state exists right now.</returns>
    public static bool TryCurrent<TState>(out TState value) where TState : struct, Enum
    {
        value = default;

        if (!TryGetSlot<TState>(out var slot)) return false;

        int read;
        var status = Native.bcs_state_get(slot, &read);

        // Missing is an answer here rather than a failure, which is the whole point of asking this
        // way. Anything else is still a failure worth reporting.
        if (status == NativeStatus.NotPresent) return false;

        Native.Check(status, $"reading state {typeof(TState).Name}");

        value = Unsafe.As<int, TState>(ref read);
        return true;
    }

    /// <summary>
    /// Asks Bevy to move <typeparamref name="TState"/> to <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// Queued rather than immediate. Bevy applies it at the next transition point, so every system
    /// in a frame agrees on which state it is in rather than seeing the change halfway through.
    /// </remarks>
    public static void Set<TState>(TState value) where TState : struct, Enum =>
        Native.Check(
            Native.bcs_state_set(SlotOf<TState>(), ToInt(value)),
            $"setting state {typeof(TState).Name}");

    /// <summary>The enum's underlying value, which the bridge stores.</summary>
    /// <remarks>
    /// Every slot holds an <see cref="int"/>, so an enum with a wider underlying type would be
    /// truncated. Refused rather than truncated, because the values would silently collide. A
    /// narrower one is widened by <see cref="Convert"/>, which keeps the sign.
    /// </remarks>
    internal static int ToInt<TState>(TState value) where TState : struct, Enum
    {
        if (Unsafe.SizeOf<TState>() > sizeof(int))
            throw new InvalidOperationException(
                $"{typeof(TState).Name} is backed by a type wider than int, which a state slot "
                + "cannot hold. Declare it over int or a narrower type.");

        // Goes through Convert rather than reinterpreting the bytes, which would widen a signed
        // narrow enum wrongly, since a byte-backed member of -1 would arrive as 255. Invariant,
        // because what is being converted is a number and a machine's regional settings have no
        // business in it.
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }
}
