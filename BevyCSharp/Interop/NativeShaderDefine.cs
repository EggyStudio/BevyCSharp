namespace Bevy.Interop;

/// <summary>A name a shader is compiled with defined. Mirrors <c>BcsShaderDefine</c>.</summary>
public unsafe struct NativeShaderDefine
{
    /// <summary>NUL-terminated UTF-8.</summary>
    public byte* Name;

    /// <summary>0 a boolean, 1 a signed integer, 2 an unsigned one.</summary>
    public int Kind;

    /// <summary>The value, with non-zero as true for a boolean.</summary>
    public int Value;
}
