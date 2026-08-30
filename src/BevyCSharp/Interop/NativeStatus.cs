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
            + "thread while a system is running - from a parallel behaviour method, queue the "
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
        throw new BevyNativeException(status, $"{operation} failed: {Describe(status)}.");
}

/// <summary>Raised when a call into the native Bevy bridge fails.</summary>
public sealed class BevyNativeException : Exception
{
    /// <summary>The raw status code the bridge returned.</summary>
    public int Status { get; }

    /// <summary>Creates the exception for a failing status code.</summary>
    public BevyNativeException(int status, string message) : base(message) => Status = status;

    /// <summary>Creates the exception for a failing status code, wrapping a cause.</summary>
    public BevyNativeException(int status, string message, Exception? inner)
        : base(message, inner) => Status = status;
}
