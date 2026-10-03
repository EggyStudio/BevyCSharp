using System.Buffers.Binary;
using System.Text.Json;

namespace Bevy;

/// <summary>One mesh or material inside a glTF file, as Bevy labels it.</summary>
/// <param name="Label">
/// What follows the <c>#</c> in the asset path that loads it, such as <c>Mesh0/Primitive1</c> or
/// <c>Material2/std</c>.
/// </param>
/// <param name="Name">What the file calls it, or a name made from its place when it calls it nothing.</param>
/// <param name="Kind"><see cref="AssetKind.Mesh"/> or <see cref="AssetKind.StandardMaterial"/>.</param>
public sealed record GltfPart(string Label, string Name, string Kind);

/// <summary>
/// What a glTF file holds, read from its JSON without loading any of it.
/// </summary>
/// <remarks>
/// <para>
/// A model file is many assets, and a path alone names none of them. Bevy names each by a label
/// after the path's <c>#</c>: every primitive of every mesh (<c>Mesh0/Primitive0</c>), since the
/// renderer draws primitives, and every material translated into one the renderer draws with
/// (<c>Material0/std</c>). This lists those labels with the names the file gives, so a tool can
/// offer a part of a model rather than its first mesh.
/// </para>
/// <para>
/// Read from the file's JSON, the whole of a <c>.gltf</c> and the first chunk of a <c>.glb</c>,
/// so nothing is decoded, uploaded or kept, and asking costs one read of the file's header.
/// </para>
/// </remarks>
public static class GltfContents
{
    /// <summary>The meshes and materials a glTF file holds, or nothing when it cannot be read as one.</summary>
    /// <param name="path">The file, relative to the asset root.</param>
    public static IReadOnlyList<GltfPart>? Read(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var full = Path.Combine(AssetIds.Root, path.Split('#')[0]);
        if (!File.Exists(full)) return null;

        try
        {
            var bytes = File.ReadAllBytes(full);
            using var document = JsonDocument.Parse(Json(bytes));
            return Parts(document.RootElement);
        }
        catch (Exception error) when (error is IOException or JsonException or InvalidDataException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The JSON of a glTF file: all of a text one, the first chunk of a binary one.</summary>
    private static ReadOnlyMemory<byte> Json(byte[] bytes)
    {
        // A binary file starts with its magic, a version and a length, then chunks, the first of
        // which is the JSON.
        if (bytes.Length >= 20 && bytes[0] == (byte)'g' && bytes[1] == (byte)'l' && bytes[2] == (byte)'T' && bytes[3] == (byte)'F')
        {
            var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12));
            if (20 + length > bytes.Length) throw new InvalidDataException("The JSON chunk runs past the file.");
            return bytes.AsMemory(20, length);
        }

        return bytes;
    }

    private static List<GltfPart> Parts(JsonElement root)
    {
        var parts = new List<GltfPart>();

        if (root.TryGetProperty("meshes", out var meshes) && meshes.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var mesh in meshes.EnumerateArray())
            {
                var named = Name(mesh, $"Mesh {index}");
                var primitives = mesh.TryGetProperty("primitives", out var list) && list.ValueKind == JsonValueKind.Array
                    ? list.GetArrayLength()
                    : 0;

                // Named by the mesh alone where it has one primitive, which is most meshes, and by
                // its place among them where it has several.
                for (var primitive = 0; primitive < primitives; primitive++)
                {
                    var name = primitives == 1 ? named : $"{named} {primitive + 1}";
                    parts.Add(new GltfPart($"Mesh{index}/Primitive{primitive}", name, AssetKind.Mesh));
                }

                index++;
            }
        }

        if (root.TryGetProperty("materials", out var materials) && materials.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var material in materials.EnumerateArray())
            {
                parts.Add(new GltfPart($"Material{index}/std", Name(material, $"Material {index}"), AssetKind.StandardMaterial));
                index++;
            }
        }

        return parts;
    }

    private static string Name(JsonElement element, string otherwise) =>
        element.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } given ? given : otherwise;
}
