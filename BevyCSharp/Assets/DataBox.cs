namespace Bevy;

/// <summary>Holds a data asset's value, so its generated fields can read and write it in place.</summary>
/// <typeparam name="T">The data asset's type.</typeparam>
/// <remarks>A box rather than the value, so a struct asset is written back as a class one is.</remarks>
public sealed class DataBox<T>
{
    /// <summary>The value.</summary>
    public T Value { get; set; } = default!;
}
