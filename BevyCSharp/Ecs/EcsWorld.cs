using System.Buffers;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Entities and components, backed directly by Bevy's ECS.
/// </summary>
/// <remarks>
/// <para>
/// This type stores nothing. Every call reaches into the live Bevy world, which means there is
/// no second copy of your game state to keep in sync and no marshalling layer between a
/// behavior and its components.
/// </para>
/// <para>
/// <b>Threading.</b> These methods are only valid on the main thread while a system is
/// running, because that is when Bevy loans its world out. A behavior method running on a
/// worker thread (the generator parallelises large iterations) must not call them, and should
/// instead write through the component reference it was handed, queueing structural changes
/// on <see cref="EcsCommands"/>. Calling anyway throws
/// <see cref="BevyNativeException"/> with <see cref="NativeStatus.NoWorld"/> rather than
/// corrupting the world.
/// </para>
/// </remarks>
public sealed unsafe class EcsWorld
{
    /// <summary>Spawns an entity with no components.</summary>
    public Entity Spawn()
    {
        var bits = Native.bcs_ecs_spawn();
        if (bits == 0)
            throw new BevyNativeException(
                NativeStatus.NoWorld,
                "Spawn failed: " + NativeStatus.Describe(NativeStatus.NoWorld) + ".");

        return new Entity(bits);
    }

    /// <summary>Spawns an entity and applies <paramref name="build"/> to it.</summary>
    public Entity Spawn(Action<Entity, EcsWorld> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        var entity = Spawn();
        build(entity, this);
        return entity;
    }

    /// <summary>Spawns <paramref name="count"/> entities, applying <paramref name="build"/> to each.</summary>
    public void SpawnBatch(int count, Action<Entity, EcsWorld> build)
    {
        ArgumentNullException.ThrowIfNull(build);
        for (var i = 0; i < count; i++) build(Spawn(), this);
    }

    /// <summary>Spawns <paramref name="count"/> entities carrying a component from <paramref name="factory"/>.</summary>
    public void SpawnBatch<T>(int count, Func<int, T> factory) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(factory);
        for (var i = 0; i < count; i++) Add(Spawn(), factory(i));
    }

    /// <summary>Destroys an entity and everything on it.</summary>
    /// <returns><see langword="false"/> if the handle was already stale.</returns>
    public bool Despawn(Entity entity)
    {
        var status = Native.bcs_ecs_despawn(entity.Bits);
        if (status == NativeStatus.NoEntity) return false;
        Native.Check(status, "Despawn");
        return true;
    }

    /// <summary>True when the handle still refers to a live entity.</summary>
    public bool IsAlive(Entity entity) =>
        Native.Check(Native.bcs_ecs_alive(entity.Bits), "IsAlive") != 0;

    /// <summary>Adds or replaces a component on an entity.</summary>
    /// <remarks>
    /// <typeparamref name="T"/> may be an ordinary C# struct, which Bevy learns from its layout,
    /// or one of Bevy's own components such as <see cref="Bevy.Transform"/>, which implements
    /// <see cref="INativeComponent"/> and so resolves to the engine's existing component. Both
    /// go through the same call; nothing here has to know which is which.
    /// </remarks>
    /// <example>
    /// <code>
    /// ctx.Ecs.Add(entity, new Position { X = 1f });                 // a C# component
    /// ctx.Ecs.Add(entity, Transform.At(0f, 5f, 0f));                // Bevy's own Transform
    /// </code>
    /// </example>
    public void Add<T>(Entity entity, T component) where T : unmanaged =>
        Native.Check(
            Native.bcs_ecs_insert(entity.Bits, ComponentType<T>.ValueId, &component),
            $"Add<{typeof(T).Name}>");

    /// <summary>Overwrites a component's value. Identical to <see cref="Add{T}"/>.</summary>
    public void Set<T>(Entity entity, T component) where T : unmanaged => Add(entity, component);

    /// <summary>Removes a component. Succeeds whether or not it was present.</summary>
    public bool Remove<T>(Entity entity) where T : unmanaged
    {
        var status = Native.bcs_ecs_remove(entity.Bits, ComponentType<T>.Id);
        if (status == NativeStatus.NoEntity) return false;
        Native.Check(status, $"Remove<{typeof(T).Name}>");
        return true;
    }

    /// <summary>True when an entity carries a component.</summary>
    public bool Has<T>(Entity entity) where T : unmanaged
    {
        var status = Native.bcs_ecs_has(entity.Bits, ComponentType<T>.Id);
        return status > 0;
    }

    /// <summary>Reads a component, reporting whether it was present.</summary>
    public bool TryGet<T>(Entity entity, out T component) where T : unmanaged
    {
        var pointer = Native.bcs_ecs_get_ptr(entity.Bits, ComponentType<T>.ValueId);
        if (pointer is null)
        {
            component = default;
            return false;
        }

        component = Unsafe.ReadUnaligned<T>(pointer);
        return true;
    }

    /// <summary>Reads a component, or returns <see langword="default"/> when absent.</summary>
    public T GetOrDefault<T>(Entity entity) where T : unmanaged =>
        TryGet<T>(entity, out var component) ? component : default;

    /// <summary>
    /// Returns a reference straight into Bevy's storage, so writes land in place.
    /// </summary>
    /// <remarks>
    /// The reference is invalidated by the next structural change to the world. Do not hold it
    /// across a spawn, despawn, insert or remove. For one of Bevy's own components this writes
    /// the component the engine's own systems read, so a change to a <see cref="Bevy.Transform"/>
    /// is picked up by propagation and rendering like any other.
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity does not carry the component.</exception>
    public ref T GetRef<T>(Entity entity) where T : unmanaged
    {
        var pointer = Native.bcs_ecs_get_ptr(entity.Bits, ComponentType<T>.ValueId);
        if (pointer is null)
            throw new BevyNativeException(
                NativeStatus.NotPresent,
                $"GetRef<{typeof(T).Name}> failed: {entity} does not carry that component.");

        return ref Unsafe.AsRef<T>(pointer);
    }

    /// <summary>
    /// Applies <paramref name="mutate"/> to a component in place, if the entity has one.
    /// </summary>
    public bool Mutate<T>(Entity entity, Func<T, T> mutate) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(mutate);
        var pointer = Native.bcs_ecs_get_ptr(entity.Bits, ComponentType<T>.ValueId);
        if (pointer is null) return false;

        ref var slot = ref Unsafe.AsRef<T>(pointer);
        slot = mutate(slot);
        return true;
    }

    /// <summary>True when a component changed since the previous frame.</summary>
    public bool Changed<T>(Entity entity) where T : unmanaged =>
        Native.bcs_ecs_changed(entity.Bits, ComponentType<T>.Id) > 0;

    /// <summary>True when a component changed since the previous frame, by raw component id.</summary>
    /// <remarks>Used by generated <c>[Changed]</c> filters, which work in ids rather than types.</remarks>
    public bool ChangedById(Entity entity, int componentId) =>
        Native.bcs_ecs_changed(entity.Bits, componentId) > 0;

    /// <summary>Counts entities carrying <typeparamref name="T"/>.</summary>
    public int Count<T>() where T : unmanaged =>
        Native.Check(Native.bcs_ecs_count(ComponentType<T>.Id), $"Count<{typeof(T).Name}>");

    /// <summary>The Bevy component id for <typeparamref name="T"/>, registering it if needed.</summary>
    public static int ComponentId<T>() where T : unmanaged => ComponentType<T>.Id;

    // -- By raw component id

    /// <summary>True when an entity carries the component with this id.</summary>
    public bool HasById(Entity entity, int componentId) =>
        Native.bcs_ecs_has(entity.Bits, componentId) > 0;

    /// <summary>Counts entities carrying the component with this id.</summary>
    public int CountById(int componentId) =>
        Native.Check(Native.bcs_ecs_count(componentId), "CountById");

    /// <summary>Removes the component with this id. Succeeds whether or not it was present.</summary>
    public bool RemoveById(Entity entity, int componentId)
    {
        var status = Native.bcs_ecs_remove(entity.Bits, componentId);
        if (status == NativeStatus.NoEntity) return false;
        Native.Check(status, "RemoveById");
        return true;
    }

    // -- Hierarchy

    /// <summary>
    /// Makes <paramref name="child"/> a child of <paramref name="parent"/>.
    /// </summary>
    /// <remarks>
    /// Goes through Bevy's own relationship API, so the matching child list is maintained and
    /// transforms propagate. Any previous parent is replaced. This is a structural change, so it
    /// invalidates outstanding component references; queue it on <see cref="EcsCommands"/> when
    /// calling from inside a loop.
    /// </remarks>
    /// <returns><see langword="false"/> if either entity is no longer alive.</returns>
    public bool SetParent(Entity child, Entity parent)
    {
        var status = Native.bcs_ecs_set_parent(child.Bits, parent.Bits);
        if (status == NativeStatus.NoEntity) return false;
        Native.Check(status, "SetParent");
        return true;
    }

    /// <summary>Detaches an entity from its parent. Succeeds whether or not it had one.</summary>
    public bool ClearParent(Entity child)
    {
        var status = Native.bcs_ecs_clear_parent(child.Bits);
        if (status == NativeStatus.NoEntity) return false;
        Native.Check(status, "ClearParent");
        return true;
    }

    /// <summary>An entity's parent, or <see cref="Entity.None"/> if it has none.</summary>
    public Entity ParentOf(Entity entity) => new(Native.bcs_ecs_parent_of(entity.Bits));

    /// <summary>An entity's direct children, in order.</summary>
    /// <remarks>Only the immediate children; walk the result to go deeper.</remarks>
    public Entity[] ChildrenOf(Entity entity)
    {
        var count = Native.bcs_ecs_children(entity.Bits, null, 0);
        if (count == NativeStatus.NoEntity) return [];
        Native.Check(count, "ChildrenOf");
        if (count == 0) return [];

        var children = new Entity[count];
        fixed (Entity* target = children)
        {
            var written = Native.bcs_ecs_children(entity.Bits, (ulong*)target, count);
            Native.Check(written, "ChildrenOf");
            if (written != count) return ChildrenOf(entity);
        }

        return children;
    }

    // -- Iteration

    /// <summary>
    /// Collects the storage runs holding <typeparamref name="T"/>, optionally filtered.
    /// </summary>
    /// <remarks>
    /// This is the primitive the generated behavior runners use. Each chunk is a direct view
    /// into Bevy's table storage, so iterating writes in place with no copy. Filters are
    /// resolved per table rather than per entity, so a <c>[With]</c>/<c>[Without]</c> filter
    /// costs nothing in the inner loop.
    /// </remarks>
    /// <param name="with">Component ids the entity must also carry.</param>
    /// <param name="without">Component ids the entity must not carry.</param>
    /// <param name="markChanged">
    /// Stamp every returned row with the current change tick, matching what Bevy's
    /// <c>Query&lt;&amp;mut T&gt;</c> does. Pass <see langword="false"/> for a read-only pass so
    /// <c>[Changed]</c> filters elsewhere stay meaningful.
    /// </param>
    /// <remarks>
    /// A filter may name a sparse-stored component (see <see cref="ISparseComponent"/>). That one
    /// cannot be answered per table, so it is answered per entity, which splits a table into the
    /// runs that satisfy it: the same rows come back, in more chunks.
    /// </remarks>
    public ChunkSet<T> Chunks<T>(
        ReadOnlySpan<int> with = default,
        ReadOnlySpan<int> without = default,
        bool markChanged = true) where T : unmanaged =>
        Chunks<T>(ComponentType<T>.ChunkId, with, without, markChanged);

    /// <summary>
    /// Collects the storage runs for an explicitly named component.
    /// </summary>
    /// <remarks>
    /// The same thing, for a component whose id did not come from <typeparamref name="T"/>: an id
    /// resolved at runtime, or a component read through a type that is not its own. A type
    /// implementing <see cref="INativeComponent"/> needs none of this, because
    /// <see cref="Chunks{T}(ReadOnlySpan{int}, ReadOnlySpan{int}, bool)"/> already resolves it to
    /// the engine's id.
    /// </remarks>
    /// <param name="componentId">The component to iterate.</param>
    /// <param name="with">Component ids the entity must also carry.</param>
    /// <param name="without">Component ids the entity must not carry.</param>
    /// <param name="markChanged">Stamp every returned row with the current change tick.</param>
    public ChunkSet<T> Chunks<T>(
        int componentId,
        ReadOnlySpan<int> with = default,
        ReadOnlySpan<int> without = default,
        bool markChanged = true) where T : unmanaged
    {
        var capacity = 8;

        while (true)
        {
            var buffer = ArrayPool<NativeChunk>.Shared.Rent(capacity);
            int count;

            fixed (NativeChunk* output = buffer)
            fixed (int* withPtr = with)
            fixed (int* withoutPtr = without)
            {
                count = Native.bcs_ecs_chunks(
                    componentId,
                    withPtr, with.Length,
                    withoutPtr, without.Length,
                    markChanged ? 1 : 0,
                    output, buffer.Length);
            }

            if (count < 0)
            {
                ArrayPool<NativeChunk>.Shared.Return(buffer);
                Native.Check(count, $"Chunks<{typeof(T).Name}>");
            }

            if (count <= buffer.Length) return new ChunkSet<T>(buffer, count);

            // The world had more archetypes than the buffer could hold; grow and retry.
            ArrayPool<NativeChunk>.Shared.Return(buffer);
            capacity = count;
        }
    }

    /// <summary>
    /// Iterates every <typeparamref name="T"/> in the world by reference.
    /// </summary>
    /// <example>
    /// <code>
    /// foreach (var row in ctx.Ecs.Query&lt;Position&gt;())
    ///     row.Component.X += speed * dt;
    /// </code>
    /// </example>
    public ComponentQuery<T> Query<T>(bool markChanged = true) where T : unmanaged =>
        new(Chunks<T>(markChanged: markChanged));

    /// <summary>The entities carrying <typeparamref name="T"/>.</summary>
    /// <remarks>
    /// Only the entity handles are read, never the component itself, so this also answers for a
    /// name-only handle such as <see cref="Bevy.Children"/>. The chunks are typed as
    /// <see cref="byte"/> to say so.
    /// </remarks>
    public Entity[] EntitiesWith<T>() where T : unmanaged
    {
        using var chunks = Chunks<byte>(ComponentType<T>.EntityId, markChanged: false);
        var result = new Entity[chunks.TotalLength];
        var offset = 0;
        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i].Entities.CopyTo(result.AsSpan(offset));
            offset += chunks[i].Length;
        }

        return result;
    }
}
