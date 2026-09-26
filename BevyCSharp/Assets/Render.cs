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
public static unsafe class Render
{
    /// <summary>
    /// Builds a mesh primitive and returns a handle to it.
    /// </summary>
    /// <param name="shape">One of the constants on <see cref="MeshShape"/>.</param>
    /// <param name="a">Width for a cuboid or plane, radius for a sphere or capsule.</param>
    /// <param name="b">Height for a cuboid, depth for a plane, length for a capsule.</param>
    /// <param name="c">Depth, for a cuboid.</param>
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
        return new AssetHandle(key);
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

        if (mesh.Indices is { } indices && indices.Any(index => index >= count))
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

            return new AssetHandle(Native.Check(
                Native.bcs_mesh_create_from(&native),
                $"building a mesh of {count} vertices"));
        }
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

        var native = new NativeMaterialConfig
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
        };

        var key = Native.bcs_material_create(&native);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Building a material");

        Native.Check(key, "building a material");
        return new AssetHandle(key);

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

    /// <summary>
    /// How long each render pass took, smoothed over the last frames, where the app asked for it
    /// with <see cref="Config.GpuTimings"/>. Empty otherwise. Callable at any time.
    /// </summary>
    /// <remarks>
    /// Bevy's passes are named as Bevy names them, and each dispatch, pass and draw a shader
    /// program makes is <c>shader</c> followed by the name of its first stage's file. Draws on a
    /// camera sharing one render pass share one timing, named after the first of them.
    /// </remarks>
    public static IReadOnlyList<PassTiming> Timings()
    {
        if (!App.HasRenderer) return [];

        var text = Native.ReadText((buffer, capacity) => Native.bcs_render_timings(buffer, capacity), "reading render timings");
        var timings = new List<PassTiming>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');
            if (parts.Length != 3) continue;

            var cpu = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
            var gpu = double.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture);
            timings.Add(new PassTiming(parts[0], cpu < 0 ? null : cpu, gpu < 0 ? null : gpu));
        }

        return timings;
    }

    /// <summary>
    /// Whether Bevy's ray-traced lighting is running, meaning the bridge was built with it
    /// (<c>--solari</c>), the app asked for it with <see cref="Config.RayTracedLighting"/>, and the
    /// adapter traces rays.
    /// </summary>
    public static bool RayTracingActive => Native.bcs_render_ray_tracing_active() != 0;

    /// <summary>
    /// Lights a camera with Bevy's ray tracing, or with <see langword="false"/> the usual way again.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Direct light from every light and every emissive surface, found by tracing rays rather than
    /// from shadow maps, and indirect light bounced off every surface taking part, which is global
    /// illumination. A red wall tints the floor beside it, and a lamp in a room lights the corners
    /// it cannot see. It builds up over a few frames and follows what moves, so a sudden cut shows
    /// a moment of settling.
    /// </para>
    /// <para>
    /// Only meshes given to <see cref="SetRayTraced"/> are met by rays, though every mesh is still
    /// drawn and lit. The camera draws in high dynamic range and once a pixel, and asks for the
    /// prepasses Solari reads itself. Turning shadows off on every light is Bevy's advice, since the
    /// rays do their work.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// Ray-traced lighting is not running (see <see cref="RayTracingActive"/>), or the entity is not
    /// a camera.
    /// </exception>
    public static void SetRayTracedLighting(Entity camera, bool on) =>
        Native.Check(
            Native.bcs_render_set_ray_traced_lighting(camera.Bits, on ? 1 : 0),
            $"setting ray-traced lighting on {camera}");

    /// <summary>
    /// Makes an entity's mesh one the rays of ray-traced lighting meet. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The mesh is reshaped in place the way ray tracing structures are built from, keeping exactly
    /// positions, normals, texture coordinates and tangents, which are worked out where it has
    /// none, and thirty-two bit indices. So the entity keeps drawing it as before, and the rays
    /// meet the triangles the picture shows. The entity's material has to be one from
    /// <see cref="CreateMaterial(MaterialSettings)"/>, and the mesh has to have loaded.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// Ray-traced lighting is not running, the mesh has not loaded, or it is not indexed triangles
    /// with normals and texture coordinates.
    /// </exception>
    public static void SetRayTraced(Entity entity, AssetHandle mesh) =>
        Native.Check(Native.bcs_render_set_ray_traced(entity.Bits, mesh.Key), $"making {entity} ray traced");

    /// <summary>
    /// Whether Bevy's meshlets are running, meaning the bridge was built with them
    /// (<c>--meshlet</c>), the app asked for them with <see cref="Config.MeshletClusters"/>, and
    /// the GPU can draw them.
    /// </summary>
    public static bool MeshletsActive => Native.bcs_render_meshlets_active() != 0;

    /// <summary>
    /// Starts cutting a mesh into clusters that Bevy's meshlet renderer culls and picks a level of
    /// detail for on the GPU, and answers the meshlet mesh at once. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Meshlets are virtualized geometry as Bevy ships it. A mesh of millions of triangles costs
    /// what the few thousand covering the screen at the moment cost, because clusters out of view,
    /// hidden behind others or too small to matter are dropped on the GPU before anything is drawn.
    /// What that costs up front is this conversion, which takes seconds for a large mesh, so it
    /// runs on a worker once the mesh has loaded, and the handle answered is empty until it is
    /// done; an entity given it draws nothing until then.
    /// </para>
    /// <para>
    /// The mesh must be indexed triangles with texture coordinates. Normals are worked out if it
    /// has none, and anything else it carries is left out, since a meshlet mesh keeps positions,
    /// normals and texture coordinates and works tangents out when drawn.
    /// <paramref name="quantization"/> is how finely positions are kept, as the number of halvings
    /// of a centimeter, and zero takes Bevy's default of four, a sixteenth. Two meshes meant to
    /// meet without a crack need the same one.
    /// </para>
    /// <para>
    /// <paramref name="saveTo"/>, a path under the asset root, also writes the finished mesh as a
    /// <c>.meshlet_mesh</c> file, which <see cref="AssetKind.MeshletMesh"/> loads. A game bakes
    /// this way, converting once, in a tool or on the first run, and loading the file after, which
    /// takes as long as reading it.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// Meshlets are not running (see <see cref="MeshletsActive"/>), or the handle names no mesh.
    /// </exception>
    public static AssetHandle CreateMeshletMesh(AssetHandle mesh, uint quantization = 0, string? saveTo = null) =>
        new(Native.Check(
            Native.bcs_render_create_meshlet_mesh(mesh.Key, quantization, saveTo),
            "making a meshlet mesh"));

    /// <summary>
    /// Gives an entity a meshlet mesh to draw, in place of any ordinary mesh it had. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// Drawn with the entity's material from <see cref="CreateMaterial(MaterialSettings)"/>, like a
    /// mesh. A material drawn by a Slang program has no meshlet path and draws nothing here. Every
    /// camera draws once a pixel while meshlets run, since Bevy's meshlet renderer cannot draw into
    /// a multisampled picture, so <see cref="PostSettings.Msaa"/> is set to one whatever it asks.
    /// </remarks>
    public static void SetMeshletMesh(EcsWorld world, Entity entity, AssetHandle meshlet) =>
        Attach(world, entity, "MeshletMesh3d", meshlet, "a meshlet mesh");

    /// <summary>
    /// Where an entity's mesh was loaded from, or empty when it was not loaded from anywhere.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A mesh and a material are Bevy's own components holding typed handles, so nothing on this
    /// side can read them the way it reads a component of its own. They can be asked where they
    /// came from, which a tool showing an entity can show and a person can point at a different
    /// file.
    /// </para>
    /// <para>
    /// Empty for anything built in memory, which is everything
    /// <see cref="CreateMesh(string, float, float, float)"/> and <see cref="CreateMesh(MeshData)"/>
    /// make, and for an entity carrying no mesh at all. Nothing on this side tells those two apart,
    /// because Bevy's <c>Mesh3d</c> has no managed mirror to ask <see cref="EcsWorld.Has{T}"/>
    /// about.
    /// </para>
    /// </remarks>
    /// <param name="entity">The entity to ask about.</param>
    public static string MeshPathOf(Entity entity) => AssetPathOf(entity, 0);

    /// <summary>Where an entity's material was loaded from, or empty when it was not.</summary>
    /// <remarks>
    /// Only a standard material answers. A shader material is always made in memory, so it has no
    /// path, and what describes it is the program drawing it, which
    /// <see cref="Shaders.ProgramOn"/> gives. <see cref="MeshPathOf"/> covers the rest of the
    /// reasoning.
    /// </remarks>
    /// <param name="entity">The entity to ask about.</param>
    public static string MaterialPathOf(Entity entity) => AssetPathOf(entity, 1);

    /// <summary>
    /// Whether an entity carries a mesh the renderer draws.
    /// </summary>
    /// <remarks>
    /// The question <see cref="MeshPathOf"/> cannot answer, since a mesh made in memory has no
    /// path and neither does an entity with no mesh at all. A tool showing what something is drawn
    /// with has to tell those apart.
    /// </remarks>
    /// <param name="entity">The entity to ask about.</param>
    public static bool IsDrawn(Entity entity) =>
        Native.bcs_render_asset_path(entity.Bits, 0, null, 0) >= 0;

    /// <summary>One of the two paths, or empty when there is nothing to say.</summary>
    private static string AssetPathOf(Entity entity, int which)
    {
        // Absent rather than refused, because a tool asks this about whatever is selected and most
        // things are not drawn at all.
        var probe = Native.bcs_render_asset_path(entity.Bits, which, null, 0);
        if (probe < 0) return string.Empty;

        return Native.ReadText(
            (buffer, capacity) =>
                Native.bcs_render_asset_path(entity.Bits, which, buffer, capacity),
            $"reading what {entity} is drawn with");
    }

    /// <summary>
    /// Spawns a 3D camera and returns it.
    /// </summary>
    /// <remarks>
    /// Bevy draws nothing without a camera. The new one sits at the origin looking down negative
    /// Z; position it by writing its <see cref="Transform"/> like any other entity.
    /// </remarks>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera3d() => new(Native.bcs_render_spawn_camera_3d(null));

    /// <summary>
    /// Gives a camera part of the window to draw into, or the whole of it.
    /// </summary>
    /// <remarks>
    /// What a docked panel needs. The interface takes the right of the window and the scene is told
    /// to draw into what is left, so the picture is the shape of the space rather than the shape of
    /// the window with something over it. A width or height of zero means the whole window again.
    /// </remarks>
    /// <param name="camera">The camera to place.</param>
    /// <param name="x">Left edge, in physical pixels.</param>
    /// <param name="y">Top edge, in physical pixels.</param>
    /// <param name="width">How wide, in physical pixels, or zero for the whole window.</param>
    /// <param name="height">How tall, in physical pixels, or zero for the whole window.</param>
    public static void SetViewport(Entity camera, uint x, uint y, uint width, uint height) =>
        Native.Check(
            Native.bcs_render_set_viewport(camera.Bits, x, y, width, height),
            "Render.SetViewport");


    /// <summary>Spawns a 3D camera set up by <paramref name="settings"/>.</summary>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnCamera3d(CameraSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeCameraConfig
        {
            Projection = (int)settings.Projection,
            FovDegrees = settings.FieldOfView,
            OrthoHeight = settings.Height,
            Near = settings.Near,
            Far = settings.Far,
            ClearMode = (int)settings.Clear,
            ClearR = settings.ClearColor.R,
            ClearG = settings.ClearColor.G,
            ClearB = settings.ClearColor.B,
            ClearA = settings.ClearColor.A,
            Order = settings.Order,
            HasViewport = settings.Viewport is null ? 0 : 1,
            ViewportX = settings.Viewport?.X ?? 0,
            ViewportY = settings.Viewport?.Y ?? 0,
            ViewportWidth = settings.Viewport?.Width ?? 0,
            ViewportHeight = settings.Viewport?.Height ?? 0,
            Layers = settings.Layers,
        };

        return new Entity(Native.bcs_render_spawn_camera_3d(&native));
    }

    /// <summary>
    /// Spawns a light and returns it.
    /// </summary>
    /// <param name="kind">Which light to spawn.</param>
    /// <param name="intensity">
    /// Illuminance in lux for a directional light, luminous power in lumens for a point light.
    /// </param>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnLight(LightKind kind, float intensity) =>
        SpawnLight(new LightSettings { Kind = kind, Intensity = intensity });

    /// <summary>Spawns a light set up by <paramref name="settings"/>.</summary>
    /// <returns><see cref="Entity.None"/> on a build with no renderer.</returns>
    public static Entity SpawnLight(LightSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeLightConfig
        {
            Kind = (int)settings.Kind,
            Intensity = settings.Intensity,
            ColorR = settings.Color.R,
            ColorG = settings.Color.G,
            ColorB = settings.Color.B,
            Range = settings.Range,
            Radius = settings.Radius,
            Shadows = settings.Shadows ? 1 : 0,
            InnerAngle = settings.InnerAngle,
            OuterAngle = settings.OuterAngle,
            ShadowDepthBias = settings.ShadowDepthBias,
            ShadowNormalBias = settings.ShadowNormalBias,
        };

        return new Entity(Native.bcs_render_spawn_light(&native));
    }

    /// <summary>
    /// Sets how a directional light divides its shadows across the distance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A directional light covers the whole scene, so one shadow map stretched over all of it is
    /// coarse near the camera, which is where it is looked at closest. Cascades split the range
    /// into a few maps, each covering a nearer and smaller slice of it. A shadow that looks blocky
    /// at arm's length is this rather than a resolution.
    /// </para>
    /// <para>
    /// Every number here keeps Bevy's own when it is left at zero, so a call setting one of them
    /// says one thing rather than restating the rest.
    /// </para>
    /// </remarks>
    /// <param name="light">A directional light from <see cref="SpawnLight(LightSettings)"/>.</param>
    /// <param name="cascades">How many maps to split the range into, up to four.</param>
    /// <param name="minimum">The nearest distance that receives a shadow.</param>
    /// <param name="maximum">
    /// The furthest. The cost of a large number is quality rather than time, since the same maps
    /// are stretched over more ground.
    /// </param>
    /// <param name="firstBound">
    /// Where the first cascade ends. The ones after it are spaced out toward
    /// <paramref name="maximum"/>, so this is the knob for how much detail the near ground gets.
    /// </param>
    /// <param name="overlap">
    /// How much of each cascade is blended into the next, as a proportion, which keeps the join
    /// between two of them from showing as a line across the ground.
    /// </param>
    /// <exception cref="BevyNativeException">The entity is not a directional light.</exception>
    public static void SetShadowCascades(
        Entity light,
        int cascades = 0,
        float minimum = 0f,
        float maximum = 0f,
        float firstBound = 0f,
        float overlap = 0f) =>
        Native.Check(
            Native.bcs_render_set_shadow_cascades(
                light.Bits, cascades, minimum, maximum, firstBound, overlap),
            $"setting the shadow cascades of {light}");

    /// <summary>
    /// Shapes a spot light's beam with a picture, the way a gobo shapes a stage light.
    /// </summary>
    /// <remarks>
    /// What puts the shadow of a window frame on the floor without a window being there, or breaks
    /// a torch beam up so it does not read as a cone of paint. Only the red channel is read, so the
    /// picture says how much light gets through rather than what color it is, and its border should
    /// be black or the light leaks past the edge of it.
    /// </remarks>
    /// <param name="light">A spot light from <see cref="SpawnLight(LightSettings)"/>.</param>
    /// <param name="cookie">
    /// The picture, or <see cref="AssetHandle.None"/> to take the shaping off and leave a plain
    /// cone.
    /// </param>
    /// <exception cref="BevyNativeException">The entity is not a spot light.</exception>
    public static void SetLightCookie(Entity light, AssetHandle cookie) =>
        Native.Check(
            Native.bcs_render_set_light_cookie(light.Bits, cookie.Key),
            $"shaping the beam of {light}");

    /// <summary>
    /// Sets what a camera does to the picture after the scene has been drawn.
    /// </summary>
    /// <remarks>
    /// The whole pipeline in one call. An effect the settings leave off is removed from the
    /// camera, so the same call turns something on and off again. Only a camera can be given
    /// these, since it is the camera's render graph that reads them.
    /// </remarks>
    /// <param name="camera">A camera entity from <see cref="SpawnCamera3d()"/> or
    /// <see cref="Render2d.SpawnCamera2d"/>.</param>
    /// <param name="settings">What the camera should do.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="AntiAliasPass.Temporal"/> is asked for together with multisampling.
    /// </exception>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var camera = Render.SpawnCamera3d();
    ///
    /// Render.SetPostProcessing(camera, new PostSettings
    /// {
    ///     Hdr = true,
    ///     Bloom = true,
    ///     BloomIntensity = 0.2f,
    ///     Tonemapper = Tonemapper.AgX,
    ///     AntiAlias = AntiAliasPass.Fxaa,
    ///     Msaa = 1,
    /// });
    /// </code>
    /// </example>
    public static void SetPostProcessing(Entity camera, PostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        // Temporal antialiasing resolves the picture from the frames before it, and a
        // multisampled target has no such history. Bevy answers the pair by warning once a frame
        // and drawing nothing, which is the kind of quiet failure worth refusing outright.
        if (settings.AntiAlias == AntiAliasPass.Temporal && settings.Msaa is 2 or 4 or 8)
        {
            throw new ArgumentException(
                "temporal antialiasing cannot run on a multisampled target, and Msaa is "
                    + $"{settings.Msaa}; set Msaa to 1 to use it",
                nameof(settings));
        }

        var native = new NativePostConfig
        {
            Tonemapping = (int)settings.Tonemapper,
            Dither = settings.Dither ? 1 : 0,
            Hdr = settings.Hdr ? 1 : 0,
            Msaa = settings.Msaa,
            AntiAlias = (int)settings.AntiAlias,
            AntiAliasQuality = (int)settings.Quality,
            Sharpen = settings.Sharpen,
            Bloom = settings.Bloom ? 1 : 0,
            BloomIntensity = settings.BloomIntensity,
            BloomThreshold = settings.BloomThreshold,
            BloomThresholdSoftness = settings.BloomThresholdSoftness,
            BloomMode = (int)settings.BloomMode,
        };

        Native.Check(
            Native.bcs_render_set_post(camera.Bits, &native),
            $"setting the post processing on {camera}");
    }

    /// <summary>
    /// Sets the lens a camera draws through.
    /// </summary>
    /// <remarks>
    /// Beside <see cref="SetPostProcessing"/>, which is the pipeline a settings screen owns, a
    /// scene sets this for a moment. The whole set in one call either way, so an effect these
    /// settings leave off is taken off the camera.
    /// </remarks>
    /// <param name="camera">A camera entity from <see cref="SpawnCamera3d()"/> or
    /// <see cref="Render2d.SpawnCamera2d"/>.</param>
    /// <param name="settings">The lens to draw through.</param>
    /// <exception cref="ArgumentException">
    /// <see cref="EffectSettings.ExposureCompensation"/> does not rise in luminance.
    /// </exception>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, an image named in the settings is not loaded, or
    /// this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// Render.SetEffects(camera, new EffectSettings
    /// {
    ///     DepthOfField = DepthOfFieldMode.Bokeh,
    ///     FocalDistance = 8f,
    ///     Aperture = 1.4f,
    ///     ShutterAngle = 0.5f,
    ///     Vignette = 0.4f,
    /// });
    /// </code>
    /// </example>
    public static void SetEffects(Entity camera, EffectSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeEffectsConfig
        {
            DofMode = (int)settings.DepthOfField,
            FocalDistance = settings.FocalDistance,
            ApertureFStops = settings.Aperture,
            SensorHeight = settings.SensorHeight,
            MaxBlurDiameter = settings.MaxBlurDiameter,
            MaxDepth = settings.MaxDepth,
            ShutterAngle = settings.ShutterAngle,
            MotionBlurSamples = settings.MotionBlurSamples,
            Aberration = settings.Aberration,
            AberrationSamples = settings.AberrationSamples,
            AberrationLut = Key(settings.AberrationColors),
            Distortion = settings.Distortion,
            DistortionScale = settings.DistortionScale,
            DistortionAxisX = settings.DistortionAxes.X,
            DistortionAxisY = settings.DistortionAxes.Y,
            DistortionCenterX = settings.DistortionCenter.X,
            DistortionCenterY = settings.DistortionCenter.Y,
            DistortionEdgeCurvature = settings.DistortionEdgeCurvature,
            Vignette = settings.Vignette,
            VignetteRadius = settings.VignetteRadius,
            VignetteSmoothness = settings.VignetteSmoothness,
            VignetteRoundness = settings.VignetteRoundness,
            VignetteCenterX = settings.VignetteCenter.X,
            VignetteCenterY = settings.VignetteCenter.Y,
            VignetteEdgeCompensation = settings.VignetteEdgeCompensation,
            VignetteColorR = settings.VignetteColor.R,
            VignetteColorG = settings.VignetteColor.G,
            VignetteColorB = settings.VignetteColor.B,
            VignetteColorA = settings.VignetteColor.A,
            AutoExposure = settings.AutoExposure ? 1 : 0,
            MeteringMin = settings.MeteringRange.Min,
            MeteringMax = settings.MeteringRange.Max,
            MeteringLow = settings.MeteringFilter.Low,
            MeteringHigh = settings.MeteringFilter.High,
            SpeedBrighten = settings.SpeedBrighten,
            SpeedDarken = settings.SpeedDarken,
            ExposureTransition = settings.ExposureTransition,
            MeteringMask = Key(settings.MeteringMask),
        };

        var curve = settings.ExposureCompensation;
        if (curve is not null)
        {
            var count = Math.Min(curve.Count, NativeEffectsConfig.CompensationPoints);
            native.CompensationCount = (uint)count;

            for (var i = 0; i < count; i++)
            {
                // The curve is read by looking a measured brightness up in it, so it has to rise
                // in luminance. Caught here rather than on the far side, where the only answer
                // the bridge can give is a status code.
                if (i > 0 && curve[i].Luminance <= curve[i - 1].Luminance)
                {
                    throw new ArgumentException(
                        $"exposure compensation point {i} is at luminance {curve[i].Luminance}, "
                            + $"which is not above the {curve[i - 1].Luminance} before it",
                        nameof(settings));
                }

                native.CompensationCurve[i * 2] = curve[i].Luminance;
                native.CompensationCurve[(i * 2) + 1] = curve[i].Compensation;
            }
        }

        Native.Check(
            Native.bcs_render_set_effects(camera.Bits, &native),
            $"setting the effects on {camera}");

        static int Key(AssetHandle handle) => handle.IsValid ? handle.Key : -1;
    }

    /// <summary>
    /// Draws the sky the air scatters, seen from a camera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two things make a sky: a planet, which is an entity the size of a world, and a camera told
    /// to sample it. This call keeps at most one planet in the world and points the camera at it,
    /// so calling it for a second camera adds a viewer rather than a second sky.
    /// </para>
    /// <para>
    /// The camera is given a high dynamic range target, which the sky needs, because a sun
    /// scattered through air is far brighter than white. Pair it with
    /// <see cref="SetPostProcessing"/> for a tonemapper to bring that range back down.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera that should see the sky.</param>
    /// <param name="settings">How thick the air is, and at what scale.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var camera = Render.SpawnCamera3d();
    /// var sun = Render.SpawnLight(LightKind.Directional, 10_000f);
    /// ctx.Ecs.Add(sun, Transform.LookingAt(new Vec3(1f, 0.6f, 0f), Vec3.Zero, Vec3.UnitY));
    ///
    /// Render.SetAtmosphere(camera, new AtmosphereSettings());
    /// Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
    /// </code>
    /// </example>
    public static void SetAtmosphere(Entity camera, AtmosphereSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeAtmosphereConfig
        {
            Enabled = 1,
            Density = settings.Density,
            Scale = settings.Scale,
            HazeDistance = settings.HazeDistance,
            Quality = (int)settings.Quality,
            GroundAlbedo = settings.GroundAlbedo,
        };

        Native.Check(
            Native.bcs_render_set_atmosphere(camera.Bits, &native),
            $"setting the atmosphere on {camera}");
    }

    /// <summary>
    /// Stops a camera drawing the sky.
    /// </summary>
    /// <remarks>
    /// The planet stays where it is. Nothing is computed for it until a camera asks again, so
    /// leaving it costs a component and no work.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The entity is gone or is not a camera, or this build has no renderer.
    /// </exception>
    public static void ClearAtmosphere(Entity camera)
    {
        var native = new NativeAtmosphereConfig();

        Native.Check(
            Native.bcs_render_set_atmosphere(camera.Bits, &native),
            $"clearing the atmosphere on {camera}");
    }

    /// <summary>
    /// Writes what is being drawn to a PNG file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The window, or the image an offscreen run draws into instead. Which one is not the caller's
    /// to choose, because a run has one thing it is drawing and a capture is a picture of that. A
    /// headless run draws nothing and captures nothing.
    /// </para>
    /// <para>
    /// The capture happens on the frame after this call, because the picture has to come back off
    /// the GPU, and the file appears once it has. Watch for the file rather than assuming it is
    /// there when this returns.
    /// </para>
    /// <para>
    /// What this is for is checking the picture without a person looking at it. A test can assert
    /// that a setting was accepted; only the picture says whether anything was drawn.
    /// </para>
    /// </remarks>
    /// <param name="path">Where to write the PNG. Relative paths are resolved by the process.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    /// <seealso cref="Config.Offscreen"/>
    public static void Screenshot(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Native.Check(
            Native.bcs_render_screenshot(path, AssetHandle.None.Key),
            $"capturing what is being drawn to {path}");
    }

    /// <summary>
    /// Writes what a camera drew into an image to a PNG file.
    /// </summary>
    /// <remarks>
    /// The other end of <see cref="CreateTarget"/>. A camera pointed at a texture draws a picture
    /// nothing on screen shows, and this is how it is read back: a minimap, a portal, or a second
    /// viewport, checked without a person looking at the window it is not in.
    /// </remarks>
    /// <param name="path">Where to write the PNG. Relative paths are resolved by the process.</param>
    /// <param name="target">The image to capture, from <see cref="CreateTarget"/>.</param>
    /// <exception cref="BevyNativeException">
    /// This build has no renderer, or the handle names no image.
    /// </exception>
    public static void Screenshot(string path, AssetHandle target)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Native.Check(
            Native.bcs_render_screenshot(path, target.Key), $"capturing {target} to {path}");
    }

    /// <summary>
    /// Lights the scene from a cubemap, filtered on the GPU.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other way to light a scene from its surroundings. <see cref="SetSkyLighting"/> derives
    /// the map from the atmosphere, which covers an outdoor scene; this takes a picture, for an
    /// indoor one or a scene lit from a photograph.
    /// </para>
    /// <para>
    /// One cubemap rather than the two a baked environment map carries, because Bevy filters it
    /// into its diffuse and specular halves itself. It is the same column of six faces
    /// <see cref="SetSkybox"/> takes, so the same file can be seen behind the scene and be the
    /// light in it.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera whose view is lit.</param>
    /// <param name="cubemap">
    /// Six square faces stacked vertically, or <see cref="AssetHandle.None"/> to take the lighting
    /// off.
    /// </param>
    /// <param name="intensity">How bright the lighting is.</param>
    /// <param name="rotation">Which way the map is turned, for one authored with another axis up.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetImageLighting(
        Entity camera,
        AssetHandle cubemap,
        float intensity = 1000f,
        Quat? rotation = null)
    {
        if (rotation is not { } turn)
        {
            Native.Check(
                Native.bcs_render_set_image_lighting(camera.Bits, cubemap.Key, intensity, null),
                $"lighting {camera} from an image");

            return;
        }

        var parts = stackalloc float[4] { turn.X, turn.Y, turn.Z, turn.W };

        Native.Check(
            Native.bcs_render_set_image_lighting(camera.Bits, cubemap.Key, intensity, parts),
            $"lighting {camera} from an image");
    }

    /// <summary>
    /// Lights the scene from a pair of cubemaps somebody baked earlier.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other end of <see cref="SetImageLighting"/>, which filters one cubemap on the GPU every
    /// time the app starts. This takes the two maps a tool produced, which costs nothing at startup
    /// and suits a shipped game, especially for an environment too large to filter again.
    /// </para>
    /// <para>
    /// <paramref name="diffuse"/> is the blurred map a rough surface reflects and
    /// <paramref name="specular"/> is the sharp one a polished surface reflects. Both are a column
    /// of six square faces like a skybox, and both are turned into cubes before the light is
    /// applied, so a handle asked for in the same frame the camera is spawned works.
    /// </para>
    /// <para>
    /// Passing <see cref="AssetHandle.None"/> for either takes the lighting off, since a baked map
    /// is the pair and half of one is not a weaker version of it.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera whose view is lit.</param>
    /// <param name="diffuse">The blurred map.</param>
    /// <param name="specular">The sharp one.</param>
    /// <param name="intensity">How bright, in candelas per square meter.</param>
    /// <param name="rotation">Which way the maps are turned, or null for not at all.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, or a handle names no image.
    /// </exception>
    public static void SetEnvironmentMap(
        Entity camera,
        AssetHandle diffuse,
        AssetHandle specular,
        float intensity = 1000f,
        Quat? rotation = null)
    {
        if (rotation is not { } turn)
        {
            Native.Check(
                Native.bcs_render_set_environment_map(
                    camera.Bits, diffuse.Key, specular.Key, intensity, null),
                $"lighting {camera} from a baked environment map");

            return;
        }

        var parts = stackalloc float[4] { turn.X, turn.Y, turn.Z, turn.W };

        Native.Check(
            Native.bcs_render_set_environment_map(
                camera.Bits, diffuse.Key, specular.Key, intensity, parts),
            $"lighting {camera} from a baked environment map");
    }

    /// <summary>
    /// Makes an entity a reflection probe: a box inside which surfaces reflect a pair of baked
    /// cubemaps rather than the camera's environment. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The box is the entity's <see cref="Transform"/>, a unit cube before its scale, so a probe
    /// covering a room is placed at the room's center and scaled to its size. The pair is the one
    /// <see cref="SetEnvironmentMap"/> takes, captured from inside the room, and a surface inside
    /// the box picks it in place of what the camera is lit by, so a room stops reflecting the sky
    /// outside it. Reflections are corrected for where in the box the surface is, so a mirror near
    /// a wall shows the wall close.
    /// </para>
    /// <para>
    /// <paramref name="falloff"/> is how much of the box, on each axis from nothing to all of it,
    /// the probe's influence fades across. With none the box has a hard edge; with some, a surface
    /// moving between two overlapping probes blends from one to the other. Bevy uses the nearest
    /// few probes to each view, which is plenty for a building and why a city is split into
    /// probes a street long.
    /// </para>
    /// <para>
    /// <see cref="AssetHandle.None"/> for either map takes the reflection off. A camera is refused,
    /// since a camera is lit by <see cref="SetEnvironmentMap"/>.
    /// </para>
    /// </remarks>
    /// <param name="probe">The entity whose transform is the box.</param>
    /// <param name="diffuse">The blurred map, a column of six square faces.</param>
    /// <param name="specular">The sharp map, the same shape.</param>
    /// <param name="intensity">How bright, in candelas per square meter.</param>
    /// <param name="falloff">The fraction of the box faded across on each axis, or null for none.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is a camera or is gone, or a handle names no image.
    /// </exception>
    public static void SetReflectionProbe(
        Entity probe,
        AssetHandle diffuse,
        AssetHandle specular,
        float intensity = 1000f,
        Vec3? falloff = null)
    {
        if (falloff is not { } fade)
        {
            Native.Check(
                Native.bcs_render_set_reflection_probe(probe.Bits, diffuse.Key, specular.Key, intensity, null),
                $"making {probe} a reflection probe");

            return;
        }

        var parts = stackalloc float[3] { fade.X, fade.Y, fade.Z };

        Native.Check(
            Native.bcs_render_set_reflection_probe(probe.Bits, diffuse.Key, specular.Key, intensity, parts),
            $"making {probe} a reflection probe");
    }

    /// <summary>
    /// Makes an entity an irradiance volume: a box inside which surfaces take their diffuse
    /// indirect light from a grid of points held in a 3D image. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each point of the grid holds the light a surface facing each of the six axis directions
    /// receives there, and a surface between points blends the nearest ones by its normal. So a
    /// wall beside a red carpet picks up red low down and less higher up, which an environment map
    /// the same everywhere cannot do. Bevy ranks it above a reflection probe and the camera's
    /// environment for diffuse light, and ambient light is added on top.
    /// </para>
    /// <para>
    /// The image is how a global illumination technique reaches Bevy's materials. It is an
    /// ordinary 3D image, so one made with <see cref="Shaders.CreateImage(uint, uint, ShaderImageFormat, uint, uint)"/>
    /// and written by a compute shader every frame lights everything drawn with a
    /// material from <see cref="CreateMaterial(MaterialSettings)"/> by whatever the shader worked out, with no change to how
    /// those materials are drawn. A grid <c>x</c> by <c>y</c> by <c>z</c> points is an image
    /// <c>x</c> wide, <c>2y</c> high and <c>3z</c> deep, and <c>bcs_scene</c>'s
    /// <c>irradiance_texel</c> and <c>irradiance_point_in_box</c> address it by point and direction,
    /// so a shader never deals with the packing. It is sampled filtered, so it is made
    /// <see cref="ShaderImageFormat.Rgba16Float"/>. A baked one can come from a file as well.
    /// </para>
    /// <para>
    /// The box is the entity's <see cref="Transform"/>, and <paramref name="falloff"/> is as for
    /// <see cref="SetReflectionProbe"/>. <see cref="AssetHandle.None"/> takes the volume off.
    /// </para>
    /// </remarks>
    /// <param name="probe">The entity whose transform is the box.</param>
    /// <param name="voxels">The 3D image.</param>
    /// <param name="intensity">
    /// What the image's values are multiplied by, in candelas per square meter, so an image holding
    /// light in its own units is brought into the scene's.
    /// </param>
    /// <param name="falloff">The fraction of the box faded across on each axis, or null for none.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is a camera or is gone, the handle names no image, or the image is not 3D.
    /// </exception>
    public static void SetIrradianceVolume(
        Entity probe,
        AssetHandle voxels,
        float intensity = 1000f,
        Vec3? falloff = null)
    {
        if (falloff is not { } fade)
        {
            Native.Check(
                Native.bcs_render_set_irradiance_volume(probe.Bits, voxels.Key, intensity, null),
                $"making {probe} an irradiance volume");

            return;
        }

        var parts = stackalloc float[3] { fade.X, fade.Y, fade.Z };

        Native.Check(
            Native.bcs_render_set_irradiance_volume(probe.Bits, voxels.Key, intensity, parts),
            $"making {probe} an irradiance volume");
    }

    /// <summary>
    /// Makes an entity a reflection probe that renders what is around it, or with
    /// <see langword="null"/> stops. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Six cameras at the probe's center draw the scene into the faces of a cube, and Bevy filters
    /// the cube into the probe's light on the GPU, so the room is reflected as it is rather than as
    /// somebody baked it. The box is the entity's <see cref="Transform"/> as for
    /// <see cref="SetReflectionProbe"/>, and the cameras follow the probe's center as it moves.
    /// </para>
    /// <para>
    /// A live probe draws the scene six more times every frame, which a reflection of something
    /// moving needs and a large one cannot afford. One that is not live captures once, over the
    /// first few frames after nothing is left compiling, so asking at startup captures the scene as
    /// it will be drawn rather than while its materials are still missing.
    /// <see cref="RecaptureProbe"/> captures again when the room changes, or once a texture that
    /// loaded late has arrived. Either way the light is a frame behind what the cameras drew.
    /// </para>
    /// <para>
    /// The cameras see the probe's own light, so each capture reflects the one before, and light
    /// bounces a little further every frame. That settles as long as the intensity only undoes the
    /// exposure the faces were drawn at, as the default does; much more makes the room brighter
    /// each frame. A camera is refused, as for the other probes.
    /// </para>
    /// </remarks>
    /// <param name="probe">The entity whose transform is the box.</param>
    /// <param name="settings">How to capture, or null to stop.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is a camera or is gone, or the size is not a power of two.
    /// </exception>
    public static void SetProbeCapture(Entity probe, ProbeCaptureSettings? settings)
    {
        if (settings is null)
        {
            Native.Check(
                Native.bcs_render_set_probe_capture(probe.Bits, 0, 0f, null, 0, 0f),
                $"stopping the capture of {probe}");

            return;
        }

        var parts = stackalloc float[3] { settings.Falloff.X, settings.Falloff.Y, settings.Falloff.Z };

        Native.Check(
            Native.bcs_render_set_probe_capture(
                probe.Bits,
                settings.Size,
                settings.Intensity,
                parts,
                settings.Live ? 1 : 0,
                settings.Near),
            $"capturing {probe} as a reflection probe");
    }

    /// <summary>
    /// Captures a probe that is not live again, after what is around it has changed. Only valid
    /// inside a system.
    /// </summary>
    /// <exception cref="BevyNativeException">The entity has no capture.</exception>
    public static void RecaptureProbe(Entity probe) =>
        Native.Check(Native.bcs_render_recapture_probe(probe.Bits), $"capturing {probe} again");

    /// <summary>
    /// Lights the scene from the sky this camera is already scattering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What makes a surface pick up the color of what is around it rather than only what a lamp
    /// points at it. The environment map is derived from the atmosphere each frame, so it follows
    /// the sun, and a scene lit this way goes warm at dusk without anything being animated.
    /// </para>
    /// <para>
    /// Needs <see cref="SetAtmosphere"/> on the same camera, because what it filters is the sky
    /// being drawn. A camera with no atmosphere has nothing to derive a map from.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera whose view is lit.</param>
    /// <param name="intensity">How bright the lighting is. One matches the sky's own.</param>
    /// <param name="size">
    /// The square resolution of the cubemap it generates, which has to be a power of two.
    /// </param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, or the size is not a power of two.
    /// </exception>
    public static void SetSkyLighting(Entity camera, float intensity = 1f, uint size = 512) =>
        Native.Check(
            Native.bcs_render_set_sky_lighting(camera.Bits, 1, intensity, size),
            $"lighting {camera} from the sky");

    /// <summary>Stops lighting the scene from the sky.</summary>
    public static void ClearSkyLighting(Entity camera) => Native.Check(
        Native.bcs_render_set_sky_lighting(camera.Bits, 0, 0f, 0),
        $"taking the sky lighting off {camera}");

    /// <summary>
    /// Draws a cubemap behind everything a camera draws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The image is a column of six square faces, which is the layout every cubemap texture ships
    /// in, and it is turned into a cube once it has loaded. An image of any other shape draws
    /// nothing and says so on the log rather than failing here, because whether the file is the
    /// right shape is not known until it has been decoded.
    /// </para>
    /// <para>
    /// <paramref name="brightness"/> scales the samples into the units the rest of the scene is lit
    /// in, which are candelas per square meter, so the useful numbers are in the hundreds or
    /// thousands. A brightness of one is a night sky and comes out black, which reads as a skybox
    /// that failed rather than one that is very dark. The skybox is seen behind the scene and does
    /// not light it; <see cref="SetImageLighting"/> lights a scene from the same file.
    /// </para>
    /// <para>
    /// Pass <see cref="AssetHandle.None"/> to take the skybox off.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera to draw it behind.</param>
    /// <param name="cubemap">Six square faces stacked vertically, from the asset server.</param>
    /// <param name="brightness">How much to scale the samples by.</param>
    /// <param name="rotation">Which way the cube is turned, for a cubemap authored Z-up.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetSkybox(
        Entity camera,
        AssetHandle cubemap,
        float brightness = 1000f,
        Quat? rotation = null)
    {
        if (rotation is not { } turn)
        {
            Native.Check(
                Native.bcs_render_set_skybox(camera.Bits, cubemap.Key, brightness, null),
                $"putting a skybox on {camera}");

            return;
        }

        var parts = stackalloc float[4] { turn.X, turn.Y, turn.Z, turn.W };

        Native.Check(
            Native.bcs_render_set_skybox(camera.Bits, cubemap.Key, brightness, parts),
            $"putting a skybox on {camera}");
    }

    /// <summary>
    /// Grades the picture a camera drew, after tonemapping.
    /// </summary>
    /// <remarks>
    /// What a look is made of once the scene is drawn. Passing <see langword="null"/> puts the
    /// camera back to the engine's own grading, which changes nothing about the picture.
    /// </remarks>
    /// <param name="camera">The camera to grade.</param>
    /// <param name="settings">The grade, or null for none.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetColorGrading(Entity camera, GradingSettings? settings)
    {
        if (settings is null)
        {
            Native.Check(
                Native.bcs_render_set_grading(camera.Bits, null),
                $"clearing the color grade on {camera}");

            return;
        }

        var native = settings.ToNative();

        Native.Check(
            Native.bcs_render_set_grading(camera.Bits, &native),
            $"grading {camera}");
    }

    /// <summary>
    /// Sets the exposure a camera meters the scene at, in EV-100.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The number a photographer would set. Sunlight is around 15, an overcast day around 12 and
    /// an interior around 7, so a scene lit in physical units and metered wrongly comes out far too
    /// bright or far too dark rather than subtly off.
    /// </para>
    /// <para>
    /// This is the base an auto exposure pass corrects rather than an alternative to it. With
    /// <see cref="EffectSettings.AutoExposure"/> on, what is set here is where it starts from.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera to meter.</param>
    /// <param name="ev100">The exposure value at ISO 100.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetExposure(Entity camera, float ev100) => Native.Check(
        Native.bcs_render_set_exposure(camera.Bits, ev100), $"metering {camera}");

    /// <summary>
    /// Sets a camera's exposure from the lens it stands in for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same three numbers a photographer sets, and the same ones a real lens is described by,
    /// so a camera can be written down once as a lens and metered from that rather than from an
    /// exposure value worked out by hand. This and <see cref="SetExposure"/> set the same thing.
    /// </para>
    /// <para>
    /// Each number keeps Bevy's own when it is left at zero, which is f/1 at a hundred and
    /// twenty-fifth of a second and ISO 100.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera to meter.</param>
    /// <param name="aperture">The f-stop. A larger number lets less light in.</param>
    /// <param name="shutter">How long the shutter is open, in seconds.</param>
    /// <param name="sensitivity">The ISO.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetLensExposure(
        Entity camera,
        float aperture = 0f,
        float shutter = 0f,
        float sensitivity = 0f) =>
        Native.Check(
            Native.bcs_render_set_lens_exposure(camera.Bits, aperture, shutter, sensitivity),
            $"metering {camera} from a lens");

    /// <summary>
    /// Sorts transparent fragments rather than whole objects, for one camera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What fixes transparent surfaces drawn in the wrong order. Ordinary alpha blending sorts
    /// objects by distance, so two panes of glass crossing each other, or one mesh whose own faces
    /// overlap, come out right from some angles and wrong from others, and no amount of reordering
    /// the scene fixes both. This sorts each pixel's fragments instead.
    /// </para>
    /// <para>
    /// It costs a buffer the size of the screen times <paramref name="layers"/>, which is why it
    /// is per camera and off unless asked for. Every number keeps Bevy's own when it is left at
    /// zero.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera to sort for.</param>
    /// <param name="on">Whether to sort at all. False takes it off again.</param>
    /// <param name="layers">
    /// How many fragments a pixel sorts exactly before the rest are merged approximately. More is
    /// more accurate and slower.
    /// </param>
    /// <param name="average">How many fragments a pixel budgets for on average.</param>
    /// <param name="threshold">The alpha below which a fragment is dropped rather than stored.</param>
    /// <exception cref="BevyNativeException">The entity is not a camera.</exception>
    public static void SetSortedTransparency(
        Entity camera,
        bool on = true,
        uint layers = 0,
        float average = 0f,
        float threshold = 0f) =>
        Native.Check(
            Native.bcs_render_set_sorted_transparency(
                camera.Bits, on ? 1 : 0, layers, average, threshold),
            $"sorting transparency for {camera}");

    /// <summary>
    /// Sets the ambient light every camera without its own is lit by. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// Light arriving from every direction at once, which is the cheapest stand-in for all the
    /// light bounced around a scene, and what ambient occlusion darkens where a surface is hemmed
    /// in. <paramref name="brightness"/> is in candela per square meter, the unit Bevy's lights
    /// use, and Bevy's own is eighty.
    /// </remarks>
    public static void SetAmbientLight((float R, float G, float B) color, float brightness) =>
        Native.Check(
            Native.bcs_render_set_ambient_light(0, color.R, color.G, color.B, Math.Max(0f, brightness)),
            "setting the ambient light");

    /// <summary>
    /// Gives a camera an ambient light of its own, or with <see langword="null"/> takes it away so
    /// the camera is lit by everyone's again. Only valid inside a system.
    /// </summary>
    public static void SetAmbientLight(Entity camera, (float R, float G, float B)? color, float brightness = 80f)
    {
        var (r, g, b) = color ?? (1f, 1f, 1f);

        Native.Check(
            Native.bcs_render_set_ambient_light(camera.Bits, r, g, b, color is null ? -1f : Math.Max(0f, brightness)),
            $"setting the ambient light of {camera}");
    }

    /// <summary>
    /// Turns Bevy's screen-space ambient occlusion on for a camera at a quality, or with
    /// <see langword="null"/> off. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Occlusion darkens the ambient and environment light Bevy's own materials receive where a
    /// surface is hemmed in, in corners, creases and under things, rather than darkening the
    /// finished picture, which would take direct light with it. It needs depth and normals, which it
    /// asks for itself, and a camera drawn once a pixel (<see cref="PostSettings.Msaa"/> of one);
    /// on a multisampled camera Bevy leaves it off with a warning.
    /// </para>
    /// <para>
    /// It is also the way in for occlusion of your own. While it is on, a compute shader on the
    /// camera sees the texture Bevy's materials read under the name <c>ambient_occlusion</c>, and
    /// one run at <see cref="FramePoint.AfterPrepass"/> writing it replaces Bevy's answer with its
    /// own before anything is lit. Bevy still computes its own first, so a camera replacing it asks
    /// for <see cref="AmbientOcclusionQuality.Low"/>.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="quality">How many samples a pixel takes, or null to turn it off.</param>
    /// <param name="thickness">
    /// How thick Bevy assumes what it sees to be, in world units, which decides how far behind a
    /// surface something has to be before it stops occluding. Zero keeps Bevy's own.
    /// </param>
    public static void SetAmbientOcclusion(Entity camera, AmbientOcclusionQuality? quality, float thickness = 0f) =>
        Native.Check(
            Native.bcs_render_set_ambient_occlusion(camera.Bits, quality is { } level ? (int)level : -1, thickness),
            $"setting the ambient occlusion of {camera}");

    /// <summary>
    /// Draws Bevy's own materials deferred, into a G-buffer lit afterward, or forward, lit as they
    /// are drawn, which is the default. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// Screen-space reflections read the deferred G-buffer, and deferred makes many lights cheap.
    /// It needs cameras drawn once a pixel (<see cref="PostSettings.Msaa"/> of one), and applies to
    /// Bevy's own materials, since one a Slang program draws is always forward, since it writes a
    /// color rather than a surface description. Every Bevy material is prepared again when it
    /// changes.
    /// </remarks>
    public static void SetDeferredRendering(bool on) =>
        Native.Check(Native.bcs_render_set_deferred(on ? 1 : 0), "switching between forward and deferred");

    /// <summary>
    /// Turns Bevy's screen-space reflections on for a camera, or with <see langword="null"/> off.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reflections are traced against the depth buffer and read the lit picture, so they show what
    /// is on screen, fading out at its edges, on surfaces smoother than the settings' roughness
    /// ranges. They read Bevy's G-buffer, so turning them on also turns on
    /// <see cref="SetDeferredRendering"/> and asks the camera for the depth and deferred prepasses.
    /// </para>
    /// <para>
    /// What leaves the screen leaves its reflection too, which is the limit of any screen-space
    /// technique. A reflection probe or a traced reflection fills that in.
    /// </para>
    /// </remarks>
    public static void SetScreenSpaceReflections(Entity camera, ReflectionSettings? settings)
    {
        if (settings is null)
        {
            Native.Check(
                Native.bcs_render_set_screen_space_reflections(camera.Bits, null),
                $"taking reflections off {camera}");
            return;
        }

        var config = new NativeReflectionConfig
        {
            MinRoughnessStart = settings.FadeInRoughness.Start,
            MinRoughnessFull = settings.FadeInRoughness.Full,
            MaxRoughnessStart = settings.FadeOutRoughness.Start,
            MaxRoughnessEnd = settings.FadeOutRoughness.Gone,
            EdgeGone = settings.EdgeFade.Gone,
            EdgeFull = settings.EdgeFade.Full,
            Thickness = settings.Thickness,
            LinearSteps = Math.Max(1u, settings.Steps),
            LinearExponent = settings.StepExponent,
            BisectionSteps = settings.RefineSteps,
            UseSecant = settings.Secant ? 1 : 0,
        };

        Native.Check(
            Native.bcs_render_set_screen_space_reflections(camera.Bits, &config),
            $"turning reflections on for {camera}");
    }

    /// <summary>
    /// Asks for a picture to be read back into memory rather than written to a file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same capture <see cref="Screenshot(string)"/> takes, delivered as bytes instead of as a
    /// PNG. A file is for a person to look at; this is for a program to inspect, so a test can
    /// assert on what was drawn.
    /// </para>
    /// <para>
    /// The picture arrives a frame or two later, because it has to come back off the GPU, so this
    /// answers with a ticket rather than with pixels. Poll
    /// <see cref="TryReadCapture(Capture, out CapturedImage?)"/> until it says yes, from a later
    /// frame rather than in a loop, because the picture waits on the frames.
    /// </para>
    /// <para>
    /// A capture of the first frames of a run is a picture of a window that has been cleared and
    /// not yet drawn into. A material's render pipeline is compiled the first time something asks
    /// to be drawn with it, and until it is ready the renderer skips the mesh and clears the frame
    /// anyway, so an early capture shows the clear color with nothing in it. That is indistinguishable
    /// from a mesh that never arrived, so give a fresh run a hundred frames before believing an
    /// empty picture.
    /// </para>
    /// </remarks>
    /// <param name="target">
    /// The image to capture, from <see cref="CreateTarget"/>, or
    /// <see cref="AssetHandle.None"/> for whatever this run is drawing into.
    /// </param>
    /// <returns>A ticket naming the capture.</returns>
    /// <exception cref="BevyNativeException">
    /// This build has no renderer, or the handle names no image.
    /// </exception>
    public static Capture BeginCapture(AssetHandle target)
    {
        var id = Native.bcs_render_capture(target.Key);
        if (id == NativeStatus.Unsupported) throw NoRenderer("Capturing a picture");

        Native.Check(id, $"asking for a capture of {target}");
        return new Capture(id);
    }

    /// <summary>Asks for a picture of whatever this run is drawing into.</summary>
    public static Capture BeginCapture() => BeginCapture(AssetHandle.None);

    /// <summary>
    /// Reads a capture once it has arrived, and forgets it.
    /// </summary>
    /// <remarks>
    /// False means the picture is still on its way, which is the ordinary answer for the first
    /// frame or two. True hands it over and drops the engine's copy, because it is a megabyte or
    /// two and nothing on that side knows when the caller would be done with it.
    /// </remarks>
    /// <param name="capture">The ticket from <see cref="BeginCapture(AssetHandle)"/>.</param>
    /// <param name="picture">The pixels, when this returns true.</param>
    /// <returns>Whether the picture had arrived.</returns>
    /// <exception cref="BevyNativeException">The ticket names no capture.</exception>
    public static bool TryReadCapture(Capture capture, out CapturedImage? picture)
    {
        picture = null;

        uint width;
        uint height;

        var needed = Native.bcs_render_capture_read(capture.Id, &width, &height, null, 0);

        // Still coming back off the GPU. Asking again next frame is the whole of the protocol.
        if (needed == NativeStatus.InvalidState) return false;
        if (needed == NativeStatus.Unsupported) throw NoRenderer("Reading a capture");

        Native.Check(needed, $"asking how large {capture} is");

        var pixels = new byte[needed];

        fixed (byte* buffer = pixels)
        {
            Native.Check(
                Native.bcs_render_capture_read(capture.Id, &width, &height, buffer, needed),
                $"reading {capture}");
        }

        picture = new CapturedImage(width, height, pixels);
        return true;
    }

    /// <summary>Forgets a capture that will not be read.</summary>
    /// <remarks>
    /// For a caller that stopped waiting. A capture that has arrived holds its pixels until
    /// something drops them, and one that never arrives holds nothing.
    /// </remarks>
    public static void ReleaseCapture(Capture capture) =>
        Native.Check(Native.bcs_render_capture_release(capture.Id), $"releasing {capture}");

    /// <summary>
    /// Creates an empty image a camera can draw into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a portal, a security monitor, a minimap or a second viewport is built from. Point a
    /// camera at it with <see cref="SetCameraTarget"/>, and give the same handle to a material as
    /// its <see cref="MaterialSettings.BaseColorTexture"/>, and the surface carrying that material
    /// shows what the camera sees.
    /// </para>
    /// <para>
    /// The image is empty until something draws into it, and nothing loads, so the handle is
    /// usable on the frame it is returned. It is sized in pixels rather than in logical units,
    /// because nothing about it is scaled by a desktop.
    /// </para>
    /// </remarks>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <returns>A handle to the image.</returns>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateTarget(uint width, uint height)
    {
        var key = Native.bcs_render_create_target(width, height);
        if (key == NativeStatus.Unsupported) throw NoRenderer("Creating a render target");

        Native.Check(key, $"creating a {width}x{height} render target");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Makes an image out of pixels held here, and hands back a handle to it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The other end of <see cref="TryReadCapture"/>. Reading gives back what was drawn; this takes
    /// a picture that was never in a file, for a texture worked out at startup, a mask built from a
    /// heightmap, or a capture handed on to a material.
    /// </para>
    /// <para>
    /// Nothing loads, so the handle is usable on the frame it is returned, and the pixels are
    /// copied rather than kept, so the array is the caller's again afterwards.
    /// </para>
    /// </remarks>
    /// <param name="pixels">
    /// <paramref name="width"/> by <paramref name="height"/> pixels of RGBA, a row at a time from
    /// the top, which is the layout <see cref="CapturedImage.Pixels"/> comes back in.
    /// </param>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="srgb">
    /// Whether the numbers are a color somebody chose, as a picture usually is. False reads them as
    /// they are, for a picture whose numbers mean something else, such as a normal map or a
    /// roughness mask.
    /// </param>
    /// <returns>A handle to the image.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="pixels"/> is not four bytes per pixel of the size given.
    /// </exception>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static AssetHandle CreateImage(
        ReadOnlySpan<byte> pixels,
        uint width,
        uint height,
        bool srgb = true)
    {
        var wanted = (long)width * height * 4;

        if (width == 0 || height == 0)
            throw new ArgumentException("An image needs a width and a height.", nameof(width));

        if (pixels.Length != wanted)
        {
            throw new ArgumentException(
                $"A {width}x{height} image is {wanted} bytes of RGBA, and {pixels.Length} were "
                + "given.",
                nameof(pixels));
        }

        int key;

        fixed (byte* at = pixels)
        {
            key = Native.bcs_render_create_image(at, width, height, srgb ? 1 : 0);
        }

        if (key < 0) throw NoRenderer($"Creating a {width}x{height} image");

        return new AssetHandle(key);
    }

    /// <summary>
    /// Has an image of six square faces stacked from top to bottom treated as a cubemap.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a shader's <c>TextureCube</c>, and the layout <see cref="SetSkybox"/> takes. The faces
    /// are in the order +X, -X, +Y, -Y, +Z, -Z.
    /// </para>
    /// <para>
    /// Applied when the pixels arrive, since the shape of a picture is not known until it has been
    /// decoded, so the handle can be passed on at once. An image that is not six squares tall is
    /// left as it is, with a warning in the log.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">The handle names no image, or there is no renderer.</exception>
    public static void MakeCubemap(AssetHandle image) =>
        Native.Check(Native.bcs_render_make_cubemap(image.Key), "making an image a cubemap");

    /// <summary>
    /// Has an image of <paramref name="layers"/> equal pictures stacked from top to bottom treated
    /// as an array of them.
    /// </summary>
    /// <remarks>
    /// For a shader's <c>Texture2DArray</c>, which holds many pictures of one size behind one
    /// binding, such as a terrain's ground types or a sprite's frames. Applied when the pixels
    /// arrive, like <see cref="MakeCubemap"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="layers"/> is less than one.</exception>
    /// <exception cref="BevyNativeException">The handle names no image, or there is no renderer.</exception>
    public static void MakeTextureArray(AssetHandle image, int layers)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(layers, 1);
        Native.Check(
            Native.bcs_render_reshape_image(image.Key, layers, 0),
            $"cutting an image into {layers} layers");
    }

    /// <summary>
    /// Has an image of <paramref name="slices"/> equal pictures stacked from top to bottom treated
    /// as a 3D texture that many deep.
    /// </summary>
    /// <remarks>
    /// For a shader's <c>Texture3D</c>, such as fog, clouds, a color grading table or anything else
    /// sampled at a point in space. The first slice is the front. Applied when the pixels arrive,
    /// like <see cref="MakeCubemap"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slices"/> is less than one.</exception>
    /// <exception cref="BevyNativeException">The handle names no image, or there is no renderer.</exception>
    public static void MakeVolume(AssetHandle image, int slices)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(slices, 1);
        Native.Check(
            Native.bcs_render_reshape_image(image.Key, slices, 1),
            $"cutting an image into {slices} slices");
    }

    /// <summary>
    /// Points a camera at an image instead of at the window.
    /// </summary>
    /// <remarks>
    /// <see cref="AssetHandle.None"/> puts it back on the window, which is where a camera starts.
    /// The camera keeps everything else it was given, because its projection, its layers, its
    /// order and its post-processing are about what it draws rather than about where the result
    /// goes.
    /// </remarks>
    /// <param name="camera">The camera, from <see cref="SpawnCamera3d(CameraSettings)"/>.</param>
    /// <param name="target">The image to draw into, or <see cref="AssetHandle.None"/> for the window.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, or the handle names no image.
    /// </exception>
    public static void SetCameraTarget(Entity camera, AssetHandle target) => Native.Check(
        Native.bcs_render_set_camera_target(camera.Bits, target.Key),
        $"pointing {camera} at {target}");

    /// <summary>
    /// Where a world point lands on a camera's viewport, in logical pixels.
    /// </summary>
    /// <remarks>
    /// The same coordinates the cursor is reported in, so something drawn in the world can be
    /// hit-tested against the pointer. A point behind the camera answers <see langword="false"/>
    /// rather than a number that would be off the screen in the wrong direction.
    /// </remarks>
    public static bool TryProject(Entity camera, Vec3 point, out float x, out float y)
    {
        var screen = stackalloc float[2];

        if (Native.bcs_render_world_to_viewport(camera.Bits, point.X, point.Y, point.Z, screen) < 0)
        {
            x = 0f;
            y = 0f;
            return false;
        }

        x = screen[0];
        y = screen[1];
        return true;
    }

    /// <summary>
    /// The ray through a point on a camera's viewport.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="TryProject"/>. Where a drag is taking something is a question
    /// about this ray and the axis being dragged along, rather than about pixels.
    /// </remarks>
    public static bool TryRay(Entity camera, float x, float y, out Vec3 origin, out Vec3 direction)
    {
        var ray = stackalloc float[6];

        if (Native.bcs_render_viewport_to_world(camera.Bits, x, y, ray) < 0)
        {
            origin = default;
            direction = default;
            return false;
        }

        origin = new Vec3(ray[0], ray[1], ray[2]);
        direction = new Vec3(ray[3], ray[4], ray[5]);
        return true;
    }

    /// <summary>
    /// The box an entity occupies in the world, or <see langword="false"/> when it has none.
    /// </summary>
    /// <remarks>
    /// Bevy computes bounds for everything it draws, in the mesh's own space; what comes back
    /// here is those bounds put through the entity's global transform, so a rotated object gets
    /// the box around its corners rather than its corners moved. Anything not drawn, a camera or
    /// a bare entity, has no bounds and answers <see langword="false"/>.
    /// </remarks>
    public static bool TryGetBounds(Entity entity, out Vec3 min, out Vec3 max)
    {
        var bounds = stackalloc float[6];

        if (Native.bcs_render_bounds(entity.Bits, bounds) < 0)
        {
            min = default;
            max = default;
            return false;
        }

        min = new Vec3(bounds[0], bounds[1], bounds[2]);
        max = new Vec3(bounds[3], bounds[4], bounds[5]);
        return true;
    }

    /// <summary>
    /// Draws an entity's mesh as its own edges, or stops drawing them.
    /// </summary>
    /// <remarks>
    /// The shape itself rather than a box round it, which an editor outlines a selection with when
    /// the box is not enough. The line pipeline it needs is a desktop one. Where a backend cannot
    /// draw lines, this is accepted and nothing appears.
    /// </remarks>
    /// <param name="entity">What to draw, or stop drawing.</param>
    /// <param name="on">Whether to draw it.</param>
    /// <param name="color">Linear RGBA for the lines.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static void SetWireframe(
        Entity entity, bool on, (float R, float G, float B, float A) color = default)
    {
        var status = Native.bcs_render_wireframe(
            entity.Bits, on ? 1 : 0, color.R, color.G, color.B, color.A);

        if (status == NativeStatus.Unsupported) throw NoRenderer("Drawing a wireframe");

        // An entity that has gone is not an error to stop drawing, because a selection outlives
        // what it pointed at by a frame, and asking after that is how it is cleaned up.
        if (status == NativeStatus.NoEntity) return;

        Native.Check(status, "drawing a wireframe");
    }

    /// <summary>
    /// Sets how large a shadow map each kind of light gets, in pixels on a side.
    /// </summary>
    /// <remarks>
    /// One size for every directional light and one for every point and spot light, because Bevy
    /// keeps these globally rather than per light. Larger is sharper and costs memory and fill
    /// rate on every shadow-casting light at once. Bevy's defaults are 2048 and 1024. Zero leaves
    /// that kind as it is, so one can be changed without knowing the other.
    /// </remarks>
    /// <param name="directional">Size for directional lights, or 0 to leave it.</param>
    /// <param name="point">Size for point and spot lights, or 0 to leave it.</param>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static void SetShadowMapSize(uint directional = 0, uint point = 0)
    {
        var status = Native.bcs_render_set_shadow_maps(directional, point);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting the shadow map size");

        Native.Check(status, "setting the shadow map size");
    }

    /// <summary>
    /// Puts an entity on a set of render layers, as a bit per layer.
    /// </summary>
    /// <remarks>
    /// A camera draws an entity only where their layers overlap. Zero takes the entity back to
    /// Bevy's default layer, which every camera sees unless it says otherwise.
    /// </remarks>
    /// <example>
    /// <code>
    /// const uint Minimap = 1u &lt;&lt; 1;
    ///
    /// Render.SetLayers(ctx.Ecs, marker, Minimap);          // only the minimap camera sees it
    /// Render.SetLayers(ctx.Ecs, player, 1u | Minimap);     // both cameras do
    /// </code>
    /// </example>
    /// <exception cref="BevyNativeException">The entity is gone, or this build has no renderer.</exception>
    public static void SetLayers(EcsWorld world, Entity entity, uint layers)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_render_set_layers(entity.Bits, layers);
        if (status == NativeStatus.Unsupported) throw NoRenderer("Setting render layers");

        Native.Check(status, $"setting the render layers of {entity}");
    }

    /// <summary>Attaches a handle through one of the components that carry one.</summary>
    private static void Attach(
        EcsWorld world,
        Entity entity,
        string component,
        AssetHandle handle,
        string described)
    {
        ArgumentNullException.ThrowIfNull(world);

        var status = Native.bcs_ecs_insert_asset(entity.Bits, component, handle.Key);
        if (status == NativeStatus.Unsupported) throw NoRenderer($"Attaching {described}");
        if (status == NativeStatus.NoEntity)
            throw new BevyNativeException(
                NativeStatus.NoEntity,
                $"Cannot attach {described} to {entity}: either the entity is no longer alive, "
                + "or the handle has been released.");
        if (status == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"That handle does not point at {described}. Check that the handle came from the "
                + "matching Create call.");

        Native.Check(status, $"attaching {described}");
    }

    /// <summary>The error for asking a headless build to do something graphical.</summary>
    private static BevyNativeException NoRenderer(string attempted) =>
        new(NativeStatus.Unsupported,
            $"{attempted} needs a native build with the renderer compiled in. Rebuild the bridge "
            + "with build/build-native.sh --render, or guard the call with App.HasRenderer.");
}

/// <summary>How many samples a pixel of Bevy's ambient occlusion takes.</summary>
public enum AmbientOcclusionQuality
{
    /// <summary>Four, plus what temporal antialiasing adds.</summary>
    Low = 0,

    /// <summary>Eight.</summary>
    Medium = 1,

    /// <summary>Eighteen, which is Bevy's own choice.</summary>
    High = 2,

    /// <summary>Fifty-four.</summary>
    Ultra = 3,
}

/// <summary>How long one render pass took. See <see cref="Render.Timings"/>.</summary>
/// <param name="Name">The pass, as Bevy names it, or <c>shader</c> and a program's file name.</param>
/// <param name="CpuMilliseconds">How long recording it took, or null where it was not measured.</param>
/// <param name="GpuMilliseconds">
/// How long the GPU spent running it, or null where the adapter has no timestamp queries.
/// </param>
public readonly record struct PassTiming(string Name, double? CpuMilliseconds, double? GpuMilliseconds);

/// <summary>How a reflection probe renders what is around it.</summary>
public sealed class ProbeCaptureSettings
{
    /// <summary>
    /// Texels along a side of each face. A power of two, since Bevy's filter needs one, and the
    /// sharpness of what a polished surface reflects.
    /// </summary>
    public uint Size { get; set; } = 256;

    /// <summary>
    /// How bright the captured light is, in candelas per square meter. The faces are drawn at
    /// Bevy's default exposure, which divides light by about a thousand, and the default undoes it.
    /// </summary>
    public float Intensity { get; set; } = 1000f;

    /// <summary>The fraction of the box faded across on each axis, as for a baked probe.</summary>
    public Vec3 Falloff { get; set; }

    /// <summary>Whether to capture every frame, or once until asked again.</summary>
    public bool Live { get; set; } = true;

    /// <summary>
    /// How far from the center the cameras start seeing, so a probe inside a small object does not
    /// capture the inside of it.
    /// </summary>
    public float Near { get; set; } = 0.05f;
}

/// <summary>How a camera's screen-space reflections are traced. The defaults are Bevy's.</summary>
public sealed class ReflectionSettings
{
    /// <summary>
    /// The roughness at which reflections start to appear and at which they are whole. Smoother
    /// than the first, a surface gets none, which is Bevy's choice, because a mirror-smooth surface
    /// shows the flaws of a screen-space trace most, and is left to a reflection probe.
    /// </summary>
    public (float Start, float Full) FadeInRoughness { get; set; } = (0.08f, 0.12f);

    /// <summary>The roughness at which reflections start to fade and at which they are gone.</summary>
    public (float Start, float Gone) FadeOutRoughness { get; set; } = (0.55f, 0.6f);

    /// <summary>
    /// Where reflections stop at the edge of the picture and where they are whole, as fractions of
    /// it, which hides the edge of what can be reflected.
    /// </summary>
    public (float Gone, float Full) EdgeFade { get; set; } = (0f, 0f);

    /// <summary>
    /// How thick what the depth buffer holds is taken to be, in world units, since a picture's
    /// depth says where a surface starts but not where it ends.
    /// </summary>
    public float Thickness { get; set; } = 0.25f;

    /// <summary>Steps of the first march along the ray.</summary>
    public uint Steps { get; set; } = 10;

    /// <summary>How the steps spread out: one for even steps, more for finer ones near the start.</summary>
    public float StepExponent { get; set; } = 1f;

    /// <summary>Steps of the search that narrows a hit down.</summary>
    public uint RefineSteps { get; set; } = 5;

    /// <summary>Whether a hit is refined once more by where the ray and the surface cross.</summary>
    public bool Secant { get; set; } = true;
}
