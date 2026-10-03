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

    /// <summary>A cylinder, sized by radius and height.</summary>
    public const string Cylinder = "Cylinder";

    /// <summary>A cone, sized by the radius of its base and its height.</summary>
    public const string Cone = "Cone";

    /// <summary>A cone with its top cut off, sized by the top radius, the bottom radius and the height.</summary>
    public const string ConicalFrustum = "ConicalFrustum";

    /// <summary>A ring with a round cross-section, sized by its inner and outer radius.</summary>
    public const string Torus = "Torus";

    /// <summary>A flat disc facing the viewer, sized by radius.</summary>
    public const string Circle = "Circle";

    /// <summary>A flat ring facing the viewer, sized by its inner and outer radius.</summary>
    public const string Annulus = "Annulus";

    /// <summary>A flat rectangle facing the viewer, sized by width and height.</summary>
    public const string Rectangle = "Rectangle";

    /// <summary>A flat triangle, Bevy's default one scaled by the first number.</summary>
    public const string Triangle = "Triangle";

    /// <summary>A four-sided solid, Bevy's default one scaled by the first number.</summary>
    public const string Tetrahedron = "Tetrahedron";
}

/// <summary>How a primitive mesh was made, which is enough to make it again.</summary>
/// <param name="Shape">One of the constants on <see cref="MeshShape"/>.</param>
/// <param name="A">The first measure, as <see cref="Render.CreateMesh(string, float, float, float)"/> takes it.</param>
/// <param name="B">The second.</param>
/// <param name="C">The third.</param>
/// <remarks>
/// Kept beside the handle when the mesh is made, so a tool can show a cylinder as a cylinder with a
/// radius and a height rather than as a list of vertices, change one and build it again, and a scene
/// can write it down as what it is.
/// </remarks>
public readonly record struct MeshRecipe(string Shape, float A, float B, float C);

/// <summary>Which vertex attributes a mesh has, beyond its positions.</summary>
[Flags]
public enum MeshAttributes : uint
{
    /// <summary>Positions alone.</summary>
    None = 0,

    /// <summary>A normal a vertex, which lighting needs.</summary>
    Normals = 1,

    /// <summary>A tangent a vertex, which a normal map needs.</summary>
    Tangents = 2,

    /// <summary>Texture coordinates, which a texture needs.</summary>
    Uvs = 4,

    /// <summary>A second set of texture coordinates, usually for a light map.</summary>
    SecondUvs = 8,

    /// <summary>A color a vertex.</summary>
    Colors = 16,

    /// <summary>Which joints of a skeleton move a vertex.</summary>
    Joints = 32,

    /// <summary>How much each of those joints does.</summary>
    Weights = 64,
}

/// <summary>What a mesh is made of, read without copying its vertices.</summary>
/// <param name="Vertices">How many vertices.</param>
/// <param name="Indices">How many indices, or zero for a mesh drawn without them.</param>
/// <param name="IndexBits">16 or 32, the width of an index, or zero for none.</param>
/// <param name="Topology">What the vertices are joined into.</param>
/// <param name="Attributes">Which attributes it has beyond positions.</param>
/// <param name="Min">The corner of its bounds with the smallest coordinates.</param>
/// <param name="Max">The corner with the largest.</param>
public readonly record struct MeshInfo(
    int Vertices,
    int Indices,
    int IndexBits,
    MeshTopology Topology,
    MeshAttributes Attributes,
    Vec3 Min,
    Vec3 Max)
{
    /// <summary>How many triangles it draws, or zero for a mesh of lines or points.</summary>
    public int Triangles => Topology switch
    {
        MeshTopology.Triangles => (Indices > 0 ? Indices : Vertices) / 3,
        MeshTopology.TriangleStrip => Math.Max(0, (Indices > 0 ? Indices : Vertices) - 2),
        _ => 0,
    };

    /// <summary>How large its bounds are along each axis.</summary>
    public Vec3 Size => Max - Min;
}

/// <summary>How a mesh is treated beyond what it looks like. See <see cref="Render.SetMeshFlags"/>.</summary>
[Flags]
public enum MeshFlags : uint
{
    /// <summary>As Bevy has it, culled when out of view and casting and receiving shadows.</summary>
    None = 0,

    /// <summary>Never culled for being out of view, for a mesh its shader moves.</summary>
    NoFrustumCulling = 1,

    /// <summary>Casts no shadow.</summary>
    NoShadowCasting = 2,

    /// <summary>Has no shadow cast on it.</summary>
    NoShadowReceiving = 4,
}

/// <summary>How a mesh's vertices join up.</summary>
public enum MeshTopology
{
    /// <summary>Every three vertices, or three indices, are a triangle.</summary>
    Triangles = 0,

    /// <summary>Every two are a line.</summary>
    Lines = 1,

    /// <summary>Every one is a point.</summary>
    Points = 2,

    /// <summary>Each joins the one before it with a line.</summary>
    LineStrip = 3,

    /// <summary>Each makes a triangle with the two before it.</summary>
    TriangleStrip = 4,
}

/// <summary>A mesh described vertex by vertex, for <see cref="Render.CreateMesh(MeshData)"/>.</summary>
public sealed class MeshData
{
    /// <summary>Where each vertex is. Required.</summary>
    public Vec3[] Positions { get; set; } = [];

    /// <summary>Which way each vertex faces, or null to have them worked out for triangles.</summary>
    public Vec3[]? Normals { get; set; }

    /// <summary>Texture coordinates, two floats a vertex, or null.</summary>
    public float[]? Uvs { get; set; }

    /// <summary>Linear RGBA, four floats a vertex, or null.</summary>
    public float[]? Colors { get; set; }

    /// <summary>Which vertices make each shape, or null to take them in order.</summary>
    public uint[]? Indices { get; set; }

    /// <summary>How the vertices join up.</summary>
    public MeshTopology Topology { get; set; } = MeshTopology.Triangles;
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
    /// Suits foliage and chain-link fences. It keeps the depth buffer honest, so nothing has to be
    /// sorted, at the cost of a hard edge.
    /// </remarks>
    Mask = 1,

    /// <summary>Blend with what is behind.</summary>
    /// <remarks>
    /// Real transparency, and the expensive one. Blended surfaces are drawn after everything
    /// else and sorted back to front, so two of them overlapping can still be drawn in the wrong
    /// order.
    /// </remarks>
    Blend = 2,

    /// <summary>Add to what is behind, which never darkens it. For fire, glows and holograms.</summary>
    Add = 3,

    /// <summary>Multiply what is behind, which never lightens it. For stained glass and tints.</summary>
    Multiply = 4,

    /// <summary>Blend with color that has already been multiplied by its alpha.</summary>
    /// <remarks>
    /// Suits a texture exported premultiplied, and lets one material be partly additive. A pixel
    /// with color and zero alpha adds, and one with full alpha covers.
    /// </remarks>
    Premultiplied = 5,
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
}
