using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// What runs when a C# component leaves an entity, by removal or by despawn.
/// </summary>
/// <remarks>
/// <para>
/// A component holding a handle into a managed store (<see cref="EcsList{T}"/>) has to give the
/// store's slot back when it goes, or every despawn leaks the list it held. Bevy calls a component's
/// remove hook in both cases, so the generator registers one, from a module initializer, for every
/// component with such a field, and the hook frees each of them. A project's code does not call
/// this.
/// </para>
/// <para>
/// Cloning an entity needs the opposite. A copy of the component holds the same handles as the
/// original, and the first of the two to go would free the list both name, so the generator also
/// registers a clone hook that gives the copy a list of its own.
/// </para>
/// <para>
/// The hook is attached when the component is first registered with an app, because Bevy refuses
/// to change a component's hooks once an entity carries it. It runs inside Bevy's despawn, with the
/// world borrowed, so it must not reach into the world, and freeing a slot in a managed store does
/// not.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class ComponentHooks
{
    private static readonly object Gate = new();

    /// <summary>The hooks declared for each component type, by the generator.</summary>
    private static readonly Dictionary<Type, Action<IntPtr>> Declared = [];

    /// <summary>The clone hooks declared for each component type, by the generator.</summary>
    private static readonly Dictionary<Type, Action<IntPtr>> DeclaredClones = [];

    /// <summary>The hooks attached in the current app, by component id.</summary>
    private static readonly Dictionary<int, Action<IntPtr>> Attached = [];

    /// <summary>The clone hooks attached in the current app, by component id.</summary>
    private static readonly Dictionary<int, Action<IntPtr>> AttachedClones = [];

    private static int _generation = -1;

    /// <summary>
    /// The hooks a game gave its app for each component type, by kind, add, insert, discard and
    /// remove, each over the entity, the component's id and its bytes.
    /// </summary>
    /// <remarks>An app's own, so they are forgotten as the next app is made, as its attachments are.</remarks>
    private static readonly Dictionary<Type, Action<Entity, int, IntPtr>?[]> Games = [];

    /// <summary>The game's hooks attached in the current app, by component id.</summary>
    private static readonly Dictionary<int, Action<Entity, int, IntPtr>?[]> AttachedGames = [];

    private static int _gameGeneration = -1;

    /// <summary>The kind of hook Bevy runs as a component is first put on an entity.</summary>
    internal const int Add = 0;

    /// <summary>The kind Bevy runs each time one is put on.</summary>
    internal const int Insert = 1;

    /// <summary>The kind Bevy runs before a value is overwritten or taken off.</summary>
    internal const int Discard = 2;

    /// <summary>The kind Bevy runs as one is taken off, by removal or by despawn.</summary>
    internal const int Remove = 3;

    /// <summary>Declares what runs when a <typeparamref name="T"/> leaves an entity.</summary>
    /// <remarks>Called by generated module initializers.</remarks>
    public static void OnRemove<T>(RemoveHook<T> hook) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(hook);
        lock (Gate) Declared[typeof(T)] = data => hook(in Unsafe.AsRef<T>((void*)data));
    }

    /// <summary>Declares what rewrites the copy of a <typeparamref name="T"/> an entity clone receives.</summary>
    /// <remarks>Called by generated module initializers.</remarks>
    public static void OnClone<T>(CloneHook<T> hook) where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(hook);
        lock (Gate) DeclaredClones[typeof(T)] = data => hook(ref Unsafe.AsRef<T>((void*)data));
    }

    /// <summary>
    /// Attaches the hooks declared for <typeparamref name="T"/>, if there are any, to the id it has
    /// been registered under.
    /// </summary>
    internal static void Attach<T>(int id) where T : struct
    {
        Action<IntPtr>? removed;
        Action<IntPtr>? cloned;
        Action<Entity, int, IntPtr>?[]? game;
        lock (Gate)
        {
            Declared.TryGetValue(typeof(T), out removed);
            DeclaredClones.TryGetValue(typeof(T), out cloned);
            ForgetIfNewApp();
            Games.TryGetValue(typeof(T), out game);
            if (removed is null && cloned is null && game is null) return;

            // Ids belong to a world, so the previous app's attachments say nothing about this one.
            if (_generation != ComponentRegistry.Generation)
            {
                Attached.Clear();
                AttachedClones.Clear();
                _generation = ComponentRegistry.Generation;
            }

            if (removed is not null) Attached[id] = removed;
            if (cloned is not null) AttachedClones[id] = cloned;
            if (game is not null) AttachedGames[id] = game;
        }

        if (game is not null)
        {
            foreach (var kind in (ReadOnlySpan<int>)[Add, Insert, Discard])
            {
                if (game[kind] is null) continue;
                Native.Check(
                    Native.bcs_component_on_hook(ComponentRegistry.AppHandle, id, kind, &Hooked),
                    $"attaching a hook of {typeof(T).Name}, which Bevy takes before any entity carries it");
            }
        }

        if (removed is not null || game?[Remove] is not null)
        {
            Native.Check(
                Native.bcs_component_on_remove(ComponentRegistry.AppHandle, id, &Removed),
                $"attaching the remove hook of {typeof(T).Name}");
        }

        if (cloned is not null)
        {
            Native.Check(
                Native.bcs_component_on_clone(id, &Cloned),
                $"attaching the clone hook of {typeof(T).Name}");
        }
    }

    /// <summary>
    /// Declares a game's hook of a kind for <typeparamref name="T"/> in the app being made, and
    /// attaches it, registering the component with the app where it is not yet.
    /// </summary>
    /// <exception cref="BevyNativeException">An entity already carries the component.</exception>
    internal static void DeclareGame<T>(int kind, Action<Entity, int, IntPtr> hook) where T : unmanaged
    {
        lock (Gate)
        {
            ForgetIfNewApp();
            if (!Games.TryGetValue(typeof(T), out var hooks)) Games[typeof(T)] = hooks = new Action<Entity, int, IntPtr>?[4];
            hooks[kind] = hook;
        }

        // Registering attaches every hook declared so far, and attaching again a component already
        // registered reaches the one declared last, the bridge keeping the latest callback.
        Attach<T>(ComponentType<T>.Id);
    }

    // The game's hooks belong to the app they were given to, so the next app starts without them.
    private static void ForgetIfNewApp()
    {
        if (_gameGeneration == ComponentRegistry.Generation) return;
        Games.Clear();
        AttachedGames.Clear();
        _gameGeneration = ComponentRegistry.Generation;
    }

    /// <summary>What Bevy calls, for every component with a game's add, insert or discard hook.</summary>
    /// <remarks>As for <see cref="Removed"/>, a hook that throws is reported and Bevy carries on.</remarks>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Hooked(int kind, ulong entity, int component, byte* data)
    {
        try
        {
            Action<Entity, int, IntPtr>?[]? hooks;
            lock (Gate) AttachedGames.TryGetValue(component, out hooks);
            hooks?[kind]?.Invoke(new Entity(entity), component, (IntPtr)data);
        }
        catch (Exception error)
        {
            EngineLog.Error(null, "hook", $"A hook of component {component} on {new Entity(entity)} failed: {error.Message}", error);
        }
    }

    /// <summary>What Bevy calls, for every component with a clone hook, with the copy to rewrite.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Cloned(int component, byte* data)
    {
        // As in Removed, nothing may escape into the native clone, so a hook that throws leaves
        // the copy sharing what the original holds, which is reported rather than hidden.
        try
        {
            Action<IntPtr>? hook;
            lock (Gate) AttachedClones.TryGetValue(component, out hook);
            hook?.Invoke((IntPtr)data);
        }
        catch (Exception error)
        {
            EngineLog.Error(null, "hook", $"The clone hook of component {component} failed: {error.Message}", error);
        }
    }

    /// <summary>What Bevy calls, for every component with a hook, as it leaves an entity.</summary>
    /// <remarks>
    /// Nothing may escape into native code, which cannot unwind a managed exception, so a hook that
    /// throws is reported on the error stream and the despawn carries on without it.
    /// </remarks>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Removed(ulong entity, int component, byte* data)
    {
        try
        {
            Action<IntPtr>? hook;
            Action<Entity, int, IntPtr>?[]? game;
            lock (Gate)
            {
                Attached.TryGetValue(component, out hook);
                AttachedGames.TryGetValue(component, out game);
            }

            // The game's first, while the handles the generator's frees are still whole.
            game?[Remove]?.Invoke(new Entity(entity), component, (IntPtr)data);
            hook?.Invoke((IntPtr)data);
        }
        catch (Exception error)
        {
            EngineLog.Error(null, "hook", $"The remove hook of component {component} on {new Entity(entity)} failed: {error.Message}", error);
        }
    }
}
