namespace Bevy;

using Bevy.Interop;

/// <summary>
/// What a pass or a dispatch runs, a program and its values by name, made by
/// <see cref="Shaders.CreateInstance"/>.
/// </summary>
/// <remarks>
/// A number rather than an object, since it names something that lives in the engine. It belongs
/// to the app that made it, and means nothing to another.
/// </remarks>
public readonly struct ShaderInstance : IShaderValues, IEquatable<ShaderInstance>
{
    private const int KindInstance = 1;

    /// <summary>The instance's number plus one, so the default value names nothing.</summary>
    private readonly int _idPlusOne;

    internal ShaderInstance(int id) => _idPlusOne = id + 1;

    (int Kind, long Id) IShaderValues.Target => (KindInstance, Id);

    /// <summary>The number the engine knows this instance by.</summary>
    public int Id => _idPlusOne - 1;

    /// <summary>True when this names an instance rather than nothing.</summary>
    public bool IsValid => _idPlusOne > 0;

    /// <summary>
    /// Which program the instance runs. Setting it runs another, keeping every value by name. Only
    /// valid inside a system.
    /// </summary>
    public ShaderProgram Program
    {
        get => new(Native.Check(
            Native.bcs_shader_target_program(KindInstance, Id),
            $"reading which program shader instance {Id} runs"));
        set
        {
            if (!value.IsValid) throw new ArgumentException("No program was given.", nameof(value));

            Native.Check(
                Native.bcs_shader_instance_set_program(Id, value.Id),
                $"having shader instance {Id} run program {value.Id}");
        }
    }

    /// <inheritdoc />
    public bool Equals(ShaderInstance other) => _idPlusOne == other._idPlusOne;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ShaderInstance other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _idPlusOne;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"ShaderInstance({Id})" : "ShaderInstance(None)";

    /// <summary>Compares two instances.</summary>
    public static bool operator ==(ShaderInstance left, ShaderInstance right) => left.Equals(right);

    /// <summary>Compares two instances.</summary>
    public static bool operator !=(ShaderInstance left, ShaderInstance right) => !left.Equals(right);
}
