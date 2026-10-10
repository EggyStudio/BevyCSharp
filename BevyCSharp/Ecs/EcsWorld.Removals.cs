using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class EcsWorld
{
    /// <summary>
    /// The entities that lost <typeparamref name="T"/>, by a removal or a despawn, since the running
    /// system last asked, oldest first. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The third of what a system reads of a component's life beside what was added and what
    /// changed, as Bevy's <c>RemovedComponents&lt;T&gt;</c> gives a Rust system. Each system keeps
    /// its own place for each component it asks about, so a removal reaches every system that asks
    /// once, however many ask and however often each runs. A system asking for the first time is
    /// given every removal Bevy still holds.
    /// </para>
    /// <para>
    /// Bevy keeps a removal for two frames, so a system that asks less often than that misses the
    /// older ones. An entity listed may already be despawned, or its index given to a new entity,
    /// so what is done with one is checked against <see cref="IsAlive"/>. A method of a behavior
    /// asks from the system that runs it, which is one system for every entity it is run for.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Called outside a system, which has no last run to count from.</exception>
    public List<Entity> Removed<T>() where T : unmanaged => RemovedById(ComponentType<T>.Id);

    /// <summary>
    /// The entities that lost the component with this id since the running system last asked, as
    /// <see cref="Removed{T}"/> lists them.
    /// </summary>
    /// <exception cref="InvalidOperationException">Called outside a system, which has no last run to count from.</exception>
    public List<Entity> RemovedById(int componentId)
    {
        Span<ulong> few = stackalloc ulong[64];
        int count;
        fixed (ulong* buffer = few)
            count = Read(buffer, few.Length);

        if (count <= few.Length) return [.. few[..count].ToArray().Select(bits => new Entity(bits))];

        // More than fitted, which moved nothing on, so all of them are read again into room enough.
        var all = new ulong[count];
        fixed (ulong* buffer = all)
            count = Read(buffer, all.Length);
        return [.. all.AsSpan(0, Math.Min(count, all.Length)).ToArray().Select(bits => new Entity(bits))];

        int Read(ulong* buffer, int capacity)
        {
            var answer = Native.bcs_ecs_removed(componentId, buffer, capacity);
            if (answer == NativeStatus.InvalidState)
                throw new InvalidOperationException("Removed is asked from inside a system, which keeps its own place in each component's removals.");
            return Native.Check(answer, $"reading the removals of component {componentId}");
        }
    }
}
