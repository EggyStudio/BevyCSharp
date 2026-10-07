using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// A standard material's settings as JSON, the form a scene's resources and a material file share.
/// </summary>
/// <remarks>
/// Colors as four linear numbers, the alpha mode by name, and each texture as a reference to its
/// file by id and path (<see cref="SceneReferences.WriteFile"/>), so a texture renamed in the asset
/// browser is still found. A texture made in memory has no file and is left off, and a texture
/// written as a bare path, as files were before ids, still reads.
/// </remarks>
internal static class MaterialJson
{
    /// <summary>Writes settings as an object.</summary>
    internal static void Write(Utf8JsonWriter json, MaterialSettings settings, SceneReferences references)
    {
        json.WriteStartObject();
        Four(json, "baseColor", settings.BaseColor);
        json.WriteNumber("metallic", settings.Metallic);
        json.WriteNumber("roughness", settings.Roughness);
        Four(json, "emissive", settings.Emissive);
        json.WriteString("alphaMode", settings.AlphaMode.ToString());
        json.WriteNumber("alphaCutoff", settings.AlphaCutoff);
        json.WriteBoolean("doubleSided", settings.DoubleSided);
        json.WriteBoolean("unlit", settings.Unlit);

        Texture(json, "baseColorTexture", settings.BaseColorTexture, references);
        Texture(json, "normalMap", settings.NormalMap, references);
        Texture(json, "metallicRoughnessTexture", settings.MetallicRoughnessTexture, references);
        Texture(json, "emissiveTexture", settings.EmissiveTexture, references);
        Texture(json, "occlusionTexture", settings.OcclusionTexture, references);

        json.WriteStartArray("uvScale");
        json.WriteNumberValue(settings.UvScale.U);
        json.WriteNumberValue(settings.UvScale.V);
        json.WriteEndArray();
        json.WriteNumber("uvRotation", settings.UvRotation);
        json.WriteStartArray("uvOffset");
        json.WriteNumberValue(settings.UvOffset.U);
        json.WriteNumberValue(settings.UvOffset.V);
        json.WriteEndArray();

        // The surface's finer settings, written only where they differ from a plain material's,
        // so a file of one says nothing of them and one written before they existed reads the same.
        var plain = new MaterialSettings();
        if (settings.Reflectance != plain.Reflectance) json.WriteNumber("reflectance", settings.Reflectance);
        if (settings.Clearcoat != plain.Clearcoat) json.WriteNumber("clearcoat", settings.Clearcoat);
        if (settings.ClearcoatRoughness != plain.ClearcoatRoughness) json.WriteNumber("clearcoatRoughness", settings.ClearcoatRoughness);
        if (settings.Transmission != plain.Transmission) json.WriteNumber("transmission", settings.Transmission);
        if (settings.DiffuseTransmission != plain.DiffuseTransmission) json.WriteNumber("diffuseTransmission", settings.DiffuseTransmission);
        if (settings.Thickness != plain.Thickness) json.WriteNumber("thickness", settings.Thickness);
        if (settings.RefractiveIndex != plain.RefractiveIndex) json.WriteNumber("refractiveIndex", settings.RefractiveIndex);

        // Infinity tints nothing and JSON has no number for it, so it is the one value left out.
        if (float.IsFinite(settings.AttenuationDistance)) json.WriteNumber("attenuationDistance", settings.AttenuationDistance);
        if (settings.AttenuationColor != plain.AttenuationColor) Four(json, "attenuationColor", settings.AttenuationColor);
        if (settings.AnisotropyStrength != plain.AnisotropyStrength) json.WriteNumber("anisotropyStrength", settings.AnisotropyStrength);
        if (settings.AnisotropyRotation != plain.AnisotropyRotation) json.WriteNumber("anisotropyRotation", settings.AnisotropyRotation);
        if (settings.LightmapExposure != plain.LightmapExposure) json.WriteNumber("lightmapExposure", settings.LightmapExposure);

        // The finer maps, each written only where one is set, as every slot is.
        Texture(json, "clearcoatTexture", settings.ClearcoatTexture, references);
        Texture(json, "clearcoatRoughnessTexture", settings.ClearcoatRoughnessTexture, references);
        Texture(json, "clearcoatNormalTexture", settings.ClearcoatNormalTexture, references);
        Texture(json, "transmissionTexture", settings.TransmissionTexture, references);
        Texture(json, "diffuseTransmissionTexture", settings.DiffuseTransmissionTexture, references);
        Texture(json, "thicknessTexture", settings.ThicknessTexture, references);
        Texture(json, "anisotropyTexture", settings.AnisotropyTexture, references);
        json.WriteEndObject();
    }

    /// <summary>Reads settings written by <see cref="Write"/>, leaving what the object leaves out at its default.</summary>
    internal static MaterialSettings Read(JsonElement json)
    {
        var settings = new MaterialSettings();
        if (Floats(json, "baseColor", 4) is { } b) settings.BaseColor = (b[0], b[1], b[2], b[3]);
        if (json.TryGetProperty("metallic", out var metallic)) settings.Metallic = metallic.GetSingle();
        if (json.TryGetProperty("roughness", out var roughness)) settings.Roughness = roughness.GetSingle();
        if (Floats(json, "emissive", 4) is { } e) settings.Emissive = (e[0], e[1], e[2], e[3]);
        if (json.TryGetProperty("alphaMode", out var mode) && Enum.TryParse<AlphaMode>(mode.GetString(), out var alpha))
            settings.AlphaMode = alpha;
        if (json.TryGetProperty("alphaCutoff", out var cutoff)) settings.AlphaCutoff = cutoff.GetSingle();
        if (json.TryGetProperty("doubleSided", out var sides)) settings.DoubleSided = sides.GetBoolean();
        if (json.TryGetProperty("unlit", out var unlit)) settings.Unlit = unlit.GetBoolean();

        settings.BaseColorTexture = Image(json, "baseColorTexture");
        settings.NormalMap = Image(json, "normalMap");
        settings.MetallicRoughnessTexture = Image(json, "metallicRoughnessTexture");
        settings.EmissiveTexture = Image(json, "emissiveTexture");
        settings.OcclusionTexture = Image(json, "occlusionTexture");

        if (Floats(json, "uvScale", 2) is { } scale) settings.UvScale = (scale[0], scale[1]);
        if (json.TryGetProperty("uvRotation", out var rotation)) settings.UvRotation = rotation.GetSingle();
        if (Floats(json, "uvOffset", 2) is { } offset) settings.UvOffset = (offset[0], offset[1]);

        if (json.TryGetProperty("reflectance", out var reflectance)) settings.Reflectance = reflectance.GetSingle();
        if (json.TryGetProperty("clearcoat", out var clearcoat)) settings.Clearcoat = clearcoat.GetSingle();
        if (json.TryGetProperty("clearcoatRoughness", out var coatRoughness)) settings.ClearcoatRoughness = coatRoughness.GetSingle();
        if (json.TryGetProperty("transmission", out var transmission)) settings.Transmission = transmission.GetSingle();
        if (json.TryGetProperty("diffuseTransmission", out var diffuse)) settings.DiffuseTransmission = diffuse.GetSingle();
        if (json.TryGetProperty("thickness", out var thickness)) settings.Thickness = thickness.GetSingle();
        if (json.TryGetProperty("refractiveIndex", out var ior)) settings.RefractiveIndex = ior.GetSingle();

        if (json.TryGetProperty("attenuationDistance", out var distance)) settings.AttenuationDistance = distance.GetSingle();
        if (Floats(json, "attenuationColor", 4) is { } tint) settings.AttenuationColor = (tint[0], tint[1], tint[2], tint[3]);
        if (json.TryGetProperty("anisotropyStrength", out var strength)) settings.AnisotropyStrength = strength.GetSingle();
        if (json.TryGetProperty("anisotropyRotation", out var turn)) settings.AnisotropyRotation = turn.GetSingle();
        if (json.TryGetProperty("lightmapExposure", out var exposure)) settings.LightmapExposure = exposure.GetSingle();

        settings.ClearcoatTexture = Image(json, "clearcoatTexture");
        settings.ClearcoatRoughnessTexture = Image(json, "clearcoatRoughnessTexture");
        settings.ClearcoatNormalTexture = Image(json, "clearcoatNormalTexture");
        settings.TransmissionTexture = Image(json, "transmissionTexture");
        settings.DiffuseTransmissionTexture = Image(json, "diffuseTransmissionTexture");
        settings.ThicknessTexture = Image(json, "thicknessTexture");
        settings.AnisotropyTexture = Image(json, "anisotropyTexture");
        return settings;
    }

    private static void Four(Utf8JsonWriter json, string name, (float, float, float, float) value)
    {
        json.WriteStartArray(name);
        json.WriteNumberValue(value.Item1);
        json.WriteNumberValue(value.Item2);
        json.WriteNumberValue(value.Item3);
        json.WriteNumberValue(value.Item4);
        json.WriteEndArray();
    }

    private static void Texture(Utf8JsonWriter json, string name, AssetHandle texture, SceneReferences references)
    {
        if (AssetServer.PathOf(texture) is not { Length: > 0 } path) return;

        json.WritePropertyName(name);
        references.WriteFile(json, path);
    }

    private static AssetHandle Image(JsonElement json, string name) =>
        json.TryGetProperty(name, out var reference) && SceneReferences.ReadFile(reference) is { Length: > 0 } file
            ? AssetServer.Load(AssetKind.Image, file)
            : AssetHandle.None;

    private static float[]? Floats(JsonElement json, string name, int count) =>
        json.TryGetProperty(name, out var array)
        && array.ValueKind == JsonValueKind.Array
        && array.GetArrayLength() == count
            ? [.. array.EnumerateArray().Select(number => number.GetSingle())]
            : null;
}

/// <summary>
/// Standard materials kept in files of their own, <c>*.material.json</c>, shared by everything
/// drawn with them.
/// </summary>
/// <remarks>
/// <para>
/// A material made in code or in the editor lives in the scene that uses it, written there as its
/// settings. One wanted by several scenes, or edited by an artist on its own, goes in a file, a
/// <see cref="MaterialSettings"/> as JSON with its textures referred to by id and path. Loading a
/// file makes one material, kept for the file's path, so every entity and every scene loading it
/// shares it, and a scene refers to the file rather than writing the settings again.
/// </para>
/// <para>
/// <see cref="Save(AssetHandle)"/> writes a loaded material's settings back to its file, through a
/// temporary file renamed over it. With <see cref="DataAssets.Watching"/> on, as the editor has it,
/// a file changed on disk is read again and laid over the material in place, so everything drawn
/// with it changes without being pointed at anything new.
/// </para>
/// </remarks>
public static class MaterialFiles
{
    /// <summary>What a material file's name ends in.</summary>
    public const string Extension = ".material.json";

    /// <summary>The format a material file says it is in.</summary>
    public const string Format = "bevycsharp.material.1";

    private static readonly object Gate = new();
    private static readonly Dictionary<string, AssetHandle> ByPath = new(StringComparer.Ordinal);
    private static readonly Dictionary<AssetHandle, string> PathByHandle = [];
    private static readonly HashSet<string> Refused = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, DateTime> Wrote = new(StringComparer.Ordinal);
    private static readonly ConcurrentQueue<string> Touched = new();
    private static FileSystemWatcher? _watcher;
    private static string? _watched;

    /// <summary>Whether a path names a material file.</summary>
    public static bool IsMaterialFile(string path) => path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The material a file holds, loaded the first time it is asked for and shared from then on.
    /// </summary>
    /// <param name="path">The file, relative to the asset root, with forward slashes.</param>
    /// <returns>
    /// The material, or <see cref="AssetHandle.None"/> for a file that is missing or is not a
    /// material in this format, which is said on the log and as <see cref="AssetLoadFailed"/>,
    /// naming the file.
    /// </returns>
    /// <remarks>
    /// A file that failed is remembered until it changes on disk where files are watched, as the
    /// editor watches them, or the next app starts, so a scene drawing many entities with it says
    /// so once rather than once an entity. <see cref="TryLoad"/> reads the file again each time it
    /// is asked, and says nothing but what it returns.
    /// </remarks>
    public static AssetHandle Load(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = path.Replace('\\', '/');

        lock (Gate)
        {
            if (ByPath.TryGetValue(path, out var known)) return known;
            if (Refused.Contains(path)) return AssetHandle.None;
        }

        if (TryLoad(path, out var material, out var problem)) return material;

        lock (Gate) Refused.Add(path);
        AssetServer.Failed(path, problem, AssetKind.StandardMaterial);
        return AssetHandle.None;
    }

    /// <summary>The material a file holds, or why it holds none, naming the file.</summary>
    /// <param name="path">The file, relative to the asset root, with forward slashes.</param>
    /// <param name="material">
    /// The material, shared as <see cref="Load"/> shares it, or none.
    /// </param>
    /// <param name="problem">Why the file gave no material, or nothing where it gave one.</param>
    /// <returns>Whether the file gave a material.</returns>
    public static bool TryLoad(string path, out AssetHandle material, [NotNullWhen(false)] out string? problem)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = path.Replace('\\', '/');

        lock (Gate)
        {
            if (ByPath.TryGetValue(path, out material))
            {
                problem = null;
                return true;
            }
        }

        try
        {
            material = Render.CreateMaterial(Read(path));
        }
        catch (Exception error) when (error is FileNotFoundException or InvalidDataException)
        {
            problem = error.Message;
            return false;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or FormatException)
        {
            // Unreadable, or JSON of the wrong shape where a number or a list belongs, which throws
            // as the value is read.
            problem = $"{path} is not a material that reads. {error.Message}";
            return false;
        }

        lock (Gate)
        {
            ByPath[path] = material;
            PathByHandle[material] = path;
        }

        problem = null;
        return true;
    }

    /// <summary>The file a material was loaded from, or nothing for one made in memory.</summary>
    public static string? PathOf(AssetHandle material)
    {
        lock (Gate) return PathByHandle.TryGetValue(material, out var path) ? path : null;
    }

    /// <summary>
    /// Writes a material's settings to a new file and makes the material that file's, so saving it
    /// again writes there and a scene refers to the file.
    /// </summary>
    /// <param name="material">A standard material.</param>
    /// <param name="path">Where, relative to the asset root.</param>
    /// <exception cref="IOException">A file is already at <paramref name="path"/>.</exception>
    /// <exception cref="ArgumentException">The handle names no standard material.</exception>
    public static void SaveAs(AssetHandle material, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = path.Replace('\\', '/');
        if (File.Exists(Full(path))) throw new IOException($"{path} is already there.");

        lock (Gate)
        {
            if (PathByHandle.TryGetValue(material, out var earlier)) ByPath.Remove(earlier);
            ByPath[path] = material;
            PathByHandle[material] = path;
        }

        Save(material);
        AssetIds.IdOf(path, create: true);
    }

    /// <summary>Writes a material loaded from a file back to that file, as it is now.</summary>
    /// <returns>Whether the material had a file to write.</returns>
    /// <exception cref="ArgumentException">The handle names no standard material.</exception>
    public static bool Save(AssetHandle material)
    {
        if (PathOf(material) is not { } path) return false;
        if (!Render.TryReadMaterial(material, out var settings) || settings is null)
            throw new ArgumentException($"{material} is not a standard material.", nameof(material));

        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("format", Format);
            json.WritePropertyName("material");
            MaterialJson.Write(json, settings, new SceneReferences { GiveIds = true });
            json.WriteEndObject();
        }

        var full = Full(path);
        UserData.WriteAtomically(full, buffer.WrittenSpan);
        lock (Gate) Wrote[Path.GetFullPath(full)] = File.GetLastWriteTimeUtc(full);
        return true;
    }

    /// <summary>
    /// Reads every loaded material file that changed on disk since the last frame and lays it over
    /// its material. Called by the app each frame.
    /// </summary>
    internal static void ReloadTouched()
    {
        Watch();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (Touched.TryDequeue(out var full))
        {
            if (!seen.Add(full) || !File.Exists(full)) continue;

            var relative = Path.GetRelativePath(AssetIds.Root, full).Replace('\\', '/');
            AssetHandle material;
            lock (Gate)
            {
                // One that failed is read again at its next load, which may find it mended.
                if (Refused.Remove(relative)) continue;
                if (!ByPath.TryGetValue(relative, out material)) continue;
                if (Wrote.TryGetValue(full, out var ours) && ours == File.GetLastWriteTimeUtc(full)) continue;
            }

            try
            {
                Render.WriteMaterial(material, Read(relative));
            }
            catch (Exception error) when (error is IOException or JsonException or InvalidDataException)
            {
                EngineLog.Error(null, "assets", $"[assets] {relative} could not be read again. {error.Message}", error);
            }
        }
    }

    /// <summary>Forgets every loaded material file, for an app starting, whose handles are its own.</summary>
    internal static void Forget()
    {
        lock (Gate)
        {
            ByPath.Clear();
            PathByHandle.Clear();
            Refused.Clear();
        }
    }

    private static MaterialSettings Read(string path)
    {
        var full = Full(path);
        if (!AssetFiles.Exists(full)) throw new FileNotFoundException($"No material file at {path}.", full);

        using var document = AssetFiles.ReadJson(full, "a material file");
        var root = document.RootElement;
        if (!root.TryGetProperty("format", out var format) || format.GetString() != Format
            || !root.TryGetProperty("material", out var material))
            throw new InvalidDataException($"{path} is not a material in the {Format} format.");

        return MaterialJson.Read(material);
    }

    private static void Watch()
    {
        var root = AssetIds.Root;
        var wanted = DataAssets.Watching && Directory.Exists(root) ? root : null;
        if (wanted == _watched) return;

        _watcher?.Dispose();
        _watcher = null;
        _watched = wanted;
        if (wanted is null) return;

        _watcher = new FileSystemWatcher(wanted, "*" + Extension)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.Created += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.Renamed += (_, change) => Touched.Enqueue(change.FullPath);
        _watcher.EnableRaisingEvents = true;
    }

    private static string Full(string path) => Path.Combine(AssetIds.Root, path);
}
