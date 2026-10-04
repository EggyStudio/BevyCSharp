using System.Globalization;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Declares an enum to be a sub-state of another, existing only while that one holds a value.
/// </summary>
/// <remarks>
/// <para>
/// A pause that only means anything during a run is a sub-state of the run, because leaving the run
/// should take the pause with it rather than leave a state nothing is looking at. While the parent
/// holds any other value the sub-state does not exist at all, and
/// <see cref="App.TryState{TState}"/> says so rather than answering with a default.
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
/// <para>
/// Put on an enum more than once, it makes a sub-state of every state it names, which exists only
/// while each of them holds the value named, such as a pause that means something only while
/// playing online. Those are kept apart from the sub-states of one state, in a fixed set of their
/// own (<see cref="StateRegistry.JointSubCount"/>), each fed every state, so which states one
/// lives inside is chosen by the game.
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
[AttributeUsage(AttributeTargets.Enum, AllowMultiple = true)]
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
/// <param name="more">
/// Further states it is worked out from at once, which makes it a joint state, worked out by a
/// rule of the game's own over all of them (<c>app.AddComputedState&lt;T, A, B&gt;(rule)</c>).
/// </param>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class ComputedFromAttribute(Type source, params Type[] more) : Attribute
{
    /// <summary>The state enum this one is worked out from, the first of several for a joint.</summary>
    public Type Source { get; } = source;

    /// <summary>Every state enum this one is worked out from, in the order they were named.</summary>
    public IReadOnlyList<Type> Sources { get; } = [source, .. more];

    /// <summary>Whether this is worked out from more than one state, as a joint state is.</summary>
    public bool IsJoint => Sources.Count > 1;
}

/// <summary>
/// Declares an enum to be a state and where it starts, so an app that uses it adds it by itself.
/// </summary>
/// <remarks>
/// <para>
/// What <c>app.AddState(Mode.Menu)</c> says, said on the enum instead, for a state declared in a
/// behavior script, which has no <c>Program.cs</c> to add it from and is compiled by a host the
/// game, the editor's Play and the player all differ in. An app adds each declared state that one
/// of its systems uses, through <c>[InState]</c>, <c>[OnEnter]</c> or <c>[OnExit]</c>, as it starts to
/// run, and leaves one it already added alone. A sub-state (<see cref="SubStateOfAttribute"/>) is
/// added after its parents, starting at the value given each time it comes into existence.
/// </para>
/// <para>
/// Registered by the source generator from a module initializer, so nothing reflects. A computed
/// state is not declared this way, since it needs its table or its rule.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [InitialState(Mode.Menu)]
/// public enum Mode { Menu, Playing }
/// </code>
/// </example>
/// <param name="value">Where the state starts, a value of the enum it is on.</param>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class InitialStateAttribute(object value) : Attribute
{
    /// <summary>Where the state starts.</summary>
    public object Value { get; } = value;
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

    /// <summary>The slot <typeparamref name="TState"/> holds, if it was ever added.</summary>
    /// <summary>
    /// Whether a system scoped to a state declared on its enum and never added says so, once, on
    /// the console.
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
    /// Refuses a source that is not an enum, and one that is itself computed. The first is a
    /// mistake the compiler cannot catch, because the attribute takes a <see cref="Type"/>; the
    /// second is a chain, which needs a slot layout this bridge does not have.
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

            if (Attribute.IsDefined(source, typeof(ComputedFromAttribute)))
                throw new InvalidOperationException(
                    $"{state.Name} is computed from {source.Name}, which is itself computed. "
                    + "The bridge sets a computed state's source aside when it is built, so a chain "
                    + "of them has nowhere to live.");

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
