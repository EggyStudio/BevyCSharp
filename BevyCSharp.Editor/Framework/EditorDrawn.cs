using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What an entity is drawn with, which is the engine's own rather than this project's.
/// </summary>
/// <remarks>
/// <para>
/// A mesh and a material are Bevy components holding typed handles, so they have no schema and the
/// inspector cannot draw them the way it draws everything else. What they can be asked is where
/// they came from, and what they can be told is to point somewhere different, which is enough for
/// the thing a person actually wants from a panel.
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
    /// A glTF holds many assets and a path alone names none of them, so a label picks one out.
    /// The first primitive of the first mesh is what a file exported from a modeling tool as one
    /// object holds, which is most of them. A file with several needs the label written by hand.
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

        var mesh = Render.MeshPathOf(entity);
        var material = Render.MaterialPathOf(entity);

        // Neither is not a mistake. Most entities are not drawn, and a heading over nothing says
        // the panel is missing something rather than that the entity is not a model. An entity
        // drawn with something made here has no path either, so what decides is whether the
        // engine answered at all rather than what it answered.
        if (!Render.IsDrawn(entity)) return;

        EditorSurface.Heading("Drawn with", DetailsPanel.Inset);

        Row(ctx, entity, "Mesh", mesh, AssetKind.Mesh, FirstMesh);

        if (Shaders.ProgramOn(entity) is { IsValid: true } program)
        {
            Shader(entity, program);
        }
        else
        {
            Row(ctx, entity, "Material", material, AssetKind.StandardMaterial, FirstMaterial);
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
    /// than from the GPU, which is what an unset name reading as zero depends on.
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

    /// <summary>Whether a name reads as a color, which is what earns it a color picker.</summary>
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

    /// <summary>One row, showing where it came from and offering somewhere else.</summary>
    private static void Row(
        BehaviorContext ctx,
        Entity entity,
        string title,
        string path,
        string kind,
        string label)
    {
        var across = new System.Numerics.Vector2(
            MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.Inset),
            0f);

        if (!EditorRows.Open($"##drawn{title}", across)) return;

        EditorRows.Line(title);

        // The label is dropped from what is shown, because a person reading a row wants to know
        // which file it is and the part after the hash is how the file is addressed rather than
        // what it is called.
        var shown = path.Length == 0
            ? "made here"
            : path.Split('#')[0];

        ImGui.PushID($"##drawn{title}");

        EditorWidgets.Picking($"##pick{title}", shown, () =>
        {
            foreach (var file in EditorAssets.Every(EditorAssets.ExtensionsFor(kind).ToArray()))
            {
                var picked = path.StartsWith(file, StringComparison.Ordinal);

                if (ImGui.Selectable(file, picked)) Point(ctx, entity, title, file, kind, label);

                RoundedRows.Row(picked);
            }
        });

        ImGui.PopID();

        EditorRows.Close();
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

        var handle = AssetServer.Load(kind, glb ? file + label : file);

        if (title == "Mesh") Render.SetMesh(ctx.Ecs, entity, handle);
        else Render.SetMaterial(ctx.Ecs, entity, handle);
    }
}
