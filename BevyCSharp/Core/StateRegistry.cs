using System.Globalization;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

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
/// <para>
/// A state is known by its enum's full name rather than by the type itself, so a script reloaded
/// into a running app, whose enum is a new type of the same name in a new assembly, reads and
/// changes the state the app was started with instead of one nothing ever added.
/// </para>
/// </remarks>
public static unsafe class StateRegistry
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Type, int> Slots = new(SameState.Instance);

    // The states the running app has been told it lacks, so it is told once for each.
    private static readonly HashSet<Type> Reported = new(SameState.Instance);

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

    /// <summary>How many computed states exist in total, which is where joint slots start.</summary>
    internal static int ComputedCount => Native.bcs_state_computed_count();

    /// <summary>How many joint states this bridge has, each worked out from several states at once.</summary>
    /// <remarks>
    /// Fixed by the bridge as every other count is, though a joint's sources are not, since a joint
    /// is worked out from every state slot and its rule reads the ones it was given.
    /// </remarks>
    public static int JointCount => Native.bcs_state_joint_count();

    /// <summary>Where joint slots start, past every state, sub-state and computed state.</summary>
    internal static int FirstJoint => SlotCount + SubCount + ComputedCount;

    /// <summary>How many sub-states of several states at once this bridge has.</summary>
    /// <remarks>Fixed by the bridge, though which states each lives inside is not, as for joints.</remarks>
    public static int JointSubCount => Native.bcs_state_joint_sub_count();

    /// <summary>Where the slots of sub-states of several states start, past every joint.</summary>
    internal static int FirstJointSub => FirstJoint + JointCount;

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

    /// <summary>Every state declared with <see cref="InitialStateAttribute"/>, in the order it was declared.</summary>
    private static readonly List<(Type State, bool Sub, Action<App> Add)> Declared = [];

    /// <summary>
    /// Declares <typeparamref name="TState"/> a state starting at <paramref name="initial"/>, which an
    /// app that uses it adds as it starts to run.
    /// </summary>
    /// <remarks>Called by the code the generator emits for <see cref="InitialStateAttribute"/>.</remarks>
    public static void Declare<TState>(TState initial) where TState : struct, Enum
    {
        lock (Gate)
        {
            Declared.RemoveAll(declared => SameState.Instance.Equals(declared.State, typeof(TState)));

            var sub = Attribute.IsDefined(typeof(TState), typeof(SubStateOfAttribute));
            Declared.Add((typeof(TState), sub, app =>
            {
                if (sub) app.AddSubState(initial);
                else app.AddState(initial);
            }));
        }
    }

    /// <summary>Whether <typeparamref name="TState"/> was declared with <see cref="InitialStateAttribute"/>.</summary>
    internal static bool IsDeclared<TState>() where TState : struct, Enum
    {
        lock (Gate) return Declared.Any(declared => SameState.Instance.Equals(declared.State, typeof(TState)));
    }

    /// <summary>
    /// The declared states an app has to add, those a system of its claimed a slot for and it has
    /// not added, parents before the sub-states that live in them.
    /// </summary>
    internal static List<(Type State, Action<App> Add)> DeclaredFor(IReadOnlySet<Type> added)
    {
        lock (Gate)
        {
            Reset();
            return
            [
                .. Declared
                    .Where(declared => Slots.ContainsKey(declared.State) && !added.Contains(declared.State))
                    .OrderBy(declared => declared.Sub)
                    .Select(declared => (declared.State, declared.Add)),
            ];
        }
    }

    /// <summary>
    /// Whether a system scoped to a state declared on its enum and never added says so, once for
    /// the state, on the console.
    /// </summary>
    /// <remarks>
    /// On, since a state a game declares and never has is usually a mistake. An app that loads a
    /// game's scripts and never plays them, as the editor does while a level is edited, turns it
    /// off and says once what it does not enter (<see cref="Unentered"/>) rather than once a system.
    /// </remarks>
    public static bool ReportUnentered { get; set; } = true;

    /// <summary>The states declared on their enums that the running app has not added, by name.</summary>
    /// <remarks>
    /// Only valid inside a system, since whether a state exists is asked of the world. A sub-state
    /// while its parent holds another value is not there either, and is listed with the rest.
    /// </remarks>
    public static IReadOnlyList<string> Unentered()
    {
        List<Type> declared;
        lock (Gate)
        {
            Reset();
            declared = [.. Declared.Select(state => state.State)];
        }

        var names = new List<string>();
        foreach (var state in declared)
        {
            bool claimed;
            int slot;
            lock (Gate) claimed = Slots.TryGetValue(state, out slot);

            int read;
            if (!claimed || Native.bcs_state_get(slot, &read) == NativeStatus.NotPresent) names.Add(state.Name);
        }

        return names;
    }

    /// <summary>The states that have a slot in the running app, each with its slot.</summary>
    /// <remarks>
    /// For the console, where a state is named by its enum's name as a person types it and so is
    /// not a type parameter. A state that claimed a slot for a system and was never added is among
    /// them, and its value cannot be read.
    /// </remarks>
    internal static List<(Type State, int Slot)> Claimed()
    {
        lock (Gate)
        {
            Reset();
            return [.. Slots.Select(pair => (pair.Key, pair.Value))];
        }
    }

    /// <summary>
    /// Whether the running app is yet to be told that <typeparamref name="TState"/> was never added,
    /// counting this as the telling.
    /// </summary>
    /// <remarks>
    /// Once an app for each state rather than once a system, since a game with many systems scoped
    /// to a state it forgot to add made one mistake, and a line for each of them would bury the
    /// one that says what it was. A test suite whose apps run every behavior it declares and add
    /// the state in only some of them would read the same line over and over as well.
    /// </remarks>
    internal static bool FirstReportOf<TState>() where TState : struct, Enum
    {
        lock (Gate)
        {
            Reset();
            return Reported.Add(typeof(TState));
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
    internal static int Claim<TState>() where TState : struct, Enum
    {
        // How this enum's transitions reach the message bus, kept by its type for every app,
        // since a slot is known only by its number when the bridge reports what it did.
        lock (Gate)
        {
            Posters.TryAdd(typeof(TState), static (bus, ecs, exited, entered) =>
            {
                var transition = new StateTransitionEvent<TState>(
                    exited is { } from ? FromInt<TState>(from) : null,
                    entered is { } to ? FromInt<TState>(to) : null);
                bus.Send(transition);
                StateDespawnRules<TState>.Apply(ecs, transition);
            });
        }

        return Claim(typeof(TState));
    }

    private static readonly Dictionary<Type, Action<MessageBus, EcsWorld, int?, int?>> Posters = new(SameState.Instance);

    /// <summary>Moves the transitions every state made since the last frame onto the message bus.</summary>
    /// <remarks>
    /// Bevy reports each as a <c>StateTransitionEvent</c> of the state's type, a slot's here, which
    /// the bridge drains slot by slot. Each is posted as a <see cref="StateTransitionEvent{TState}"/>
    /// of the enum that holds the slot, found by its number among this app's slots. A slot no enum
    /// was claimed for by its type, which no state a game adds is, has nothing to post as. The
    /// entities <see cref="EcsWorld.DespawnWhen{TState}"/> left waiting on a rule are despawned
    /// as their rule answers true.
    /// </remarks>
    internal static void PostTransitions(MessageBus bus, EcsWorld ecs)
    {
        const int Capacity = 32;
        NativeStateTransition* buffer = stackalloc NativeStateTransition[Capacity];

        int count;
        do
        {
            count = Native.bcs_state_transitions(buffer, Capacity);
            for (var i = 0; i < count; i++)
            {
                var transition = buffer[i];
                Action<MessageBus, EcsWorld, int?, int?>? post = null;
                lock (Gate)
                {
                    foreach (var (state, slot) in Slots)
                    {
                        if (slot == transition.Slot && Posters.TryGetValue(state, out post)) break;
                    }
                }

                post?.Invoke(
                    bus,
                    ecs,
                    (transition.Flags & 1) != 0 ? transition.Exited : null,
                    (transition.Flags & 2) != 0 ? transition.Entered : null);
            }
        }
        while (count == Capacity);
    }

    /// <summary>The enum value a slot's number stands for.</summary>
    private static TState FromInt<TState>(int value) where TState : struct, Enum => Unsafe.As<int, TState>(ref value);

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

            // A sub-state of several states takes one of the slots set aside for those, whichever
            // states they are, since each is fed every state slot and checks the ones it names.
            if (DescribeAll(state) is { Length: > 1 } parents)
            {
                foreach (var parent in parents) Claim(parent.Parent);

                var room = JointSubCount;
                var first = FirstJointSub;

                for (var offset = 0; offset < room; offset++)
                {
                    if (Slots.ContainsValue(first + offset)) continue;

                    Slots[state] = first + offset;
                    return first + offset;
                }

                throw new InvalidOperationException(
                    $"All {room} sub-states of several states are in use, so {state.Name} cannot be "
                    + "another. Each is a Rust type, so how many there are is fixed when the bridge "
                    + "is built.");
            }

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

            // A joint takes one of the joint slots, past every computed state, whichever states
            // it is worked out from, since a joint is fed every state slot and reads its own.
            if (DescribeComputed(state) is { IsJoint: true } joint)
            {
                foreach (var source in joint.Sources) Claim(source);

                var joints = JointCount;
                var first = FirstJoint;

                for (var offset = 0; offset < joints; offset++)
                {
                    if (Slots.ContainsValue(first + offset)) continue;

                    Slots[state] = first + offset;
                    return first + offset;
                }

                throw new InvalidOperationException(
                    $"All {joints} joint states are in use, so {state.Name} cannot be another. A "
                    + "joint is a Rust type, so how many there are is fixed when the bridge is built.");
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
    internal static SubStateOfAttribute? Describe(Type state) =>
        DescribeAll(state) is { Length: 1 } one ? one[0] : null;

    /// <summary>
    /// Every parent an enum names as a sub-state, empty when it names none, with each checked as
    /// <see cref="Describe"/> checks one.
    /// </summary>
    /// <remarks>
    /// Several parents also refuse one that is computed, since a sub-state of several states is fed
    /// the state slots and a computed state has none of its own, and the same parent named twice.
    /// </remarks>
    internal static SubStateOfAttribute[] DescribeAll(Type state)
    {
        var subs = (SubStateOfAttribute[])Attribute.GetCustomAttributes(state, typeof(SubStateOfAttribute));

        foreach (var sub in subs)
        {
            if (!sub.Parent.IsEnum)
                throw new InvalidOperationException(
                    $"{state.Name} names {sub.Parent.Name} as its parent state, which is not an "
                    + "enum. A state is an enum, so a sub-state's parent is one too.");

            if (Attribute.IsDefined(sub.Parent, typeof(SubStateOfAttribute)))
                throw new InvalidOperationException(
                    $"{state.Name} is a sub-state of {sub.Parent.Name}, which is itself a sub-state. "
                    + "The bridge pairs one sub-state with one state, so a chain of them has nowhere "
                    + "to live.");

            if (subs.Length > 1 && Attribute.IsDefined(sub.Parent, typeof(ComputedFromAttribute)))
                throw new InvalidOperationException(
                    $"{state.Name} is a sub-state of {sub.Parent.Name}, which is computed. A "
                    + "sub-state of several states lives inside states of their own, and a computed "
                    + "state is not one.");
        }

        if (subs.Select(sub => sub.Parent).Distinct().Count() != subs.Length)
            throw new InvalidOperationException($"{state.Name} names the same parent state twice.");

        return subs;
    }

    /// <summary>
    /// What an enum says about being computed from another, or null when it says nothing.
    /// </summary>
    /// <remarks>
    /// Refuses a source that is not an enum, and one that is itself computed unless a joint reads a
    /// state computed from one state. The first is a mistake the compiler cannot catch, because the
    /// attribute takes a <see cref="Type"/>. The second is a chain, whose computed states sit in
    /// slots set aside for a state, which a computed state does not have. A joint is fed the state
    /// slots instead, and reads a state computed from one of them by working it out from that
    /// state's value.
    /// </remarks>
    internal static ComputedFromAttribute? DescribeComputed(Type state)
    {
        var computed = (ComputedFromAttribute?)Attribute.GetCustomAttribute(
            state, typeof(ComputedFromAttribute));

        if (computed is null) return null;

        foreach (var source in computed.Sources)
        {
            if (!source.IsEnum)
                throw new InvalidOperationException(
                    $"{state.Name} names {source.Name} as its source, which is not an enum. "
                    + "A state is an enum, so what one is computed from is one too.");

            if (Attribute.GetCustomAttribute(source, typeof(ComputedFromAttribute)) is ComputedFromAttribute inner)
            {
                if (!computed.IsJoint)
                    throw new InvalidOperationException(
                        $"{state.Name} is computed from {source.Name}, which is itself computed. "
                        + "The bridge sets a computed state's source aside when it is built, so a "
                        + "chain of them has nowhere to live.");

                if (inner.IsJoint || !IsPlainState(inner.Source))
                    throw new InvalidOperationException(
                        $"{state.Name} is worked out from {source.Name}, which is computed from "
                        + $"{Named(inner)}. A state worked out from several reads a computed one "
                        + "through the one state it is computed from.");
            }

            // A joint is fed the state slots, and a sub-state lives in a slot of its parent's.
            if (computed.IsJoint && Attribute.IsDefined(source, typeof(SubStateOfAttribute)))
                throw new InvalidOperationException(
                    $"{state.Name} is worked out from {source.Name}, which is a sub-state. A state "
                    + "worked out from several reads states of their own, and a sub-state is not one.");
        }

        if (computed.Sources.Distinct().Count() != computed.Sources.Count)
            throw new InvalidOperationException(
                $"{state.Name} names the same state twice among what it is worked out from.");

        return computed;
    }

    // A state of its own, neither computed nor living inside another, the kind a slot holds.
    private static bool IsPlainState(Type state) =>
        state.IsEnum
        && !Attribute.IsDefined(state, typeof(ComputedFromAttribute))
        && !Attribute.IsDefined(state, typeof(SubStateOfAttribute));

    private static string Named(ComputedFromAttribute computed) => string.Join(" and ", computed.Sources.Select(source => source.Name));

    /// <summary>Drops every assignment when a new app is created.</summary>
    private static void Reset()
    {
        if (_generation == ComponentRegistry.Generation) return;

        Slots.Clear();
        Reported.Clear();
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

    /// <summary>
    /// Whether this enum exists only while other states hold values, as a sub-state or a computed
    /// state does, so its absence is how it works rather than a state never added.
    /// </summary>
    internal static bool ComesAndGoes<TState>() where TState : struct, Enum =>
        Attribute.IsDefined(typeof(TState), typeof(SubStateOfAttribute))
        || Attribute.IsDefined(typeof(TState), typeof(ComputedFromAttribute));

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

/// <summary>Two state enums as one when their full names match, which a reloaded script's are.</summary>
internal sealed class SameState : IEqualityComparer<Type>
{
    /// <summary>The one there is.</summary>
    public static readonly SameState Instance = new();

    /// <inheritdoc/>
    public bool Equals(Type? x, Type? y) =>
        ReferenceEquals(x, y) || (x is not null && y is not null && string.Equals(x.FullName, y.FullName, StringComparison.Ordinal));

    /// <inheritdoc/>
    public int GetHashCode(Type obj) => StringComparer.Ordinal.GetHashCode(obj.FullName ?? obj.Name);
}
