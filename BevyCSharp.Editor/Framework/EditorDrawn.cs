using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What an entity is drawn with, which is the engine's own rather than this project's.
/// </summary>
/// <remarks>
/// <para>
/// A mesh and a material are Bevy components holding typed handles, so they have no schema and the
/// inspector cannot draw them the way it draws everything else. They can be asked where they came
/// from, and told to point somewhere different, which is enough for the thing a person actually
/// wants from a panel.
/// </para>
/// <para>
/// Anything built in memory has no path, which is every mesh <c>Render.CreateMesh</c> makes, so the
/// row says so rather than inventing a name for it.
/// </para>
/// </remarks>
internal static class EditorDrawn
{
    /// <summary>What a glTF file's first mesh is called inside it.</summary>
    /// <remarks>
    /// A glTF holds many assets and a path alone names none of them, so a label picks one out. A
    /// file exported from a modeling tool as one object holds its first primitive of the first
    /// mesh, and most files are exported that way. A file with several needs the label written by
    /// hand.
    /// </remarks>
    private const string FirstMesh = "#Mesh0/Primitive0";

    /// <summary>
    /// What its first material is called, translated into one the renderer draws with.
    /// </summary>
    /// <remarks>
    /// The <c>/std</c> half is Bevy's label for the translated material, which sits beside the raw
    /// one the glTF loader produces. Loading the raw one gives a handle the renderer cannot use.
    /// </remarks>
    private const string FirstMaterial = "#Material0/std";

    /// <summary>Draws the mesh and material rows, if the entity has either.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">What is selected.</param>
    internal static void Draw(BehaviorContext ctx, Entity entity)
    {
        if (!App.HasRenderer) return;

        var mesh = Render.MeshPathOf(entity) is { Length: > 0 } loadedMesh
            ? loadedMesh
            : MeshFiles.PathOf(Render.MeshOf(ctx.Ecs, entity)) ?? string.Empty;

        // A material file is read on this side, so the engine knows no path for it and the file
        // is asked for by the material instead.
        var material = Render.MaterialPathOf(entity) is { Length: > 0 } loaded
            ? loaded
            : MaterialFiles.PathOf(Render.MaterialOf(ctx.Ecs, entity)) ?? string.Empty;

        // Neither is not a mistake. Most entities are not drawn, and a heading over nothing says
        // the panel is missing something rather than that the entity is not a model. An entity
        // drawn with something made here has no path either, so what decides is whether the
        // engine answered at all rather than what it answered.
        if (!Render.IsDrawn(entity))
        {
            Parts(ctx, entity);
            return;
        }

        EditorSurface.Heading("Drawn with", DetailsPanel.Inset);

        Row(ctx, entity, "Mesh", mesh, AssetKind.Mesh, FirstMesh);
        DrawnCards.Mesh(ctx, entity);

        if (Shaders.ProgramOn(entity) is { IsValid: true } program)
        {
            Shader(entity, program);
        }
        else
        {
            Row(ctx, entity, "Material", material, AssetKind.StandardMaterial, FirstMaterial);
            DrawnCards.Material(ctx, entity);
        }
    }

    /// <summary>
    /// A material drawn by a shader the game wrote: which program, whether it compiled, and every
    /// value its shaders declare, which can be dragged while it draws.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A row per name the shader declares, with a widget for what it is: a drag for numbers and
    /// vectors, a color picker for a <c>float3</c> or <c>float4</c> whose name says it is a color,
    /// a box for a <c>bool</c>. What cannot be dragged (textures, buffers, samplers, structs and
    /// matrices) is listed with its kind, so the row still says the name exists.
    /// </para>
    /// <para>
    /// An array shows its first <see cref="ArrayShown"/> elements, because a thousand drags is not
    /// an inspector, and says how many more there are. Values are read back as they were set rather
    /// than from the GPU, so an unset name reads as zero.
    /// </para>
    /// </remarks>
    private static void Shader(Entity entity, ShaderProgram program)
    {
        var theme = EditorTheme.Current;

        var across = new System.Numerics.Vector2(
            MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.Inset),
            0f);

        if (EditorRows.Open("##drawnShader", across))
        {
            EditorRows.Line("Shader");

            var (word, color) = program.State switch
            {
                ShaderProgramState.Ready => ("", theme.Dim),
                ShaderProgramState.Failed => ("failed, ", theme.Bad),
                _ => ("compiling, ", theme.Warn),
            };

            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.TextUnformatted($"{word}#{program.Id} {program.Files}");
            ImGui.PopStyleColor();

            EditorRows.Close();
        }

        var material = Shaders.MaterialOn(entity);

        foreach (var parameter in material.Parameters)
        {
            if (!EditorRows.Open($"##shader_{parameter.Name}", across)) continue;

            EditorRows.Line(parameter.Count > 1 ? $"{parameter.Name}[{parameter.Count}]" : parameter.Name);
            ImGui.SetNextItemWidth(-1f);

            if (parameter.Kind == ShaderParameterKind.Number && parameter.Components <= 4)
            {
                Numbers(material, parameter, theme);
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, theme.Dim);
                ImGui.TextUnformatted(Describe(parameter));
                ImGui.PopStyleColor();
            }

            EditorRows.Close();
        }
    }

    /// <summary>How many elements of an array the inspector offers to drag.</summary>
    private const int ArrayShown = 8;

    /// <summary>A drag, a color or a box per element of a number, a vector or an array of them.</summary>
    private static void Numbers(ShaderMaterial material, ShaderParameter parameter, EditorTheme theme)
    {
        var shown = Math.Min(parameter.Count, ArrayShown);
        var components = parameter.Components;
        var width = components * shown;

        if (parameter.Scalar == ShaderScalar.Float)
        {
            var values = Fill(material.GetFloats(parameter.Name), width);
            var colorish = components >= 3 && LooksLikeColor(parameter.Name);
            var changed = false;

            for (var element = 0; element < shown; element++)
            {
                var at = element * components;
                var id = $"##{parameter.Name}_{element}";

                if (shown > 1) ImGui.SetNextItemWidth(-1f);

                changed |= components switch
                {
                    1 => ImGui.DragFloat(id, ref values[at], 0.01f),
                    2 => Drag2(id, values, at),
                    3 when colorish => Color3(id, values, at),
                    3 => Drag3(id, values, at),
                    _ when colorish => Color4(id, values, at),
                    _ => Drag4(id, values, at),
                };
            }

            if (changed) material.Set(parameter.Name, Trim(values, components));
        }
        else if (parameter.Scalar == ShaderScalar.Bool && components == 1 && shown == 1)
        {
            var values = Fill(material.GetUInts(parameter.Name), 1);
            var on = values[0] != 0;

            if (ImGui.Checkbox($"##{parameter.Name}", ref on)) material.Set(parameter.Name, on);
        }
        else
        {
            var values = Fill(material.GetInts(parameter.Name), width);
            var changed = false;

            for (var element = 0; element < shown; element++)
            {
                var at = element * components;
                if (shown > 1) ImGui.SetNextItemWidth(-1f);

                for (var c = 0; c < components; c++)
                {
                    if (components > 1)
                    {
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X / (components - c));
                        if (c > 0) ImGui.SameLine();
                    }

                    changed |= ImGui.DragInt($"##{parameter.Name}_{element}_{c}", ref values[at + c]);
                }
            }

            if (changed)
            {
                var trimmed = Trim(values, components);

                if (parameter.Scalar == ShaderScalar.UInt || parameter.Scalar == ShaderScalar.Bool)
                {
                    var words = Array.ConvertAll(trimmed, value => unchecked((uint)Math.Max(0, value)));
                    material.SetNumbers(parameter.Name, components, words);
                }
                else
                {
                    material.SetNumbers(parameter.Name, components, trimmed);
                }
            }
        }

        if (parameter.Count > shown)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, theme.Dim);
            ImGui.TextUnformatted($"and {parameter.Count - shown} more, set from code");
            ImGui.PopStyleColor();
        }
    }

    /// <summary>Whether a name reads as a color, which earns it a color picker.</summary>
    private static bool LooksLikeColor(string name)
    {
        var lower = name.ToLowerInvariant();
        return lower.Contains("color") || lower.Contains("color") || lower.Contains("tint")
            || lower.Contains("albedo") || lower.Contains("glow") || lower.Contains("emissive");
    }

    /// <summary>What a name is, for one the inspector cannot drag.</summary>
    private static string Describe(ShaderParameter parameter)
    {
        var what = parameter.Kind switch
        {
            ShaderParameterKind.Texture => "texture",
            ShaderParameterKind.Image => "image written",
            ShaderParameterKind.Buffer => "buffer",
            ShaderParameterKind.Sampler => "sampler",
            ShaderParameterKind.Number => "matrix",
            _ => "struct",
        };

        return parameter.Count > 1 ? $"{parameter.Count} of {what}" : what;
    }

    /// <summary>What was set, padded with zeros to what is shown.</summary>
    private static T[] Fill<T>(T[] set, int length) where T : unmanaged
    {
        if (set.Length >= length) return set;

        var filled = new T[length];
        set.CopyTo(filled, 0);
        return filled;
    }

    /// <summary>
    /// Drops what would not fit the parameter's elements, which a value set longer than the shader
    /// now declares would otherwise have refused.
    /// </summary>
    private static T[] Trim<T>(T[] values, int components) where T : unmanaged =>
        values[..(values.Length / components * components)];

    private static bool Drag2(string id, float[] values, int at)
    {
        var v = new System.Numerics.Vector2(values[at], values[at + 1]);
        if (!ImGui.DragFloat2(id, ref v, 0.01f)) return false;
        (values[at], values[at + 1]) = (v.X, v.Y);
        return true;
    }

    private static bool Drag3(string id, float[] values, int at)
    {
        var v = new System.Numerics.Vector3(values[at], values[at + 1], values[at + 2]);
        if (!ImGui.DragFloat3(id, ref v, 0.01f)) return false;
        (values[at], values[at + 1], values[at + 2]) = (v.X, v.Y, v.Z);
        return true;
    }

    private static bool Drag4(string id, float[] values, int at)
    {
        var v = new System.Numerics.Vector4(values[at], values[at + 1], values[at + 2], values[at + 3]);
        if (!ImGui.DragFloat4(id, ref v, 0.01f)) return false;
        (values[at], values[at + 1], values[at + 2], values[at + 3]) = (v.X, v.Y, v.Z, v.W);
        return true;
    }

    private static bool Color3(string id, float[] values, int at)
    {
        var v = new System.Numerics.Vector3(values[at], values[at + 1], values[at + 2]);
        if (!ImGui.ColorEdit3(id, ref v, ImGuiColorEditFlags.Float | ImGuiColorEditFlags.HDR)) return false;
        (values[at], values[at + 1], values[at + 2]) = (v.X, v.Y, v.Z);
        return true;
    }

    private static bool Color4(string id, float[] values, int at)
    {
        var v = new System.Numerics.Vector4(values[at], values[at + 1], values[at + 2], values[at + 3]);
        if (!ImGui.ColorEdit4(id, ref v, ImGuiColorEditFlags.Float | ImGuiColorEditFlags.HDR)) return false;
        (values[at], values[at + 1], values[at + 2], values[at + 3]) = (v.X, v.Y, v.Z, v.W);
        return true;
    }

    /// <summary>
    /// The material of each part of a mesh with several, for an entity that is not drawn itself
    /// but whose children are, as a glTF node's are.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy spawns a glTF mesh as an entity for the node and a child for each of its primitives,
    /// each drawn with its own mesh and material and named after them, so a model whose hull and
    /// glass are one mesh in the modeling tool is two entities here. Selecting the node showed
    /// nothing drawn, and changing the glass meant finding the right child in the hierarchy. This
    /// lists the children that are drawn, a material row each under the child's name, as a
    /// renderer with several materials lists them in Unity.
    /// </para>
    /// <para>
    /// The material differs from part to part, so it is the row. A part's mesh and its card
    /// are the child's, shown when the child is selected.
    /// </para>
    /// </remarks>
    private static void Parts(BehaviorContext ctx, Entity entity)
    {
        var parts = ctx.Ecs.ChildrenOf(entity).Where(Render.IsDrawn).ToArray();
        if (parts.Length == 0) return;

        EditorSurface.Heading(parts.Length == 1 ? "Drawn with, in its part" : $"Drawn with, in {parts.Length} parts", DetailsPanel.Inset);

        foreach (var part in parts)
        {
            var material = Render.MaterialPathOf(part) is { Length: > 0 } loaded
                ? loaded
                : MaterialFiles.PathOf(Render.MaterialOf(ctx.Ecs, part)) ?? string.Empty;

            ImGui.PushID((int)part.Index);
            Row(ctx, part, "Material", material, AssetKind.StandardMaterial, FirstMaterial, ctx.Ecs.NameOf(part) ?? $"Part {part.Index}");
            ImGui.PopID();
        }
    }

    /// <summary>
    /// One row, showing where it came from and offering somewhere else, called by its title or, for
    /// a part of a model, by <c>named</c>.
    /// </summary>
    private static void Row(
        BehaviorContext ctx,
        Entity entity,
        string title,
        string path,
        string kind,
        string label,
        string? named = null)
    {
        var across = new System.Numerics.Vector2(
            MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.Inset),
            0f);

        if (!EditorRows.Open($"##drawn{title}", across)) return;

        EditorRows.Line(named ?? title);

        // A part of a model is shown by the name the model gives it and the file, since the label
        // after the hash is how the part is addressed rather than what it is called, and two parts
        // of one file would otherwise read the same.
        var shown = path.Length == 0
            ? "made here"
            : path.Contains('#', StringComparison.Ordinal) ? EditorAssets.NameOf(path) : path;

        ImGui.PushID($"##drawn{title}");

        // A window of its own rather than a dropdown, since what can be picked is three lists (the
        // shapes the engine makes, what the scene already uses and files) and a search over them.
        if (ImGui.Button($"{shown}##pick{title}", new System.Numerics.Vector2(-1f, 0f)))
        {
            var mesh = title == "Mesh";
            PickerWindow.Open(
                mesh ? "Pick a mesh" : "Pick a material",
                () => mesh ? Meshes(ctx, entity, kind, label) : Materials(ctx, entity, kind, label),
                "Nothing to pick",
                "Pick",
                grid: true);
        }

        ImGui.PopID();

        EditorRows.Close();
    }

    /// <summary>
    /// What a mesh can be: one of the engine's shapes, a mesh the scene already draws with, or a
    /// model file's first mesh.
    /// </summary>
    /// <remarks>
    /// A shape is made fresh at a size that reads at a glance, and its card edits nothing yet.
    /// Picking a mesh the scene already uses shares it, as a scene file writes it once for every
    /// entity drawn with it.
    /// </remarks>
    private static IReadOnlyList<PickerItem> Meshes(BehaviorContext ctx, Entity entity, string kind, string label)
    {
        var items = new List<PickerItem>();

        // Pictured as a file is, though none of these has one, so the grid shows a shape rather than
        // the same icon thirteen times. A shape is made once to be pictured, when its turn comes.
        foreach (var (shape, a, b, c) in Shapes)
        {
            items.Add(new PickerItem(
                Spaced(shape),
                EditorIcons.Mesh,
                pick => Give(pick.Ecs, entity, "Mesh", Render.CreateMesh(shape, a, b, c)),
                "Built in",
                Drawn: () => Thumbnails.Drawn($"shape:{shape}", () => new PreviewSubject.Mesh(Render.CreateMesh(shape, a, b, c)))));
        }

        foreach (var (handle, name) in InScene(ctx.Ecs, entity, Render.MeshOf, mesh => Render.RecipeOf(mesh) is { } recipe
            ? Spaced(recipe.Shape)
            : null))
        {
            // Named with its measures, so a shape resized in its card is pictured again.
            items.Add(new PickerItem(
                name,
                EditorIcons.Mesh,
                pick => Give(pick.Ecs, entity, "Mesh", handle),
                "In this scene",
                Drawn: () => Thumbnails.Drawn($"mesh:{handle}:{Render.RecipeOf(handle)}", () => new PreviewSubject.Mesh(handle))));
        }

        foreach (var file in EditorAssets.Every([".json"]).Where(MeshFiles.IsMeshFile))
            items.Add(new PickerItem(file, EditorIcons.Mesh, pick => Give(pick.Ecs, entity, "Mesh", MeshFiles.Load(file)), "Files", file));

        foreach (var file in EditorAssets.Every(EditorAssets.ExtensionsFor(kind).ToArray()))
            items.AddRange(Parts(entity, "Mesh", file, kind, label));

        return items;
    }

    /// <summary>What a material can be: a new one, one the scene already draws with, or a model file's first.</summary>
    private static IReadOnlyList<PickerItem> Materials(BehaviorContext ctx, Entity entity, string kind, string label)
    {
        var items = new List<PickerItem>
        {
            new(
                "New material",
                EditorIcons.Add,
                pick => Give(pick.Ecs, entity, "Material", Render.CreateMaterial(new MaterialSettings())),
                "Built in",
                Drawn: () => Thumbnails.Drawn("material:new", () => new PreviewSubject.Material(Render.CreateMaterial(new MaterialSettings())))),
        };

        foreach (var (handle, name) in InScene(ctx.Ecs, entity, Render.MaterialOf, material =>
            Render.TryReadMaterial(material, out var settings) && settings is not null
                ? Hex(settings.BaseColor)
                : null))
        {
            // Named with what it looks like, so a material changed in its card is pictured again.
            items.Add(new PickerItem(
                name,
                EditorIcons.Data,
                pick => Give(pick.Ecs, entity, "Material", handle),
                "In this scene",
                Drawn: () => Thumbnails.Drawn($"material:{handle}:{Look(handle)}", () => new PreviewSubject.Material(handle))));
        }

        foreach (var file in EditorAssets.Every([".json"]).Where(MaterialFiles.IsMaterialFile))
            items.Add(new PickerItem(file, EditorIcons.Image, pick => Give(pick.Ecs, entity, "Material", MaterialFiles.Load(file)), "Files", file));

        foreach (var file in EditorAssets.Every(EditorAssets.ExtensionsFor(kind).ToArray()))
            items.AddRange(Parts(entity, "Material", file, kind, label));

        return items;
    }

    /// <summary>
    /// What a model file offers a mesh or a material row: each of its meshes or materials by the
    /// name the file gives it, or the file alone when it lists none, such as an OBJ.
    /// </summary>
    private static IEnumerable<PickerItem> Parts(Entity entity, string title, string file, string kind, string label)
    {
        var wanted = title == "Mesh" ? AssetKind.Mesh : AssetKind.StandardMaterial;
        var parts = GltfContents.Read(file)?.Where(part => part.Kind == wanted).ToList() ?? [];

        if (parts.Count == 0)
        {
            yield return new PickerItem(file, EditorIcons.File, pick => Point(pick, entity, title, file, kind, label), "Files");
            yield break;
        }

        foreach (var part in parts)
        {
            var path = part.PathIn(file);
            yield return new PickerItem(
                $"{part.Name} in {file}",
                title == "Mesh" ? EditorIcons.Mesh : EditorIcons.Image,
                pick => Give(pick.Ecs, entity, title, AssetServer.Load(kind, path)),
                "Files",
                path);
        }
    }

    /// <summary>
    /// The meshes or materials the scene draws with, other than the entity's own, each once, named
    /// by the file it came from, by what it was made as, or by the first entity using it.
    /// </summary>
    private static IEnumerable<(AssetHandle Handle, string Name)> InScene(
        EcsWorld world, Entity entity, Func<EcsWorld, Entity, AssetHandle> of, Func<AssetHandle, string?> made)
    {
        var own = of(world, entity);
        var seen = new HashSet<AssetHandle>();

        foreach (var user in world.All())
        {
            if (PreviewRenderer.Owns(user) || EditorEntity.IsInterface(world, user) || !Render.IsDrawn(user)) continue;

            var handle = of(world, user);
            if (!handle.IsValid || handle == own || !seen.Add(handle)) continue;

            var called = AssetServer.PathOf(handle) ?? made(handle) ?? "made here";
            yield return (handle, $"{called}, on {world.NameOf(user) ?? "an unnamed entity"}");
        }
    }

    /// <summary>Points the entity at a mesh or a material, as one step to undo.</summary>
    private static void Give(EcsWorld world, Entity entity, string title, AssetHandle handle)
    {
        var mesh = title == "Mesh";
        var was = mesh ? Render.MeshOf(world, entity) : Render.MaterialOf(world, entity);

        Set(world, handle);
        EditorHistory.Record($"{title.ToLowerInvariant()} picked", undo => Set(undo, was), redo => Set(redo, handle));

        void Set(EcsWorld on, AssetHandle to)
        {
            if (!to.IsValid) return;
            if (mesh) Render.SetMesh(on, entity, to);
            else Render.SetMaterial(on, entity, to);
        }
    }

    /// <summary>What a material looks like in a few words, which changes when its picture would.</summary>
    private static string Look(AssetHandle material) =>
        Render.TryReadMaterial(material, out var settings) && settings is not null
            ? $"{settings.BaseColor}{settings.Metallic}{settings.Roughness}{settings.Emissive}{settings.AlphaMode}{settings.Unlit}"
              + $"{settings.BaseColorTexture}{settings.NormalMap}{settings.EmissiveTexture}"
            : string.Empty;

    /// <summary>A linear color as the sRGB hex a color picker shows, to tell materials apart by.</summary>
    private static string Hex((float R, float G, float B, float A) linear)
    {
        var shown = new Color(linear.R, linear.G, linear.B, linear.A).ToSrgb();
        static int Byte(float channel) => (int)MathF.Round(Math.Clamp(channel, 0f, 1f) * 255f);
        return $"#{Byte(shown.X):x2}{Byte(shown.Y):x2}{Byte(shown.Z):x2}";
    }

    /// <summary>Bevy's shapes, each at a size that reads at a glance.</summary>
    private static readonly (string Shape, float A, float B, float C)[] Shapes =
    [
        (MeshShape.Cuboid, 1f, 1f, 1f),
        (MeshShape.Sphere, 0.5f, 1f, 1f),
        (MeshShape.Plane, 1f, 1f, 1f),
        (MeshShape.Capsule, 0.25f, 0.5f, 1f),
        (MeshShape.Cylinder, 0.5f, 1f, 1f),
        (MeshShape.Cone, 0.5f, 1f, 1f),
        (MeshShape.ConicalFrustum, 0.25f, 0.5f, 1f),
        (MeshShape.Torus, 0.25f, 0.5f, 1f),
        (MeshShape.Circle, 0.5f, 1f, 1f),
        (MeshShape.Annulus, 0.25f, 0.5f, 1f),
        (MeshShape.Rectangle, 1f, 1f, 1f),
        (MeshShape.Triangle, 1f, 1f, 1f),
        (MeshShape.Tetrahedron, 1f, 1f, 1f),
    ];

    /// <summary>A shape's name as words, "Conical frustum" for <c>ConicalFrustum</c>.</summary>
    private static string Spaced(string shape)
    {
        var words = new System.Text.StringBuilder();
        foreach (var letter in shape)
        {
            if (char.IsUpper(letter) && words.Length > 0) words.Append(' ').Append(char.ToLowerInvariant(letter));
            else words.Append(letter);
        }

        return words.ToString();
    }

    /// <summary>Points the entity at a file, naming a part of it where the format holds many.</summary>
    private static void Point(
        BehaviorContext ctx,
        Entity entity,
        string title,
        string file,
        string kind,
        string label)
    {
        var glb = file.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase)
                  || file.EndsWith(".glb", StringComparison.OrdinalIgnoreCase);

        Give(ctx.Ecs, entity, title, AssetServer.Load(kind, glb ? file + label : file));
    }
}
