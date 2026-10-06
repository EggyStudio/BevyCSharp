namespace Bevy;

/// <summary>
/// The asset types this build can load.
/// </summary>
/// <remarks>
/// An asset type is named rather than passed as a generic parameter, because these are Rust types
/// that C# cannot name. Which ones are accepted depends on how the native bridge was compiled. The
/// first two are data and work in any build, and the rest need a render build.
/// </remarks>
public static class AssetKind
{
    /// <summary>Geometry. Available in every build.</summary>
    public const string Mesh = "Mesh";

    /// <summary>Texture data. Available in every build.</summary>
    public const string Image = "Image";

    /// <summary>A physically based material. Render builds only.</summary>
    public const string StandardMaterial = "StandardMaterial";

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
    /// Text is set in the font Bevy compiles in unless one of these is named, so a game only loads
    /// a font when it has its own. See <see cref="UiTextSettings.Font"/>.
    /// </remarks>
    public const string Font = "Font";

    /// <summary>
    /// A baked meshlet mesh, a <c>.meshlet_mesh</c> file written by
    /// <see cref="Render.CreateMeshletMesh"/>. Needs meshlets running (see
    /// <see cref="Render.MeshletsActive"/>).
    /// </summary>
    public const string MeshletMesh = "MeshletMesh";

    /// <summary>
    /// A saved world, the entities and components of a `.scn` or `.scn.ron` file.
    /// </summary>
    /// <remarks>
    /// The same asset a glTF file's scenes are, which is why one call spawns either. Available in
    /// every build, since a world is data.
    /// </remarks>
    public const string Scene = "Scene";
}
