namespace Bevy;

/// <summary>The value a shader define has.</summary>
/// <remarks>
/// A boolean, a signed integer or an unsigned one. A false boolean is left undefined rather than
/// defined as zero, because <c>#ifdef</c> asks whether a name is present rather than what it holds,
/// so it reads as off.
/// </remarks>
public readonly record struct ShaderDefine
{
    internal ShaderDefineKind Kind { get; }

    internal int RawValue { get; }

    private ShaderDefine(ShaderDefineKind kind, int value)
    {
        Kind = kind;
        RawValue = value;
    }

    /// <summary>A boolean define.</summary>
    public static implicit operator ShaderDefine(bool value) =>
        new(ShaderDefineKind.Bool, value ? 1 : 0);

    /// <summary>A signed integer define.</summary>
    public static implicit operator ShaderDefine(int value) => new(ShaderDefineKind.Int, value);

    /// <summary>An unsigned integer define.</summary>
    public static implicit operator ShaderDefine(uint value) =>
        new(ShaderDefineKind.UInt, unchecked((int)value));

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        ShaderDefineKind.Bool => RawValue != 0 ? "true" : "false",
        ShaderDefineKind.UInt => unchecked((uint)RawValue).ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => RawValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
}
