using Bevy.Interop;

namespace Bevy;

/// <summary>
/// What was clicked in the scene.
/// </summary>
/// <remarks>
/// <para>
/// A click on a mesh, resolved by Bevy's own picking, which raycasts the scene against the
/// pointer. The interface is not reported here: a click that landed on a panel belongs to that
/// panel, and arrives through <see cref="Xui.Drain"/> with the element it hit.
/// </para>
/// <para>
/// Needs a bridge built with the editor profile, which <see cref="App.HasEditor"/> reports.
/// Drained rather than subscribed to, for the same reason the interface events are: a C# system
/// is handed the world and cannot hold an observer.
/// </para>
/// </remarks>
public static unsafe class Picking
{
    /// <summary>How many picks one call carries at most.</summary>
    private const int BatchSize = 16;

    /// <summary>Takes the scene entities clicked since the last call.</summary>
    public static Entity[] Drain()
    {
        if (!App.HasEditor) return [];

        var drained = new List<Entity>();
        var buffer = stackalloc ulong[BatchSize];

        int count;
        do
        {
            count = Native.Check(
                Native.bcs_pick_events(buffer, BatchSize), "draining the scene picks");

            for (var i = 0; i < count; i++) drained.Add(new Entity(buffer[i]));
        }
        while (count == BatchSize);

        return [.. drained];
    }
}
