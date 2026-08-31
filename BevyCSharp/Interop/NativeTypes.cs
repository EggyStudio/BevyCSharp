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

    /// <summary>Seconds one <see cref="Stage.FixedUpdate"/> step covers.</summary>
    public double FixedDeltaSeconds;
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

    /// <summary>Bytes of typed text one frame can carry.</summary>
    public const int TextCapacity = 32;

    /// <summary>Simultaneous touches one frame can carry.</summary>
    public const int TouchCapacity = 8;

    /// <summary>Bit per mouse button currently held.</summary>
    public uint MouseDown;

    /// <summary>Bit per mouse button that went down this frame.</summary>
    public uint MousePressed;

    /// <summary>Bit per mouse button that went up this frame.</summary>
    public uint MouseReleased;

    /// <summary>Alignment padding; matches the native struct.</summary>
    public uint Padding;
    /// <summary>Bytes of <see cref="Text"/> that are in use.</summary>
    public uint TextLength;

    /// <summary>Entries of <see cref="Touches"/> that are in use.</summary>
    public uint TouchCount;

    /// <summary>What the keyboard produced this frame, as UTF-8.</summary>
    public fixed byte Text[TextCapacity];

    /// <summary>Touches in progress.</summary>
    public NativeTouchArray Touches;
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

/// <summary>One finger on a touchscreen, as the bridge reports it.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeTouch
{
    /// <summary>Identifies this finger while it stays down.</summary>
    public ulong Id;

    /// <summary>Position in physical window pixels.</summary>
    public float X;

    /// <summary>Position in physical window pixels.</summary>
    public float Y;

    /// <summary>0 held, 1 started this frame, 2 ended this frame.</summary>
    public int Phase;

    /// <summary>Padding, so the array strides evenly.</summary>
    public int Padding;
}

/// <summary>
/// A fixed-size run of <see cref="NativeTouch"/>, laid out inline in the input snapshot.
/// </summary>
/// <remarks>
/// C# has no <c>fixed</c> buffer of a struct type, so the slots are spelled out. The array is
/// short by design, so this stays smaller than the machinery to avoid it.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct NativeTouchArray
{
#pragma warning disable CS0649 // Written by the bridge, never by C#.
    private NativeTouch _0;
    private NativeTouch _1;
    private NativeTouch _2;
    private NativeTouch _3;
    private NativeTouch _4;
    private NativeTouch _5;
    private NativeTouch _6;
    private NativeTouch _7;
#pragma warning restore CS0649

    /// <summary>The touch at <paramref name="index"/>.</summary>
    public NativeTouch this[int index]
    {
        get
        {
            if ((uint)index >= NativeInput.TouchCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));

            return System.Runtime.CompilerServices.Unsafe.Add(
                ref System.Runtime.CompilerServices.Unsafe.AsRef(in _0), index);
        }
    }
}

/// <summary>How a camera should see, handed to the bridge when one is spawned.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeCameraConfig
{
    /// <summary>0 perspective, 1 orthographic.</summary>
    public int Projection;

    /// <summary>Vertical field of view in degrees. Perspective only.</summary>
    public float FovDegrees;

    /// <summary>World units visible vertically. Orthographic only.</summary>
    public float OrthoHeight;

    /// <summary>Nearest visible distance.</summary>
    public float Near;

    /// <summary>Furthest visible distance.</summary>
    public float Far;

    /// <summary>0 world clear colour, 1 the one below, 2 no clear.</summary>
    public int ClearMode;

    /// <summary>Clear colour red.</summary>
    public float ClearR;

    /// <summary>Clear colour green.</summary>
    public float ClearG;

    /// <summary>Clear colour blue.</summary>
    public float ClearB;

    /// <summary>Clear colour alpha.</summary>
    public float ClearA;

    /// <summary>Draw order; higher draws over lower.</summary>
    public int Order;
}

/// <summary>What kind of light to spawn and how it behaves.</summary>
[StructLayout(LayoutKind.Sequential)]
public struct NativeLightConfig
{
    /// <summary>0 directional, 1 point, 2 spot.</summary>
    public int Kind;

    /// <summary>Lux for a directional light, lumens for the other two.</summary>
    public float Intensity;

    /// <summary>Linear red.</summary>
    public float ColorR;

    /// <summary>Linear green.</summary>
    public float ColorG;

    /// <summary>Linear blue.</summary>
    public float ColorB;

    /// <summary>How far the light reaches. Point and spot only.</summary>
    public float Range;

    /// <summary>Radius of the emitting sphere. Point and spot only.</summary>
    public float Radius;

    /// <summary>Non-zero to cast shadows.</summary>
    public int Shadows;

    /// <summary>Radians of full brightness about the axis. Spot only.</summary>
    public float InnerAngle;

    /// <summary>Radians at which a spot light has fallen to nothing.</summary>
    public float OuterAngle;
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

    /// <summary>Graphics API to pin the renderer to; 0 leaves the choice to wgpu.</summary>
    public uint Backend;

    /// <summary>Fixed timestep rate in Hz; 0 keeps Bevy's default of 64.</summary>
    public double FixedHz;

    /// <summary>UTF-8 assets directory, or null for Bevy's default.</summary>
    public byte* AssetRoot;
}
