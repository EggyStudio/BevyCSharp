using System.Buffers;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// A mesh made in memory as JSON, the form a scene's resources and a mesh file share: a primitive
/// as its shape and measures, a mesh built vertex by vertex as its geometry.
/// </summary>
internal static class MeshJson
{
    /// <summary>Writes a primitive's shape and measures as an object.</summary>
    internal static void WriteRecipe(Utf8JsonWriter json, MeshRecipe recipe)
    {
        json.WriteStartObject();
        json.WriteString("shape", recipe.Shape);
        json.WriteNumber("a", recipe.A);
        json.WriteNumber("b", recipe.B);
        json.WriteNumber("c", recipe.C);
        json.WriteEndObject();
    }

    /// <summary>Reads a primitive written by <see cref="WriteRecipe"/>, or nothing when it names no shape.</summary>
    internal static MeshRecipe? ReadRecipe(JsonElement json) =>
        json.TryGetProperty("shape", out var shape) && shape.GetString() is { Length: > 0 } named
            ? new MeshRecipe(named, Number(json, "a"), Number(json, "b"), Number(json, "c"))
            : null;

    private static float Number(JsonElement json, string name) =>
        json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetSingle() : 1f;

    /// <summary>
    /// Writes geometry as flat arrays of numbers, three a position or a normal, two a UV and
    /// four a color, which is how the mesh holds them and the shortest way to write them.
    /// </summary>
    internal static void WriteGeometry(Utf8JsonWriter json, MeshData geometry)
    {
        json.WriteStartObject();
        json.WriteString("topology", geometry.Topology.ToString());

        json.WriteStartArray("positions");
        foreach (var at in geometry.Positions)
        {
            json.WriteNumberValue(at.X);
            json.WriteNumberValue(at.Y);
            json.WriteNumberValue(at.Z);
        }

        json.WriteEndArray();

        if (geometry.Normals is { } normals)
        {
            json.WriteStartArray("normals");
            foreach (var normal in normals)
            {
                json.WriteNumberValue(normal.X);
                json.WriteNumberValue(normal.Y);
                json.WriteNumberValue(normal.Z);
            }

            json.WriteEndArray();
        }

        Numbers(json, "uvs", geometry.Uvs);
        Numbers(json, "colors", geometry.Colors);

        if (geometry.Indices is { } indices)
        {
            json.WriteStartArray("indices");
            foreach (var index in indices) json.WriteNumberValue(index);
            json.WriteEndArray();
        }

        json.WriteEndObject();

        static void Numbers(Utf8JsonWriter json, string name, float[]? values)
        {
            if (values is null) return;

            json.WriteStartArray(name);
            foreach (var value in values) json.WriteNumberValue(value);
            json.WriteEndArray();
        }
    }

    /// <summary>Reads geometry written by <see cref="WriteGeometry"/>, or nothing when it has no positions.</summary>
    internal static MeshData? ReadGeometry(JsonElement json)
    {
        if (!json.TryGetProperty("positions", out var positions) || positions.ValueKind != JsonValueKind.Array)
            return null;

        var data = new MeshData
        {
            Positions = Vectors(positions),
            Normals = json.TryGetProperty("normals", out var normals) ? Vectors(normals) : null,
            Uvs = json.TryGetProperty("uvs", out var uvs) ? [.. uvs.EnumerateArray().Select(value => value.GetSingle())] : null,
            Colors = json.TryGetProperty("colors", out var colors) ? [.. colors.EnumerateArray().Select(value => value.GetSingle())] : null,
            Indices = json.TryGetProperty("indices", out var indices) ? [.. indices.EnumerateArray().Select(value => value.GetUInt32())] : null,
        };

        if (json.TryGetProperty("topology", out var topology) && Enum.TryParse<MeshTopology>(topology.GetString(), out var shape))
            data.Topology = shape;

        return data.Positions.Length > 0 ? data : null;

        static Vec3[] Vectors(JsonElement flat)
        {
            var numbers = flat.EnumerateArray().Select(value => value.GetSingle()).ToArray();
            var vectors = new Vec3[numbers.Length / 3];
            for (var i = 0; i < vectors.Length; i++)
                vectors[i] = new Vec3(numbers[i * 3], numbers[(i * 3) + 1], numbers[(i * 3) + 2]);
            return vectors;
        }
    }
}
