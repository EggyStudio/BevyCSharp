using System.Globalization;

namespace Bevy;

/// <summary>
/// Marks an entity a save game keeps track of, by an id that stays the same between runs.
/// </summary>
/// <remarks>
/// Set in the editor, where adding it gives a random one, or in code with <see cref="New"/> when
/// something is spawned during play that a save should keep. An entity without one is the scene's
/// alone, and loading a save leaves it as the scene has it.
/// </remarks>
public struct SaveId : IEquatable<SaveId>
{
    /// <summary>The id, never zero for one that was given.</summary>
    public ulong Value;

    /// <summary>A random id, which two entities are as good as certain never to share.</summary>
    public static SaveId New()
    {
        Span<byte> bytes = stackalloc byte[8];
        ulong value;
        do
        {
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            value = BitConverter.ToUInt64(bytes);
        }
        while (value == 0);

        return new SaveId { Value = value };
    }

    /// <summary>An id written as sixteen hex digits, or nothing when the text is not one.</summary>
    public static SaveId? Parse(string text) =>
        ulong.TryParse(text.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) && value != 0
            ? new SaveId { Value = value }
            : null;

    /// <summary>The id as sixteen hex digits.</summary>
    public override readonly string ToString() => Value.ToString("x16", CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public readonly bool Equals(SaveId other) => Value == other.Value;

    /// <inheritdoc/>
    public override readonly bool Equals(object? obj) => obj is SaveId other && Equals(other);

    /// <inheritdoc/>
    public override readonly int GetHashCode() => Value.GetHashCode();

    /// <summary>Whether two ids are the same.</summary>
    public static bool operator ==(SaveId left, SaveId right) => left.Equals(right);

    /// <summary>Whether two ids differ.</summary>
    public static bool operator !=(SaveId left, SaveId right) => !left.Equals(right);
}
