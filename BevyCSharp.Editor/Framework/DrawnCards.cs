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
            if (PreviewRenderer.Show(ctx, MeshKey, new PreviewSubject.Mesh(mesh)))
                PreviewRenderer.Draw(MeshKey, Side(), turnable: true);

            var wire = PreviewRenderer.Wireframe(MeshKey);
            if (Toggle("Wireframe", ref wire)) PreviewRenderer.SetWireframe(MeshKey, wire);

            MeshActions(ctx.Ecs, entity, mesh);

            Facts(entity, mesh);
            ImGui.TreePop();
        }

        ImGui.Unindent();
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
            if (PreviewRenderer.Show(ctx, MaterialKey, new PreviewSubject.Material(material)))
                PreviewRenderer.Draw(MaterialKey, Side(), turnable: true);

            Actions(ctx, entity, material, file, users);

            var schema = SchemaOf(material);
            foreach (var field in schema.Fields) ComponentFields.Row(ctx, entity, schema, field);

            ImGui.TreePop();
        }

        ImGui.Unindent();
    }

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

        var users = world.All().Count(other => Render.IsDrawn(other) && Render.MeshOf(world, other) == mesh);
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
        if (Render.MeshPathOf(entity) is { Length: > 0 } path) return path;
        if (MeshFiles.PathOf(mesh) is { } file) return file;

        if (Render.RecipeOf(mesh) is { } recipe)
        {
            var plain = CultureInfo.InvariantCulture;
            return string.Create(plain, $"{recipe.Shape} {recipe.A:0.###}, {recipe.B:0.###}, {recipe.C:0.###}");
        }

        return "built in code";
    }

    /// <summary>How many entities are drawn with a material.</summary>
    private static int Users(EcsWorld world, AssetHandle material) =>
        world.All().Count(entity => Render.IsDrawn(entity) && Render.MaterialOf(world, entity) == material);

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
        };

        var schema = new ComponentSchema("Material", "Bevy.StandardMaterial", static () => -1, fields);
        Materials[material] = schema;
        return schema;

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
