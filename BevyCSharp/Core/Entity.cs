using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// A handle to an entity in the Bevy world.
/// </summary>
/// <remarks>
/// This is Bevy's own <c>Entity</c>, bit for bit: an index paired with a generation counter.
/// The generation is what makes a stale handle detectable. Once an entity is despawned its
/// index is reused with a higher generation, so an old handle no longer matches and
/// <see cref="EcsWorld.IsAlive"/> reports false instead of silently addressing a new entity.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly unsafe struct Entity : IEquatable<Entity>
{
    /// <summary>
    /// The handle exactly as Bevy stores it.
    /// </summary>
    /// <remarks>
    /// Treat this as opaque. Bevy documents the bit layout as not meaningful, and it packs the
    /// index in a form that is not the number you would expect, so read the parts with
    /// <see cref="Index"/> and <see cref="Generation"/>. Comparing, hashing and round-tripping
    /// this value is fine, and is how handles travel across the boundary.
    /// </remarks>
    public readonly ulong Bits;

    /// <summary>Wraps a raw handle produced by the engine.</summary>
    public Entity(ulong bits) => Bits = bits;

    /// <summary>A handle that refers to no entity.</summary>
    public static Entity None => default;

    /// <summary>
    /// The entity's slot index, decoded by the engine.
    /// </summary>
    /// <remarks>
    /// Intended for logging and debugging. It costs a call into the bridge, so do not read it
    /// per entity in a hot loop, compare <see cref="Bits"/> instead.
    /// </remarks>
    public uint Index
    {
        get
        {
            uint index;
            Native.bcs_entity_parts(Bits, &index, null);
            return index;
        }
    }

    /// <summary>How many times this slot has been reused. See <see cref="Index"/> on cost.</summary>
    public uint Generation
    {
        get
        {
            uint generation;
            Native.bcs_entity_parts(Bits, null, &generation);
            return generation;
        }
    }

    /// <summary>True when this handle does not refer to an entity.</summary>
    public bool IsNone => Bits == 0;

    /// <inheritdoc/>
    public bool Equals(Entity other) => Bits == other.Bits;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Entity other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Bits.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => $"Entity({Index}v{Generation})";

    /// <summary>Compares two handles for equality.</summary>
    public static bool operator ==(Entity left, Entity right) => left.Bits == right.Bits;

    /// <summary>Compares two handles for inequality.</summary>
    public static bool operator !=(Entity left, Entity right) => left.Bits != right.Bits;
}
