using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// The bridge's entry points for diagnostics, Bevy's measurements, the profile of the frame, what
/// the bridge holds and the system it runs on.
/// </summary>
internal static unsafe partial class NativeDiagnostics
{
    // The bridge every entry point here is imported from, named once in Native.
    private const string Library = Native.Library;

    // Runs the loader's check before this class's first call, as Native's remarks say.
    static NativeDiagnostics() => NativeLoader.Initialize(typeof(NativeDiagnostics).Assembly);

    /// <summary>Registers a diagnostic in Bevy's store.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_diagnostic_register(string path, string suffix, uint history);

    /// <summary>Adds a measurement to a diagnostic, taken now.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_diagnostic_measure(string path, double value);

    /// <summary>Reads what a diagnostic holds now.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_diagnostic_read(string path, NativeDiagnostic* reading);

    /// <summary>Turns a diagnostic on or off.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_diagnostic_set_enabled(string path, int on);

    /// <summary>Every diagnostic in the store, a line each.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_diagnostics_list(byte* buffer, int capacity);

    /// <summary>Says which diagnostics Bevy's log prints, a path a line, or all where null.</summary>
    [LibraryImport(Library, StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_diagnostics_set_log_filter(string? paths);

    /// <summary>Writes what the bridge holds, as name and number pairs.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_memory_describe(byte* buffer, int capacity);

    /// <summary>Writes the OS, the kernel, the CPU, its cores and the memory installed, a line each, read once for the process.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_system_describe(byte* buffer, int capacity);

    /// <summary>Turns the bridge's frame counting on or off, clearing what was counted.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_profile_enable(int on);

    /// <summary>The render schedule's phases' times since the last read, and their names a line each.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_profile_phases(ulong* nanos, int count, byte* names, int capacity);

    /// <summary>Does nothing, through the guard every entry point takes, for timing a crossing alone.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_profile_noop();

    /// <summary>Reads what the bridge counted since the last read, and starts the count over.</summary>
    [LibraryImport(Library)]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    internal static partial int bcs_profile_take(NativeProfile* profile);
}
