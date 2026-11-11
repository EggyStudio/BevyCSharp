namespace Bevy.Interop;

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
