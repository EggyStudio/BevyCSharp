namespace Bevy;

/// <summary>
/// A set of the vegetation's texture collection, its three files under the asset root, found by
/// name in <see cref="TreeTextures"/>.
/// </summary>
/// <param name="Name">Its name, which a species gives as its <see cref="TreeSpecies.Bark"/> or <see cref="TreeSpecies.Leaves"/>.</param>
/// <param name="Kind">Whether it is a bark or an atlas of leaves.</param>
/// <param name="Albedo">The color in RGB and the opacity in A, BC7 in sRGB.</param>
/// <param name="Normal">The tangent-space normal's X and Y, BC5, which Bevy's material reads as a two-channel normal map.</param>
/// <param name="Surface">Occlusion in R, roughness in G, metallic in B and translucency in A, BC7.</param>
/// <remarks>
/// <para>
/// Each file is a KTX2 with its mipmaps, compressed in the formats a desktop GPU samples as they
/// are, a byte a texel, and each level supercompressed with zstd on disk. The surface's channels
/// are the ones Bevy's material reads its occlusion, its metallic and roughness and its diffuse
/// transmission from, so the one image is given as all three of
/// <see cref="MaterialSettings.OcclusionTexture"/>, <see cref="MaterialSettings.MetallicRoughnessTexture"/>
/// and <see cref="MaterialSettings.DiffuseTransmissionTexture"/>.
/// </para>
/// <para>
/// The paths are read as any asset is, so <see cref="AssetServer.LoadImage"/> loads them, and
/// <see cref="Load"/> loads the three with the samplers they are meant for.
/// </para>
/// </remarks>
public sealed record TreeTextureSet(string Name, TreeTextureKind Kind, string Albedo, string Normal, string Surface)
{
    /// <summary>
    /// Starts loading the set's three images, tiling and filtered between texels and between
    /// levels with eight anisotropic samples, the albedo as color and the other two as data. Only
    /// valid inside a system.
    /// </summary>
    /// <returns>The three images' handles, usable at once, each drawing once it has loaded.</returns>
    /// <remarks>The collection is read under the asset root in every app that loads from it, which this sees to.</remarks>
    public (AssetHandle Albedo, AssetHandle Normal, AssetHandle Surface) Load()
    {
        TreeTextures.Carry();
        TextureSettings Sampled(bool srgb) => new()
        {
            Wrap = TextureWrap.Repeat,
            MagFilter = TextureFilter.Linear,
            MinFilter = TextureFilter.Linear,
            MipmapFilter = TextureFilter.Linear,
            Anisotropy = 8,
            Srgb = srgb,
        };

        return (AssetServer.LoadImage(Albedo, Sampled(true)), AssetServer.LoadImage(Normal, Sampled(false)), AssetServer.LoadImage(Surface, Sampled(false)));
    }
}
