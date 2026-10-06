namespace Bevy;

using Bevy.Interop;

/// <summary>
/// A material drawn by a <see cref="ShaderProgram"/>, made by
/// <see cref="Shaders.CreateMaterial(ShaderProgram, AlphaMode)"/>, whose values are set by name
/// through <see cref="ShaderValues"/>.
/// </summary>
/// <remarks>
/// A handle to an asset in the engine rather than the asset itself, so copies of it name the same
/// material, and it converts to the <see cref="AssetHandle"/> <see cref="Render.SetMaterial"/>
/// takes. One from <see cref="Shaders.MaterialOn"/> names whatever material an entity is drawn with
/// instead, and has no handle.
/// </remarks>
public readonly unsafe struct ShaderMaterial : IShaderValues, IEquatable<ShaderMaterial>
{
    private const int KindMaterial = 0;
    private const int KindEntity = 2;

    private readonly int _kind;
    private readonly long _id;

    /// <summary>A material by its asset handle.</summary>
    public ShaderMaterial(AssetHandle handle)
    {
        _kind = KindMaterial;
        _id = handle.Key;
    }

    private ShaderMaterial(Entity entity)
    {
        _kind = KindEntity;
        _id = unchecked((long)entity.Bits);
    }

    internal static ShaderMaterial OnEntity(Entity entity) => new(entity);

    (int Kind, long Id) IShaderValues.Target => (_kind, _id);

    /// <summary>Whether this names an entity's material rather than a material by its handle.</summary>
    internal bool IsEntity => _kind == KindEntity;

    /// <summary>
    /// The material's handle, or <see cref="AssetHandle.None"/> for one named by its entity.
    /// </summary>
    public AssetHandle Handle => _kind == KindMaterial ? new AssetHandle((int)_id) : AssetHandle.None;

    /// <summary>The material's handle.</summary>
    public static implicit operator AssetHandle(ShaderMaterial material) => material.Handle;

    /// <summary>
    /// Which program draws the material. Setting it has another program draw it, keeping every
    /// value by name. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// A value the new program does not declare is kept, unused, so switching back finds it again.
    /// </remarks>
    public ShaderProgram Program
    {
        get => new(Native.Check(
            Native.bcs_shader_target_program(_kind, _id),
            "reading which program draws a shader material"));
        set
        {
            if (!value.IsValid) throw new ArgumentException("No program was given.", nameof(value));

            Native.Check(
                Native.bcs_shader_material_configure(Material, value.Id, -1, 0, 0, 0),
                $"having shader program {value.Id} draw a material");
        }
    }

    /// <summary>
    /// Changes what the renderer does where the material is not opaque, which faces it leaves
    /// undrawn and how far its depth is pushed toward the camera. Only valid inside a system.
    /// </summary>
    public ShaderMaterial Configure(
        AlphaMode alpha,
        float cutoff = 0.5f,
        CullMode cull = CullMode.Back,
        float depthBias = 0)
    {
        Native.Check(
            Native.bcs_shader_material_configure(Material, -1, (int)alpha, cutoff, (int)cull, depthBias),
            "changing how a shader material is drawn");
        return this;
    }

    /// <summary>The asset key, for the calls that take a material by its handle alone.</summary>
    private int Material => _kind == KindMaterial
        ? (int)_id
        : throw new InvalidOperationException(
            "A material named by its entity is changed through its values. Its program and alpha "
            + "are changed on the material's own handle.");

    /// <inheritdoc />
    public bool Equals(ShaderMaterial other) => _kind == other._kind && _id == other._id;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ShaderMaterial other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_kind, _id);

    /// <inheritdoc />
    public override string ToString() =>
        _kind == KindMaterial ? $"ShaderMaterial({_id})" : $"ShaderMaterial(entity {_id})";

    /// <summary>Compares two materials.</summary>
    public static bool operator ==(ShaderMaterial left, ShaderMaterial right) => left.Equals(right);

    /// <summary>Compares two materials.</summary>
    public static bool operator !=(ShaderMaterial left, ShaderMaterial right) => !left.Equals(right);
}
