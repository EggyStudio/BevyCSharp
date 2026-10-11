using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

public sealed unsafe partial class EcsWorld
{
    // Where each component was last set from, with the tick Bevy marked it changed at then, so a
    // later write that kept no place, as a behavior's ref does, is not credited to the older call.
    private readonly Dictionary<(ulong Entity, int Component), (string By, uint Tick)> _setters = [];

    /// <summary>
    /// Whether an entity's component was added since the running system last ran, as Bevy's
    /// <c>Ref::is_added</c> answers. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <see cref="Changed{T}"/> answers the same for a change, which an addition is too. A system
    /// sees what was added before its first run as added on that run, as Bevy has it.
    /// </remarks>
    public bool Added<T>(Entity entity) where T : unmanaged =>
        TryTicks<T>(entity, out var ticks) && (ticks.Flags & 2) != 0;

    /// <summary>
    /// Overwrites a component's value only where it differs from the one held, as Bevy's
    /// <c>set_if_neq</c> does, so what watches for changes sees a change only when there is one,
    /// and answers whether it wrote.
    /// </summary>
    /// <remarks>An entity without the component is given it, which is a change.</remarks>
    public bool SetIfNeq<T>(Entity entity, T component, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        where T : unmanaged
    {
        if (TryGet<T>(entity, out var held) && EqualityComparer<T>.Default.Equals(held, component)) return false;
        Set(entity, component, file, line);
        return true;
    }

    /// <summary>
    /// The file and line of the call that last changed an entity's component, as Bevy's
    /// <c>changed_by</c> gives it, or <see langword="null"/> where it is not known. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// Kept where <see cref="Config.TrackChangeLocations"/> is on, as Bevy keeps it under its
    /// <c>track_location</c> feature, for a change made with <see cref="Add{T}"/>,
    /// <see cref="Set{T}"/> or <see cref="SetIfNeq{T}"/>. A change made any other way, a behavior's <c>ref</c> among them or
    /// one of Bevy's systems, has no call to name, and answers <see langword="null"/> until the next
    /// such call. The place of every entity ever set is kept while the switch is on.
    /// </remarks>
    public string? ChangedBy<T>(Entity entity) where T : unmanaged =>
        _setters.TryGetValue((entity.Bits, ComponentType<T>.Id), out var setter)
        && TryTicks<T>(entity, out var ticks)
        && ticks.Changed == setter.Tick
            ? setter.By
            : null;

    // Keeps the place a call set a component from, where the switch is on.
    private void Wrote<T>(Entity entity, string file, int line) where T : unmanaged
    {
        if (Owner?.TrackChangeLocations != true || !TryTicks<T>(entity, out var ticks)) return;
        _setters[(entity.Bits, ComponentType<T>.Id)] = (World.Place(file, line), ticks.Changed);
    }

    private static bool TryTicks<T>(Entity entity, out NativeChangeTicks ticks) where T : unmanaged
    {
        NativeChangeTicks read;
        var answer = Native.bcs_ecs_change_ticks(entity.Bits, ComponentType<T>.Id, &read);
        ticks = read;
        if (answer is NativeStatus.NotPresent or NativeStatus.NoEntity) return false;
        Native.Check(answer, $"reading when {typeof(T).Name} of {entity} changed");
        return true;
    }
}
