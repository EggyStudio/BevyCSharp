using Bevy.Interop;

namespace Bevy;

/// <summary>How far along an asset load is.</summary>
public enum AssetLoadState
{
    /// <summary>The handle is not one this app is holding.</summary>
    Unknown = 0,

    /// <summary>Queued, but not started.</summary>
    NotLoaded = 1,

    /// <summary>In progress.</summary>
    Loading = 2,

    /// <summary>Available.</summary>
    Loaded = 3,

    /// <summary>The load was abandoned. The reason is on Bevy's log.</summary>
    Failed = 4,
}

/// <summary>
/// A reference to a loaded asset.
/// </summary>
/// <remarks>
/// <para>
/// Bevy's own handle is generic and reference counted, and neither property survives a trip
/// through a C ABI. What C# holds instead is a key into a table on the engine side that owns the
/// real handle. Holding one keeps the asset loaded; <see cref="AssetServer.Release"/> lets it go.
/// </para>
/// <para>
/// Table slots are reused, so the key carries a generation as well as an index. A handle that has
/// been released does not start naming whatever took its slot; it reports
/// <see cref="AssetLoadState.Unknown"/> instead.
/// </para>
/// </remarks>
public readonly struct AssetHandle : IEquatable<AssetHandle>
{
    /// <summary>The packed slot and generation the engine knows this asset by.</summary>
    internal readonly int Key;

    internal AssetHandle(int key) => Key = key;

    /// <summary>A handle that refers to nothing.</summary>
    public static AssetHandle None => new(-1);

    /// <summary>True when this handle was produced by a successful load.</summary>
    public bool IsValid => Key >= 0;

    /// <summary>How far along this asset's load is.</summary>
    public AssetLoadState State => AssetServer.StateOf(this);

    /// <summary>True when the asset is ready to use.</summary>
    public bool IsLoaded => State == AssetLoadState.Loaded;

    /// <inheritdoc/>
    public bool Equals(AssetHandle other) => Key == other.Key;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is AssetHandle other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Key;

    /// <summary>Compares two handles.</summary>
    public static bool operator ==(AssetHandle a, AssetHandle b) => a.Key == b.Key;

    /// <summary>Compares two handles.</summary>
    public static bool operator !=(AssetHandle a, AssetHandle b) => a.Key != b.Key;

    /// <inheritdoc/>
    public override string ToString() => IsValid ? $"Asset({Key})" : "Asset(none)";
}

/// <summary>
/// The asset types this build can load.
/// </summary>
/// <remarks>
/// An asset type is named rather than passed as a generic parameter, because these are Rust types
/// that C# cannot name. Which ones are accepted depends on how the native bridge was compiled:
/// the first two are data and work in any build, the rest need a render build.
/// </remarks>
public static class AssetKind
{
    /// <summary>Geometry. Available in every build.</summary>
    public const string Mesh = "Mesh";

    /// <summary>Texture data. Available in every build.</summary>
    public const string Image = "Image";

    /// <summary>A physically based material. Render builds only.</summary>
    public const string StandardMaterial = "StandardMaterial";

    /// <summary>A shader. Render builds only.</summary>
    public const string Shader = "Shader";

    /// <summary>
    /// A whole glTF file: its meshes, materials and nodes. Render builds only.
    /// </summary>
    /// <remarks>
    /// The description of a model rather than anything drawable. To draw part of one, load that
    /// part directly with <see cref="AssetServer.LoadGltfMesh"/>.
    /// </remarks>
    public const string Gltf = "Gltf";

    /// <summary>
    /// A sound: Ogg Vorbis, WAV, FLAC or MP3. Render builds only.
    /// </summary>
    /// <remarks>
    /// Play it with <see cref="Audio.Play(AssetHandle, AudioSettings)"/>. Sound belongs to the
    /// render profile because that is the one that takes a system library, not because it draws.
    /// </remarks>
    public const string Audio = "Audio";

    /// <summary>
    /// A font, as TrueType or OpenType. Render builds only.
    /// </summary>
    /// <remarks>
    /// Text is set in the font Bevy compiles in unless one of these is named, so a game only
    /// loads a font when it wants its own. See <see cref="UiTextSettings.Font"/>.
    /// </remarks>
    public const string Font = "Font";

    /// <summary>
    /// A saved world: the entities and components of a `.scn` or `.scn.ron` file.
    /// </summary>
    /// <remarks>
    /// The same asset a glTF file's scenes are, which is why one call spawns either. Available in
    /// every build, since a world is data.
    /// </remarks>
    public const string Scene = "Scene";
}

/// <summary>What happens outside a texture's zero-to-one range.</summary>
public enum TextureWrap
{
    /// <summary>Hold the edge pixel. Bevy's default, and what a decal or a skybox wants.</summary>
    Clamp = 0,

    /// <summary>Start over, so the texture tiles.</summary>
    Repeat = 1,

    /// <summary>Tile, flipping every other repeat so the seams line up.</summary>
    Mirror = 2,
}

/// <summary>How a texture is filtered when it does not land on pixel boundaries.</summary>
public enum TextureFilter
{
    /// <summary>Take the nearest pixel. Bevy's default, and what pixel art wants.</summary>
    Nearest = 0,

    /// <summary>Blend between pixels, which is what everything else wants.</summary>
    Linear = 1,
}

/// <summary>
/// How an image should be sampled, and how its bytes should be read.
/// </summary>
/// <remarks>
/// The defaults match Bevy's: clamped and nearest-filtered, read as sRGB. Both are worth changing
/// for most textures, which is why this exists.
/// </remarks>
public sealed class TextureSettings
{
    /// <summary>What happens past the edge, in both directions.</summary>
    public TextureWrap Wrap { get; set; } = TextureWrap.Clamp;

    /// <summary>Filtering when the texture is drawn larger than it is.</summary>
    public TextureFilter MagFilter { get; set; } = TextureFilter.Nearest;

    /// <summary>Filtering when it is drawn smaller.</summary>
    public TextureFilter MinFilter { get; set; } = TextureFilter.Nearest;

    /// <summary>Filtering between mip levels.</summary>
    public TextureFilter MipmapFilter { get; set; } = TextureFilter.Nearest;

    /// <summary>
    /// Maximum anisotropic samples, which keeps a surface seen edge-on from smearing.
    /// </summary>
    /// <remarks>
    /// Ignored unless all three filters are <see cref="TextureFilter.Linear"/>, because asking
    /// for both is a validation failure in the graphics API rather than something it overlooks.
    /// </remarks>
    public uint Anisotropy { get; set; } = 1;

    /// <summary>
    /// Whether the file holds colour, in which case it is read as sRGB.
    /// </summary>
    /// <remarks>
    /// Set false for a texture whose bytes are data rather than colour: a normal map, a
    /// metallic-roughness map, an occlusion map. Reading one as sRGB bends every value in it.
    /// </remarks>
    public bool Srgb { get; set; } = true;

    /// <summary>Repeat filtering, for a texture meant to tile across a large surface.</summary>
    public static TextureSettings Tiling => new()
    {
        Wrap = TextureWrap.Repeat,
        MagFilter = TextureFilter.Linear,
        MinFilter = TextureFilter.Linear,
        MipmapFilter = TextureFilter.Linear,
    };

    /// <summary>Linear filtering with no colour conversion, for a normal or roughness map.</summary>
    public static TextureSettings Data => new()
    {
        MagFilter = TextureFilter.Linear,
        MinFilter = TextureFilter.Linear,
        MipmapFilter = TextureFilter.Linear,
        Srgb = false,
    };
}

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
    /// <see cref="Render.CreateMesh"/> built, so <see cref="Render.SetMesh"/> takes it as it is.
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
    /// data rather than colour.
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
    /// material rather than being one the renderer draws with. Bevy's PBR plugin translates it
    /// and publishes the result under a second label, which is what this asks for, and that
    /// plugin comes with the window. A windowless run has no such asset and the load fails.
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
    /// A scene is the file's own arrangement of its meshes: what an artist laid out, with the
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
