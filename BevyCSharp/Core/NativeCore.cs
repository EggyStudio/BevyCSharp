using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for the core, the ECS with its components, their reflection, the
/// hierarchy and the observers, the states and stepping, the windows, input, the clock and colors.
/// </summary>
/// <remarks>
/// Every entry point that touches the ECS is <em>ambient</em>, meaning it takes no world handle and
/// instead operates on the world Bevy has loaned to the currently running system callback on this
/// thread. That is why they are only valid from inside a system, on the main thread. Calls from
/// anywhere else fail with <see cref="NativeStatus.NoWorld"/> rather than corrupting state, which
/// steers parallel behavior methods onto <see cref="EcsCommands"/> instead.
/// </remarks>
internal static unsafe partial class NativeCore
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeCore() => NativeLoader.Initialize();

    // -- Asset sources

    /// <summary>
    /// Hands the bridge a reader over the assets the game's assembly carries and the paths it
    /// carries, newline-joined, or takes them back with a null reader. Read by the next app built.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_assets_carried(delegate* unmanaged[Cdecl]<byte*, nuint, byte*, nuint, long> read, byte* paths, nuint pathsLength);

    /// <summary>Names a folder as an asset source of its own for the next app, or forgets them all with a null root.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_asset_source_add(string? name, string? root);

    // -- The graphics adapter

    /// <summary>Describes the graphics adapter the renderer chose, as UTF-8.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_render_adapter(byte* buffer, int capacity);

    // -- Colors

    /// <summary>Converts four components in one of Bevy's color spaces to linear RGBA.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_color_from(int space, float* components, float* linear);

    /// <summary>Converts linear RGBA to four components in one of Bevy's color spaces.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_color_to(int space, float* linear, float* components);

    // -- The clock

    /// <summary>Pauses or resumes the game's clock and sets its speed, a negative speed leaving it.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_set_virtual(int paused, float speed);

    /// <summary>Writes whether the game's clock is paused and how fast it runs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_virtual(int* paused, float* speed);

    /// <summary>Sets the seconds each frame advances the clock by, zero for the machine's clock.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_set_frame_seconds(double seconds);

    /// <summary>Writes the seconds each frame advances the clock by, zero for the machine's clock.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_frame_seconds(double* seconds);

    /// <summary>Sets what holds a set clock still from the next frame, 1 a load and 2 until cleared.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_set_hold(uint holds);

    /// <summary>Writes what holds a set clock still, 1 a load and 2 until cleared.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_hold(uint* holds);

    /// <summary>Writes how far the fixed clock has run past its last step, as a share of one.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_time_fixed_overstep(float* fraction);

    // -- Input

    /// <summary>Says something as the input method would.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_ime(int kind, byte* text, uint length, int start, int end);

    /// <summary>Moves, presses or releases the pointer, as though a hand had.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_pointer(float x, float y, int action, int button);

    /// <summary>Rolls the wheel, as though a hand had.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_wheel(float x, float y, int unit);

    /// <summary>Copies the connected gamepads out, answering how many there are.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepads(NativeGamepad* gamepads, int capacity);

    /// <summary>Copies the logical keys held, pressed or released this frame out, answering how many there are.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_logical_keys(NativeLogicalKey* keys, int capacity);

    /// <summary>Rumbles a pad, or stops it where the seconds are zero or less.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepad_rumble(ulong entity, float strong, float weak, float seconds);

    /// <summary>Connects a pretended pad, answering its entity.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial ulong bcs_gamepad_connect(byte* name, uint length);

    /// <summary>Disconnects a pretended pad.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepad_disconnect(ulong entity);

    /// <summary>Sets one of a pad's buttons.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepad_button(ulong entity, int button, float value);

    /// <summary>Sets one of a pad's axes.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_gamepad_axis(ulong entity, int axis, float value);

    /// <summary>Presses or releases a key, with whatever text it typed.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_key(int key, int action, byte* text, uint length);

    /// <summary>Copies Bevy's input messages since the last call, answering how many.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_input_messages(NativeInputMessage* messages, int capacity);
}
