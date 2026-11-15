using System.Globalization;

namespace Bevy;

/// <summary>
/// Marks an entity a save game keeps track of, by an id that stays the same between runs.
/// </summary>
/// <remarks>
/// Set in the editor, where adding it gives a random one, or in code with <see cref="New"/> when
/// something is spawned during play that a save should keep. An entity without one is the scene's
/// alone, and loading a save leaves it as the scene has it. An entity of a scene placed inside
/// another as a subscene is kept by its id mixed with its placing's, so each placing of one scene
/// is saved apart, two chests of one prefab as two.
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

    /// <summary>
    /// The id an entity of a scene placed inside another is kept by, its own in the placed scene's
    /// file mixed with the placing's id in the scene placing it.
    /// </summary>
    /// <remarks>
    /// A scene placed twice reads its file twice, which would give the entities of both placings
    /// the same ids, and a save would then keep one chest of two for both. Mixed with the placing,
    /// which a scene file keeps from run to run as it keeps every entity's, each placing's ids are
    /// its own and the same in every run. The two are multiplied by large odd numbers, each a step
    /// that sends no two ids to one, and folded on themselves, so a change in either reaches every
    /// bit, and zero, which no id is, comes out as one.
    /// </remarks>
    /// <param name="placing">The placing's id in the scene that places it.</param>
    internal readonly SaveId PlacedAt(int placing)
    {
        var mixed = Value ^ ((ulong)(uint)placing * 0x9E3779B97F4A7C15ul);
        mixed = (mixed ^ (mixed >> 30)) * 0xBF58476D1CE4E5B9ul;
        mixed = (mixed ^ (mixed >> 27)) * 0x94D049BB133111EBul;
        mixed ^= mixed >> 31;
        return new SaveId { Value = mixed == 0 ? 1 : mixed };
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
