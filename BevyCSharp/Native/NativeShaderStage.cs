namespace Bevy.Interop;

/// <summary>A file, or Slang source, and an entry point in it. Mirrors <c>BcsShaderStage</c>.</summary>
public unsafe struct NativeShaderStage
{
    /// <summary>NUL-terminated UTF-8, or null to leave the stage out.</summary>
    public byte* Path;

    /// <summary>NUL-terminated UTF-8, or null for the usual name.</summary>
    public byte* Entry;

    /// <summary>Slang source, in place of a path, or null.</summary>
    public byte* Source;
}
