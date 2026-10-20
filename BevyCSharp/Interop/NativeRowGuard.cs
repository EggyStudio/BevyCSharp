using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's record of the row a behavior's method holds while it runs, its
/// <c>BcsRowGuard</c>, one a thread.
/// </summary>
/// <remarks>
/// The runner names the entity and the address of the component it handed the method, and the
/// bridge, as each call the method makes returns, sets <see cref="Moved"/> where the component is
/// no longer at that address, because the call moved the entity to another table, grew the table's
/// columns into new memory or took the entity or the component away. The offsets are the bridge's,
/// written out, so a field reordered on one side cannot read another field on the other.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Size = 32)]
internal struct NativeRowGuard
{
    /// <summary>The running entity's bits, read while <see cref="Running"/> is set.</summary>
    [FieldOffset(0)] public ulong Entity;

    /// <summary>The address of its component as the runner handed it to the method.</summary>
    [FieldOffset(8)] public nint Address;

    /// <summary>The component's id, as the bridge lists its runs by.</summary>
    [FieldOffset(16)] public int Component;

    /// <summary>Whether a walk is under way, so the bridge looks.</summary>
    [FieldOffset(20)] public uint Running;

    /// <summary>Set by the bridge where a call left the component elsewhere, or gone.</summary>
    [FieldOffset(24)] public uint Moved;
}
