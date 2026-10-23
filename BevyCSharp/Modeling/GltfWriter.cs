using System.Text.Json;

namespace Bevy;

/// <summary>
/// Writes an edit mesh, built with its modifiers, as a glTF 2.0 file, the JSON with its buffer
/// beside it or inside it, a primitive for each material slot, so a model blocked out in the game
/// is opened in Blender or any program that reads glTF.
/// </summary>
/// <remarks>
/// <para>
/// The engine's own writer, in managed code, so a game exports with nothing but the library. Each
/// slot with faces is a primitive of one mesh, of positions, normals, texture coordinates and
/// indices as the build makes them, a face's corners its own where faces meet at an angle, and
/// each slot is a material of the glTF metallic and roughness kind, its color, its factors and its
/// textures, a texture named by its path from the file rather than copied into it.
/// </para>
/// <para>
/// Colors are linear in glTF and sRGB in a slot's look, and are converted. A slot drawn with the
/// prototype grid is written as its prototype's color, the grid being a picture the engine makes.
/// An emissive color brighter than one is written as one with
/// <c>KHR_materials_emissive_strength</c>, glTF's way of saying how much brighter.
/// </para>
/// </remarks>
internal static class GltfWriter
{
    private const int Floats = 5126;
    private const int UnsignedInts = 5125;
    private const int Vertices = 34962;
    private const int Indices = 34963;

    /// <summary>One accessor, its view, its kind and its count, with the bounds glTF asks of positions.</summary>
    private readonly record struct Accessor(int View, int ComponentType, int Count, string Type, Vec3? Low = null, Vec3? High = null);

    /// <summary>Writes the file, and the buffer beside it unless it is embedded, and answers the file's full path.</summary>
    public static string Write(EditMesh mesh, string path, bool embedded)
    {
        var full = Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(AssetIds.Root, path));
        var folder = Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(folder);

        var built = mesh.Evaluated();
        var slots = Modeling.Build(mesh);

        var buffer = new MemoryStream();
        var views = new List<(int Offset, int Length, int Target)>();
        var accessors = new List<Accessor>();
        var primitives = new List<(int Positions, int Normals, int Uvs, int Indices, int Material)>();

        int View(ReadOnlySpan<byte> bytes, int target)
        {
            // Each view starts on four bytes, as glTF asks of a view a float accessor reads.
            while (buffer.Length % 4 != 0) buffer.WriteByte(0);
            views.Add(((int)buffer.Length, bytes.Length, target));
            buffer.Write(bytes);
            return views.Count - 1;
        }

        for (var slot = 0; slot < slots.Length; slot++)
        {
            var data = slots[slot];
            if (data.Positions.Length == 0 || data.Indices is not { Length: > 0 } order) continue;

            var count = data.Positions.Length;
            var (low, high) = (data.Positions[0], data.Positions[0]);
            foreach (var point in data.Positions)
            {
                low = new Vec3(MathF.Min(low.X, point.X), MathF.Min(low.Y, point.Y), MathF.Min(low.Z, point.Z));
                high = new Vec3(MathF.Max(high.X, point.X), MathF.Max(high.Y, point.Y), MathF.Max(high.Z, point.Z));
            }

            accessors.Add(new Accessor(View(Bytes(data.Positions), Vertices), Floats, count, "VEC3", low, high));
            accessors.Add(new Accessor(View(Bytes(data.Normals ?? new Vec3[count]), Vertices), Floats, count, "VEC3"));
            accessors.Add(new Accessor(View(Bytes(data.Uvs ?? new float[count * 2]), Vertices), Floats, count, "VEC2"));
            accessors.Add(new Accessor(View(Bytes(order), Indices), UnsignedInts, order.Length, "SCALAR"));
            primitives.Add((accessors.Count - 4, accessors.Count - 3, accessors.Count - 2, accessors.Count - 1, slot));
        }

        var name = Path.GetFileNameWithoutExtension(full);
        var bin = name + ".bin";
        var bytes = buffer.ToArray();

        var json = new MemoryStream();
        using (var writer = new Utf8JsonWriter(json, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();

            writer.WriteStartObject("asset");
            writer.WriteString("version", "2.0");
            writer.WriteString("generator", "BevyCSharp EditMesh");
            writer.WriteEndObject();

            var strong = Enumerable.Range(0, built.Slots.Count).Any(slot => Emissive(built.LookAt(slot)).Strength > 1f);
            if (strong)
            {
                writer.WriteStartArray("extensionsUsed");
                writer.WriteStringValue("KHR_materials_emissive_strength");
                writer.WriteEndArray();
            }

            writer.WriteNumber("scene", 0);
            writer.WriteStartArray("scenes");
            writer.WriteStartObject();
            writer.WriteStartArray("nodes");
            writer.WriteNumberValue(0);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteEndArray();

            writer.WriteStartArray("nodes");
            writer.WriteStartObject();
            writer.WriteString("name", name);
            if (primitives.Count > 0) writer.WriteNumber("mesh", 0);
            writer.WriteEndObject();
            writer.WriteEndArray();

            if (primitives.Count > 0)
            {
                writer.WriteStartArray("meshes");
                writer.WriteStartObject();
                writer.WriteString("name", name);
                writer.WriteStartArray("primitives");
                foreach (var (positions, normals, uvs, indices, material) in primitives)
                {
                    writer.WriteStartObject();
                    writer.WriteStartObject("attributes");
                    writer.WriteNumber("POSITION", positions);
                    writer.WriteNumber("NORMAL", normals);
                    writer.WriteNumber("TEXCOORD_0", uvs);
                    writer.WriteEndObject();
                    writer.WriteNumber("indices", indices);
                    writer.WriteNumber("material", material);
                    writer.WriteNumber("mode", 4);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteEndArray();
            }

            Materials(writer, built, folder);

            writer.WriteStartArray("accessors");
            foreach (var accessor in accessors)
            {
                writer.WriteStartObject();
                writer.WriteNumber("bufferView", accessor.View);
                writer.WriteNumber("componentType", accessor.ComponentType);
                writer.WriteNumber("count", accessor.Count);
                writer.WriteString("type", accessor.Type);
                if (accessor is { Low: { } low, High: { } high })
                {
                    Numbers(writer, "min", low.X, low.Y, low.Z);
                    Numbers(writer, "max", high.X, high.Y, high.Z);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            writer.WriteStartArray("bufferViews");
            foreach (var (offset, length, target) in views)
            {
                writer.WriteStartObject();
                writer.WriteNumber("buffer", 0);
                writer.WriteNumber("byteOffset", offset);
                writer.WriteNumber("byteLength", length);
                writer.WriteNumber("target", target);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            if (bytes.Length > 0)
            {
                writer.WriteStartArray("buffers");
                writer.WriteStartObject();
                writer.WriteNumber("byteLength", bytes.Length);
                writer.WriteString("uri", embedded ? "data:application/octet-stream;base64," + Convert.ToBase64String(bytes) : Uri.EscapeDataString(bin));
                writer.WriteEndObject();
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        // Each written under a name of its own and moved into place, so a program reading the
        // model while it is written reads the old file or the new one whole.
        if (!embedded && bytes.Length > 0) Replace(Path.Combine(folder, bin), bytes);
        Replace(full, json.ToArray());
        return full;
    }

    /// <summary>A material for each slot, and the textures and images its looks name.</summary>
    private static void Materials(Utf8JsonWriter writer, EditMesh built, string folder)
    {
        var images = new List<string>();
        int Texture(string path)
        {
            var index = images.IndexOf(path);
            if (index >= 0) return index;
            images.Add(path);
            return images.Count - 1;
        }

        writer.WriteStartArray("materials");
        for (var slot = 0; slot < built.Slots.Count; slot++)
        {
            var name = built.Slots[slot];
            var look = built.LookAt(slot);
            var color = look.Color is { } given ? Color.FromSrgb(given.X, given.Y, given.Z, given.W)
                : look.Texture is null && Modeling.PrototypeColors.TryGetValue(name, out var prototype) ? prototype : Color.White;

            writer.WriteStartObject();
            writer.WriteString("name", name);

            writer.WriteStartObject("pbrMetallicRoughness");
            Numbers(writer, "baseColorFactor", color.R, color.G, color.B, color.A);
            if (look.Texture is { } texture) TextureInfo(writer, "baseColorTexture", Texture(texture));
            writer.WriteNumber("metallicFactor", look.Metallic ?? 0f);
            writer.WriteNumber("roughnessFactor", look.Roughness ?? (look.Texture is null ? 0.85f : 0.5f));
            if (look.MetallicRoughnessMap is { } metal) TextureInfo(writer, "metallicRoughnessTexture", Texture(metal));
            writer.WriteEndObject();

            if (look.NormalMap is { } normal) TextureInfo(writer, "normalTexture", Texture(normal));
            if (look.OcclusionMap is { } occlusion) TextureInfo(writer, "occlusionTexture", Texture(occlusion));
            if (look.EmissiveMap is { } glow) TextureInfo(writer, "emissiveTexture", Texture(glow));

            var (light, strength) = Emissive(look);
            if (light != Vec3.Zero) Numbers(writer, "emissiveFactor", light.X, light.Y, light.Z);
            if (strength > 1f)
            {
                writer.WriteStartObject("extensions");
                writer.WriteStartObject("KHR_materials_emissive_strength");
                writer.WriteNumber("emissiveStrength", strength);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            if (color.A < 1f) writer.WriteString("alphaMode", "BLEND");
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        if (images.Count == 0) return;

        writer.WriteStartArray("textures");
        for (var i = 0; i < images.Count; i++)
        {
            writer.WriteStartObject();
            writer.WriteNumber("source", i);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();

        // Named by their path from the file's folder, each part of it escaped as a URI's is.
        writer.WriteStartArray("images");
        foreach (var image in images)
        {
            var relative = Path.GetRelativePath(folder, Path.GetFullPath(Path.Combine(AssetIds.Root, image)));
            var uri = string.Join('/', relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Select(Uri.EscapeDataString));
            writer.WriteStartObject();
            writer.WriteString("uri", uri);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>The emissive color as glTF takes it, at most one, and how many times brighter it is than that.</summary>
    private static (Vec3 Factor, float Strength) Emissive(SlotLook look)
    {
        var light = look.Emissive ?? (look.EmissiveMap is null ? Vec3.Zero : Vec3.One);
        var brightest = MathF.Max(light.X, MathF.Max(light.Y, light.Z));
        return brightest > 1f ? (light * (1f / brightest), brightest) : (light, 1f);
    }

    private static void TextureInfo(Utf8JsonWriter writer, string name, int index)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("index", index);
        writer.WriteEndObject();
    }

    private static void Numbers(Utf8JsonWriter writer, string name, params ReadOnlySpan<float> values)
    {
        writer.WriteStartArray(name);
        foreach (var value in values) writer.WriteNumberValue(value);
        writer.WriteEndArray();
    }

    private static ReadOnlySpan<byte> Bytes<T>(T[] values) where T : unmanaged =>
        System.Runtime.InteropServices.MemoryMarshal.AsBytes(values.AsSpan());

    private static void Replace(string path, byte[] bytes)
    {
        var part = path + ".part";
        File.WriteAllBytes(part, bytes);
        File.Move(part, path, overwrite: true);
    }
}
