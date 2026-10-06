using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Loads assets and tracks what has been loaded.
/// </summary>
/// <remarks>
/// Paths are resolved by Bevy relative to the <c>assets</c> directory beside the executable.
/// Loading is asynchronous: <see cref="Load"/> returns as soon as the request is queued, and the
/// handle reports <see cref="AssetLoadState.Loading"/> until the file has been read and parsed.
/// </remarks>
public static unsafe class AssetServer
{
    /// <summary>
    /// Starts loading one drawable piece of geometry out of a glTF file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A glTF file is a scene graph, and nothing in it maps one-to-one onto "a thing to draw".
    /// Its meshes are named groups, and it is the primitive inside one that carries geometry and
    /// a material, which is why both indices are here. A file exported as a single object is
    /// mesh 0, primitive 0, and that is the default.
    /// </para>
    /// <para>
    /// What comes back is an ordinary mesh handle, indistinguishable from one
    /// <see cref="Render.CreateMesh(MeshData)"/> built, so <see cref="Render.SetMesh"/> takes it as it is.
    /// </para>
    /// <para>
    /// The file's own material comes from <see cref="LoadGltfMaterial"/>, which needs a window.
    /// Give the entity one from <see cref="Render.CreateMaterial(MaterialSettings)"/> otherwise.
    /// </para>
    /// </remarks>
    /// <param name="path">Path to the glTF file, relative to the assets directory.</param>
    /// <param name="mesh">Which of the file's meshes, in the order the file declares them.</param>
    /// <param name="primitive">Which primitive within that mesh.</param>
    /// <example>
    /// <code>
    /// Render.SetMesh(ctx.Ecs, entity, AssetServer.LoadGltfMesh("models/ship.gltf"));
    /// Render.SetMaterial(ctx.Ecs, entity, Render.CreateMaterial(0.6f, 0.6f, 0.62f));
    /// </code>
    /// </example>
    public static AssetHandle LoadGltfMesh(string path, int mesh = 0, int primitive = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentOutOfRangeException.ThrowIfNegative(mesh);
        ArgumentOutOfRangeException.ThrowIfNegative(primitive);

        return Load(AssetKind.Mesh, $"{path}#Mesh{mesh}/Primitive{primitive}");
    }

    /// <summary>
    /// Starts loading an image with the sampler it should be drawn with.
    /// </summary>
    /// <remarks>
    /// <see cref="Load"/> takes Bevy's default sampler, which clamps at the edges and filters to
    /// the nearest pixel. A texture meant to tile has to say so, and so does one whose bytes are
    /// data rather than color.
    /// </remarks>
    /// <example>
    /// <code>
    /// var floor = AssetServer.LoadImage("textures/tiles.png", TextureSettings.Tiling);
    /// var bumps = AssetServer.LoadImage("textures/tiles-normal.png", TextureSettings.Data);
    /// </code>
    /// </example>
    public static AssetHandle LoadImage(string path, TextureSettings settings)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentNullException.ThrowIfNull(settings);

        var native = new NativeImageConfig
        {
            AddressU = (int)settings.Wrap,
            AddressV = (int)settings.Wrap,
            MagFilter = (int)settings.MagFilter,
            MinFilter = (int)settings.MinFilter,
            MipmapFilter = (int)settings.MipmapFilter,
            Anisotropy = settings.Anisotropy,
            Srgb = settings.Srgb ? 1 : 0,
            Layers = settings.Layers,
        };

        var key = Native.bcs_asset_load_image(path, &native);
        Native.Check(key, $"loading image '{path}'");
        return new AssetHandle(key);
    }

    /// <summary>
    /// Starts loading one material out of a glTF file, as the renderer's own material type.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Needs a window.</b> A glTF material loads as a <c>GltfMaterial</c>, which describes a
    /// material rather than being one the renderer draws with. Bevy's PBR plugin translates it and
    /// publishes the result under a second label, which this asks for, and that plugin comes with
    /// the window. A windowless run has no such asset and the load fails.
    /// </para>
    /// <para>
    /// Materials are numbered per file rather than per mesh, so this index is not the one passed
    /// to <see cref="LoadGltfMesh"/>. A file with one material has only index 0.
    /// </para>
    /// </remarks>
    /// <param name="path">Path to the glTF file, relative to the assets directory.</param>
    /// <param name="material">Which of the file's materials.</param>
    /// <example>
    /// <code>
    /// Render.SetMesh(ctx.Ecs, entity, AssetServer.LoadGltfMesh("models/ship.gltf"));
    /// Render.SetMaterial(ctx.Ecs, entity, AssetServer.LoadGltfMaterial("models/ship.gltf"));
    /// </code>
    /// </example>
    public static AssetHandle LoadGltfMaterial(string path, int material = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentOutOfRangeException.ThrowIfNegative(material);

        // The `/std` half is Bevy's label for the translated material, beside the raw
        // GltfMaterial the loader produces at `Material{n}`.
        return Load(AssetKind.StandardMaterial, $"{path}#Material{material}/std");
    }

    /// <summary>
    /// Starts loading one scene out of a glTF file.
    /// </summary>
    /// <remarks>
    /// A scene is the file's own arrangement of its meshes, as an artist laid them out, with the
    /// nodes and the parenting they gave it. Spawn it with
    /// <see cref="EcsWorld.SpawnScene"/>, which produces those entities under one of yours.
    /// </remarks>
    /// <param name="path">Path to the glTF file, relative to the assets directory.</param>
    /// <param name="scene">Which of the file's scenes. Most files define one.</param>
    public static AssetHandle LoadGltfScene(string path, int scene = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        ArgumentOutOfRangeException.ThrowIfNegative(scene);

        return Load(AssetKind.Scene, $"{path}#Scene{scene}");
    }

    /// <summary>
    /// The path an asset was loaded from, or <see langword="null"/> when it has none.
    /// </summary>
    /// <remarks>
    /// What turns a handle back into something a person recognizes. A field holding an asset shows
    /// the file it points at rather than a number, and something saving a world writes the path
    /// rather than a key that means nothing the next time the program runs. An asset built rather
    /// than loaded has no path, and answers nothing.
    /// </remarks>
    public static unsafe string? PathOf(AssetHandle handle)
    {
        if (!handle.IsValid) return null;

        var length = Native.bcs_asset_path(handle.Key, null, 0);
        if (length < 0) return null;
        if (length == 0) return string.Empty;

        return Native.ReadText(
            (buffer, capacity) => Native.bcs_asset_path(handle.Key, buffer, capacity),
            $"reading the path of {handle}");
    }

    /// <summary>
    /// Starts loading an asset and returns a handle to it.
    /// </summary>
    /// <param name="kind">One of the constants on <see cref="AssetKind"/>.</param>
    /// <param name="path">Path relative to the assets directory.</param>
    /// <exception cref="BevyNativeException">
    /// The kind is not one this build knows, or the app has no asset server.
    /// </exception>
    public static AssetHandle Load(string kind, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        ArgumentException.ThrowIfNullOrEmpty(path);

        var key = Native.bcs_asset_load(kind, path);
        if (key == NativeStatus.NoComponent)
            throw new BevyNativeException(
                NativeStatus.NoComponent,
                $"'{kind}' is not an asset type this native build can load. The material and "
                + "shader kinds need a build with the renderer compiled in.");

        Native.Check(key, $"loading '{path}' as {kind}");
        return new AssetHandle(key);
    }

    /// <summary>How far along an asset's load is.</summary>
    public static AssetLoadState StateOf(AssetHandle handle) =>
        handle.IsValid
            ? (AssetLoadState)Native.bcs_asset_load_state(handle.Key)
            : AssetLoadState.Unknown;

    /// <summary>
    /// How far along an asset's load is, counting everything it depends on.
    /// </summary>
    /// <remarks>
    /// <see cref="StateOf"/> answers for the asset's own file, and a glTF scene is
    /// <see cref="AssetLoadState.Loaded"/> by that as soon as the file is read, while its meshes,
    /// materials and textures are still on their way. This waits for those too, and fails when
    /// any of them fails, so a loading screen asks this to know a scene can be shown whole. An
    /// asset made in memory has nothing to wait on and answers loaded.
    /// </remarks>
    public static AssetLoadState StateWithDependenciesOf(AssetHandle handle) =>
        handle.IsValid
            ? (AssetLoadState)Native.bcs_asset_load_state_with_dependencies(handle.Key)
            : AssetLoadState.Unknown;

    /// <summary>True when the engine is still holding this handle.</summary>
    public static bool IsAlive(AssetHandle handle) =>
        handle.IsValid && Native.bcs_asset_is_valid(handle.Key) > 0;

    /// <summary>
    /// Releases a handle.
    /// </summary>
    /// <remarks>
    /// The asset itself stays loaded while anything else still refers to it, including a
    /// component on an entity. Releasing only gives up this reference.
    /// </remarks>
    /// <returns><see langword="false"/> if the handle was already released.</returns>
    public static bool Release(AssetHandle handle) =>
        handle.IsValid && Native.bcs_asset_release(handle.Key) > 0;

    /// <summary>
    /// How many handles the engine is holding on C#'s behalf.
    /// </summary>
    /// <remarks>Intended for leak checks in tests.</remarks>
    public static int LiveHandleCount => Native.Check(
        Native.bcs_asset_live_count(), "counting live asset handles");
}
