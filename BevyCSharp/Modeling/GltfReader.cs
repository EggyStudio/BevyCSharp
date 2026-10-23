using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Reads the triangles and materials of a glTF 2.0 file, a <c>.gltf</c> with its buffers beside it
/// or inside it or a <c>.glb</c>, in managed code, for an edit mesh to import.
/// </summary>
/// <remarks>
/// <para>
/// The default scene's nodes are walked down from the root, each node's transform, a matrix or a
/// translation, rotation and scale, put on its children's, and every primitive of every node's
/// mesh read in the space of the scene, so a model of parts placed in Blender comes in as it was
/// placed. Triangles, strips and fans are read, and points and lines are left, since they make no
/// faces.
/// </para>
/// <para>
/// A material keeps its name and what of its look a slot takes, its color, metalness and roughness,
/// emissive color and textures named by a path, each image's path read from the file's folder and
/// made relative to the asset root. An image held inside the file has no path to name and is left,
/// as is a path with a space in it, which a slot's line cannot hold. Accessors of every component
/// type are read, normalized where they say so, and a sparse accessor is refused.
/// </para>
/// </remarks>
internal static class GltfReader
{
    /// <summary>A triangle's corner, its place in the scene, its normal and its texture coordinates where the file has them.</summary>
    internal readonly record struct Corner(Vec3 Position, Vec3? Normal, Vec2? Uv);

    /// <summary>A triangle, its corners counter-clockwise seen from the side it faces, and the index of its material in the file's, or less than zero for none.</summary>
    internal readonly record struct Triangle(Corner A, Corner B, Corner C, int Material);

    /// <summary>A material's name and the look a slot takes from it.</summary>
    internal readonly record struct Material(string Name, SlotLook Look);

    private const uint Magic = 0x46546C67, JsonChunk = 0x4E4F534A, BinaryChunk = 0x004E4942;

    /// <summary>The file's triangles in the scene's space and its materials.</summary>
    /// <param name="path">The file, relative to the asset root unless it is a full path.</param>
    /// <exception cref="FileNotFoundException">There is no such file, or a buffer it names is not there.</exception>
    /// <exception cref="FormatException">It is not glTF this reads, which the message says with the file's path.</exception>
    public static (List<Triangle> Triangles, List<Material> Materials) Read(string path)
    {
        if (!AssetFiles.Exists(path)) throw new FileNotFoundException($"There is no model at {path}.", path);

        var bytes = AssetFiles.ReadAllBytes(path);
        try
        {
            return Parse(path, bytes);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or KeyNotFoundException or InvalidOperationException
                                          or IndexOutOfRangeException or ArgumentException or OverflowException)
        {
            throw new FormatException($"{path} is not a glTF model this reads, as {error.Message.TrimEnd('.')}.", error);
        }
    }

    private static (List<Triangle>, List<Material>) Parse(string path, byte[] bytes)
    {
        var folder = Path.GetDirectoryName(path) ?? "";
        ReadOnlyMemory<byte> json = bytes;
        byte[]? binary = null;

        if (bytes.Length >= 12 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == Magic)
        {
            // A binary file is a header and chunks, the JSON first and the buffer after it.
            json = default;
            for (var at = 12; at + 8 <= bytes.Length;)
            {
                var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at));
                var type = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at + 4));
                if (length < 0 || at + 8 + length > bytes.Length) throw new InvalidDataException("a chunk runs past the end of the file");
                if (type == JsonChunk) json = bytes.AsMemory(at + 8, length);
                else if (type == BinaryChunk) binary = bytes.AsSpan(at + 8, length).ToArray();
                at += 8 + length;
            }

            if (json.IsEmpty) throw new InvalidDataException("the binary file has no JSON chunk");
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("asset", out var asset) || !asset.TryGetProperty("version", out var version) || !(version.GetString() ?? "").StartsWith("2.", StringComparison.Ordinal))
            throw new InvalidDataException("its asset names no glTF version of 2");

        var buffers = Array(root, "buffers").Select(buffer => Buffer(buffer, folder, binary)).ToArray();
        var file = new Source(root, buffers);

        var materials = Array(root, "materials").Select((material, index) => Look(material, index, folder, root)).ToList();
        var triangles = new List<Triangle>();

        // The default scene's nodes, or every node no other names as a child where there is none.
        var nodes = Array(root, "nodes");
        IEnumerable<int> roots;
        var scenes = Array(root, "scenes");
        if (scenes.Length > 0)
        {
            var scene = root.TryGetProperty("scene", out var chosen) ? chosen.GetInt32() : 0;
            roots = Array(scenes[scene], "nodes").Select(node => node.GetInt32());
        }
        else
        {
            var children = nodes.SelectMany(node => Array(node, "children")).Select(child => child.GetInt32()).ToHashSet();
            roots = Enumerable.Range(0, nodes.Length).Where(node => !children.Contains(node));
        }

        foreach (var node in roots) Walk(file, nodes, node, Matrix4x4.Identity, triangles, 0);
        return (triangles, materials);
    }

    /// <summary>Reads a node's mesh in the scene's space, and its children's below it.</summary>
    private static void Walk(Source file, JsonElement[] nodes, int index, Matrix4x4 parent, List<Triangle> triangles, int depth)
    {
        if (depth > 64) throw new InvalidDataException("its nodes are children of one another past 64 deep");

        var node = nodes[index];
        var world = Local(node) * parent;

        if (node.TryGetProperty("mesh", out var mesh))
        {
            var mirrored = world.GetDeterminant() < 0f;
            foreach (var primitive in Array(Array(file.Root, "meshes")[mesh.GetInt32()], "primitives")) Primitive(file, primitive, world, mirrored, triangles);
        }

        foreach (var child in Array(node, "children")) Walk(file, nodes, child.GetInt32(), world, triangles, depth + 1);
    }

    private static void Primitive(Source file, JsonElement primitive, Matrix4x4 world, bool mirrored, List<Triangle> triangles)
    {
        var mode = primitive.TryGetProperty("mode", out var given) ? given.GetInt32() : 4;
        if (mode is not (4 or 5 or 6)) return;

        var attributes = primitive.GetProperty("attributes");
        var positions = file.Floats(attributes.GetProperty("POSITION").GetInt32(), 3);
        var count = positions.Length / 3;
        var normals = attributes.TryGetProperty("NORMAL", out var normal) ? file.Floats(normal.GetInt32(), 3) : null;
        var uvs = attributes.TryGetProperty("TEXCOORD_0", out var uv) ? file.Floats(uv.GetInt32(), 2) : null;
        var order = primitive.TryGetProperty("indices", out var indices)
            ? file.Indices(indices.GetInt32())
            : Enumerable.Range(0, count).ToArray();
        var material = primitive.TryGetProperty("material", out var named) ? named.GetInt32() : -1;

        Corner At(int vertex)
        {
            if ((uint)vertex >= (uint)count) throw new InvalidDataException($"an index names vertex {vertex} of {count}");
            var point = Vector3.Transform(new Vector3(positions[3 * vertex], positions[3 * vertex + 1], positions[3 * vertex + 2]), world);
            Vec3? facing = null;
            if (normals is not null)
            {
                var turned = Vector3.Normalize(Vector3.TransformNormal(new Vector3(normals[3 * vertex], normals[3 * vertex + 1], normals[3 * vertex + 2]), world));
                facing = new Vec3(turned.X, turned.Y, turned.Z);
            }

            return new Corner(new Vec3(point.X, point.Y, point.Z), facing, uvs is null ? null : new Vec2(uvs[2 * vertex], uvs[2 * vertex + 1]));
        }

        void Add(int a, int b, int c) =>
            triangles.Add(mirrored ? new Triangle(At(a), At(c), At(b), material) : new Triangle(At(a), At(b), At(c), material));

        switch (mode)
        {
            case 4:
                for (var i = 0; i + 2 < order.Length; i += 3) Add(order[i], order[i + 1], order[i + 2]);
                break;
            case 5:
                // Every other triangle of a strip runs the other way, which its first two corners swapped mend.
                for (var i = 0; i + 2 < order.Length; i++)
                {
                    if (i % 2 == 0) Add(order[i], order[i + 1], order[i + 2]);
                    else Add(order[i + 1], order[i], order[i + 2]);
                }

                break;
            default:
                for (var i = 1; i + 1 < order.Length; i++) Add(order[0], order[i], order[i + 1]);
                break;
        }
    }

    /// <summary>A node's own transform, its matrix or its translation, rotation and scale.</summary>
    private static Matrix4x4 Local(JsonElement node)
    {
        // glTF lists a matrix by its columns, and the numerics' matrix holds a column as a row.
        if (node.TryGetProperty("matrix", out var matrix))
        {
            var m = matrix.EnumerateArray().Select(value => value.GetSingle()).ToArray();
            if (m.Length != 16) throw new InvalidDataException("a node's matrix is not sixteen numbers");
            return new Matrix4x4(m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);
        }

        var t = Numbers(node, "translation", 0f, 0f, 0f);
        var r = Numbers(node, "rotation", 0f, 0f, 0f, 1f);
        var s = Numbers(node, "scale", 1f, 1f, 1f);
        return Matrix4x4.CreateScale(s[0], s[1], s[2]) * Matrix4x4.CreateFromQuaternion(new Quaternion(r[0], r[1], r[2], r[3])) * Matrix4x4.CreateTranslation(t[0], t[1], t[2]);
    }

    /// <summary>What a slot takes from a material, and its name, one word.</summary>
    private static Material Look(JsonElement material, int index, string folder, JsonElement root)
    {
        var given = material.TryGetProperty("name", out var named) ? named.GetString() : null;
        var name = string.IsNullOrWhiteSpace(given) ? $"material-{index}" : string.Join('-', given.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        string? Image(JsonElement owner, string property)
        {
            if (!owner.TryGetProperty(property, out var info) || !info.TryGetProperty("index", out var texture)) return null;
            var textures = Array(root, "textures");
            if (texture.GetInt32() >= textures.Length || !textures[texture.GetInt32()].TryGetProperty("source", out var source)) return null;
            var images = Array(root, "images");
            if (source.GetInt32() >= images.Length || !images[source.GetInt32()].TryGetProperty("uri", out var uri)) return null;

            var text = uri.GetString() ?? "";
            if (text.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return null;

            var full = Path.GetFullPath(Path.Combine(AssetIds.Root, folder, Uri.UnescapeDataString(text)));
            var relative = Path.GetRelativePath(AssetIds.Root, full);
            var chosen = (relative.StartsWith("..", StringComparison.Ordinal) ? full : relative).Replace('\\', '/');
            return chosen.Any(char.IsWhiteSpace) ? null : chosen;
        }

        var pbr = material.TryGetProperty("pbrMetallicRoughness", out var found) ? found : default;
        var hasPbr = pbr.ValueKind == JsonValueKind.Object;
        var color = hasPbr ? Numbers(pbr, "baseColorFactor", 1f, 1f, 1f, 1f) : [1f, 1f, 1f, 1f];
        var emissive = Numbers(material, "emissiveFactor", 0f, 0f, 0f);
        var strength = material.TryGetProperty("extensions", out var extensions) && extensions.TryGetProperty("KHR_materials_emissive_strength", out var strong)
            && strong.TryGetProperty("emissiveStrength", out var amount) ? amount.GetSingle() : 1f;

        var look = new SlotLook
        {
            Color = color.All(part => part == 1f) ? null : Rounded(new Color(color[0], color[1], color[2], color[3]).ToSrgb()),
            Texture = hasPbr ? Image(pbr, "baseColorTexture") : null,
            MetallicRoughnessMap = hasPbr ? Image(pbr, "metallicRoughnessTexture") : null,
            Metallic = hasPbr && pbr.TryGetProperty("metallicFactor", out var metal) ? Math.Clamp(metal.GetSingle(), 0f, 1f) : 1f,
            Roughness = hasPbr && pbr.TryGetProperty("roughnessFactor", out var rough) ? Math.Clamp(rough.GetSingle(), 0f, 1f) : 1f,
            NormalMap = Image(material, "normalTexture"),
            OcclusionMap = Image(material, "occlusionTexture"),
            EmissiveMap = Image(material, "emissiveTexture"),
            Emissive = emissive.Any(part => part != 0f) ? new Vec3(emissive[0], emissive[1], emissive[2]) * strength : null,
        };

        return new Material(name, look);
    }

    /// <summary>
    /// A color to five places, as a color picker gives it, so a color written from sRGB as linear
    /// and read back is the color it was rather than one a float's rounding off.
    /// </summary>
    private static Vec4 Rounded(Vec4 color) => new(MathF.Round(color.X, 5), MathF.Round(color.Y, 5), MathF.Round(color.Z, 5), MathF.Round(color.W, 5));

    /// <summary>A buffer's bytes, from inside the file, from beside it, or from a binary file's own chunk.</summary>
    private static byte[] Buffer(JsonElement buffer, string folder, byte[]? binary)
    {
        if (!buffer.TryGetProperty("uri", out var uri)) return binary ?? throw new InvalidDataException("a buffer has no URI and the file no binary chunk");

        var text = uri.GetString() ?? "";
        if (text.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = text.IndexOf(',');
            if (comma < 0 || !text[..comma].EndsWith(";base64", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("a buffer's data URI is not base64");
            return Convert.FromBase64String(text[(comma + 1)..]);
        }

        var beside = Path.Combine(folder, Uri.UnescapeDataString(text));
        if (!AssetFiles.Exists(beside)) throw new FileNotFoundException($"The buffer {beside} the model names is not there.", beside);
        return AssetFiles.ReadAllBytes(beside);
    }

    private static JsonElement[] Array(JsonElement owner, string property) =>
        owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(property, out var list) && list.ValueKind == JsonValueKind.Array ? [.. list.EnumerateArray()] : [];

    private static float[] Numbers(JsonElement owner, string property, params float[] otherwise) =>
        owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(property, out var list) && list.ValueKind == JsonValueKind.Array
            ? [.. list.EnumerateArray().Select(value => value.GetSingle())]
            : otherwise;

    /// <summary>The file's JSON and buffers, and what reads an accessor from them.</summary>
    private sealed class Source(JsonElement root, byte[][] buffers)
    {
        public readonly JsonElement Root = root;

        /// <summary>An accessor's numbers, so many to an element, made floats and normalized where it says so.</summary>
        public float[] Floats(int index, int components)
        {
            var (bytes, start, stride, type, count, normalized) = Layout(index, components);
            var values = new float[count * components];
            for (var element = 0; element < count && bytes.Length > 0; element++)
            {
                for (var component = 0; component < components; component++)
                {
                    var at = bytes.AsSpan(start + element * stride + component * Size(type));
                    values[element * components + component] = type switch
                    {
                        5126 => BinaryPrimitives.ReadSingleLittleEndian(at),
                        5121 => normalized ? at[0] / 255f : at[0],
                        5120 => normalized ? MathF.Max((sbyte)at[0] / 127f, -1f) : (sbyte)at[0],
                        5123 => normalized ? BinaryPrimitives.ReadUInt16LittleEndian(at) / 65535f : BinaryPrimitives.ReadUInt16LittleEndian(at),
                        5122 => normalized ? MathF.Max(BinaryPrimitives.ReadInt16LittleEndian(at) / 32767f, -1f) : BinaryPrimitives.ReadInt16LittleEndian(at),
                        _ => BinaryPrimitives.ReadUInt32LittleEndian(at),
                    };
                }
            }

            return values;
        }

        /// <summary>An accessor of indices, whole numbers of one, two or four bytes, read as they are.</summary>
        public int[] Indices(int index)
        {
            var (bytes, start, stride, type, count, _) = Layout(index, 1);
            var values = new int[count];
            for (var element = 0; element < count && bytes.Length > 0; element++)
            {
                var at = bytes.AsSpan(start + element * stride);
                values[element] = type switch
                {
                    5121 => at[0],
                    5123 => BinaryPrimitives.ReadUInt16LittleEndian(at),
                    5125 => (int)Math.Min(BinaryPrimitives.ReadUInt32LittleEndian(at), int.MaxValue),
                    _ => throw new InvalidDataException($"indices of component type {type} are not whole numbers"),
                };
            }

            return values;
        }

        /// <summary>Where an accessor's elements lie in its buffer, no bytes where it has no view and its elements are zeros.</summary>
        private (byte[] Bytes, int Start, int Stride, int Type, int Count, bool Normalized) Layout(int index, int components)
        {
            var accessor = Array(Root, "accessors")[index];
            if (accessor.TryGetProperty("sparse", out _)) throw new InvalidDataException("a sparse accessor is not read");

            var count = accessor.GetProperty("count").GetInt32();
            var type = accessor.GetProperty("componentType").GetInt32();
            var width = Width(accessor.GetProperty("type").GetString());
            if (width != components) throw new InvalidDataException($"an accessor holds {width} numbers an element where {components} were looked for");
            var normalized = accessor.TryGetProperty("normalized", out var flag) && flag.GetBoolean();
            if (!accessor.TryGetProperty("bufferView", out var viewIndex)) return ([], 0, 0, type, count, normalized);

            var view = Array(Root, "bufferViews")[viewIndex.GetInt32()];
            var bytes = buffers[view.GetProperty("buffer").GetInt32()];
            var size = Size(type);
            var stride = view.TryGetProperty("byteStride", out var given) ? given.GetInt32() : size * components;
            var start = (view.TryGetProperty("byteOffset", out var viewOffset) ? viewOffset.GetInt32() : 0)
                + (accessor.TryGetProperty("byteOffset", out var offset) ? offset.GetInt32() : 0);

            if (count > 0 && start + (long)(count - 1) * stride + size * components > bytes.Length) throw new InvalidDataException("an accessor runs past its buffer");
            return (bytes, start, stride, type, count, normalized);
        }

        private static int Size(int type) => type switch
        {
            5120 or 5121 => 1,
            5122 or 5123 => 2,
            5125 or 5126 => 4,
            _ => throw new InvalidDataException($"component type {type} is no glTF type"),
        };

        private static int Width(string? type) => type switch
        {
            "SCALAR" => 1,
            "VEC2" => 2,
            "VEC3" => 3,
            "VEC4" => 4,
            _ => throw new InvalidDataException($"accessor type {type} is not read"),
        };
    }
}
