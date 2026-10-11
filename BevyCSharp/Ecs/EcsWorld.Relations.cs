using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class EcsWorld
{
    // Which of the bridge's relationship slots each C# relationship took, by its type.
    private static readonly Dictionary<Type, int> RelationSlots = [];

    /// <summary>
    /// Makes <paramref name="source"/> relate to <paramref name="target"/> by
    /// <typeparamref name="TRelation"/>, in place of what it related to by it before, as inserting
    /// one of Bevy's relationship components does. Only valid inside a system.
    /// </summary>
    /// <typeparam name="TRelation">
    /// The relationship, any type of the game's own naming it, as Bevy's relationships example names
    /// its <c>Targeting</c>.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// A relationship is one way, an entity relating to one other at a time and an entity related
    /// to by any number, which Bevy keeps a list of on the entity related to, as its own
    /// <c>ChildOf</c> and <c>Children</c> are kept. <see cref="RelatedTo{TRelation}"/> reads that
    /// list. Despawning an entity takes its relation away and takes it off the lists it is on, and
    /// one related to that is despawned is left relating to nothing.
    /// </para>
    /// <para>
    /// Bevy's relationships are Rust types, which C# cannot declare, so the bridge declares a fixed
    /// number of them and each type of the game's takes one the first time it is used, for the life
    /// of the process.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Every relationship slot the bridge has is taken.</exception>
    /// <exception cref="BevyNativeException">Either entity is gone.</exception>
    public void Relate<TRelation>(Entity source, Entity target) =>
        Native.Check(Native.bcs_relation_set(RelationSlot<TRelation>(), source.Bits, target.Bits), $"relating {source} to {target} by {typeof(TRelation).Name}");

    /// <summary>
    /// Takes <paramref name="source"/>'s relation by <typeparamref name="TRelation"/> away, as
    /// removing Bevy's relationship component does. Only valid inside a system.
    /// </summary>
    /// <exception cref="BevyNativeException">The entity is gone.</exception>
    public void Unrelate<TRelation>(Entity source) =>
        Native.Check(Native.bcs_relation_remove(RelationSlot<TRelation>(), source.Bits), $"taking {source}'s {typeof(TRelation).Name} away");

    /// <summary>
    /// What <paramref name="source"/> relates to by <typeparamref name="TRelation"/>, or
    /// <see langword="null"/> where it relates to nothing by it. Only valid inside a system.
    /// </summary>
    public Entity? RelationOf<TRelation>(Entity source)
    {
        ulong target;
        var answer = Native.bcs_relation_target(RelationSlot<TRelation>(), source.Bits, &target);
        if (answer == NativeStatus.NotPresent) return null;
        Native.Check(answer, $"reading {source}'s {typeof(TRelation).Name}");
        return new Entity(target);
    }

    /// <summary>
    /// The entities relating to <paramref name="target"/> by <typeparamref name="TRelation"/>, in
    /// the order they came to, as Bevy's relationship target lists them. Only valid inside a system.
    /// </summary>
    /// <exception cref="BevyNativeException">The entity is gone.</exception>
    public Entity[] RelatedTo<TRelation>(Entity target)
    {
        var slot = RelationSlot<TRelation>();
        var count = Native.Check(Native.bcs_relation_sources(slot, target.Bits, null, 0), $"reading what relates to {target} by {typeof(TRelation).Name}");
        if (count == 0) return [];

        var sources = new ulong[count];
        fixed (ulong* written = sources) Native.Check(Native.bcs_relation_sources(slot, target.Bits, written, count), $"reading what relates to {target} by {typeof(TRelation).Name}");
        return [.. sources.Select(bits => new Entity(bits))];
    }

    /// <summary>
    /// What <paramref name="entity"/> relates to by <typeparamref name="TRelation"/>, what that
    /// relates to, and on, as Bevy's <c>iter_ancestors</c> follows a relationship. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// Read one step at a time as it is walked, and endless where the relations go round in a
    /// circle, which Bevy's relationships example looks for by keeping what it has seen.
    /// </remarks>
    public IEnumerable<Entity> RelationAncestors<TRelation>(Entity entity)
    {
        var at = entity;
        while (RelationOf<TRelation>(at) is { } next)
        {
            yield return next;
            at = next;
        }
    }

    private static int RelationSlot<TRelation>()
    {
        lock (RelationSlots)
        {
            if (RelationSlots.TryGetValue(typeof(TRelation), out var slot)) return slot;

            slot = RelationSlots.Count;
            var slots = Native.bcs_relation_slots();
            if (slot >= slots)
                throw new InvalidOperationException(
                    $"The bridge has {slots} relationship slots and every one is taken, so {typeof(TRelation).Name} has none. "
                    + "Taken are " + string.Join(", ", RelationSlots.Keys.Select(type => type.Name)) + ".");

            RelationSlots[typeof(TRelation)] = slot;
            return slot;
        }
    }
}
