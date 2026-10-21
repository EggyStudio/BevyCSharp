namespace Bevy;

/// <summary>
/// What a material slot of an <see cref="EditMesh"/> is drawn with, a color, a texture and the maps
/// of the engine's material, each nothing until it is given.
/// </summary>
/// <remarks>
/// <para>
/// A look with nothing given is the prototype material of the slot's name
/// (<see cref="Modeling.Prototype(string)"/>), the grid tinted by the name's color, so a level
/// blocked out reads by color before it has textures. A color alone tints the grid in its place, and
/// a texture takes the grid's place, tinted by the color where one is given.
/// </para>
/// <para>
/// A path is read as the game reads its assets, relative to the asset root. Every texture repeats,
/// since a face's coordinates run past one as far as it is meters wide, and the maps are read as
/// data, without the color conversion a picture's colors take.
/// </para>
/// </remarks>
public sealed record SlotLook
{
    /// <summary>The color, as sRGB numbers from zero to one as a color picker shows them, with its alpha, or nothing.</summary>
    public Vec4? Color { get; init; }

    /// <summary>The picture the faces show, the material's base color texture, or nothing.</summary>
    public string? Texture { get; init; }

    /// <summary>The normal map, or nothing.</summary>
    public string? NormalMap { get; init; }

    /// <summary>The ambient occlusion map, or nothing.</summary>
    public string? OcclusionMap { get; init; }

    /// <summary>The map of metalness in its blue channel and roughness in its green, as glTF lays it out, or nothing.</summary>
    public string? MetallicRoughnessMap { get; init; }

    /// <summary>The emissive color, in linear numbers that may pass one, or nothing.</summary>
    public Vec3? Emissive { get; init; }

    /// <summary>The emissive map, emissive in white where no <see cref="Emissive"/> is given, or nothing.</summary>
    public string? EmissiveMap { get; init; }

    /// <summary>How metal the faces are, from zero to one, or nothing.</summary>
    public float? Metallic { get; init; }

    /// <summary>How rough the faces are, from zero to one, or nothing.</summary>
    public float? Roughness { get; init; }

    /// <summary>Whether nothing is given, so the slot is drawn with the prototype material of its name.</summary>
    public bool IsPrototype =>
        Color is null && Texture is null && NormalMap is null && OcclusionMap is null && MetallicRoughnessMap is null
        && Emissive is null && EmissiveMap is null && Metallic is null && Roughness is null;
}
