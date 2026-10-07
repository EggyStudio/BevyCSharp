using Bevy.Interop;
using System.Runtime.CompilerServices;

namespace Bevy;

public sealed unsafe partial class App
{
    /// <summary>Runs <paramref name="hook"/> the first time a <typeparamref name="T"/> is put on an entity, Bevy's <c>on_add</c>.</summary>
    /// <remarks>
    /// <para>
    /// A component's hooks are its own, one of each kind, and run inside Bevy as the component goes on
    /// and comes off, before any observer and in the same command. They suit what has to stay in step
    /// with the component wherever it is put on or taken off, an index from a value to the entity
    /// holding it, as Bevy's <c>component_hooks</c> keeps. A hook reaches the app's managed side and
    /// queues what it changes in the world (<see cref="HookContext"/>).
    /// </para>
    /// <para>
    /// Bevy takes a component's hooks before any entity carries it, so they are given while the app
    /// is made, and given after an entity carries the component they are refused. Inserting a value
    /// over one already there runs <see cref="OnDiscard{T}"/> with the old value and then
    /// <see cref="OnInsert{T}"/> with the new, and taking it off runs <see cref="OnDiscard{T}"/>
    /// and then <see cref="OnRemove{T}"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// app.OnAdd((HookContext ctx, in Tag tag) => index[tag.Value] = ctx.Entity);
    /// app.OnRemove((HookContext ctx, in Tag tag) => ctx.Cmd.Despawn(ctx.Entity));
    /// </code>
    /// </example>
    /// <exception cref="BevyNativeException">An entity already carries the component.</exception>
    public App OnAdd<T>(ComponentHook<T> hook) where T : unmanaged => Hook(ComponentHooks.Add, hook);

    /// <summary>Runs <paramref name="hook"/> each time a <typeparamref name="T"/> is put on an entity, after <see cref="OnAdd{T}"/> the first time, Bevy's <c>on_insert</c>.</summary>
    /// <remarks>See <see cref="OnAdd{T}"/>.</remarks>
    /// <exception cref="BevyNativeException">An entity already carries the component.</exception>
    public App OnInsert<T>(ComponentHook<T> hook) where T : unmanaged => Hook(ComponentHooks.Insert, hook);

    /// <summary>Runs <paramref name="hook"/> with a <typeparamref name="T"/>'s value before it is overwritten or taken off, Bevy's <c>on_discard</c>.</summary>
    /// <remarks>See <see cref="OnAdd{T}"/>.</remarks>
    /// <exception cref="BevyNativeException">An entity already carries the component.</exception>
    public App OnDiscard<T>(ComponentHook<T> hook) where T : unmanaged => Hook(ComponentHooks.Discard, hook);

    /// <summary>Runs <paramref name="hook"/> as a <typeparamref name="T"/> comes off an entity, by removal or by despawn, Bevy's <c>on_remove</c>.</summary>
    /// <remarks>See <see cref="OnAdd{T}"/>.</remarks>
    /// <exception cref="BevyNativeException">An entity already carries the component.</exception>
    public App OnRemove<T>(ComponentHook<T> hook) where T : unmanaged => Hook(ComponentHooks.Remove, hook);

    private App Hook<T>(int kind, ComponentHook<T> hook) where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(hook);

        var world = World;
        ComponentHooks.DeclareGame<T>(kind, (entity, id, data) =>
            hook(new HookContext(entity, id, world), in Unsafe.AsRef<T>((void*)data)));
        return this;
    }
}
