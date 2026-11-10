using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>The events placed on clips, by the numbers the bridge reports them under.</summary>
/// <remarks>
/// A clip holds the number alone, since the event is a C# value Bevy cannot hold, and the number
/// finds what to trigger. The table outlives an app, as the clips placed in one do not, which
/// costs a delegate for each event ever placed, of which a game places few.
/// </remarks>
internal static unsafe class ClipEvents
{
    private static readonly Lock Gate = new();
    private static readonly List<Action<ObserverRegistry, Entity>> Placed = [];

    /// <summary>Keeps an event, answering the number the bridge reports it under.</summary>
    internal static uint Keep<TEvent>(TEvent value) where TEvent : IAnimationEvent
    {
        lock (Gate)
        {
            Placed.Add((registry, at) => registry.TriggerAt(value, at));
            return (uint)(Placed.Count - 1);
        }
    }

    /// <summary>
    /// Asks the bridge to report the events this app's clips reach, once, as the first observer of
    /// an event placed on clips is added, which the animation's plugin has the registry ask.
    /// </summary>
    /// <remarks>A bridge without the renderer has no animation, answers so, and reports nothing.</remarks>
    internal static void Watch(ObserverRegistry registry, object? observed)
    {
        if (observed is not IAnimationEvent || !registry.FirstAsked(typeof(ClipEvents))) return;

        var status = NativeAnimation.bcs_observe_clip_events(ComponentRegistry.AppHandle, &Reported, registry.Handle);
        if (status != NativeStatus.Unsupported) Native.Check(status, "observing the events placed on clips");
    }

    /// <summary>Where the bridge reports a clip reaching an event placed on it, with the world on loan.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static void Reported(uint number, ulong entity, IntPtr user)
    {
        try
        {
            if (GCHandle.FromIntPtr(user).Target is not ObserverRegistry registry) return;
            Fire(registry, number, new Entity(entity));
        }
        catch (Exception ex)
        {
            // An exception crossing back into Rust is undefined, so it ends here, said.
            EngineLog.Error(null, "observer", $"[BevyCSharp] An observer threw: {ex}", ex);
        }
    }

    /// <summary>Triggers the event the bridge reported, at the entity it happened at.</summary>
    internal static void Fire(ObserverRegistry registry, uint number, Entity at)
    {
        Action<ObserverRegistry, Entity>? fire;
        lock (Gate) fire = number < Placed.Count ? Placed[(int)number] : null;
        fire?.Invoke(registry, at);
    }
}
