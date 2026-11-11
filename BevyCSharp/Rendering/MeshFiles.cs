using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Bevy;

/// <summary>
/// Meshes made in memory kept in files of their own, <c>*.mesh.json</c>, shared by everything drawn
/// with them.
/// </summary>
/// <remarks>
/// <para>
/// A primitive is written as its shape and measures and a mesh built vertex by vertex as its
/// geometry, the two forms a scene writes a mesh made in memory in, so a mesh file is a scene's
/// resource lifted out to be shared. Loading one makes one mesh, kept for the file's path, so every
/// entity and every scene loading it shares it, and a scene refers to the file.
/// </para>
/// <para>
/// JSON rather than <c>.glb</c> for generated geometry, so one reader serves both forms and a
/// primitive stays a primitive, which a card can edit as one, rather than becoming triangles.
/// </para>
/// </remarks>
public static class MeshFiles
{
    /// <summary>What a mesh file's name ends in.</summary>
    public const string Extension = ".mesh.json";

    /// <summary>The format a mesh file says it is in.</summary>
    public const string Format = "bevycsharp.mesh.1";

    private static readonly object Gate = new();
    private static readonly Dictionary<string, AssetHandle> ByPath = new(StringComparer.Ordinal);
    private static readonly Dictionary<AssetHandle, string> PathByHandle = [];
    private static readonly HashSet<string> Refused = new(StringComparer.Ordinal);

    /// <summary>Whether a path names a mesh file.</summary>
    public static bool IsMeshFile(string path) => path.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>The mesh a file holds, loaded the first time it is asked for and shared from then on.</summary>
    /// <param name="path">The file, relative to the asset root, with forward slashes.</param>
    /// <returns>
    /// The mesh, or <see cref="AssetHandle.None"/> for a file that is missing or is not a mesh in
    /// this format, which is said on the log and as <see cref="AssetLoadFailed"/>, naming the file.
    /// </returns>
    /// <remarks>
    /// A file that failed is remembered until the next app, so a scene drawing many entities with
    /// it says so once rather than once an entity. <see cref="TryLoad"/> reads the file again each
    /// time it is asked, and says nothing but what it returns.
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

        if (TryLoad(path, out var mesh, out var problem)) return mesh;

        lock (Gate) Refused.Add(path);
        AssetServer.Failed(path, problem, AssetKind.Mesh);
        return AssetHandle.None;
    }

    /// <summary>The mesh a file holds, or why it holds none, naming the file.</summary>
    /// <param name="path">The file, relative to the asset root, with forward slashes.</param>
    /// <param name="mesh">The mesh, shared as <see cref="Load"/> shares it, or none.</param>
    /// <param name="problem">Why the file gave no mesh, or nothing where it gave one.</param>
    /// <returns>Whether the file gave a mesh.</returns>
    public static bool TryLoad(string path, out AssetHandle mesh, [NotNullWhen(false)] out string? problem)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = path.Replace('\\', '/');
        problem = null;

        lock (Gate)
        {
            if (ByPath.TryGetValue(path, out mesh)) return true;
        }

        var full = Path.Combine(AssetIds.Root, path);
        if (!AssetFiles.Exists(full))
        {
            problem = $"No mesh file at {path}.";
            return false;
        }

        using var document = AssetFiles.TryReadJson(full, "a mesh file", out var unread);
        if (document is null)
        {
            problem = unread!;
            return false;
        }

        var root = document.RootElement;
        if (!root.TryGetProperty("format", out var format) || format.ValueKind != JsonValueKind.String || format.GetString() != Format)
        {
            problem = $"{path} is not a mesh in the {Format} format.";
            return false;
        }

        try
        {
            if (root.TryGetProperty("mesh", out var shape) && MeshJson.ReadRecipe(shape) is { } recipe)
                mesh = Render.CreateMesh(recipe.Shape, recipe.A, recipe.B, recipe.C);
            else if (root.TryGetProperty("geometry", out var geometry) && MeshJson.ReadGeometry(geometry) is { } data)
                mesh = Render.CreateMesh(data);
            else
                problem = $"{path} holds neither a shape nor geometry.";
        }
        catch (Exception error) when (error is ArgumentException or Interop.BevyNativeException)
        {
            // A shape this build does not know, or geometry Bevy refuses, such as an index past the
            // last vertex, is a file that is not a mesh it can make.
            problem = $"{path} holds a mesh that could not be made. {error.Message}";
        }

        if (problem is not null) return false;

        lock (Gate)
        {
            ByPath[path] = mesh;
            PathByHandle[mesh] = path;
        }

        return true;
    }

    /// <summary>The file a mesh was loaded from or saved to, or nothing for one that has none.</summary>
    public static string? PathOf(AssetHandle mesh)
    {
        lock (Gate) return PathByHandle.TryGetValue(mesh, out var path) ? path : null;
    }

    /// <summary>
    /// Writes a mesh made in memory to a new file and makes the mesh that file's, so a scene refers to
    /// the file from then on.
    /// </summary>
    /// <param name="mesh">A primitive, or a mesh built vertex by vertex.</param>
    /// <param name="path">Where, relative to the asset root.</param>
    /// <exception cref="IOException">A file is already at <paramref name="path"/>.</exception>
    /// <exception cref="ArgumentException">
    /// The mesh was not made in memory by this side, such as one loaded from a model file.
    /// </exception>
    public static void SaveAs(AssetHandle mesh, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        path = path.Replace('\\', '/');

        var full = Path.Combine(AssetIds.Root, path);
        if (File.Exists(full)) throw new IOException($"{path} is already there.");

        if (Render.RecipeOf(mesh) is null && Render.DataOf(mesh) is null)
            throw new ArgumentException($"{mesh} was not made in memory, so there is nothing to write.", nameof(mesh));

        Write(mesh, full);
        AssetIds.IdOf(path, create: true);

        lock (Gate)
        {
            if (PathByHandle.TryGetValue(mesh, out var earlier)) ByPath.Remove(earlier);
            ByPath[path] = mesh;
            PathByHandle[mesh] = path;
        }
    }

    /// <summary>Writes a mesh loaded from a file back to that file, as it is now.</summary>
    /// <returns>Whether the mesh had a file to write.</returns>
    public static bool Save(AssetHandle mesh)
    {
        if (PathOf(mesh) is not { } path) return false;

        Write(mesh, Path.Combine(AssetIds.Root, path));
        return true;
    }

    /// <summary>Writes a mesh's recipe or geometry to a file, through a temporary one.</summary>
    private static void Write(AssetHandle mesh, string full)
    {
        var recipe = Render.RecipeOf(mesh);
        var data = recipe is null ? Render.DataOf(mesh) : null;
        if (recipe is null && data is null) return;

        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            json.WriteStartObject();
            json.WriteString("format", Format);
            if (recipe is { } shape)
            {
                json.WritePropertyName("mesh");
                MeshJson.WriteRecipe(json, shape);
            }
            else
            {
                json.WritePropertyName("geometry");
                MeshJson.WriteGeometry(json, data!);
            }

            json.WriteEndObject();
        }

        UserData.WriteAtomically(full, buffer.WrittenSpan);
    }

    /// <summary>Forgets every loaded mesh file, for an app starting, whose handles are its own.</summary>
    internal static void Forget()
    {
        lock (Gate)
        {
            ByPath.Clear();
            PathByHandle.Clear();
            Refused.Clear();
        }
    }
}
