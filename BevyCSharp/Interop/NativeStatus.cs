namespace Bevy.Interop;

/// <summary>Status codes returned across the C ABI. Negative values are failures.</summary>
public static class NativeStatus
{
    /// <summary>The call succeeded.</summary>
    public const int Ok = 0;

    /// <summary>A Rust panic was caught at the boundary.</summary>
    public const int Panic = -1;

    /// <summary>A required pointer argument was null.</summary>
    public const int NullArgument = -2;

    /// <summary>No world is loaned to this thread; the call was made outside a system.</summary>
    public const int NoWorld = -3;

    /// <summary>The entity does not exist or was already despawned.</summary>
    public const int NoEntity = -4;

    /// <summary>The component id was never registered.</summary>
    public const int NoComponent = -5;

    /// <summary>The entity does not carry the requested component.</summary>
    public const int NotPresent = -6;

    /// <summary>The supplied output buffer was too small.</summary>
    public const int BufferTooSmall = -7;

    /// <summary>The app was already run.</summary>
    public const int AlreadyRunning = -8;

    /// <summary>The operation is invalid at this point in the lifecycle.</summary>
    public const int InvalidState = -9;

    /// <summary>The native library was not built with the requested feature.</summary>
    public const int Unsupported = -10;

    /// <summary>Renders a status code as a diagnosable sentence.</summary>
    public static string Describe(int status) => status switch
    {
        Ok => "ok",
        Panic => "the native bridge panicked; see stderr for the Rust backtrace",
        NullArgument => "a required pointer argument was null",
        NoWorld =>
            "no Bevy world is available on this thread. ECS calls are only valid on the main "
            + "thread while a system is running. From a parallel behavior method, queue the "
            + "change on ctx.Cmd instead of calling ctx.Ecs",
        NoEntity => "the entity does not exist or has been despawned",
        NoComponent => "the component type was never registered with the Bevy world",
        NotPresent => "the entity does not carry that component",
        BufferTooSmall => "the output buffer was too small",
        AlreadyRunning => "the app has already been run and cannot be run again",
        InvalidState => "the operation is not valid at this point in the app lifecycle",
        Unsupported => "this native build does not support that operation",
        _ => $"unknown native status {status}",
    };

    /// <summary>Throws a <see cref="BevyNativeException"/> describing a failure code.</summary>
    public static void Throw(int status, string operation) =>
        throw new BevyNativeException(status, $"{operation} failed: {Said(status)}.");

    /// <summary>
    /// What a status says, with what the panic said for a panic, so the exception carries where
    /// the bridge failed rather than sending its reader to a stream a player's run never shows.
    /// </summary>
    private static string Said(int status)
    {
        if (status != Panic) return Describe(status);

        string panic;
        try
        {
            panic = CrashLog.LastPanic();
        }
        catch (Exception error) when (error is BevyNativeException or DllNotFoundException or EntryPointNotFoundException)
        {
            panic = string.Empty;
        }

        // The first line is the thread and the place, and the second what it said, and the stack
        // after them is the crash log's to write.
        var said = string.Join(' ', panic.Split('\n', 3).Take(2)).Trim();
        return said.Length == 0 ? Describe(status) : $"the native bridge panicked, {said}";
    }
}
