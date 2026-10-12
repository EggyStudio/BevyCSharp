using System.Buffers;
using System.Runtime.CompilerServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>One entity and a writable reference to its component.</summary>
/// <typeparam name="T">The component type.</typeparam>
public readonly ref struct ComponentRow<T> where T : unmanaged
{
    private readonly ref T _component;

    /// <summary>The entity this row belongs to.</summary>
    public readonly Entity Entity;

    internal ComponentRow(Entity entity, ref T component)
    {
        Entity = entity;
        _component = ref component;
    }

    /// <summary>The component, by reference. Assigning to it writes into Bevy's storage.</summary>
    public ref T Component
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref _component;
    }
}
