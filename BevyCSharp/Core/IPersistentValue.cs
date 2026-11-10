using System.Text.Json;

namespace Bevy;

/// <summary>A persistent value as a save game carries it, whatever its type.</summary>
public interface IPersistentValue
{
    /// <summary>What it is called, which a save keys it by.</summary>
    string Name { get; }

    /// <summary>Writes the value as JSON.</summary>
    void Write(Utf8JsonWriter json);

    /// <summary>Replaces the value with the one JSON holds, reporting whether it could be read.</summary>
    /// <remarks>The value is changed and not written to its own file, as <c>Set</c> leaves it.</remarks>
    bool Read(JsonElement json);
}
