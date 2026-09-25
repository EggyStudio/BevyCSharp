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
    /// The first primitive of the first mesh is what a file exported from a modelling tool as one
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
    /// A material drawn by a shader the game wrote: which program, whether it compiled, and its
    /// numbers, which can be dragged while it draws.
    /// </summary>
    /// <remarks>
    /// The numbers are sixteen rows of four, as the shader reads them, and only as many rows are
    /// shown as reach the last one that is not zero, plus one to grow into, so a material using
    /// four floats is one row and not sixteen. What each number means is the shader's to say, so
    /// the rows are numbered rather than named.
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

        var values = Shaders.GetParameters(entity);

        var last = Array.FindLastIndex(values, value => value != 0f);
        var rows = Math.Min(Shaders.ParameterCount / 4, (last / 4) + 2);

        for (var row = 0; row < rows; row++)
        {
            if (!EditorRows.Open($"##shaderRow{row}", across)) continue;

            EditorRows.Line($"Row {row}");

            var four = new System.Numerics.Vector4(
                values[row * 4],
                values[(row * 4) + 1],
                values[(row * 4) + 2],
                values[(row * 4) + 3]);

            ImGui.SetNextItemWidth(-1f);

            if (ImGui.DragFloat4($"##shaderValues{row}", ref four, 0.01f))
            {
                Shaders.SetParameters(entity, [four.X, four.Y, four.Z, four.W], row * 4);
            }

            EditorRows.Close();
        }
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
