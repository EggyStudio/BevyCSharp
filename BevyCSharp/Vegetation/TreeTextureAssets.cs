using Vegetation;

namespace Bevy;

/// <summary>
/// The Vegetation module's texture sets as this engine's assets, every file of
/// <see cref="TreeTextures"/> read under <see cref="Folder"/> in the asset root and a set's images
/// loaded with the samplers they are meant for.
/// </summary>
/// <remarks>
/// <para>
/// The module carries its sets inside its own assembly, and a folder a game adds with
/// <see cref="TreeTextures.Add"/> is read where it is. Either way a file is read under
/// <see cref="Folder"/> by the path its set gives, so Bevy's own loader reads it as any asset and a
/// game ships no folder for them. The collection is read under the asset root in every app that
/// loads from it, which <see cref="Load"/> and <see cref="LoadDepth"/> see to.
/// </para>
/// <para>
/// The surface's channels are the ones Bevy's material reads its occlusion, its metallic and
/// roughness and its diffuse transmission from, so the one image is given as all three of
/// <see cref="MaterialSettings.OcclusionTexture"/>, <see cref="MaterialSettings.MetallicRoughnessTexture"/>
/// and <see cref="MaterialSettings.DiffuseTransmissionTexture"/>.
/// </para>
/// </remarks>
public static class TreeTextureAssets
{
    /// <summary>The folder of the asset root the collection's files are read under, each by its path in the collection.</summary>
    public const string Folder = "bevycsharp/vegetation";

    private static readonly Lock Gate = new();
    private static IReadOnlyList<string>? _carried;
    private static int _carriedFor = -1;

    /// <summary>
    /// Starts loading a set's depth as data, tiling and filtered as <see cref="Load"/>'s images
    /// are, or answers nothing for a set without one. Only valid inside a system.
    /// </summary>
    /// <param name="set">The set, as <see cref="TreeTextures.Find"/> answers it.</param>
    public static AssetHandle LoadDepth(this TreeTextureSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        if (set.Depth is null) return AssetHandle.None;
        Carry();
        return AssetServer.LoadImage($"{Folder}/{set.Depth}", Sampled(srgb: false));
    }

    /// <summary>
    /// Starts loading a set's three images, tiling and filtered between texels and between levels
    /// with sixteen anisotropic samples, the albedo as color and the other two as data. Only valid
    /// inside a system.
    /// </summary>
    /// <param name="set">The set, as <see cref="TreeTextures.Find"/> answers it.</param>
    /// <returns>The three images' handles, usable at once, each drawing once it has loaded.</returns>
    public static (AssetHandle Albedo, AssetHandle Normal, AssetHandle Surface) Load(this TreeTextureSet set)
    {
        ArgumentNullException.ThrowIfNull(set);
        Carry();
        return (AssetServer.LoadImage($"{Folder}/{set.Albedo}", Sampled(true)), AssetServer.LoadImage($"{Folder}/{set.Normal}", Sampled(false)),
            AssetServer.LoadImage($"{Folder}/{set.Surface}", Sampled(false)));
    }

    /// <summary>
    /// Sees that the collection's files are read under the asset root in this app, since each app
    /// takes its files afresh as it starts, and again once a folder is added, since the module lists
    /// its files anew then. Called before a set's files are loaded.
    /// </summary>
    internal static void Carry()
    {
        lock (Gate)
        {
            var files = TreeTextures.Files;
            if (_carriedFor == AssetFiles.Started && ReferenceEquals(_carried, files)) return;
            if (files.Count > 0) AssetFiles.Mount(Folder, files, TreeTextures.Open);
            _carried = files;
            _carriedFor = AssetFiles.Started;
        }
    }

    private static TextureSettings Sampled(bool srgb) => new()
    {
        Wrap = TextureWrap.Repeat,
        MagFilter = TextureFilter.Linear,
        MinFilter = TextureFilter.Linear,
        MipmapFilter = TextureFilter.Linear,
        Srgb = srgb,
    };
}
