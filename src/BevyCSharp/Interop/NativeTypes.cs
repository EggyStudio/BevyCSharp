using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// One contiguous run of a component's storage inside Bevy's tables.
/// </summary>
/// <remarks>
/// The pointers reference Bevy's own memory, so writing through <see cref="Components{T}"/>
/// updates the component in place with no copy. They stay valid only until the next
/// structural change to the world (a spawn, despawn, insert or remove) - which is exactly why
/// behaviour methods queue structural work on <see cref="EcsCommands"/> instead of applying it
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

/// <summary>Frame timing mirrored from Bevy's <c>Time</c> resource.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeTime
{
    /// <summary>Seconds since app start.</summary>
    public double ElapsedSeconds;

    /// <summary>Seconds since the previous frame, clamped by Bevy's max delta.</summary>
    public double DeltaSeconds;

    /// <summary>Unclamped seconds since the previous frame.</summary>
    public double RawDeltaSeconds;

    /// <summary>Frames completed since app start.</summary>
    public ulong FrameCount;
}

/// <summary>
/// Input state mirrored from Bevy's <c>ButtonInput</c> and accumulated-mouse resources.
/// </summary>
/// <remarks>
/// Keyboard state arrives as a bitset indexed by <see cref="Key"/>. The bit order is pinned by
/// the key table in the native crate; <see cref="Key"/> declares the identical order.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeInput
{
    /// <summary>Number of 64-bit words in each keyboard bitset.</summary>
    public const int KeyWords = 2;

    /// <summary>Cursor X in physical window pixels.</summary>
    public float MouseX;

    /// <summary>Cursor Y in physical window pixels.</summary>
    public float MouseY;

    /// <summary>Cursor X movement since the previous frame.</summary>
    public float MouseDeltaX;

    /// <summary>Cursor Y movement since the previous frame.</summary>
    public float MouseDeltaY;

    /// <summary>Horizontal scroll since the previous frame.</summary>
    public float WheelX;

    /// <summary>Vertical scroll since the previous frame.</summary>
    public float WheelY;

    /// <summary>Bit per key currently held.</summary>
    public fixed ulong KeysDown[KeyWords];

    /// <summary>Bit per key that went down this frame.</summary>
    public fixed ulong KeysPressed[KeyWords];

    /// <summary>Bit per key that went up this frame.</summary>
    public fixed ulong KeysReleased[KeyWords];

    /// <summary>Bit per mouse button currently held.</summary>
    public uint MouseDown;

    /// <summary>Bit per mouse button that went down this frame.</summary>
    public uint MousePressed;

    /// <summary>Bit per mouse button that went up this frame.</summary>
    public uint MouseReleased;

    /// <summary>Alignment padding; matches the native struct.</summary>
    public uint Padding;
}

/// <summary>Everything C# refreshes at the top of a frame.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeFrameState
{
    /// <summary>Timing snapshot.</summary>
    public NativeTime Time;

    /// <summary>Input snapshot.</summary>
    public NativeInput Input;
}

/// <summary>App configuration handed to the native bridge at construction.</summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct NativeConfig
{
    /// <summary>UTF-8 window title.</summary>
    public byte* Title;

    /// <summary>Requested window width.</summary>
    public uint Width;

    /// <summary>Requested window height.</summary>
    public uint Height;

    /// <summary>Non-zero to present with vsync.</summary>
    public uint Vsync;

    /// <summary>Non-zero to skip window creation.</summary>
    public uint Headless;

    /// <summary>Frame cap for headless runs; 0 for uncapped.</summary>
    public uint HeadlessFps;

    /// <summary>Frames to run before exiting; 0 to run until exit is requested.</summary>
    public uint HeadlessFrames;
}
