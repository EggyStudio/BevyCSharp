namespace Bevy;

/// <summary>
/// The members the asset server gives <see cref="AssetHandle"/>, what a handle says of its asset's
/// load, which a game reads as the handle's own.
/// </summary>
/// <remarks>
/// They extend <see cref="AssetHandle"/> rather than being a part of it, since the asset server
/// answers them and the handle stands in Core, which every area names.
/// </remarks>
public static class AssetHandleLoading
{
    extension(AssetHandle handle)
    {
        /// <summary>How far along this asset's load is.</summary>
        public AssetLoadState State => AssetServer.StateOf(handle);

        /// <summary>True when the asset is ready to use.</summary>
        public bool IsLoaded => handle.State == AssetLoadState.Loaded;
    }
}
