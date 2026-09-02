using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// One contiguous run of a component's storage inside Bevy's tables.
/// </summary>
/// <remarks>
/// The pointers reference Bevy's own memory, so writing through <see cref="Components{T}"/>
/// updates the component in place with no copy. They stay valid only until the next
/// structural change to the world (a spawn, despawn, insert or remove), which is exactly why
/// behavior methods queue structural work on <see cref="EcsCommands"/> instead of applying it
/// mid-iteration.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct NativeChunk
{
    /// <summary>Pointer to <see cref="Length"/> entity handles.</summary>
    private readonly ulong* _entities;

    /// <summary>Pointer to <see cref="Length"/> * <see cref="Stride"/> component bytes.</summary>
    private readonly byte* _data;

    /// <summary>Number of entities in this run.</summary>
    public readonly int Length;

    /// <summary>Size in bytes of one component.</summary>
    public readonly int Stride;

    /// <summary>The entities in this run, in storage order.</summary>
    public ReadOnlySpan<Entity> Entities => new(_entities, Length);

    /// <summary>
    /// The component data as a writable span. Writes land directly in Bevy's storage.
    /// </summary>
    /// <typeparam name="T">The component type; must be the one the chunk was queried for.</typeparam>
    public Span<T> Components<T>() where T : unmanaged => new(_data, Length);

    /// <summary>The entity at <paramref name="index"/> within this run.</summary>
    public Entity EntityAt(int index) => new(_entities[index]);

    /// <summary>Base address of the component data, for callers that must cross a lambda.</summary>
    internal IntPtr DataPointer => (IntPtr)_data;

    /// <summary>Base address of the entity handles, for callers that must cross a lambda.</summary>
    internal IntPtr EntityPointer => (IntPtr)_entities;
}
