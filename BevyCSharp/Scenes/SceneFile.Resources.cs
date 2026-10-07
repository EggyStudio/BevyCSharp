using System.Text.Json;

namespace Bevy;

public static partial class SceneFile
{
    /// <summary>
    /// The meshes and materials a scene makes again rather than loads, written once each.
    /// </summary>
    /// <remarks>
    /// A primitive mesh is written as its shape and measures and a standard material as its
    /// settings, so the scene holds how to make each again. Two entities sharing one in memory share
    /// one resource, and loading makes it once and hands both the same handle, so they still share
    /// it. A mesh built vertex by vertex has no recipe and is not written yet.
    /// </remarks>
    private sealed class Resources(SceneReferences references)
    {
        /// <summary>How a material's textures are named, as every other file the scene refers to is.</summary>
        private SceneReferences References { get; } = references;

        private readonly Dictionary<int, int> _ids = [];
        private readonly List<object> _held = [];

        /// <summary>
        /// The resource id of a mesh made in memory: a primitive as its recipe, one built vertex
        /// by vertex as its geometry, or zero for one that is neither.
        /// </summary>
        public int Mesh(AssetHandle mesh) =>
            Add(mesh, () => Render.RecipeOf(mesh) is { } recipe ? recipe : Render.DataOf(mesh));

        /// <summary>The resource id of a standard material, or zero for one that cannot be read.</summary>
        public int Material(AssetHandle material) =>
            Add(material, () => Render.TryReadMaterial(material, out var settings) ? settings : null);

        private int Add(AssetHandle handle, Func<object?> describe)
        {
            if (!handle.IsValid) return 0;
            if (_ids.TryGetValue(handle.Key, out var known)) return known;
            if (describe() is not { } described) return 0;

            _held.Add(described);
            return _ids[handle.Key] = _held.Count;
        }

        /// <summary>Writes the resources after the entities, when every one has been found.</summary>
        public void Write(Utf8JsonWriter json)
        {
            if (_held.Count == 0) return;

            json.WriteStartArray("resources");
            for (var i = 0; i < _held.Count; i++)
            {
                json.WriteStartObject();
                json.WriteNumber("id", i + 1);

                switch (_held[i])
                {
                    case MeshRecipe recipe:
                        json.WritePropertyName("mesh");
                        MeshJson.WriteRecipe(json, recipe);
                        break;

                    case MaterialSettings settings:
                        json.WritePropertyName("material");
                        MaterialJson.Write(json, settings, References);
                        break;

                    case MeshData geometry:
                        json.WritePropertyName("geometry");
                        MeshJson.WriteGeometry(json, geometry);
                        break;
                }

                json.WriteEndObject();
            }

            json.WriteEndArray();
        }

        /// <summary>Makes every resource a scene names, by its id.</summary>
        public static Dictionary<int, AssetHandle> Make(JsonElement resources)
        {
            var made = new Dictionary<int, AssetHandle>();
            foreach (var resource in resources.EnumerateArray())
            {
                if (!resource.TryGetProperty("id", out var id)) continue;

                if (resource.TryGetProperty("mesh", out var mesh) && MeshJson.ReadRecipe(mesh) is { } recipe)
                {
                    made[id.GetInt32()] = Render.CreateMesh(recipe.Shape, recipe.A, recipe.B, recipe.C);
                }
                else if (resource.TryGetProperty("material", out var material))
                {
                    made[id.GetInt32()] = Render.CreateMaterial(MaterialJson.Read(material));
                }
                else if (resource.TryGetProperty("geometry", out var geometry) && MeshJson.ReadGeometry(geometry) is { } data)
                {
                    made[id.GetInt32()] = Render.CreateMesh(data);
                }
            }

            return made;
        }
    }
}
