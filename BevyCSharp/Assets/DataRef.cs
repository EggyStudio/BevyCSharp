using System.Globalization;
using System.Runtime.InteropServices;

namespace Bevy;

/// <summary>
/// A reference to a data asset of type <typeparamref name="T"/>, by the id of its file.
/// </summary>
/// <typeparam name="T">The data asset's type.</typeparam>
/// <remarks>
/// An unmanaged eight bytes, so it sits in a component. It holds the file's id rather than its
/// path (<see cref="AssetIds"/>), so renaming or moving the file keeps every reference to it.
/// <c>default</c> refers to nothing.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct DataRef<T> : IDataRef, IEquatable<DataRef<T>>
{
    /// <summary>Refers to the asset with the file id given.</summary>
    public DataRef(ulong id) => Id = id;

    /// <inheritdoc/>
    public ulong Id { get; }

    /// <summary>Whether it refers to anything.</summary>
    public bool IsSet => Id != 0;

    /// <summary>
    /// The asset's value, loaded once and shared, or the type's defaults where its file cannot be
    /// read, which is said once on the log and as <see cref="AssetLoadFailed"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="DataAssets.TryGet{T}(DataRef{T}, out T, out string?)"/> says why, and
    /// <see cref="DataAssets.Get{T}"/> throws, for a game that asks.
    /// </remarks>
    /// <exception cref="ArgumentException">The reference names nothing.</exception>
    public T Value => DataAssets.ValueOf(this);

    /// <inheritdoc/>
    public bool Equals(DataRef<T> other) => Id == other.Id;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is DataRef<T> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() =>
        IsSet ? AssetIds.PathOf(Id) ?? Id.ToString("x16", CultureInfo.InvariantCulture) : "none";
}
