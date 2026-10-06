namespace Bevy;

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

    /// <summary>
    /// True when this handle was produced by a successful load.
    /// </summary>
    /// <remarks>
    /// Zero is not a handle. A component holding an asset is a struct that starts out zeroed, and
    /// the engine's table therefore never hands out a key of zero, so a freshly added component
    /// holds nothing rather than whatever was loaded first.
    /// </remarks>
    public bool IsValid => Key > 0;

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
