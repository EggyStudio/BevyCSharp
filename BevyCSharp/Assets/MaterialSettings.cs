namespace Bevy;

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
/// matching factor rather than replacing it, so a base color map on a white base color shows the
/// map unchanged, and tinting it is a matter of setting a color. The image need not have
/// finished loading, because the material holds a handle rather than pixels.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var crate = Render.CreateMaterial(new MaterialSettings
/// { BaseColorTexture = AssetServer.Load(AssetKind.Image, "textures/crate.png"), Roughness = 0.8f,
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
    /// The three color channels are a luminance in nits, not a fraction of white, so the numbers
    /// that read as bright are far larger than one. The alpha decides whether the camera's exposure
    /// is applied to them, and at 1 it is. A camera left at Bevy's own exposure divides by about a
    /// thousand, so 12 nits arrives as a hundredth of white and 12000 arrives as twelve times it.
    /// It takes that much to blow out to white and to give bloom something to scatter.
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
    /// For anything modeled as a single sheet: a leaf, a flag, a curtain. It doubles the work
    /// for that surface, and lighting on the back face uses the front face's normal.
    /// </remarks>
    public bool DoubleSided { get; set; }

    /// <summary>Show the base color flat, with no lighting at all.</summary>
    /// <remarks>
    /// For a skybox, a UI panel in the world, or anything meant to read as its own color. It takes
    /// <see cref="Emissive"/> with it, because Bevy adds the emission inside the lighting, so an
    /// unlit material shows its base color and nothing else. A surface that should glow needs an
    /// emissive color and no unlit flag, and <see cref="BaseColor"/> can exceed one for a flat
    /// color brighter than white.
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

    /// <summary>
    /// How much light a non-metal reflects when seen head on, from zero to one, half by default.
    /// </summary>
    /// <remarks>
    /// Half is four percent, which water, plastic and most other things a scene is made of reflect.
    /// Raise it for a gemstone. Metals take their reflection from their color instead.
    /// </remarks>
    public float Reflectance { get; set; } = 0.5f;

    /// <summary>How strong a clear varnish over the surface is, from none at zero to one.</summary>
    /// <remarks>
    /// A second, glossy layer over the first, as a car's paint or a lacquered table has, which
    /// keeps a sharp highlight over a rough or colored base.
    /// </remarks>
    public float Clearcoat { get; set; }

    /// <summary>How rough that varnish is, from a mirror near zero to one.</summary>
    public float ClearcoatRoughness { get; set; } = 0.5f;

    /// <summary>
    /// How much light passes straight through, as through glass or water, from none at zero.
    /// </summary>
    /// <remarks>
    /// Drawn by sampling the picture of what is behind the surface, bent by
    /// <see cref="RefractiveIndex"/> through <see cref="Thickness"/>. Bevy's camera takes one such
    /// step by default, which shows what is behind one pane of glass and not a second pane through
    /// the first. Leave the material opaque, since the light it lets through is drawn by the
    /// transmission rather than by alpha.
    /// </remarks>
    public float Transmission { get; set; }

    /// <summary>
    /// How much light passes through and scatters on the way, as through a leaf, paper or wax,
    /// from none at zero.
    /// </summary>
    public float DiffuseTransmission { get; set; }

    /// <summary>How thick the material is where light passes through, in world units.</summary>
    public float Thickness { get; set; }

    /// <summary>How much light bends passing in, 1.5 for glass and 1.33 for water.</summary>
    public float RefractiveIndex { get; set; } = 1.5f;

    /// <summary>
    /// How far light travels inside before it has taken on <see cref="AttenuationColor"/>, in world
    /// units, or infinity for a material that tints nothing.
    /// </summary>
    /// <remarks>
    /// What makes thick glass greener at its edge than its face, or a deep pool bluer than a
    /// shallow one. Read with <see cref="Thickness"/>, since a ray's path inside is worked out from
    /// it, and seen only through a material that transmits.
    /// </remarks>
    public float AttenuationDistance { get; set; } = float.PositiveInfinity;

    /// <summary>The color light takes on inside, linear RGBA. White tints nothing.</summary>
    public (float R, float G, float B, float A) AttenuationColor { get; set; } = (1f, 1f, 1f, 1f);

    /// <summary>
    /// How much the highlight stretches along the surface, as brushed metal's and hair's do, from
    /// none at zero to one.
    /// </summary>
    /// <remarks>
    /// Stretched along the mesh's tangents, so a mesh needs them, which a glTF file exported with
    /// tangents has and a primitive generates.
    /// </remarks>
    public float AnisotropyStrength { get; set; }

    /// <summary>Radians the stretch is turned by, from the tangent.</summary>
    public float AnisotropyRotation { get; set; }

    /// <summary>Where the clearcoat is, in the red channel, multiplied by <see cref="Clearcoat"/>.</summary>
    public AssetHandle ClearcoatTexture { get; set; } = AssetHandle.None;

    /// <summary>How rough the clearcoat is, in the green channel, multiplied by <see cref="ClearcoatRoughness"/>.</summary>
    public AssetHandle ClearcoatRoughnessTexture { get; set; } = AssetHandle.None;

    /// <summary>The clearcoat's own normal map, so the varnish can be smooth over a bumpy surface or the other way round.</summary>
    public AssetHandle ClearcoatNormalTexture { get; set; } = AssetHandle.None;

    /// <summary>Where light passes straight through, in the red channel, multiplied by <see cref="Transmission"/>.</summary>
    public AssetHandle TransmissionTexture { get; set; } = AssetHandle.None;

    /// <summary>Where light passes through and scatters, in the alpha channel, multiplied by <see cref="DiffuseTransmission"/>.</summary>
    public AssetHandle DiffuseTransmissionTexture { get; set; } = AssetHandle.None;

    /// <summary>How thick the material is, in the green channel, multiplied by <see cref="Thickness"/>.</summary>
    public AssetHandle ThicknessTexture { get; set; } = AssetHandle.None;

    /// <summary>
    /// The stretch's direction in red and green and its strength in blue, turned by
    /// <see cref="AnisotropyRotation"/> and multiplied by <see cref="AnisotropyStrength"/>.
    /// </summary>
    public AssetHandle AnisotropyTexture { get; set; } = AssetHandle.None;

    /// <summary>What a baked lightmap's values are multiplied by, in nits, where one is the stored value.</summary>
    /// <remarks>
    /// A lightmap is light worked out ahead of time and stored in an image, which Bevy reads where
    /// an entity carries its <c>Lightmap</c> component. An image holds values from zero to one, or
    /// not much past it, while a lit scene is metered in the hundreds of nits, so a lightmap baked
    /// at one scale is shown at another by raising this, as Bevy's lightmap example raises it to
    /// 250. It changes nothing on a material no lightmap is given to.
    /// </remarks>
    public float LightmapExposure { get; set; } = 1f;
}
