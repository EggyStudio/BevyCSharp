using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// The managed store an <see cref="EcsList{T}"/> keeps its items in, a slot per list.
/// </summary>
/// <remarks>
/// <para>
/// A slot carries a generation, bumped when the slot is freed, and a handle holds both, the same
/// arrangement Bevy uses for an entity. A handle to a slot that was freed and handed to another
/// list then fails loudly rather than reading somebody else's items.
/// </para>
/// <para>
/// A slot is freed when the component holding the handle leaves its entity, through the remove
/// hook the generator registers for that component. A handle kept outside a component, in a static
/// or a local, is the program's to free with <see cref="EcsList{T}.Free"/>.
/// </para>
/// </remarks>
internal static class EcsStore
{
    private static readonly object Gate = new();
    private static readonly List<object?> Items = [null];
    private static readonly List<int> Generations = [0];
    private static readonly Stack<int> Free = [];

    /// <summary>How many slots hold something, for a test to see a despawn give one back.</summary>
    internal static int Live { get; private set; }

    /// <summary>Takes a slot for <paramref name="value"/>, returning the slot and its generation.</summary>
    internal static (int Slot, int Generation) Allocate(object value)
    {
        lock (Gate)
        {
            int slot;
            if (Free.Count > 0)
            {
                slot = Free.Pop();
                Items[slot] = value;
            }
            else
            {
                // Slot zero is never handed out, so a handle that was never made, all zeroes, names
                // nothing rather than the first list ever made.
                slot = Items.Count;
                Items.Add(value);
                Generations.Add(1);
            }

            Live++;
            return (slot, Generations[slot]);
        }
    }

    /// <summary>What a slot holds, or nothing when the handle names a slot freed since.</summary>
    internal static object? Get(int slot, int generation)
    {
        lock (Gate)
        {
            return slot > 0 && slot < Items.Count && Generations[slot] == generation ? Items[slot] : null;
        }
    }

    /// <summary>Gives a slot back, reporting whether the handle still named it.</summary>
    internal static bool Release(int slot, int generation)
    {
        lock (Gate)
        {
            if (slot <= 0 || slot >= Items.Count || Generations[slot] != generation) return false;

            Items[slot] = null;
            Generations[slot] = Generations[slot] == int.MaxValue ? 1 : Generations[slot] + 1;
            Free.Push(slot);
            Live--;
            return true;
        }
    }
}

/// <summary>
/// A list a component holds a handle to, with as many items as it needs, of any type.
/// </summary>
/// <typeparam name="T">What the list holds, which may be a string or any other managed type.</typeparam>
/// <remarks>
/// <para>
/// A component lives in Bevy's storage as bytes and cannot hold a <see cref="List{T}"/>. This is
/// two numbers, a slot and a generation, into a store on the managed side, so it sits in a
/// component and the list it names grows without bound. Where the bound is small and known,
/// <see cref="InlineList8{T}"/> and its siblings keep the items in the component's own bytes
/// instead, which iterates faster and needs nothing freed.
/// </para>
/// <para>
/// The list is made the first time something is added, so a component starts with an empty one
/// and needs no setup. Copies of the handle name the same list, because the handle is a reference,
/// so a component read, changed and written back keeps its list. The list is freed when the
/// component leaves its entity, by removal or despawn, through a hook the generator registers. A
/// handle used after that throws <see cref="ObjectDisposedException"/>, rather than reading a list
/// that has since been given to another entity.
/// </para>
/// <para>
/// Cloning the entity gives the clone a copy of the list, through a clone hook the generator
/// registers beside the remove hook. Writing a component over one that holds a different list
/// replaces the handle without freeing the list it held, so that list is the program's to
/// <see cref="Free"/>.
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct EcsList<T> : IEquatable<EcsList<T>>
{
    private int _slot;
    private int _generation;

    /// <summary>Makes an empty list now, rather than when its first item is added.</summary>
    public static EcsList<T> New()
    {
        var (slot, generation) = EcsStore.Allocate(new List<T>());
        return new EcsList<T> { _slot = slot, _generation = generation };
    }

    /// <summary>Whether a list has been made for this handle.</summary>
    public readonly bool IsCreated => _generation != 0;

    /// <summary>How many items it holds, which is none for a list never made.</summary>
    public readonly int Count => Existing?.Count ?? 0;

    /// <summary>The items, in order, which are none for a list never made.</summary>
    public readonly IReadOnlyList<T> Items => (IReadOnlyList<T>?)Existing ?? [];

    /// <summary>One item, by its place.</summary>
    /// <exception cref="ArgumentOutOfRangeException">There is no item at that place.</exception>
    public readonly T this[int index]
    {
        get => (Existing ?? throw new ArgumentOutOfRangeException(nameof(index)))[index];
        set => (Existing ?? throw new ArgumentOutOfRangeException(nameof(index)))[index] = value;
    }

    /// <summary>Adds an item at the end, making the list if this is its first.</summary>
    /// <remarks>
    /// Making it writes the new handle into this value, so it has to be called on the component's
    /// own field (a <c>ref</c> to it, or <c>this</c> in a behavior method) for the component to keep
    /// the list. Adding to a copy makes a list only the copy knows about.
    /// </remarks>
    public void Add(T item) => Made().Add(item);

    /// <summary>Takes the first item equal to <paramref name="item"/> out, reporting whether there was one.</summary>
    public readonly bool Remove(T item) => Existing?.Remove(item) ?? false;

    /// <summary>Takes the item at a place out.</summary>
    public readonly void RemoveAt(int index) =>
        (Existing ?? throw new ArgumentOutOfRangeException(nameof(index))).RemoveAt(index);

    /// <summary>Empties the list, keeping it.</summary>
    public readonly void Clear() => Existing?.Clear();

    /// <summary>Whether it holds an item equal to <paramref name="item"/>.</summary>
    public readonly bool Contains(T item) => Existing?.Contains(item) ?? false;

    /// <summary>
    /// A handle to a new list holding the same items, or a handle to nothing when this names none.
    /// </summary>
    /// <remarks>
    /// The items are copied as they are, so a list of strings or numbers is wholly separate, and a
    /// list of objects shares the objects. The clone hook the generator registers calls this, so
    /// a cloned entity's list is its own.
    /// </remarks>
    public readonly EcsList<T> Copy()
    {
        if (Existing is not { } items) return default;

        var (slot, generation) = EcsStore.Allocate(new List<T>(items));
        return new EcsList<T> { _slot = slot, _generation = generation };
    }

    /// <summary>
    /// Frees the list, after which this handle and every copy of it name nothing.
    /// </summary>
    /// <remarks>
    /// The remove hook does this for a list held by a component as the component goes. Call it for
    /// a list held anywhere else, or one replaced in a component by another.
    /// </remarks>
    /// <returns>Whether there was a list to free.</returns>
    public bool Free()
    {
        var freed = EcsStore.Release(_slot, _generation);
        _slot = 0;
        _generation = 0;
        return freed;
    }

    /// <summary>Walks the items in order.</summary>
    public readonly IEnumerator<T> GetEnumerator() => Items.GetEnumerator();

    /// <summary>The list, or nothing for one never made, throwing for one freed since.</summary>
    private readonly List<T>? Existing
    {
        get
        {
            if (_generation == 0) return null;

            return EcsStore.Get(_slot, _generation) as List<T>
                ?? throw new ObjectDisposedException(
                    nameof(EcsList<T>),
                    "The list was freed with the component that held it, and its slot may hold "
                    + "another list now.");
        }
    }

    /// <summary>The list, made now if this handle has none yet.</summary>
    private List<T> Made()
    {
        if (Existing is { } list) return list;

        this = New();
        return Existing!;
    }

    /// <inheritdoc/>
    public readonly bool Equals(EcsList<T> other) => _slot == other._slot && _generation == other._generation;

    /// <inheritdoc/>
    public readonly override bool Equals(object? obj) => obj is EcsList<T> other && Equals(other);

    /// <inheritdoc/>
    public readonly override int GetHashCode() => HashCode.Combine(_slot, _generation);

    /// <inheritdoc/>
    public readonly override string ToString() => IsCreated ? $"[{Count} items]" : "[]";
}
