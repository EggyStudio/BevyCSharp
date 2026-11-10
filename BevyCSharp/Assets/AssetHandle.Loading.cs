namespace Bevy;

// What a handle says of its asset's load, which the asset server answers, kept with the asset
// server while the handle itself stands in Core, which every area names.
public readonly partial struct AssetHandle
{
    /// <summary>How far along this asset's load is.</summary>
    public AssetLoadState State => AssetServer.StateOf(this);

    /// <summary>True when the asset is ready to use.</summary>
    public bool IsLoaded => State == AssetLoadState.Loaded;
}
