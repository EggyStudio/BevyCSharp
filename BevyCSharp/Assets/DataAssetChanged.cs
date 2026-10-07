namespace Bevy;

/// <summary>Posted on the bus when a data asset's file was written or read again.</summary>
/// <param name="Id">The asset file's id.</param>
public readonly record struct DataAssetChanged(ulong Id);
