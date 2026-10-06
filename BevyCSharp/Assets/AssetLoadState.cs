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
