using System.Globalization;
using Bevy.Interop;

namespace Bevy;

/// <summary>What a program holds, read from the console, the command line and the editor's console alike.</summary>
/// <remarks>
/// <para>
/// A leak shows as a number that keeps climbing while a game is played as it would be, which no
/// single reading tells apart from a cache filling. So these are read at intervals over a long
/// play, as <c>build/soak.sh</c> does, and the readings of the second half compared with those of
/// the first.
/// </para>
/// <para>
/// Every figure is a name and a whole number on one line, for a script to read without parsing a
/// sentence. Managed memory is the garbage collector's, and the process's resident size, with the
/// most it has held since it started (<see cref="MemoryGuard.PeakBytes"/>), takes in what a
/// graphics driver holds, which on a software renderer is the device's memory too. The rest is the
/// bridge's, the bytes Rust's allocator holds, the entities and their indices, the asset handles
/// this side keeps, the assets of each kind Bevy holds, and where a renderer runs what wgpu has
/// asked the GPU for, its allocator's bytes and how many of each resource are alive, as
/// <c>gpu.</c> pairs. A resident size that climbs while every
/// <c>gpu.</c> count stands still is memory the driver keeps for itself.
/// </para>
/// </remarks>
internal static unsafe class ConsoleMemoryCommands
{
    /// <summary>Says what the program holds as it is, without a collection.</summary>
    /// <remarks>
    /// Read as the program left it, so a climb shows as it happens. <c>memory.collect</c> tells
    /// what is held apart from what waits to be collected.
    /// </remarks>
    [Command("memory", "What the program holds, as name and number pairs: managed memory, the process and its peak, the bridge's allocations, entities, assets and the GPU's resources")]
    internal static string Memory()
    {
        var info = GC.GetGCMemoryInfo();
        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"managed {GC.GetTotalMemory(forceFullCollection: false)} heap {info.HeapSizeBytes} gen2 {GC.CollectionCount(2)} process {MemoryGuard.ResidentBytes()} peak {MemoryGuard.PeakBytes()}");

        // The handles need a world, which a command asked from outside a frame has not got.
        if (ConsoleHost.World is not null)
            line += string.Create(CultureInfo.InvariantCulture, $" handles {Native.bcs_asset_live_count()}");

        return line + " " + Native.ReadText(Native.bcs_memory_describe, "reading what the bridge holds");
    }

    /// <summary>Says what the program holds after a full collection.</summary>
    /// <remarks>
    /// Collected twice around the finalizers, since an object a finalizer lets go of is collected
    /// only by the collection after it ran, and a native block a finalizer frees is counted gone
    /// only once it has.
    /// </remarks>
    [Command("memory.collect", "The same as memory, read after a full collection, so what is held shows apart from what waits to be collected")]
    internal static string Collected()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return Memory();
    }
}
