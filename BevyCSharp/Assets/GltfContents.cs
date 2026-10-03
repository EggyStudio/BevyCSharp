using System.Buffers.Binary;
using System.Text.Json;

namespace Bevy;

/// <summary>One mesh, material or texture inside a glTF file, as Bevy labels it.</summary>
/// <param name="Label">
/// What follows the <c>#</c> in the asset path that loads it, such as <c>Mesh0/Primitive1</c>,
/// <c>Material2/std</c> or <c>Texture0</c>.
/// </param>
/// <param name="Name">What the file calls it, or a name made from its place when it calls it nothing.</param>
/// <param name="Kind">
/// <see cref="AssetKind.Mesh"/>, <see cref="AssetKind.StandardMaterial"/> or <see cref="AssetKind.Image"/>.
/// </param>
/// <param name="File">
/// For a texture whose picture is a file beside the model rather than bytes inside it, that file,
/// relative to the asset root. Nothing for every other part.
/// </param>
/// <remarks>
/// A texture is a part in two ways. Bytes inside the file, in a buffer or a data URI, are an image
/// Bevy labels <c>Texture{n}</c>. A URI naming a file is loaded from that file as any image is,
/// and Bevy gives it no label in the model, so the path that loads it is the file's own
/// (<see cref="File"/>), and <see cref="PathIn"/> answers it.
/// </remarks>
public sealed record GltfPart(string Label, string Name, string Kind, string? File = null)
{
    /// <summary>The asset path that loads this part of <paramref name="model"/>.</summary>
    /// <param name="model">The model file, relative to the asset root, as given to <see cref="GltfContents.Read"/>.</param>
    public string PathIn(string model) => File ?? model.Split('#')[0] + "#" + Label;
}

/// <summary>
/// What a glTF file holds, read from its JSON without loading any of it.
/// </summary>
/// <remarks>
/// <para>
/// A model file is many assets, and a path alone names none of them. Bevy names each by a label
/// after the path's <c>#</c>: every primitive of every mesh (<c>Mesh0/Primitive0</c>), since the
/// renderer draws primitives, every material translated into one the renderer draws with
/// (<c>Material0/std</c>), and every texture held inside the file (<c>Texture0</c>). This lists
/// those labels with the names the file gives, so a tool can offer a part of a model rather than
/// its first mesh.
/// </para>
/// <para>
/// Read from the file's JSON, the whole of a <c>.gltf</c> and the first chunk of a <c>.glb</c>,
/// so nothing is decoded, uploaded or kept, and asking costs one read of the file's header.
/// </para>
/// </remarks>
public static class GltfContents
{
    /// <summary>The meshes, materials and textures a glTF file holds, or nothing when it cannot be read as one.</summary>
    /// <param name="path">The file, relative to the asset root.</param>
    public static IReadOnlyList<GltfPart>? Read(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        var full = Path.Combine(AssetIds.Root, path.Split('#')[0]);
        if (!AssetFiles.Exists(full)) return null;

        try
        {
            var bytes = AssetFiles.ReadAllBytes(full);
            using var document = JsonDocument.Parse(Json(bytes));
            return Parts(document.RootElement, path.Split('#')[0]);
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

    private static List<GltfPart> Parts(JsonElement root, string model)
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

        if (root.TryGetProperty("textures", out var textures) && textures.ValueKind == JsonValueKind.Array)
        {
            var images = root.TryGetProperty("images", out var list) && list.ValueKind == JsonValueKind.Array
                ? list.EnumerateArray().ToArray()
                : [];

            var index = 0;
            foreach (var texture in textures.EnumerateArray())
            {
                var image = Source(texture) is { } source && source < images.Length ? images[source] : (JsonElement?)null;
                var uri = image is { } found && found.TryGetProperty("uri", out var given) ? given.GetString() : null;
                var inside = uri is null || uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
                var file = inside ? null : Beside(model, uri!);

                // A URI that climbs out of the asset root names nothing the engine can load, so it
                // is left out rather than offered and failed.
                if (!inside && file is null)
                {
                    index++;
                    continue;
                }

                var otherwise = file is not null ? System.IO.Path.GetFileNameWithoutExtension(file) : $"Texture {index}";
                var name = Name(texture, image is { } named ? Name(named, otherwise) : otherwise);

                parts.Add(new GltfPart($"Texture{index}", name, AssetKind.Image, file));
                index++;
            }
        }

        return parts;
    }

    /// <summary>
    /// Which image a texture shows, from its own <c>source</c> or, for a texture only an extension
    /// can decode, the one that extension names.
    /// </summary>
    private static int? Source(JsonElement texture)
    {
        if (texture.TryGetProperty("source", out var source) && source.TryGetInt32(out var index)) return index;
        if (!texture.TryGetProperty("extensions", out var extensions) || extensions.ValueKind != JsonValueKind.Object) return null;

        foreach (var extension in extensions.EnumerateObject())
        {
            if (extension.Value.ValueKind == JsonValueKind.Object
                && extension.Value.TryGetProperty("source", out var named)
                && named.TryGetInt32(out var found))
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// A URI the model names, as a path from the asset root, or nothing when it leaves the root.
    /// </summary>
    /// <remarks>
    /// Relative to the model's folder and percent-encoded, as glTF writes it, the way Bevy resolves
    /// it when it loads the model, so the path given here is the one the model's material uses.
    /// </remarks>
    private static string? Beside(string model, string uri)
    {
        var folder = model.Contains('/', StringComparison.Ordinal) ? model[..model.LastIndexOf('/')] : "";
        var steps = new List<string>(folder.Split('/', StringSplitOptions.RemoveEmptyEntries));

        foreach (var step in Uri.UnescapeDataString(uri).Replace('\\', '/').Split('/'))
        {
            if (step is "" or ".") continue;

            if (step == "..")
            {
                if (steps.Count == 0) return null;
                steps.RemoveAt(steps.Count - 1);
                continue;
            }

            steps.Add(step);
        }

        return steps.Count == 0 ? null : string.Join('/', steps);
    }

    private static string Name(JsonElement element, string otherwise) =>
        element.TryGetProperty("name", out var name) && name.GetString() is { Length: > 0 } given ? given : otherwise;
}
