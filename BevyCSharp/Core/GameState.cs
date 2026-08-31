using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Maps C# enums onto Bevy's app states.
/// </summary>
/// <remarks>
/// <para>
/// A Bevy state is a Rust type, and C# cannot define one, so the bridge provides a fixed number
/// of state slots that each hold an integer and let the managed side decide what the numbers
/// mean. An enum claims a slot the first time it is added, which is what keeps two unrelated
/// state machines apart: Bevy keys its state resource and its transitions on the type, so two
/// slots really are two independent state machines.
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
    internal static int Claim<TState>() where TState : struct, Enum
    {
        lock (Gate)
        {
            Reset();
            if (Slots.TryGetValue(typeof(TState), out var existing)) return existing;

            var count = SlotCount;
            if (_next >= count)
                throw new InvalidOperationException(
                    $"All {count} state slots are in use, so {typeof(TState).Name} cannot have "
                    + "one. Independent state machines are rarer than they look: a pause that "
                    + "only matters while playing is a value of the state it belongs to, not a "
                    + "second machine beside it.");

            var slot = _next++;
            Slots[typeof(TState)] = slot;
            return slot;
        }
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
        Native.Check(
            Native.bcs_state_get(slot, &read),
            $"reading state {typeof(TState).Name}");

        value = read;
        return true;
    }

    /// <summary>
    /// Asks Bevy to move <typeparamref name="TState"/> to <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// Queued rather than immediate. Bevy applies it at the next transition point, which is what
    /// lets every system in a frame agree on which state it is in rather than seeing the change
    /// halfway through.
    /// </remarks>
    public static void Set<TState>(TState value) where TState : struct, Enum =>
        Native.Check(
            Native.bcs_state_set(SlotOf<TState>(), ToInt(value)),
            $"setting state {typeof(TState).Name}");

    /// <summary>The enum's underlying value, which is what the bridge stores.</summary>
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
        // narrow enum wrongly: a byte-backed member of -1 would arrive as 255.
        return Convert.ToInt32(value);
    }
}
