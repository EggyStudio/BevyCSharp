using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class App : IDisposable
{
    // -- States

    /// <summary>
    /// Adds a state machine over <typeparamref name="TState"/>, starting at
    /// <paramref name="initial"/>.
    /// </summary>
    /// <remarks>
    /// Before the run, because adding a state also adds the systems that apply its transitions,
    /// and a schedule cannot be added to once the loop owns it. Adding the same enum twice keeps
    /// the first slot and re-inserts the initial value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The app is already running, or every state slot is taken.
    /// </exception>
    public App AddState<TState>(TState initial) where TState : struct, Enum
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot add state {typeof(TState).Name}: the app is already running. Add states "
                + "from a plugin's Build method or before calling Run.");

        Native.Check(
            Native.bcs_state_add(_handle, StateRegistry.Claim<TState>(), StateRegistry.ToInt(initial)),
            $"adding state {typeof(TState).Name}");

        _addedStates.Add(typeof(TState));
        return this;
    }

    /// <summary>
    /// Adds a sub-state over <typeparamref name="TState"/>, which exists only while its parent
    /// holds the value its <see cref="SubStateOfAttribute"/> names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parent has to have been added first, because Bevy works out whether a sub-state exists
    /// from its parent's value and has nothing to read otherwise.
    /// </para>
    /// <para>
    /// <paramref name="initial"/> is where it starts each time it comes into existence, which is
    /// every time the parent enters the value it lives under. A pause that is left on when a run
    /// ends is off again when the next run starts, as a player expects and a remembered value would
    /// get wrong.
    /// </para>
    /// <para>
    /// An enum naming several parents exists while every one of them holds its value, and every
    /// parent has to have been added first.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// The app is running, the enum carries no <see cref="SubStateOfAttribute"/>, or its parent
    /// was never added.
    /// </exception>
    public App AddSubState<TState>(TState initial) where TState : struct, Enum
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot add sub-state {typeof(TState).Name}, because the app is already "
                + "running. Add states from a plugin's Build method or before calling Run.");

        // A sub-state of several states is handed one wanted value a state slot, the slots it
        // does not live inside wanting nothing.
        if (StateRegistry.DescribeAll(typeof(TState)) is { Length: > 1 } parents)
        {
            var wants = new int[StateRegistry.SlotCount];
            Array.Fill(wants, int.MinValue);

            foreach (var parent in parents)
                wants[StateRegistry.Claim(parent.Parent)] = Convert.ToInt32(parent.WhileIn, System.Globalization.CultureInfo.InvariantCulture);

            var joint = StateRegistry.Claim<TState>() - StateRegistry.FirstJointSub;

            fixed (int* wanted = wants)
            {
                Native.Check(
                    Native.bcs_joint_substate_add(_handle, joint, wanted, wants.Length, StateRegistry.ToInt(initial)),
                    $"adding sub-state {typeof(TState).Name} under {string.Join(" and ", parents.Select(parent => parent.Parent.Name))}");
            }

            _addedStates.Add(typeof(TState));
            return this;
        }

        var sub = StateRegistry.Describe(typeof(TState))
                  ?? throw new InvalidOperationException(
                      $"{typeof(TState).Name} is not a sub-state of anything. Put "
                      + "[SubStateOf(typeof(Parent), Parent.Value)] on the enum, which is where a "
                      + "reader looks for what it belongs to.");

        // Claiming the sub-state gives it the slot everything else addresses it by, and the bridge
        // is told where it sits among the sub-states rather than among the states, so the state
        // count comes back off again here.
        var slot = StateRegistry.Claim<TState>() - StateRegistry.SlotCount;

        Native.Check(
            Native.bcs_substate_add(
                _handle,
                slot,
                Convert.ToInt32(sub.WhileIn, System.Globalization.CultureInfo.InvariantCulture),
                StateRegistry.ToInt(initial)),
            $"adding sub-state {typeof(TState).Name} under {sub.Parent.Name}");

        _addedStates.Add(typeof(TState));
        return this;
    }

    /// <summary>
    /// Adds a state worked out from another rather than set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The third shape of state, for a fact that follows from another fact. Whether the interface
    /// is up is true on three screens and false on the rest, and writing that as a plain state
    /// leaves two facts to keep in step until one of them lies.
    /// </para>
    /// <para>
    /// The source has to be added first, because Bevy works the value out whenever the source
    /// changes and a source that holds no state never changes. A source value the table says
    /// nothing about means the computed state does not exist at all, so a system scoped to it does
    /// not run and <see cref="TryState{TState}"/> answers false.
    /// </para>
    /// <para>
    /// <see cref="SetState{TState}"/> on one is always refused, since there is nothing to set.
    /// </para>
    /// </remarks>
    /// <typeparam name="TState">The enum being computed, carrying <see cref="ComputedFromAttribute"/>.</typeparam>
    /// <typeparam name="TSource">The enum it is computed from.</typeparam>
    /// <param name="table">
    /// What the state is while the source holds each value. A value named twice takes the first
    /// answer, and one not named at all means the state does not exist.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// The app is running, the enum says nothing about what it is computed from, or the table is
    /// empty.
    /// </exception>
    /// <example>
    /// <code>
    /// app.AddState(Screen.Menu);
    /// app.AddComputedState((Screen.Playing, Hud.Shown), (Screen.Paused, Hud.Shown));
    /// </code>
    /// </example>
    public App AddComputedState<TState, TSource>(params (TSource Source, TState Is)[] table)
        where TState : struct, Enum
        where TSource : struct, Enum
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(table);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot add computed state {typeof(TState).Name}, because the app is already "
                + "running. Add states from a plugin's Build method or before calling Run.");

        if (table.Length == 0)
            throw new InvalidOperationException(
                $"{typeof(TState).Name} was given an empty table, so it would never exist. Name "
                + "at least one value of its source that it applies to.");

        var computed = StateRegistry.DescribeComputed(typeof(TState))
                       ?? throw new InvalidOperationException(
                           $"{typeof(TState).Name} is not computed from anything. Put "
                           + "[ComputedFrom(typeof(Source))] on the enum, which is where a reader "
                           + "looks for what it follows from.");

        if (computed.IsJoint || computed.Source != typeof(TSource))
            throw new InvalidOperationException(
                $"{typeof(TState).Name} is computed from {Named(computed)}, and the table was "
                + $"written in terms of {typeof(TSource).Name}. The two have to be the same, or "
                + "the table says nothing about when it applies.");

        // Past the states and every sub-state, which is where the computed block begins.
        var slot = StateRegistry.Claim<TState>()
                   - StateRegistry.SlotCount
                   - StateRegistry.SubCount;

        var from = new int[table.Length];
        var to = new int[table.Length];

        for (var i = 0; i < table.Length; i++)
        {
            from[i] = StateRegistry.ToInt(table[i].Source);
            to[i] = StateRegistry.ToInt(table[i].Is);
        }

        fixed (int* sources = from)
        fixed (int* results = to)
        {
            Native.Check(
                Native.bcs_computed_add(_handle, slot, sources, results, table.Length),
                $"adding computed state {typeof(TState).Name} from {computed.Source.Name}");
        }

        // The first answer for a value, as the bridge reads the table, for a joint that reads it.
        ComputedRules.Know(slot, raw => Array.IndexOf(from, raw) is >= 0 and var at ? to[at] : null);

        _addedStates.Add(typeof(TState));
        return this;
    }

    /// <summary>
    /// Adds a state worked out from another by a rule, for what a table cannot state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same as the table form, with the table replaced by a function of the source's value,
    /// answering what the state is or nothing for a value it does not exist under. "The boss music
    /// plays on every level past the tenth" is a rule, and a table of it would be as long as the
    /// levels.
    /// </para>
    /// <para>
    /// Bevy asks whenever the source changes, from whichever thread runs the transition, with the
    /// source's value and nothing else, so the rule is a function of that value alone and reads no
    /// world. One that throws is taken as answering nothing, with the exception written to the
    /// console, since an exception has nowhere to go from inside Bevy's transition.
    /// </para>
    /// </remarks>
    /// <typeparam name="TState">The enum being computed, carrying <see cref="ComputedFromAttribute"/>.</typeparam>
    /// <typeparam name="TSource">The enum it is computed from.</typeparam>
    /// <param name="rule">What the state is while the source holds a value, or nothing.</param>
    /// <exception cref="InvalidOperationException">
    /// The app is running, or the enum says nothing about what it is computed from.
    /// </exception>
    /// <example>
    /// <code>
    /// app.AddState(Level.One);
    /// app.AddComputedState&lt;Music, Level&gt;(level => level > Level.Ten ? Music.Boss : Music.Calm);
    /// </code>
    /// </example>
    public App AddComputedState<TState, TSource>(Func<TSource, TState?> rule)
        where TState : struct, Enum
        where TSource : struct, Enum
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(rule);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot add computed state {typeof(TState).Name}, because the app is already "
                + "running. Add states from a plugin's Build method or before calling Run.");

        var computed = StateRegistry.DescribeComputed(typeof(TState))
                       ?? throw new InvalidOperationException(
                           $"{typeof(TState).Name} is not computed from anything. Put "
                           + "[ComputedFrom(typeof(Source))] on the enum, which is where a reader "
                           + "looks for what it follows from.");

        if (computed.IsJoint || computed.Source != typeof(TSource))
            throw new InvalidOperationException(
                $"{typeof(TState).Name} is computed from {Named(computed)}, and the rule was "
                + $"written in terms of {typeof(TSource).Name}. The two have to be the same.");

        var slot = StateRegistry.Claim<TState>()
                   - StateRegistry.SlotCount
                   - StateRegistry.SubCount;

        ComputedRules.Set(slot, raw =>
            rule((TSource)Enum.ToObject(typeof(TSource), raw)) is { } value ? StateRegistry.ToInt(value) : null);

        Native.Check(
            Native.bcs_computed_add_rule(_handle, slot),
            $"adding computed state {typeof(TState).Name} from {computed.Source.Name} by a rule");

        _addedStates.Add(typeof(TState));
        return this;
    }

    /// <summary>
    /// Adds a state worked out from two others at once, by a rule of the game's own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A joint state, for a fact that follows from two facts together, such as the music, which is
    /// the boss theme on the last level unless the game is paused. The enum names both sources,
    /// <c>[ComputedFrom(typeof(A), typeof(B))]</c>, and both are added as states first. The rule
    /// answers what the state is, or nothing for values it does not exist under, and is asked
    /// whenever either source changes. While either source holds no state the joint does not
    /// exist.
    /// </para>
    /// <para>
    /// The bridge has a fixed number of joints (<see cref="StateRegistry.JointCount"/>), each fed
    /// every state slot, so a joint can be worked out from any of them. The rule is asked from
    /// inside Bevy's transition, so it reads no world, and one that throws is taken as answering
    /// nothing, as a computed state's rule is.
    /// </para>
    /// <para>
    /// A source may be a state computed from one state, added before the joint, which is read by
    /// working it out again from its own source's value, the same answer Bevy reaches from the same
    /// value. Bevy's <c>computed_states</c> shows its tutorial worked out so, from whether it is on
    /// and from two states computed from the app's.
    /// </para>
    /// </remarks>
    /// <typeparam name="TState">The enum being worked out, carrying <see cref="ComputedFromAttribute"/>.</typeparam>
    /// <typeparam name="TFirst">The first state it is worked out from.</typeparam>
    /// <typeparam name="TSecond">The second.</typeparam>
    /// <param name="rule">What the state is while the two hold their values, or nothing.</param>
    /// <exception cref="InvalidOperationException">
    /// The app is running, the enum does not name these two as its sources, or every joint is in use.
    /// </exception>
    /// <example>
    /// <code>
    /// [ComputedFrom(typeof(Level), typeof(Pause))]
    /// public enum Music { Calm, Boss, Quiet }
    ///
    /// app.AddComputedState&lt;Music, Level, Pause&gt;((level, pause) =>
    ///     pause == Pause.On ? Music.Quiet : level == Level.Last ? Music.Boss : Music.Calm);
    /// </code>
    /// </example>
    public App AddComputedState<TState, TFirst, TSecond>(Func<TFirst, TSecond, TState?> rule)
        where TState : struct, Enum
        where TFirst : struct, Enum
        where TSecond : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(rule);

        return AddJoint<TState>([typeof(TFirst), typeof(TSecond)], held =>
            rule(As<TFirst>(held[0]), As<TSecond>(held[1])) is { } value ? StateRegistry.ToInt(value) : null);
    }

    /// <summary>Adds a state worked out from three others at once, by a rule of the game's own.</summary>
    /// <remarks>The same as the form with two, with a third source.</remarks>
    /// <typeparam name="TState">The enum being worked out, carrying <see cref="ComputedFromAttribute"/>.</typeparam>
    /// <typeparam name="TFirst">The first state it is worked out from.</typeparam>
    /// <typeparam name="TSecond">The second.</typeparam>
    /// <typeparam name="TThird">The third.</typeparam>
    /// <param name="rule">What the state is while the three hold their values, or nothing.</param>
    /// <exception cref="InvalidOperationException">
    /// The app is running, the enum does not name these three as its sources, or every joint is in use.
    /// </exception>
    public App AddComputedState<TState, TFirst, TSecond, TThird>(Func<TFirst, TSecond, TThird, TState?> rule)
        where TState : struct, Enum
        where TFirst : struct, Enum
        where TSecond : struct, Enum
        where TThird : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(rule);

        return AddJoint<TState>([typeof(TFirst), typeof(TSecond), typeof(TThird)], held =>
            rule(As<TFirst>(held[0]), As<TSecond>(held[1]), As<TThird>(held[2])) is { } value ? StateRegistry.ToInt(value) : null);
    }

    /// <summary>
    /// Claims a joint for <typeparamref name="TState"/> and gives the bridge its rule, asked with
    /// the value of each source in order once every one of them holds a state.
    /// </summary>
    private App AddJoint<TState>(Type[] sources, Func<int[], int?> rule)
        where TState : struct, Enum
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot add computed state {typeof(TState).Name}, because the app is already "
                + "running. Add states from a plugin's Build method or before calling Run.");

        var computed = StateRegistry.DescribeComputed(typeof(TState))
                       ?? throw new InvalidOperationException(
                           $"{typeof(TState).Name} is not computed from anything. Put "
                           + "[ComputedFrom(typeof(A), typeof(B))] on the enum, which is where a "
                           + "reader looks for what it follows from.");

        if (!computed.Sources.SequenceEqual(sources))
            throw new InvalidOperationException(
                $"{typeof(TState).Name} is computed from {Named(computed)}, and the rule was "
                + $"written in terms of {string.Join(" and ", sources.Select(source => source.Name))}. "
                + "The two have to name the same states in the same order.");

        var reads = sources.Select(source => ReadOf<TState>(source)).ToArray();
        var joint = StateRegistry.Claim<TState>() - StateRegistry.FirstJoint;

        ComputedRules.SetJoint(joint, (values, present) =>
        {
            var held = new int[reads.Length];
            for (var i = 0; i < reads.Length; i++)
            {
                var (slot, derive) = reads[i];
                if ((present & (1u << slot)) == 0) return null;

                if (derive is null) held[i] = values[slot];
                else if (derive(values[slot]) is { } derived) held[i] = derived;
                else return null;
            }

            return rule(held);
        });

        Native.Check(
            Native.bcs_joint_add(_handle, joint),
            $"adding computed state {typeof(TState).Name} from {Named(computed)}");

        _addedStates.Add(typeof(TState));
        return this;
    }

    /// <summary>
    /// Where a joint reads one of its sources, the slot the bridge puts the value of and, for a
    /// computed state, how it is worked out from that value.
    /// </summary>
    /// <remarks>
    /// A joint is fed the state slots alone, and a computed state is a function of its source's
    /// value, so a computed source is read as its own source's slot worked out again here. That
    /// needs how it is worked out, so it is added before the joint, as a joint's other sources are.
    /// </remarks>
    private (int Slot, Func<int, int?>? Derive) ReadOf<TState>(Type source)
    {
        if (StateRegistry.DescribeComputed(source) is not { } computed) return (StateRegistry.Claim(source), null);

        if (!_addedStates.Contains(source))
            throw new InvalidOperationException(
                $"{typeof(TState).Name} is worked out from {source.Name}, which is computed and "
                + "was not added before it. Add it first, since it is read through how it is "
                + "worked out.");

        var slot = StateRegistry.Claim(source) - StateRegistry.SlotCount - StateRegistry.SubCount;
        var derive = ComputedRules.Derivation(slot)
                     ?? throw new InvalidOperationException($"{source.Name} was added without a table or a rule to read it by.");
        return (StateRegistry.Claim(computed.Source), derive);
    }

    /// <summary>A raw state value as the enum it is.</summary>
    private static T As<T>(int raw) where T : struct, Enum => (T)Enum.ToObject(typeof(T), raw);

    /// <summary>What a computed state is worked out from, by name.</summary>
    private static string Named(ComputedFromAttribute computed) =>
        string.Join(" and ", computed.Sources.Select(source => source.Name));

    /// <summary>The current value of <typeparamref name="TState"/>. Only valid inside a system.</summary>
    public static TState State<TState>() where TState : struct, Enum =>
        StateRegistry.Current<TState>();

    /// <summary>
    /// The current value of <typeparamref name="TState"/>, or false when it does not exist.
    /// </summary>
    /// <remarks>
    /// What a sub-state needs, because while its parent holds another value there is no state to
    /// read. Only valid inside a system.
    /// </remarks>
    public static bool TryState<TState>(out TState value) where TState : struct, Enum =>
        StateRegistry.TryCurrent(out value);

    /// <summary>Queues a transition of <typeparamref name="TState"/>. Only valid inside a system.</summary>
    public static void SetState<TState>(TState value) where TState : struct, Enum =>
        StateRegistry.Set(value);

    /// <summary>
    /// Registers a system to run once when <typeparamref name="TState"/> enters or leaves
    /// <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// What <c>[OnEnter]</c> and <c>[OnExit]</c> emit. Unlike
    /// <see cref="AddSystem(Stage, SystemDescriptor)"/> this runs once per transition rather than
    /// once per frame, which makes it the place to build a screen or take one away.
    /// </remarks>
    /// <param name="value">The state value whose edge to run on.</param>
    /// <param name="entering">True for the enter edge, false for the exit edge.</param>
    /// <param name="descriptor">The system to run.</param>
    /// <exception cref="InvalidOperationException">
    /// The app is already running, or the state was never added.
    /// </exception>
    public App AddStateSystem<TState>(TState value, bool entering, SystemDescriptor descriptor)
        where TState : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ObjectDisposedException.ThrowIf(_disposed, this);

        descriptor.Source ??= SystemRegistrationSourceScope.Current;

        // Added while running, as a script reloaded during play is, it is watched for rather than
        // scheduled, since Bevy's transition schedules take nothing once the app runs. The value
        // the state holds as it arrives is not an edge, as Bevy would not run it either.
        if (IsRunning && _dynamicStages is not null)
        {
            var slot = StateRegistry.SlotForRegistration<TState>();
            var edge = new RegisteredSystem(this, descriptor, Stage.Update);
            _systems.Add(edge);
            _dynamicEdges.Add(new DynamicEdge(slot, StateRegistry.ToInt(value), entering, edge));
            if (!_seenStates.ContainsKey(slot)) _seenStates[slot] = ReadSlot(slot);
            return this;
        }

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot register system '{descriptor.Name}': the app is already running. "
                + "Register systems from a plugin's Build method, or call EnableDynamicSystems before Run.");

        // The stage is only a label here, because a transition system belongs to no frame stage,
        // and Startup is the closest thing to "runs outside the ordinary loop".
        var registration = new RegisteredSystem(this, descriptor, Stage.Startup);
        _systems.Add(registration);

        Native.Check(
            Native.bcs_state_add_system(
                _handle,
                StateRegistry.SlotForRegistration<TState>(),
                StateRegistry.ToInt(value),
                entering ? 0 : 1,
                &RegisteredSystem.Trampoline,
                registration.UserData),
            $"registering system '{descriptor.Name}' on a {typeof(TState).Name} transition");

        return this;
    }

    /// <summary>
    /// Registers a system to run once when <typeparamref name="TState"/> moves from
    /// <paramref name="from"/> to <paramref name="to"/>, and on no other move.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What <c>[OnTransition]</c> emits, for what depends on where a state came from as well as
    /// where it went. Play resumed from the pause keeps the level as it was, and play entered from
    /// the menu builds it, so the build is a transition from the menu and not an entry to play.
    /// </para>
    /// <para>
    /// It runs after <paramref name="from"/>'s exit systems and before <paramref name="to"/>'s
    /// entry ones, as Bevy orders them, so it sees what leaving took away and nothing entering has
    /// built yet. A state set to the value it already holds moves from that value to itself, as
    /// Bevy has it, and runs a system naming that value twice along with the value's exit and
    /// entry.
    /// </para>
    /// </remarks>
    /// <param name="from">The value the state leaves.</param>
    /// <param name="to">The value it enters.</param>
    /// <param name="descriptor">The system to run.</param>
    /// <exception cref="InvalidOperationException">
    /// The app is already running, or the state was never added.
    /// </exception>
    public App AddTransitionSystem<TState>(TState from, TState to, SystemDescriptor descriptor)
        where TState : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ObjectDisposedException.ThrowIf(_disposed, this);

        descriptor.Source ??= SystemRegistrationSourceScope.Current;

        // Watched for while running, as an entry or an exit added then is.
        if (IsRunning && _dynamicStages is not null)
        {
            var slot = StateRegistry.SlotForRegistration<TState>();
            var edge = new RegisteredSystem(this, descriptor, Stage.Update);
            _systems.Add(edge);
            _dynamicEdges.Add(new DynamicEdge(slot, StateRegistry.ToInt(to), true, edge, StateRegistry.ToInt(from)));
            if (!_seenStates.ContainsKey(slot)) _seenStates[slot] = ReadSlot(slot);
            return this;
        }

        if (IsRunning)
            throw new InvalidOperationException(
                $"Cannot register system '{descriptor.Name}': the app is already running. "
                + "Register systems from a plugin's Build method, or call EnableDynamicSystems before Run.");

        var registration = new RegisteredSystem(this, descriptor, Stage.Startup);
        _systems.Add(registration);

        Native.Check(
            Native.bcs_state_add_transition(
                _handle,
                StateRegistry.SlotForRegistration<TState>(),
                StateRegistry.ToInt(from),
                StateRegistry.ToInt(to),
                &RegisteredSystem.Trampoline,
                registration.UserData),
            $"registering system '{descriptor.Name}' on a {typeof(TState).Name} move from {from} to {to}");

        return this;
    }
}
