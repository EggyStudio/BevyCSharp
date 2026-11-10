using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The raw C ABI of <c>bevy_csharp</c>, the Rust bridge that owns the Bevy app, for the bridge
/// itself, its version and build, the main thread, the app and its systems, the frame, the log and
/// the panics.
/// </summary>
/// <remarks>
/// <para>
/// Each area's entry points stand in a class of the area's own, <c>NativeAudio</c> and the like, so
/// that each goes with its area. Every such class runs <see cref="NativeLoader.Initialize"/> from
/// its static constructor, as this one does, so the library is resolved and its ABI compared with
/// <see cref="ExpectedAbiVersion"/> before the first call into it, whichever area makes that call.
/// </para>
/// <para>
/// These are deliberately not public. The engine's own types are the supported surface, and these
/// may change with any release.
/// </para>
/// </remarks>
internal static unsafe partial class Native
{
    /// <summary>Base name of the native library, resolved by <see cref="NativeLoader"/>.</summary>
    internal const string Library = "bevy_csharp";

    /// <summary>ABI revision this assembly was built against.</summary>
    internal const int ExpectedAbiVersion = 323;

    static Native() => NativeLoader.Initialize();

    // -- The bridge and its build

    /// <summary>Returns the ABI revision the loaded native library implements.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_abi_version();

    /// <summary>Returns 1 if the native library was built with the renderer compiled in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_render();

    /// <summary>Reports whether the caller is on the process main thread.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_is_main_thread();

    /// <summary>Reports whether the HTML and CSS surface is compiled in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_editor();

    /// <summary>Reports whether a game's assets are compiled in.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_embedded_assets();

    /// <summary>Reports whether the running app installed the interface.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_has_interface();

    // -- App lifecycle

    /// <summary>Creates the Bevy app. Returns <see cref="IntPtr.Zero"/> on failure.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial IntPtr bcs_app_create(NativeConfig* config);

    /// <summary>Destroys the app and everything it owns.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial void bcs_app_destroy(IntPtr app);

    /// <summary>Runs the app, blocking until exit, then runs the cleanup systems.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_run(IntPtr app);

    /// <summary>Asks the running app to shut down after the current frame.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_request_exit();

    /// <summary>Asks the running app to shut down after the current frame, ending the run with a code.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_request_exit_code(int code);

    /// <summary>Registers a C# system callback into a Bevy schedule, under the name Bevy reports it by.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_add_system(
        IntPtr app,
        int stage,
        string name,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

    /// <summary>Orders one C# system of a stage before another, by the numbers registration gave them.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_order_systems(IntPtr handle, int stage, int first, int then);

    /// <summary>Places a schedule of the game's own in Bevy's main order, after a stage.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_place_schedule(IntPtr app, string schedule, int after, uint singleThreaded);

    /// <summary>Registers a C# system in a schedule of the game's own, by the schedule's name.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_app_add_schedule_system(
        IntPtr app,
        string schedule,
        string name,
        delegate* unmanaged[Cdecl]<IntPtr, void> callback,
        IntPtr user);

    // -- The log and the panics

    /// <summary>Writes a line into Bevy's log at a level, from trace at zero to error at four.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_write(int level, string message);

    /// <summary>
    /// Takes the oldest line Bevy logged at the error level and answers its length, or 0 when none
    /// is kept. A buffer too small for the line leaves it kept and answers the length it needs.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_take_error(byte* output, int capacity);

    /// <summary>
    /// Starts or stops keeping every line Bevy logs for <see cref="bcs_log_take_line"/>.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_collect(int on);

    /// <summary>
    /// Takes the oldest line kept since <see cref="bcs_log_collect"/> with its level, from trace at
    /// zero to error at four, and answers its length, or 0 when none is kept.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_log_take_line(byte* output, int capacity, int* level);

    /// <summary>
    /// Gives the bridge the writer for a panic on a thread outside the guard, or none.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_crash_writer(delegate* unmanaged[Cdecl]<byte*, int, void> writer);

    /// <summary>
    /// Writes what the most recent panic in the bridge said, or nothing where none has happened.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_last_panic(byte* output, int capacity);

    /// <summary>
    /// Panics on purpose, inside the guard where <paramref name="guarded"/> is not zero and on a
    /// thread of its own otherwise, for a crash log to be tried.
    /// </summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_panic_on_purpose(int guarded);

    // -- Frame state

    /// <summary>Copies this frame's time and input snapshot out of Bevy.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_frame_state(NativeFrameState* state);

    /// <summary>Writes text into a caller-owned buffer, returning its length in bytes.</summary>
    /// <returns>
    /// The length the text needs, whether or not it fitted, or a negative status code.
    /// </returns>
    internal delegate int TextWriter(byte* buffer, int capacity);

    /// <summary>
    /// Reads a string out of an entry point that follows the text convention.
    /// </summary>
    /// <remarks>
    /// Text cannot be handed back in a fixed struct field the way a number can, and returning a
    /// pointer would leave the question of who frees it. So the caller owns the buffer and the
    /// bridge reports the length, so one call covers anything short and only longer text pays for a
    /// second call against a buffer sized from the first answer.
    /// </remarks>
    internal static string ReadText(TextWriter write, string operation)
    {
        const int Probe = 256;

        byte* probe = stackalloc byte[Probe];
        var length = Check(write(probe, Probe), operation);

        if (length == 0) return string.Empty;
        if (length <= Probe) return System.Text.Encoding.UTF8.GetString(probe, length);

        var buffer = new byte[length];
        fixed (byte* target = buffer)
        {
            var written = Check(write(target, length), operation);
            return System.Text.Encoding.UTF8.GetString(target, Math.Min(written, length));
        }
    }

    /// <summary>Throws if <paramref name="status"/> is a failure code.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Check(int status, string operation)
    {
        if (status < 0) NativeStatus.Throw(status, operation);
        return status;
    }

    /// <summary>What the most recent panic the bridge caught said, or nothing.</summary>
    internal static string LastPanic() =>
        ReadText((buffer, capacity) => bcs_last_panic(buffer, capacity), "reading the bridge's last panic");
}
