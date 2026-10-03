using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// The Mesh card and the Material card: a turnable picture of what an entity is drawn with, what
/// the mesh is made of, and the material's settings as rows.
/// </summary>
/// <remarks>
/// <para>
/// A fold under each row of "Drawn with", since the row already picks the file and the card says
/// what the picked one is. Each picture is a <see cref="PreviewRenderer"/> slot of its own,
/// the mesh alone in a plain gray and the material on a sphere, so neither depends on the lighting
/// of the scene around the entity.
/// </para>
/// <para>
/// The material's rows are fields of a schema over the material itself, read through
/// <see cref="Render.TryReadMaterial"/> and written through <see cref="Render.WriteMaterial"/>, so
/// they are drawn, spread over a selection and taken back by undo as any component's fields are.
/// A material is shared by everything drawn with it, which the fold says by counting them.
/// </para>
/// </remarks>
internal static class DrawnCards
{
    /// <summary>The schema over each material, by handle, kept so one is not made a frame.</summary>
    private static readonly Dictionary<AssetHandle, ComponentSchema> Materials = [];

    /// <summary>What the mesh card's picture is drawn under.</summary>
    private const string MeshKey = "mesh card";

    /// <summary>What the material card's picture is drawn under.</summary>
    private const string MaterialKey = "material card";

    /// <summary>The Mesh card, folded under the Mesh row.</summary>
    internal static void Mesh(BehaviorContext ctx, Entity entity)
    {
        var mesh = Render.MeshOf(ctx.Ecs, entity);
        if (!mesh.IsValid) return;

        ImGui.Indent();
        if (ImGui.TreeNodeEx("Mesh##card", ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DefaultOpen))
        {
            MeshBody(ctx, mesh, entity);
            ImGui.TreePop();
        }

        ImGui.Unindent();
    }

    /// <summary>
    /// What the Mesh card holds, for a mesh an entity is drawn with or one picked as a file: the
    /// picture, the measures of a primitive, and what it is made of.
    /// </summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="mesh">The mesh.</param>
    /// <param name="entity">The entity drawn with it, which the actions for one entity need, or none.</param>
    internal static void MeshBody(BehaviorContext ctx, AssetHandle mesh, Entity entity)
    {
        if (PreviewRenderer.Show(ctx, MeshKey, new PreviewSubject.Mesh(mesh)))
            PreviewRenderer.Draw(MeshKey, Side(), turnable: true);

        var wire = PreviewRenderer.Wireframe(MeshKey);
        if (Toggle("Wireframe", ref wire)) PreviewRenderer.SetWireframe(MeshKey, wire);

        // How the mesh is shown: lit, in a checker that shows its UVs, or with its normals drawn.
        ImGui.SameLine();
        ImGui.SetNextItemWidth(MathF.Max(80f, ImGui.GetContentRegionAvail().X - DetailsPanel.RightInset));
        EditorWidgets.Choice(
            "##view",
            Views[(int)PreviewRenderer.ViewOf(MeshKey)],
            Views,
            chosen => PreviewRenderer.SetView(MeshKey, (PreviewView)Array.IndexOf(Views, chosen)));

        if (!entity.IsNone) MeshActions(ctx.Ecs, entity, mesh);

        // A primitive's measures, which rebuild it in place, so everything sharing it changes.
        if (MeasuresOf(mesh) is { } measures)
            foreach (var field in measures.Fields) ComponentFields.Row(ctx, entity, measures, field);

        Facts(entity, mesh);
    }

    /// <summary>The Material card, folded under the Material row.</summary>
    internal static void Material(BehaviorContext ctx, Entity entity)
    {
        var material = Render.MaterialOf(ctx.Ecs, entity);
        if (!material.IsValid || !Render.TryReadMaterial(material, out _)) return;

        var users = Users(ctx.Ecs, material);
        var file = MaterialFiles.PathOf(material);
        var title = (users > 1, file) switch
        {
            (true, { } path) => $"Material, {path}, shared by {users}##card",
            (true, null) => $"Material, shared by {users}##card",
            (false, { } path) => $"Material, {path}##card",
            _ => "Material##card",
        };

        ImGui.Indent();
        var open = ImGui.TreeNodeEx(title, ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DefaultOpen);
        if (users > 1 && ImGui.IsItemHovered())
            EditorWidgets.Tip("A change here reaches every entity drawn with this material.");

        if (open)
        {
            MaterialBody(ctx, material, entity, users);
            ImGui.TreePop();
        }

        ImGui.Unindent();
    }

    /// <summary>
    /// What the Material card holds, for a material an entity is drawn with or one picked as a
    /// file: the picture and the settings as rows.
    /// </summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="material">The material.</param>
    /// <param name="entity">The entity drawn with it, which the actions for one entity need, or none.</param>
    /// <param name="users">How many entities share it, which decides whether a copy is offered.</param>
    internal static void MaterialBody(BehaviorContext ctx, AssetHandle material, Entity entity, int users)
    {
        if (PreviewRenderer.Show(ctx, MaterialKey, new PreviewSubject.Material(material)))
            PreviewRenderer.Draw(MaterialKey, Side(), turnable: true);

        if (!entity.IsNone) Actions(ctx, entity, material, MaterialFiles.PathOf(material), users);

        var schema = SchemaOf(material);
        foreach (var field in schema.Fields) ComponentFields.Row(ctx, entity, schema, field);
    }

    /// <summary>How many entities of the scene are drawn with a material, for a file picked in the browser.</summary>
    internal static int UsersOf(EcsWorld world, AssetHandle material) => Users(world, material);

    /// <summary>
    /// What can be done with a material besides changing it: give this entity a copy of its own,
    /// and keep one made here in a file.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Make unique" is offered while something else shares the material, since changing a shared
    /// one changes all of them, and a copy is how one entity's look changes alone. It is a step to
    /// undo, which points the entity back at the shared material.
    /// </para>
    /// <para>
    /// "Save as asset" is offered for a material made here, which lives in the scene. It writes the
    /// material to a file in the folder the asset browser is looking at, named after the entity,
    /// and the material is that file's from then on, so every entity sharing it now refers to the
    /// file and a change to it is written there.
    /// </para>
    /// </remarks>
    private static void Actions(BehaviorContext ctx, Entity entity, AssetHandle material, string? file, int users)
    {
        var any = false;

        if (users > 1)
        {
            if (ImGui.Button("Make unique")) Unique(ctx.Ecs, entity, material);
            any = true;
        }

        if (file is null && Render.MaterialPathOf(entity).Length == 0)
        {
            if (any) ImGui.SameLine();
            if (ImGui.Button("Save as asset")) SaveAs(ctx.Ecs, entity, material);
        }
    }

    /// <summary>
    /// What can be done with a mesh made here: give this entity a copy of its own while others share
    /// it, and keep it in a file.
    /// </summary>
    /// <remarks>
    /// Only for a mesh made in memory, a primitive or one built vertex by vertex, since those are
    /// the ones this side can copy and write. A mesh from a model file is the file's.
    /// </remarks>
    private static void MeshActions(EcsWorld world, Entity entity, AssetHandle mesh)
    {
        var made = Render.RecipeOf(mesh) is not null || Render.DataOf(mesh) is not null;
        if (!made) return;

        var users = world.All().Count(other =>
            !PreviewRenderer.Owns(other) && Render.IsDrawn(other) && Render.MeshOf(world, other) == mesh);
        var any = false;

        if (users > 1)
        {
            if (ImGui.Button($"Make unique, shared by {users}")) UniqueMesh(world, entity, mesh);
            any = true;
        }

        if (MeshFiles.PathOf(mesh) is null)
        {
            if (any) ImGui.SameLine();
            if (ImGui.Button("Save as asset##mesh")) SaveMeshAs(world, entity, mesh);
        }
    }

    /// <summary>Gives the entity a copy of a mesh of its own, as one step to undo.</summary>
    private static void UniqueMesh(EcsWorld world, Entity entity, AssetHandle shared)
    {
        var copy = Render.RecipeOf(shared) is { } recipe
            ? Render.CreateMesh(recipe.Shape, recipe.A, recipe.B, recipe.C)
            : Render.DataOf(shared) is { } data ? Render.CreateMesh(data) : AssetHandle.None;
        if (!copy.IsValid) return;

        Render.SetMesh(world, entity, copy);
        EditorHistory.Record(
            "mesh made unique",
            undo => Render.SetMesh(undo, entity, shared),
            redo => Render.SetMesh(redo, entity, copy));
    }

    /// <summary>Writes a mesh made here to a file named after the entity, beside the browser's folder.</summary>
    private static void SaveMeshAs(EcsWorld world, Entity entity, AssetHandle mesh)
    {
        var path = Unused(world, entity, "Mesh", MeshFiles.Extension);
        MeshFiles.SaveAs(mesh, path);
        Console.WriteLine($"[editor] saved the mesh as {path}");
    }

    /// <summary>A file name in the browser's folder, after the entity, that nothing is called.</summary>
    private static string Unused(EcsWorld world, Entity entity, string fallback, string extension)
    {
        var folder = EditorAssets.Directory;
        var stem = world.NameOf(entity) is { Length: > 0 } named ? named : fallback;
        var path = Join(folder, stem + extension);

        for (var n = 2; File.Exists(EditorAssets.Absolute(path)); n++)
            path = Join(folder, $"{stem} {n}{extension}");

        return path;

        static string Join(string folder, string name) => folder.Length == 0 ? name : folder + "/" + name;
    }

    /// <summary>Gives the entity a copy of a material of its own, as one step to undo.</summary>
    private static void Unique(EcsWorld world, Entity entity, AssetHandle shared)
    {
        if (!Render.TryReadMaterial(shared, out var settings) || settings is null) return;

        var copy = Render.CreateMaterial(settings);
        Render.SetMaterial(world, entity, copy);

        EditorHistory.Record(
            "material made unique",
            undo => Render.SetMaterial(undo, entity, shared),
            redo => Render.SetMaterial(redo, entity, copy));
    }

    /// <summary>Writes a material made here to a file named after the entity, beside the browser's folder.</summary>
    private static void SaveAs(EcsWorld world, Entity entity, AssetHandle material)
    {
        var path = Unused(world, entity, "Material", MaterialFiles.Extension);
        MaterialFiles.SaveAs(material, path);
        Console.WriteLine($"[editor] saved the material as {path}");
    }

    /// <summary>What the Mesh card's view choice offers, in the order of <see cref="PreviewView"/>.</summary>
    private static readonly string[] Views = ["Surface", "UV checker", "Normals"];

    /// <summary>How large a card's picture is: the width there is, up to a size that still reads as a swatch.</summary>
    private static float Side() => MathF.Min(200f, MathF.Max(64f, ImGui.GetContentRegionAvail().X - DetailsPanel.RightInset));

    /// <summary>A labeled tick box.</summary>
    private static bool Toggle(string label, ref bool on)
    {
        var changed = EditorWidgets.Ticked($"##{label}", ref on);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        return changed;
    }

    /// <summary>What a mesh is made of and where it came from, as rows.</summary>
    private static void Facts(Entity entity, AssetHandle mesh)
    {
        var across = new Vector2(MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.RightInset), 0f);
        if (!EditorRows.Open("##facts", across)) return;

        Fact("From", Origin(entity, mesh));

        if (Render.TryGetMeshInfo(mesh, out var info))
        {
            var plain = CultureInfo.InvariantCulture;
            Fact("Vertices", info.Vertices.ToString("N0", plain));
            if (info.Triangles > 0) Fact("Triangles", info.Triangles.ToString("N0", plain));
            Fact("Indices", info.IndexBits == 0 ? "none" : $"{info.Indices.ToString("N0", plain)}, {info.IndexBits}-bit");
            Fact("Topology", info.Topology.ToString());
            Fact("Attributes", info.Attributes == MeshAttributes.None ? "positions only" : info.Attributes.ToString());

            var size = info.Max - info.Min;
            Fact("Size", string.Create(plain, $"{size.X:0.###} × {size.Y:0.###} × {size.Z:0.###}"));
        }
        else
        {
            Fact("Vertices", "still loading");
        }

        EditorRows.Close();
    }

    /// <summary>One fact, as a row with a name and a value nobody edits.</summary>
    private static void Fact(string name, string value)
    {
        EditorRows.Line(name);
        ImGui.AlignTextToFramePadding();
        ImGui.TextDisabled(value);
    }

    /// <summary>Where a mesh came from: a file, a primitive with its measures, or code.</summary>
    private static string Origin(Entity entity, AssetHandle mesh)
    {
        if (!entity.IsNone && Render.MeshPathOf(entity) is { Length: > 0 } path) return path;
        if (MeshFiles.PathOf(mesh) is { } file) return file;

        if (Render.RecipeOf(mesh) is { } recipe)
        {
            var plain = CultureInfo.InvariantCulture;
            return string.Create(plain, $"{recipe.Shape} {recipe.A:0.###}, {recipe.B:0.###}, {recipe.C:0.###}");
        }

        return "built in code";
    }

    /// <summary>How many entities of the scene are drawn with a material, the cards' own pictures left out.</summary>
    private static int Users(EcsWorld world, AssetHandle material) =>
        world.All().Count(entity =>
            !PreviewRenderer.Owns(entity) && Render.IsDrawn(entity) && Render.MaterialOf(world, entity) == material);

    /// <summary>The fields of a material's settings, over the material itself.</summary>
    private static ComponentSchema SchemaOf(AssetHandle material)
    {
        if (Materials.TryGetValue(material, out var known)) return known;

        var fields = new List<ComponentField>
        {
            Field("Base color", FieldKind.Color, settings => Linear(settings.BaseColor), (settings, value) =>
            {
                if (value is not Color color) return false;
                settings.BaseColor = (color.R, color.G, color.B, color.A);
                return true;
            }),
            Field("Metallic", FieldKind.Float, settings => settings.Metallic, (settings, value) => Number(value, number => settings.Metallic = number), Unit),
            Field("Roughness", FieldKind.Float, settings => settings.Roughness, (settings, value) => Number(value, number => settings.Roughness = number), Unit),
            Field("Emissive", FieldKind.Color, settings => Linear(settings.Emissive), (settings, value) =>
            {
                if (value is not Color color) return false;
                settings.Emissive = (color.R, color.G, color.B, color.A);
                return true;
            }),
            Field("Alpha", FieldKind.Enum, settings => settings.AlphaMode.ToString(), (settings, value) =>
            {
                if (!Enum.TryParse<AlphaMode>(value.ToString(), out var mode)) return false;
                settings.AlphaMode = mode;
                return true;
            }, options: Enum.GetNames<AlphaMode>()),
            Field("Double-sided", FieldKind.Bool, settings => settings.DoubleSided, (settings, value) =>
            {
                if (value is not bool on) return false;
                settings.DoubleSided = on;
                return true;
            }),
            Field("Unlit", FieldKind.Bool, settings => settings.Unlit, (settings, value) =>
            {
                if (value is not bool on) return false;
                settings.Unlit = on;
                return true;
            }),

            // The texture slots, each picked in the grid from the images under the asset root and
            // inside models, named short enough for the name column, with what each is in full on
            // the name's tooltip.
            Texture("Color map", "The base color texture, multiplied by the base color.", settings => settings.BaseColorTexture, (settings, map) => settings.BaseColorTexture = map),
            Texture("Normal map", "Bends the lighting across the surface without changing its shape.", settings => settings.NormalMap, (settings, map) => settings.NormalMap = map),
            Texture("Metal map", "Metallic in the blue channel and roughness in the green, as glTF packs them.", settings => settings.MetallicRoughnessTexture, (settings, map) => settings.MetallicRoughnessTexture = map),
            Texture("Glow map", "The emissive texture, multiplied by the emissive color.", settings => settings.EmissiveTexture, (settings, map) => settings.EmissiveTexture = map),
            Texture("AO map", "Ambient occlusion, darkening the creases light reaches least.", settings => settings.OcclusionTexture, (settings, map) => settings.OcclusionTexture = map),
        };

        var schema = new ComponentSchema("Material", "Bevy.StandardMaterial", static () => -1, fields);
        Materials[material] = schema;
        return schema;

        // A slot holding an image, or no image at all.
        ComponentField Texture(string name, string says, Func<MaterialSettings, AssetHandle> read, Action<MaterialSettings, AssetHandle> write) =>
            Field(name, FieldKind.Asset, settings => read(settings), (settings, value) =>
            {
                if (value is not AssetHandle map) return false;
                write(settings, map);
                return true;
            }, new FieldHints(Tooltip: says, Asset: AssetKind.Image));

        // Each field reads the material whole and writes it back whole, since the bridge takes a
        // material's settings together, and a write changes the one setting it names.
        ComponentField Field(
            string name,
            FieldKind kind,
            Func<MaterialSettings, object> read,
            Func<MaterialSettings, object, bool> write,
            FieldHints? hints = null,
            IReadOnlyList<string>? options = null) =>
            new(
                name,
                kind,
                kind.ToString(),
                (_, _) => Render.TryReadMaterial(material, out var settings) && settings is not null ? read(settings) : null,
                (_, _, value) =>
                {
                    if (!Render.TryReadMaterial(material, out var settings) || settings is null) return false;
                    if (!write(settings, value) || !Render.WriteMaterial(material, settings)) return false;

                    // A material from a file is written back to it, so the change outlives the run
                    // and reaches every scene using the file.
                    MaterialFiles.Save(material);
                    return true;
                },
                options,
                hints);
    }

    /// <summary>The measures of each primitive, by handle, kept so one schema is not made a frame.</summary>
    private static readonly Dictionary<AssetHandle, (string Shape, ComponentSchema Schema)> Measures = [];

    /// <summary>What each primitive calls its measures, in the order the bridge takes them.</summary>
    private static readonly Dictionary<string, string[]> MeasureNames = new(StringComparer.Ordinal)
    {
        [MeshShape.Cuboid] = ["Width", "Height", "Depth"],
        [MeshShape.Sphere] = ["Radius"],
        [MeshShape.Plane] = ["Width", "Depth"],
        [MeshShape.Capsule] = ["Radius", "Length"],
        [MeshShape.Cylinder] = ["Radius", "Height"],
        [MeshShape.Cone] = ["Radius", "Height"],
        [MeshShape.ConicalFrustum] = ["Top radius", "Bottom radius", "Height"],
        [MeshShape.Torus] = ["Inner radius", "Outer radius"],
        [MeshShape.Circle] = ["Radius"],
        [MeshShape.Annulus] = ["Inner radius", "Outer radius"],
        [MeshShape.Rectangle] = ["Width", "Height"],
        [MeshShape.Triangle] = ["Size"],
        [MeshShape.Tetrahedron] = ["Size"],
    };

    /// <summary>
    /// A primitive's measures as fields, each rebuilding the mesh in place when written, or nothing
    /// for a mesh that is not a primitive.
    /// </summary>
    /// <remarks>
    /// Fields over the mesh's recipe, so they are drawn, dragged and undone as a component's are.
    /// Kept per handle and shape, since a mesh picked as another shape needs other names.
    /// </remarks>
    private static ComponentSchema? MeasuresOf(AssetHandle mesh)
    {
        if (Render.RecipeOf(mesh) is not { } recipe || !MeasureNames.TryGetValue(recipe.Shape, out var names)) return null;
        if (Measures.TryGetValue(mesh, out var known) && known.Shape == recipe.Shape) return known.Schema;

        var fields = names.Select((name, index) => new ComponentField(
            name,
            FieldKind.Float,
            "float",
            (_, _) => Render.RecipeOf(mesh) is { } held ? index switch { 0 => held.A, 1 => held.B, _ => held.C } : null,
            (_, _, value) =>
            {
                if (Render.RecipeOf(mesh) is not { } held || !ComponentSchemas.TryCoerce<float>(value, out var number)) return false;

                // Kept above nothing, since a shape with no size has no faces to draw.
                number = MathF.Max(0.001f, number);
                var (a, b, c) = index switch
                {
                    0 => (number, held.B, held.C),
                    1 => (held.A, number, held.C),
                    _ => (held.A, held.B, number),
                };

                return Render.RebuildMesh(mesh, held.Shape, a, b, c);
            },
            hints: new FieldHints(Minimum: 0.001d, Step: 0.01d))).ToList();

        var schema = new ComponentSchema("Measures", "Bevy.MeshMeasures", static () => -1, fields);
        Measures[mesh] = (recipe.Shape, schema);
        return schema;
    }

    /// <summary>A slider from nothing to all, as metallic and roughness are.</summary>
    private static readonly FieldHints Unit = new(Minimum: 0d, Maximum: 1d);

    private static Color Linear((float R, float G, float B, float A) color) => new(color.R, color.G, color.B, color.A);

    private static bool Number(object value, Action<float> set)
    {
        if (!ComponentSchemas.TryCoerce<float>(value, out var number)) return false;
        set(number);
        return true;
    }
}
