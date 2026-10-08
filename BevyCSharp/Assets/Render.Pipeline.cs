using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
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
    /// True once the renderer has compiled every pipeline it was asked for, so whatever has been
    /// spawned can be drawn. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy compiles a pipeline the first time something needs it, over several frames and away
    /// from the main thread, and draws nothing with it until then. A loaded asset is therefore not
    /// yet a visible one, and a loading screen that should come down only when the level can be
    /// seen waits for this as well as for
    /// <see cref="AssetServer.StateWithDependenciesOf(AssetHandle)"/>, and for some frames of both
    /// together, since a pipeline asked for late starts the count again.
    /// </para>
    /// <para>
    /// False before the first frame has been drawn, when nothing is known yet, and in a headless
    /// build, which compiles nothing because it draws nothing.
    /// </para>
    /// </remarks>
    public static bool PipelinesReady()
    {
        if (!App.HasRenderer) return false;
        uint waiting;
        var status = Native.bcs_render_pipelines_waiting(&waiting);
        if (status == NativeStatus.NotPresent) return false;
        Native.Check(status, "Render.PipelinesReady");
        return waiting == 0;
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
    /// meet the triangles the picture shows. A mesh with no texture coordinates, as a model drawn in
    /// plain colors often is, is given coordinates and tangents of zero, as Bevy's own example does.
    /// The entity's material has to be one from <see cref="CreateMaterial(MaterialSettings)"/>, and
    /// the mesh has to have loaded.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// Ray-traced lighting is not running, the mesh has not loaded, or it is not indexed triangles
    /// with normals.
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
    /// <paramref name="saveTo"/>, a path under the asset root or a full one, also writes the
    /// finished mesh as a <c>.meshlet_mesh</c> file, its folders made, which
    /// <see cref="AssetKind.MeshletMesh"/> loads. A game bakes this way, converting once, in a tool
    /// or on the first run, and loading the file after, which takes as long as reading it; a file
    /// written to a cache outside the asset root is loaded once its folder is mounted
    /// (<see cref="AssetFiles.Mount(string, string)"/>).
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
    /// Makes a material that draws each cluster of a meshlet mesh in a color of its own. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// For seeing how a mesh was cut and which level of detail is drawn where: clusters far from
    /// the camera are fewer and larger. Each cluster's color is picked from its number, so it holds
    /// still as the camera moves until a different level is drawn. It is Bevy's own, the material
    /// its meshlet renderer draws when no fragment program is named, and is given to an entity with
    /// <see cref="SetMaterial"/> as any other. On an ordinary mesh it draws as Bevy's default
    /// material.
    /// </remarks>
    /// <exception cref="BevyNativeException">Meshlets are not running (see <see cref="MeshletsActive"/>).</exception>
    public static AssetHandle CreateClusterMaterial() =>
        new(Native.Check(Native.bcs_render_cluster_material_create(), "making a cluster material"));

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

    /// <summary>The type path of the component holding an entity's mesh.</summary>
    internal const string MeshComponent = "bevy_mesh::components::Mesh3d";

    /// <summary>The type path of the component holding an entity's standard material.</summary>
    internal const string MaterialComponent =
        "bevy_pbr::mesh_material::MeshMaterial3d<bevy_pbr::pbr_material::StandardMaterial>";

    /// <summary>
    /// The mesh an entity is drawn with, or <see cref="AssetHandle.None"/> when it has none.
    /// </summary>
    /// <remarks>
    /// Read off Bevy's <c>Mesh3d</c> through its reflection, so it answers for a mesh made in memory
    /// as well as one loaded from a file, and the handle is the one the program already holds when it
    /// holds one. <see cref="RecipeOf"/> says how a primitive was made, and <see cref="TryGetMeshInfo"/>
    /// what any mesh holds.
    /// </remarks>
    public static AssetHandle MeshOf(EcsWorld world, Entity entity) => Held(world, entity, MeshComponent);

    /// <summary>
    /// The standard material an entity is drawn with, or <see cref="AssetHandle.None"/> when it has
    /// none, as <see cref="MeshOf"/> reads the mesh.
    /// </summary>
    public static AssetHandle MaterialOf(EcsWorld world, Entity entity) => Held(world, entity, MaterialComponent);

    /// <summary>The handle a component of Bevy's holds in its one field, or none.</summary>
    private static AssetHandle Held(EcsWorld world, Entity entity, string component)
    {
        ArgumentNullException.ThrowIfNull(world);

        try
        {
            return world.GetReflectedAsset(entity, component, ".0") ?? AssetHandle.None;
        }
        catch (BevyNativeException)
        {
            // A build with no renderer has no such component, which is an entity with no mesh.
            return AssetHandle.None;
        }
    }

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
}
