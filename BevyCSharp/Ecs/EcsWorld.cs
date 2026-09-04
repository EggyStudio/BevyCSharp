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

    /// <summary>
    /// Spawns a scene asset under a new entity, and returns that entity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The entity comes back at once; the scene under it does not. Bevy spawns the world as its
    /// children when the asset has loaded, so no children on the first frame is the normal
    /// answer rather than a failure.
    /// </para>
    /// <para>
    /// <b>Wait for the children, not for <see cref="Bevy.WorldInstance"/>.</b> That component
    /// marks the spawn as done, but it can appear a frame before the entities it produced are
    /// visible, so treating it as "the scene is ready" is a race that fails about one run in
    /// three. Poll <see cref="ChildrenOf"/> until it returns something.
    /// </para>
    /// <para>
    /// Takes a glTF scene from <see cref="AssetServer.LoadGltfScene"/> or a <c>.scn.ron</c> world
    /// from <see cref="AssetKind.Scene"/>; both load as the same asset, so one call spawns
    /// either. Compose on top of what it produced the ordinary way, by walking
    /// <see cref="ChildrenOf"/> and adding components to the entities you find.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The handle names no scene, or no world is loaned.</exception>
    public Entity SpawnScene(AssetHandle scene)
    {
        var bits = Native.bcs_scene_spawn(scene.Key);
        if (bits == 0)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"SpawnScene failed: {scene} does not name a loaded scene asset, or no world is "
                + "loaned to this thread.");

        return new Entity(bits);
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

    /// <summary>
    /// Despawns an entity when a state leaves the value it belongs to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What takes a level away without a teardown system listing everything in it: each entity a
    /// screen spawns says which screen it belongs to, and leaving that screen takes it with it.
    /// The despawn is Bevy's own and reaches the entity's children like any other.
    /// </para>
    /// <para>
    /// It happens at the transition rather than in an <c>[OnExit]</c> method, so it covers every
    /// way out of the value, including one queued from somewhere that knows nothing about the
    /// entity.
    /// </para>
    /// </remarks>
    /// <param name="entity">The entity to tie to the state.</param>
    /// <param name="state">The value it belongs to.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone, or the state was never added with <see cref="App.AddState{TState}"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// [OnEnter(Screen.Playing)]
    /// public static void Build(BehaviorContext ctx)
    /// {
    ///     var enemy = ctx.Ecs.Spawn();
    ///     ctx.Ecs.DespawnOnExit(enemy, Screen.Playing);
    /// }
    /// </code>
    /// </example>
    public void DespawnOnExit<TState>(Entity entity, TState state) where TState : struct, Enum =>
        Native.Check(
            Native.bcs_state_despawn_on_exit(
                entity.Bits, StateRegistry.SlotOf<TState>(), StateRegistry.ToInt(state)),
            $"scoping {entity} to {typeof(TState).Name}");

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

    // -- Introspection
    //
    // What an editor needs and a game does not. Everything above answers a question about a
    // component the caller already knows about; these answer what is there at all.

    /// <summary>
    /// Every live entity in the world.
    /// </summary>
    /// <remarks>
    /// Every one, including the entities the engine spawned for itself: the window, the monitors,
    /// the cameras and the observers hung off them. Which of those are worth showing is a
    /// question about the tool rather than about the world, so nothing is filtered here.
    /// </remarks>
    public Entity[] All()
    {
        var count = Native.Check(Native.bcs_ecs_entities(null, 0), "counting the entities");
        if (count == 0) return [];

        var entities = new Entity[count];
        fixed (Entity* target = entities)
        {
            var written = Native.Check(
                Native.bcs_ecs_entities((ulong*)target, count), "reading the entities");

            // The world can gain one between the two calls, and the answer would then be a
            // buffer's worth of a longer list. Asking again is cheaper than locking the world.
            if (written != count) return All();
        }

        return entities;
    }

    /// <summary>
    /// The ids of the components an entity carries.
    /// </summary>
    /// <remarks>
    /// Ids rather than types, because most of them name a type this side has never heard of.
    /// <see cref="ComponentName"/> turns one into something to show.
    /// </remarks>
    public int[] ComponentsOf(Entity entity)
    {
        var count = Native.bcs_ecs_components_of(entity.Bits, null, 0);
        if (count == NativeStatus.NoEntity) return [];
        Native.Check(count, $"counting the components on {entity}");
        if (count == 0) return [];

        var components = new int[count];
        fixed (int* target = components)
        {
            var written = Native.Check(
                Native.bcs_ecs_components_of(entity.Bits, target, count),
                $"reading the components on {entity}");

            if (written != count) return ComponentsOf(entity);
        }

        return components;
    }

    /// <summary>
    /// What a component is called.
    /// </summary>
    /// <remarks>
    /// The other direction of resolving a component by name, and the one a tool showing an
    /// entity needs: it is handed ids and has to label them. A C# component answers with the
    /// managed type's full name, one of the engine's own with its Rust path.
    /// </remarks>
    /// <exception cref="BevyNativeException">No component has that id.</exception>
    public string ComponentName(int component) => Native.ReadText(
        (buffer, capacity) => Native.bcs_component_name(component, buffer, capacity),
        $"reading the name of component {component}");

    /// <summary>
    /// What an entity is called, or null when it is called nothing.
    /// </summary>
    /// <remarks>
    /// Most entities have no name. A name is something a scene or a caller gave it rather than
    /// something every entity carries, so null here is the ordinary answer and not a failure.
    /// </remarks>
    public string? NameOf(Entity entity)
    {
        var needed = Native.bcs_ecs_entity_name(entity.Bits, null, 0);
        if (needed == NativeStatus.NotPresent || needed == NativeStatus.NoEntity) return null;

        return Native.ReadText(
            (buffer, capacity) => Native.bcs_ecs_entity_name(entity.Bits, buffer, capacity),
            $"reading the name of {entity}");
    }

    /// <summary>
    /// Names an entity, or takes its name away when given nothing.
    /// </summary>
    /// <remarks>
    /// The one thing about an entity that is for people rather than for the program, which is
    /// why it is also the one an editor has to be able to write: a list of "Entity 42" is a list
    /// nobody can work in. Bevy's <c>Name</c> holds a string, so it is set through here rather
    /// than through the generic component API, which carries blittable structs only.
    /// </remarks>
    public void SetName(Entity entity, string? name) => Native.Check(
        Native.bcs_ecs_set_entity_name(entity.Bits, name ?? string.Empty),
        $"naming {entity}");

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
