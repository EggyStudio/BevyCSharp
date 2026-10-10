using System.Diagnostics.CodeAnalysis;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Builds renderable assets and attaches them to entities.
/// </summary>
/// <remarks>
/// <para>
/// Everything here needs a native build with the renderer compiled in. On a headless build the
/// calls report that rather than failing obscurely, so the same behavior code runs either way and
/// draws nothing.
/// </para>
/// <para>
/// Meshes and materials are Rust values that have to be constructed rather than described by a
/// layout, and the components carrying them hold a typed handle that raw bytes cannot represent.
/// That is why these are named operations rather than a component written through
/// <see cref="EcsWorld.Add{T}"/>, the way <see cref="Transform"/> is.
/// </para>
/// </remarks>
public static unsafe partial class Render
{
    /// <summary>
    /// Builds a mesh primitive and returns a handle to it.
    /// </summary>
    /// <param name="shape">
    /// One of the constants on <see cref="MeshShape"/>, a band around a flat one from
    /// <see cref="MeshShape.Ring"/>, a flat one made solid by <see cref="MeshShape.Extrusion"/>, or a
    /// sector or a segment with its image turned by <see cref="MeshShape.UvAngle"/>.
    /// </param>
    /// <param name="a">
    /// Width for a cuboid, plane or rectangle, radius for a sphere, capsule, cylinder, cone or
    /// circle, top radius for a conical frustum, inner radius for a torus or annulus, and the scale
    /// for a triangle or tetrahedron.
    /// </param>
    /// <param name="b">
    /// Height for a cuboid, cylinder, cone or rectangle, depth for a plane, length for a capsule,
    /// bottom radius for a conical frustum, outer radius for a torus or annulus, and the slices
    /// around a UV sphere.
    /// </param>
    /// <param name="c">
    /// Depth for a cuboid, height for a conical frustum, the rings of a UV sphere from pole to pole,
    /// the band's width for a ring, the depth of an extrusion and the angle an image is turned by,
    /// whose first two numbers measure the flat shape they are made from.
    /// </param>
    /// <remarks>
    /// The shape and its measures are kept beside the handle (<see cref="RecipeOf"/>), so the mesh
    /// can be shown and saved as the shape it is.
    /// </remarks>
    public static AssetHandle CreateMesh(string shape, float a = 1f, float b = 1f, float c = 1f)
    {
        ArgumentException.ThrowIfNullOrEmpty(shape);

        var key = Native.bcs_mesh_create(shape, a, b, c);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a mesh");
        if (key == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"'{shape}' is not a mesh primitive the engine can build. Use one of the "
                + "constants on MeshShape.");

        Native.Check(key, $"building a {shape} mesh");

        lock (Recipes) Recipes[key] = new MeshRecipe(shape, a, b, c);
        lock (Built) Built.Remove(key);
        return new AssetHandle(key);
    }

    /// <summary>How each primitive mesh was made, by its key.</summary>
    private static readonly Dictionary<int, MeshRecipe> Recipes = [];

    /// <summary>
    /// The shape and measures a mesh was made from, or <see langword="null"/> for one that was not
    /// made by <see cref="CreateMesh(string, float, float, float)"/>.
    /// </summary>
    public static MeshRecipe? RecipeOf(AssetHandle mesh)
    {
        lock (Recipes) return Recipes.TryGetValue(mesh.Key, out var recipe) ? recipe : null;
    }

    /// <summary>
    /// Forgets how every mesh was made, for an app starting, whose keys say nothing about the last
    /// app's meshes.
    /// </summary>
    /// <remarks>
    /// A key is a slot in a table the bridge keeps per app, so the first mesh of a new app takes a
    /// key an old one had, and a recipe kept from before would describe the wrong mesh.
    /// </remarks>
    internal static void ForgetMade()
    {
        lock (Recipes) Recipes.Clear();
        lock (Built) Built.Clear();
    }

    /// <summary>Forgets how a mesh was made, once its handle has been released.</summary>
    /// <remarks>
    /// A released key is given to the next asset made, with another generation, so a recipe kept
    /// for it would describe nothing and only take room.
    /// </remarks>
    internal static void Forget(AssetHandle mesh)
    {
        lock (Recipes) Recipes.Remove(mesh.Key);
        lock (Built) Built.Remove(mesh.Key);
    }

    /// <summary>The geometry each mesh built vertex by vertex was made from, by its key.</summary>
    private static readonly Dictionary<int, MeshData> Built = [];

    /// <summary>
    /// Builds a primitive again with other measures, in place, so everything drawn with the mesh
    /// changes and keeps its handle.
    /// </summary>
    /// <remarks>
    /// The mesh is replaced whole and its recipe with it, so <see cref="RecipeOf"/> and a scene
    /// written afterward give the new measures. A mesh from a <see cref="MeshFiles">mesh file</see>
    /// is written back to it.
    /// </remarks>
    /// <returns>Whether the handle named a mesh and the shape is one of Bevy's primitives.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static bool RebuildMesh(AssetHandle mesh, string shape, float a = 1f, float b = 1f, float c = 1f)
    {
        ArgumentException.ThrowIfNullOrEmpty(shape);

        var status = Native.bcs_mesh_rebuild(mesh.Key, shape, a, b, c);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Rebuilding a mesh");
        if (status != NativeStatus.Ok) return false;

        lock (Recipes) Recipes[mesh.Key] = new MeshRecipe(shape, a, b, c);
        lock (Built) Built.Remove(mesh.Key);
        MeshFiles.Save(mesh);
        return true;
    }

    /// <summary>
    /// The geometry a mesh was built from, or <see langword="null"/> for one that was not made by
    /// <see cref="CreateMesh(MeshData)"/>.
    /// </summary>
    /// <remarks>
    /// A copy taken when the mesh was made, normals, UVs and colors included, so a scene can write
    /// the mesh down as it was built, which reading it back from the GPU's copy could not, since that
    /// gives positions and indices alone. Changing what this returns changes nothing drawn.
    /// </remarks>
    public static MeshData? DataOf(AssetHandle mesh)
    {
        lock (Built) return Built.TryGetValue(mesh.Key, out var data) ? data : null;
    }

    /// <summary>
    /// What a mesh is made of: its counts, its attributes and its bounds, read without copying
    /// its vertices.
    /// </summary>
    /// <returns>Whether the mesh could be read, which one still loading cannot.</returns>
    public static bool TryGetMeshInfo(AssetHandle mesh, out MeshInfo info)
    {
        info = default;
        if (!mesh.IsValid) return false;

        NativeMeshInfo read;
        var answer = Native.bcs_render_mesh_info(mesh.Key, &read);
        if (answer is NativeStatus.NotPresent or NativeStatus.InvalidState or NativeStatus.Unsupported)
            return false;
        Native.Check(answer, $"reading what {mesh} holds");

        info = new MeshInfo(
            (int)read.Vertices,
            (int)read.Indices,
            (int)read.IndexBits,
            // The bridge numbers these as wgpu lists them, which is not the order this enum has.
            read.Topology switch
            {
                1 => MeshTopology.TriangleStrip,
                2 => MeshTopology.Lines,
                3 => MeshTopology.LineStrip,
                4 => MeshTopology.Points,
                _ => MeshTopology.Triangles,
            },
            (MeshAttributes)read.Attributes,
            new Vec3(read.MinX, read.MinY, read.MinZ),
            new Vec3(read.MaxX, read.MaxY, read.MaxZ));
        return true;
    }

    /// <summary>
    /// Works out tangents for a mesh that has none, from its normals and texture coordinates, as a
    /// normal map and anisotropy read them, which Bevy's primitives are made without.
    /// </summary>
    /// <param name="mesh">A mesh that has loaded, such as one <see cref="CreateMesh(string, float, float, float)"/> made.</param>
    /// <returns>
    /// Whether it has tangents now, which a mesh with no normals, no texture coordinates or no
    /// indexed triangles cannot be given, and a headless build gives none, drawing nothing they are
    /// read by.
    /// </returns>
    /// <remarks>
    /// A sphere with an anisotropic material and no tangents is drawn as a blaze of white, since the
    /// direction the surface is brushed in is read from them, as Bevy's <c>anisotropy</c> example
    /// makes its sphere with <c>with_generated_tangents</c>. A mesh with tangents keeps them.
    /// </remarks>
    public static bool GenerateTangents(AssetHandle mesh)
    {
        if (!mesh.IsValid) return false;

        var answer = Native.bcs_render_mesh_generate_tangents(mesh.Key);
        if (answer is NativeStatus.NotPresent or NativeStatus.InvalidState or NativeStatus.Unsupported) return false;

        Native.Check(answer, $"working out the tangents of {mesh}");
        return true;
    }

    /// <summary>
    /// A standard material's settings, read back from the engine, whether code made the material or
    /// a glTF file brought it.
    /// </summary>
    /// <remarks>
    /// The inverse of <see cref="CreateMaterial(MaterialSettings)"/>, so what comes back makes the
    /// same material again. Each texture comes back as the handle the program already holds for it,
    /// or a new one.
    /// </remarks>
    /// <returns>Whether there was a standard material to read, and so whether the settings are there.</returns>
    public static bool TryReadMaterial(AssetHandle material, [NotNullWhen(true)] out MaterialSettings? settings)
    {
        settings = null;
        if (!material.IsValid) return false;

        NativeMaterialConfig read;
        var answer = Native.bcs_render_material_read(material.Key, &read);
        if (answer is NativeStatus.NotPresent or NativeStatus.InvalidState) return false;
        if (answer == NativeStatus.Unsupported) throw NoRenderer("Reading a material");
        Native.Check(answer, $"reading {material}");

        settings = new MaterialSettings
        {
            BaseColor = (read.BaseR, read.BaseG, read.BaseB, read.BaseA),
            Metallic = read.Metallic,
            Roughness = read.Roughness,
            Emissive = (read.EmissiveR, read.EmissiveG, read.EmissiveB, read.EmissiveA),
            AlphaMode = (AlphaMode)read.AlphaMode,
            AlphaCutoff = read.AlphaCutoff,
            DoubleSided = read.DoubleSided != 0,
            Unlit = read.Unlit != 0,
            BaseColorTexture = Texture(read.BaseColorTexture),
            NormalMap = Texture(read.NormalMap),
            MetallicRoughnessTexture = Texture(read.MetallicRoughnessTexture),
            EmissiveTexture = Texture(read.EmissiveTexture),
            OcclusionTexture = Texture(read.OcclusionTexture),
            UvScale = (read.UvScaleX, read.UvScaleY),
            UvRotation = read.UvRotation,
            UvOffset = (read.UvOffsetX, read.UvOffsetY),
            Reflectance = read.Reflectance,
            Clearcoat = read.Clearcoat,
            ClearcoatRoughness = read.ClearcoatRoughness,
            Transmission = read.SpecularTransmission,
            DiffuseTransmission = read.DiffuseTransmission,
            Thickness = read.Thickness,
            RefractiveIndex = read.Ior,
            AttenuationDistance = read.AttenuationDistance,
            AttenuationColor = (read.AttenuationR, read.AttenuationG, read.AttenuationB, read.AttenuationA),
            AnisotropyStrength = read.AnisotropyStrength,
            AnisotropyRotation = read.AnisotropyRotation,
            ClearcoatTexture = Texture(read.ClearcoatTexture),
            ClearcoatRoughnessTexture = Texture(read.ClearcoatRoughnessTexture),
            ClearcoatNormalTexture = Texture(read.ClearcoatNormalTexture),
            TransmissionTexture = Texture(read.SpecularTransmissionTexture),
            DiffuseTransmissionTexture = Texture(read.DiffuseTransmissionTexture),
            ThicknessTexture = Texture(read.ThicknessTexture),
            AnisotropyTexture = Texture(read.AnisotropyTexture),
            LightmapExposure = read.LightmapExposure,
            DepthMap = Texture(read.DepthMap),
            ParallaxDepthScale = read.ParallaxDepthScale,
            ParallaxMethod = (ParallaxMethod)read.ParallaxMethod,
            ReliefSteps = read.ReliefSteps,
            ParallaxLayers = read.ParallaxLayers,
            SpecularTint = (read.SpecularTintR, read.SpecularTintG, read.SpecularTintB, read.SpecularTintA),
            SpecularTexture = Texture(read.SpecularTexture),
            SpecularTintTexture = Texture(read.SpecularTintTexture),
            OpaqueRenderMethod = (OpaqueRenderMethod)read.OpaqueRenderMethod,
        };
        return true;

        static AssetHandle Texture(int key) => key >= 0 ? new AssetHandle(key) : AssetHandle.None;
    }

    /// <summary>
    /// Builds a mesh from vertices and returns a handle to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a shape no primitive describes, such as a terrain from a heightmap, a ribbon, a line of
    /// points, or a mesh of ten thousand quads whose vertex shader places each one from a buffer a
    /// compute shader writes. Built in any profile, since a mesh is data until something draws it.
    /// </para>
    /// <para>
    /// A triangle mesh given no normals has them worked out, smooth where it is indexed and flat
    /// where it is not, because every lit material and every shader reading a normal would
    /// otherwise read zeros. The attributes land where Bevy's shaders look for them, with positions
    /// at location zero, normals at one, UVs at two and colors at five.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// There are no positions, an array is the wrong length for the vertices, or an index names
    /// no vertex.
    /// </exception>
    public static AssetHandle CreateMesh(MeshData mesh)
    {
        var key = Send(mesh, AssetHandle.None);
        Keep(key, mesh);
        return new AssetHandle(key);
    }

    /// <summary>Writes vertices over a mesh, so everything drawn with it changes. Only valid inside a system.</summary>
    /// <remarks>
    /// Bevy's way of changing a mesh in place, as a mesh deformed while the game runs is, rather
    /// than making a new one and pointing every entity at it. <see cref="TryReadMesh"/> reads what a
    /// mesh holds, so a loaded model can be read, changed and written back. What
    /// <see cref="CreateMesh(MeshData)"/> checks, this checks, and the mesh's recipe, if it was
    /// built from a primitive, is forgotten, since it no longer describes it.
    /// </remarks>
    /// <exception cref="ArgumentException">As for <see cref="CreateMesh(MeshData)"/>.</exception>
    /// <exception cref="BevyNativeException">The handle names no mesh, or this build has no renderer.</exception>
    public static void WriteMesh(AssetHandle mesh, MeshData data)
    {
        Send(data, mesh);
        Keep(mesh.Key, data);
    }

    /// <summary>
    /// Checks a mesh's vertices and hands them over, as a new mesh, or written over the one
    /// <paramref name="into"/> names.
    /// </summary>
    private static int Send(MeshData mesh, AssetHandle into)
    {
        ArgumentNullException.ThrowIfNull(mesh);

        var count = mesh.Positions.Length;

        if (count == 0)
            throw new ArgumentException("A mesh needs at least one position.", nameof(mesh));

        if (mesh.Normals is { } normals && normals.Length != count)
            throw new ArgumentException($"{normals.Length} normals for {count} vertices.", nameof(mesh));

        if (mesh.Uvs is { } uvs && uvs.Length != count * 2)
            throw new ArgumentException($"{uvs.Length} UV floats for {count} vertices, which is two each.", nameof(mesh));

        if (mesh.Colors is { } colors && colors.Length != count * 4)
            throw new ArgumentException($"{colors.Length} color floats for {count} vertices, which is four each.", nameof(mesh));

        // A strip is broken where an index is the largest a uint holds, which starts it again from
        // the next, as the GPU reads it, and anywhere else that index names no vertex.
        var strip = mesh.Topology is MeshTopology.LineStrip or MeshTopology.TriangleStrip;
        if (mesh.Indices is { } indices && indices.Any(index => index >= count && !(strip && index == uint.MaxValue)))
            throw new ArgumentException($"An index names a vertex past the {count} there are.", nameof(mesh));

        fixed (Vec3* positions = mesh.Positions)
        fixed (Vec3* normalsAt = mesh.Normals)
        fixed (float* uvsAt = mesh.Uvs)
        fixed (float* colorsAt = mesh.Colors)
        fixed (uint* indicesAt = mesh.Indices)
        {
            var native = new NativeMeshData
            {
                Positions = (float*)positions,
                VertexCount = count,
                Normals = (float*)normalsAt,
                Uvs = uvsAt,
                Colors = colorsAt,
                Indices = indicesAt,
                IndexCount = mesh.Indices?.Length ?? 0,
                Topology = (int)mesh.Topology,
            };

            if (into == AssetHandle.None)
                return Native.Check(Native.bcs_mesh_create_from(&native), $"building a mesh of {count} vertices");

            Native.Check(Native.bcs_mesh_write(into.Key, &native), $"writing {count} vertices over the mesh {into}");
            return into.Key;
        }
    }

    /// <summary>
    /// Keeps a copy of the vertices a mesh was given, so the caller changing its arrays afterward
    /// does not change what a scene writes for a mesh already drawn.
    /// </summary>
    private static void Keep(int key, MeshData mesh)
    {
        var kept = new MeshData
        {
            Positions = [.. mesh.Positions],
            Normals = mesh.Normals is { } n ? [.. n] : null,
            Uvs = mesh.Uvs is { } u ? [.. u] : null,
            Colors = mesh.Colors is { } c ? [.. c] : null,
            Indices = mesh.Indices is { } i ? [.. i] : null,
            Topology = mesh.Topology,
        };
        lock (Built) Built[key] = kept;
        lock (Recipes) Recipes.Remove(key);
    }

    /// <summary>
    /// Reads an image's texels from the copy the app keeps of it, or answers false while it is
    /// loading or where no copy is kept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The texels are row after row from the top, each the image format's own bytes, four for the
    /// sRGB and linear eight-bit formats a picture loads as. An image loaded from a file keeps its
    /// copy unless it was loaded for the GPU alone, and one made with <see cref="CreateImage"/> keeps
    /// its own. A compressed format, whose texels are blocks, is refused.
    /// </para>
    /// <para>
    /// With <see cref="WriteImagePixels"/>, how an image is changed in place, inverted, painted on or
    /// tinted, so every sprite and material showing it changes with it, as Bevy's
    /// <c>Assets&lt;Image&gt;::get_mut</c> changes one.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The format is compressed, or this build has no renderer.</exception>
    public static bool TryReadImage(AssetHandle image, out ImagePixels? pixels)
    {
        var size = stackalloc uint[3];
        var length = Native.bcs_render_image_pixels(image.Key, size, null, 0);
        if (length == NativeStatus.NotPresent)
        {
            pixels = null;
            return false;
        }
        if (length == NativeStatus.Unsupported) throw NoRenderer("Reading an image");
        Native.Check(length, $"reading the image {image}");

        var data = new byte[length];
        fixed (byte* at = data)
            Native.Check(Native.bcs_render_image_pixels(image.Key, size, at, length), $"reading the image {image}");

        pixels = new ImagePixels(size[0], size[1], size[2], data);
        return true;
    }

    /// <summary>
    /// Reads how large an image is in texels, once it has loaded. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The depth is a 3D image's slices, or a cube's or an array's layers, and one for a plain
    /// picture. For sizing something by an image the game did not make, a viewer's square to the
    /// picture it shows or a cube for every voxel of a volume read from a file. Unlike
    /// <see cref="TryReadImage"/> it answers for any image Bevy holds, compressed or kept on the GPU
    /// alone, since the size is Bevy's description of the texture and not its texels.
    /// </remarks>
    /// <returns>False while the image is loading.</returns>
    /// <exception cref="BevyNativeException">The handle names no image, or this build has no renderer.</exception>
    public static bool TryImageSize(AssetHandle image, out uint width, out uint height, out uint depth)
    {
        var size = stackalloc uint[3];
        var status = Native.bcs_render_image_size(image.Key, size);
        (width, height, depth) = (0, 0, 0);
        if (status == NativeStatus.NotPresent) return false;
        if (status == NativeStatus.Unsupported) throw NoRenderer("Reading an image's size");
        Native.Check(status, $"reading the size of the image {image}");

        (width, height, depth) = (size[0], size[1], size[2]);
        return true;
    }

    /// <summary>
    /// Writes texels over the copy an image keeps, as many bytes as it holds, so the GPU is given
    /// them again and everything showing the image changes.
    /// </summary>
    /// <remarks>See <see cref="TryReadImage"/> for the layout, which these bytes follow.</remarks>
    /// <exception cref="BevyNativeException">
    /// The image is loading or keeps no copy, the bytes are not as many as it holds, or this build
    /// has no renderer.
    /// </exception>
    public static void WriteImagePixels(AssetHandle image, ReadOnlySpan<byte> texels)
    {
        fixed (byte* at = texels)
        {
            var status = Native.bcs_render_image_set_pixels(image.Key, at, texels.Length);
            if (status == NativeStatus.Unsupported) throw NoRenderer("Writing an image");
            Native.Check(status, $"writing {texels.Length} bytes over the image {image}");
        }
    }

    /// <summary>
    /// Reads a mesh's triangles back: where each vertex is, and which three make each triangle.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For whatever needs the shape rather than the picture, such as a collision shape built from a
    /// level loaded from a glTF file. The answer is a <see cref="MeshData"/> with
    /// <see cref="MeshData.Positions"/> and <see cref="MeshData.Indices"/> filled, in the mesh's own
    /// space, and nothing else.
    /// </para>
    /// <para>
    /// A mesh from a file is read once it has loaded, so this answers false until then, the way a
    /// capture answers false until it has arrived.
    /// </para>
    /// </remarks>
    /// <returns>True with the triangles once the mesh has loaded.</returns>
    /// <exception cref="BevyNativeException">
    /// The handle names no mesh, the mesh is not triangles, or this build has no renderer.
    /// </exception>
    public static bool TryReadMesh(AssetHandle mesh, out MeshData? triangles)
    {
        triangles = null;

        var counts = stackalloc int[2];
        var answer = Native.bcs_render_mesh_triangles(mesh.Key, null, 0, null, 0, counts);

        if (answer == NativeStatus.NotPresent) return false;
        Native.Check(answer, $"reading the triangles of {mesh}");

        var positions = new Vec3[counts[0]];
        var indices = new uint[counts[1]];

        fixed (Vec3* corners = positions)
        fixed (uint* order = indices)
        {
            Native.Check(
                Native.bcs_render_mesh_triangles(mesh.Key, (float*)corners, positions.Length * 3, order, indices.Length, counts),
                $"reading the triangles of {mesh}");
        }

        triangles = new MeshData { Positions = positions, Indices = indices };
        return true;
    }

    /// <summary>
    /// Reads a mesh's positions back with the normal at each. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// For drawing which way a surface faces, a short line out of every vertex, as an editor's
    /// preview does. A mesh from a file is read once it has loaded, so this answers false until
    /// then, and false for a mesh with no normals, such as one of lines.
    /// </remarks>
    /// <returns>True with a position and a normal a vertex once the mesh has loaded.</returns>
    /// <exception cref="BevyNativeException">The handle names no mesh, or this build has no renderer.</exception>
    public static bool TryReadNormals(AssetHandle mesh, out Vec3[] positions, out Vec3[] normals)
    {
        positions = [];
        normals = [];

        var count = 0;
        var answer = Native.bcs_render_mesh_normals(mesh.Key, null, null, 0, &count);
        if (answer is NativeStatus.NotPresent or NativeStatus.NullArgument) return false;
        if (answer == NativeStatus.Unsupported) throw NoRenderer("Reading a mesh's normals");
        Native.Check(answer, $"reading the normals of {mesh}");

        positions = new Vec3[count];
        normals = new Vec3[count];

        fixed (Vec3* at = positions)
        fixed (Vec3* facing = normals)
        {
            Native.Check(
                Native.bcs_render_mesh_normals(mesh.Key, (float*)at, (float*)facing, count * 3, &count),
                $"reading the normals of {mesh}");
        }

        return true;
    }

    /// <summary>
    /// Says how an entity's mesh is treated beyond what it looks like. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The flags are the whole answer rather than additions, so a flag left out takes that
    /// behavior off again and <see cref="MeshFlags.None"/> puts everything back as Bevy has it.
    /// </para>
    /// <para>
    /// A mesh drawn where its own bounds do not say needs <see cref="MeshFlags.NoFrustumCulling"/>,
    /// such as one a vertex shader moves far from where it was built, or one whose vertices a
    /// buffer places. Bevy culls by the bounds it worked out from the mesh, so such a mesh vanishes
    /// whenever those stale bounds leave the view.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The entity does not exist, or there is no renderer.</exception>
    public static void SetMeshFlags(EcsWorld world, Entity entity, MeshFlags flags)
    {
        ArgumentNullException.ThrowIfNull(world);
        Native.Check(
            Native.bcs_render_set_mesh_flags(entity.Bits, (uint)flags),
            $"setting how entity {entity} is drawn");
    }

    /// <summary>
    /// Builds a physically based material and returns a handle to it.
    /// </summary>
    /// <param name="red">Linear sRGB red, from zero to one.</param>
    /// <param name="green">Linear sRGB green, from zero to one.</param>
    /// <param name="blue">Linear sRGB blue, from zero to one.</param>
    /// <param name="alpha">Opacity, from zero to one.</param>
    /// <param name="metallic">Zero for a dielectric, one for a metal.</param>
    /// <param name="roughness">Near zero for a mirror, one for a matte surface.</param>
    public static AssetHandle CreateMaterial(
        float red,
        float green,
        float blue,
        float alpha = 1f,
        float metallic = 0f,
        float roughness = 0.5f) =>
        CreateMaterial(new MaterialSettings
        {
            BaseColor = (red, green, blue, alpha),
            Metallic = metallic,
            Roughness = roughness,
        });

    /// <summary>
    /// Builds a material of one color and returns a handle to it, as Bevy makes a
    /// <c>StandardMaterial</c> from a <c>Color</c>.
    /// </summary>
    /// <param name="color">The color, linear, which <see cref="Color.FromSrgb"/> makes from a picked one.</param>
    /// <remarks>Neither metal nor a mirror, as Bevy's own default leaves the rest.</remarks>
    public static AssetHandle CreateMaterial((float R, float G, float B, float A) color) =>
        CreateMaterial(new MaterialSettings { BaseColor = color });

    /// <summary>Builds a material from <paramref name="settings"/> and returns a handle to it.</summary>
    /// <remarks>
    /// A texture the settings leave at <see cref="AssetHandle.None"/> is one the material does
    /// without. A handle that names nothing, as a released one does, is refused instead, because
    /// drawing the surface untextured and reporting success would leave the caller with a wrong
    /// picture and nothing pointing at why.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// A texture handle names nothing, or this build has no renderer.
    /// </exception>
    public static AssetHandle CreateMaterial(MaterialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = Config(settings);
        var key = Native.bcs_material_create(&native);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a material");

        Native.Check(key, "building a material");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Writes settings over a standard material in place, so everything drawn with it changes.
    /// </summary>
    /// <remarks>
    /// The material keeps its handle, so every entity sharing it, and a scene or a glTF file that
    /// brought it, sees the change. A tool editing one entity's look alone copies the material
    /// first with <see cref="CreateMaterial(MaterialSettings)"/> from what
    /// <see cref="TryReadMaterial"/> gives.
    /// </remarks>
    /// <returns>Whether the handle named a standard material that took the settings.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static bool WriteMaterial(AssetHandle material, MaterialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = Config(settings);
        var status = Native.bcs_render_material_write(material.Key, &native);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Writing a material");

        return status == NativeStatus.Ok;
    }

    /// <summary>The settings as the bridge takes them, with an unset texture as no texture.</summary>
    private static NativeMaterialConfig Config(MaterialSettings settings)
    {
        return new NativeMaterialConfig
        {
            BaseR = settings.BaseColor.R,
            BaseG = settings.BaseColor.G,
            BaseB = settings.BaseColor.B,
            BaseA = settings.BaseColor.A,
            Metallic = settings.Metallic,
            Roughness = settings.Roughness,
            EmissiveR = settings.Emissive.R,
            EmissiveG = settings.Emissive.G,
            EmissiveB = settings.Emissive.B,
            EmissiveA = settings.Emissive.A,
            AlphaMode = (int)settings.AlphaMode,
            AlphaCutoff = settings.AlphaCutoff,
            DoubleSided = settings.DoubleSided ? 1 : 0,
            Unlit = settings.Unlit ? 1 : 0,
            BaseColorTexture = Key(settings.BaseColorTexture),
            NormalMap = Key(settings.NormalMap),
            MetallicRoughnessTexture = Key(settings.MetallicRoughnessTexture),
            EmissiveTexture = Key(settings.EmissiveTexture),
            OcclusionTexture = Key(settings.OcclusionTexture),
            UvScaleX = settings.UvScale.U,
            UvScaleY = settings.UvScale.V,
            UvRotation = settings.UvRotation,
            UvOffsetX = settings.UvOffset.U,
            UvOffsetY = settings.UvOffset.V,
            Reflectance = settings.Reflectance,
            Clearcoat = settings.Clearcoat,
            ClearcoatRoughness = settings.ClearcoatRoughness,
            SpecularTransmission = settings.Transmission,
            DiffuseTransmission = settings.DiffuseTransmission,
            Thickness = settings.Thickness,
            Ior = settings.RefractiveIndex,
            AttenuationDistance = settings.AttenuationDistance,
            AttenuationR = settings.AttenuationColor.R,
            AttenuationG = settings.AttenuationColor.G,
            AttenuationB = settings.AttenuationColor.B,
            AttenuationA = settings.AttenuationColor.A,
            AnisotropyStrength = settings.AnisotropyStrength,
            AnisotropyRotation = settings.AnisotropyRotation,
            ClearcoatTexture = Key(settings.ClearcoatTexture),
            ClearcoatRoughnessTexture = Key(settings.ClearcoatRoughnessTexture),
            ClearcoatNormalTexture = Key(settings.ClearcoatNormalTexture),
            SpecularTransmissionTexture = Key(settings.TransmissionTexture),
            DiffuseTransmissionTexture = Key(settings.DiffuseTransmissionTexture),
            ThicknessTexture = Key(settings.ThicknessTexture),
            AnisotropyTexture = Key(settings.AnisotropyTexture),
            LightmapExposure = settings.LightmapExposure,
            DepthMap = Key(settings.DepthMap),
            ParallaxDepthScale = settings.ParallaxDepthScale,
            ParallaxMethod = (int)settings.ParallaxMethod,
            ReliefSteps = settings.ReliefSteps,
            ParallaxLayers = settings.ParallaxLayers,
            SpecularTintR = settings.SpecularTint.R,
            SpecularTintG = settings.SpecularTint.G,
            SpecularTintB = settings.SpecularTint.B,
            SpecularTintA = settings.SpecularTint.A,
            SpecularTexture = Key(settings.SpecularTexture),
            SpecularTintTexture = Key(settings.SpecularTintTexture),
            OpaqueRenderMethod = (int)settings.OpaqueRenderMethod,
        };

        // An unset handle is -1, which the bridge reads as "no texture here".
        static int Key(AssetHandle handle) => handle.IsValid ? handle.Key : -1;
    }

    /// <summary>
    /// Gives an entity a mesh to draw.
    /// </summary>
    /// <remarks>
    /// Inserting this also pulls in the components Bevy requires alongside it, such as
    /// <see cref="Transform"/> and visibility, so an entity needs nothing else to be drawable
    /// beyond a material.
    /// </remarks>
    public static void SetMesh(EcsWorld world, Entity entity, AssetHandle mesh) =>
        Attach(world, entity, "Mesh3d", mesh, "a mesh");

    /// <summary>Gives an entity a material to draw its mesh with.</summary>
    public static void SetMaterial(EcsWorld world, Entity entity, AssetHandle material) =>
        Attach(world, entity, "MeshMaterial3d", material, "a material");
}
