namespace Bevy;

/// <summary>The mesh primitives the engine can build without an asset file.</summary>
public static class MeshShape
{
    /// <summary>A box, sized by width, height and depth.</summary>
    public const string Cuboid = "Cuboid";

    /// <summary>A sphere, sized by radius.</summary>
    public const string Sphere = "Sphere";

    /// <summary>A flat plane on the XZ axes, sized by width and depth.</summary>
    public const string Plane = "Plane";

    /// <summary>A capsule, sized by radius and length.</summary>
    public const string Capsule = "Capsule";
}

/// <summary>What a material does where it is not fully opaque.</summary>
public enum AlphaMode
{
    /// <summary>Ignore alpha entirely. The cheapest, and the right default.</summary>
    Opaque = 0,

    /// <summary>
    /// Draw a pixel or skip it, deciding at <see cref="MaterialSettings.AlphaCutoff"/>.
    /// </summary>
    /// <remarks>
    /// What foliage and chain-link fences want: it keeps the depth buffer honest, so nothing has
    /// to be sorted, at the cost of a hard edge.
    /// </remarks>
    Mask = 1,

    /// <summary>Blend with what is behind.</summary>
    /// <remarks>
    /// Real transparency, and the expensive one: blended surfaces are drawn after everything
    /// else and sorted back to front, so two of them overlapping can still be drawn in the wrong
    /// order.
    /// </remarks>
    Blend = 2,

    /// <summary>Add to what is behind, which never darkens it. For fire, glows and holograms.</summary>
    Add = 3,
}

/// <summary>
/// Everything a physically based material is made of.
/// </summary>
/// <remarks>
/// <para>
/// Every value has a usable default, so setting one property and leaving the rest is the normal
/// way to use this.
/// </para>
/// <para>
/// A texture is an image handle from <see cref="AssetServer.Load"/>, and is combined with the
/// matching factor rather than replacing it: a base color map on a white base color shows the
/// map unchanged, and tinting it is a matter of setting a color. The image need not have
/// finished loading, because the material holds a handle rather than pixels.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var crate = Render.CreateMaterial(new MaterialSettings
/// {
///     BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/crate.png"),
///     Roughness = 0.8f,
/// });
/// </code>
/// </example>
public sealed class MaterialSettings
{
    /// <summary>Base color, linear RGBA. White by default, so a texture shows unchanged.</summary>
    public (float R, float G, float B, float A) BaseColor { get; set; } = (1f, 1f, 1f, 1f);

    /// <summary>Zero for a dielectric, one for a metal. Values between are rarely physical.</summary>
    public float Metallic { get; set; }

    /// <summary>Near zero for a mirror, one for a matte surface.</summary>
    public float Roughness { get; set; } = 0.5f;

    /// <summary>
    /// Light the surface gives off, which no lamp affects. Black by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The three color channels are a luminance in nits, not a fraction of white, so the
    /// numbers that read as bright are far larger than one. The alpha decides whether the
    /// camera's exposure is applied to them, and at 1 it is: a camera left at Bevy's own
    /// exposure divides by about a thousand, so 12 nits arrives as a hundredth of white and 12000
    /// arrives as twelve times it. That is what it takes to blow out to white and to give bloom
    /// something to scatter.
    /// </para>
    /// <para>
    /// Set the alpha to 0 to opt out of that scaling and have the numbers mean multiples of white
    /// directly, which suits an effect tuned by eye rather than in physical units.
    /// </para>
    /// <para>
    /// Nothing here is drawn on a material that is also <see cref="Unlit"/>, because Bevy adds
    /// the emission as part of the lighting that flag skips.
    /// </para>
    /// </remarks>
    public (float R, float G, float B, float A) Emissive { get; set; } = (0f, 0f, 0f, 1f);

    /// <summary>What to do where the material is not fully opaque.</summary>
    public AlphaMode AlphaMode { get; set; } = AlphaMode.Opaque;

    /// <summary>Where a masked material stops drawing.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>
    /// Draw back faces as well as front ones.
    /// </summary>
    /// <remarks>
    /// For anything modelled as a single sheet: a leaf, a flag, a curtain. It doubles the work
    /// for that surface, and lighting on the back face uses the front face's normal.
    /// </remarks>
    public bool DoubleSided { get; set; }

    /// <summary>Show the base color flat, with no lighting at all.</summary>
    /// <remarks>
    /// For a skybox, a UI panel in the world, or anything meant to read as its own color. It
    /// takes <see cref="Emissive"/> with it: Bevy adds the emission inside the lighting, so an
    /// unlit material shows its base color and nothing else. A surface that should glow wants
    /// an emissive color and no unlit flag, and <see cref="BaseColor"/> can exceed one if what
    /// is wanted is a flat color brighter than white.
    /// </remarks>
    public bool Unlit { get; set; }

    /// <summary>The base color map, which is the texture people mean by "the texture".</summary>
    public AssetHandle BaseColorTexture { get; set; } = AssetHandle.None;

    /// <summary>
    /// A tangent-space normal map, which fakes detail the geometry does not have.
    /// </summary>
    /// <remarks>Must not be loaded as sRGB; a normal map holds directions rather than colors.</remarks>
    public AssetHandle NormalMap { get; set; } = AssetHandle.None;

    /// <summary>
    /// Metallic in the blue channel and roughness in the green, as glTF packs them.
    /// </summary>
    public AssetHandle MetallicRoughnessTexture { get; set; } = AssetHandle.None;

    /// <summary>Where the surface glows, multiplied by <see cref="Emissive"/>.</summary>
    public AssetHandle EmissiveTexture { get; set; } = AssetHandle.None;

    /// <summary>Where ambient light fails to reach, as a single channel.</summary>
    public AssetHandle OcclusionTexture { get; set; } = AssetHandle.None;

    /// <summary>
    /// How many times the texture repeats across the surface.
    /// </summary>
    /// <remarks>
    /// The other half of tiling. A mesh's UVs run from zero to one however large it is, so a
    /// floor drawn with a repeating texture still shows one stretched copy until this is raised.
    /// The texture must also have been loaded with <see cref="TextureWrap.Repeat"/>, or the
    /// values past one are clamped to the edge pixel.
    /// </remarks>
    public (float U, float V) UvScale { get; set; } = (1f, 1f);

    /// <summary>Radians the texture is turned by.</summary>
    public float UvRotation { get; set; }

    /// <summary>How far the texture is shifted, in UV units.</summary>
    public (float U, float V) UvOffset { get; set; }
}
