using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What is selected, and everything known about it.
/// </summary>
/// <remarks>
/// <para>
/// A component's fields come from the schema the generator emitted, so nothing here names a type
/// and nothing reflects at runtime. What a field is drawn as follows from its kind and from the
/// hints its attributes declared.
/// </para>
/// <para>
/// Bevy's own components are not shown as rows: an entity carries a dozen of them and not one is
/// something a person edits. The ones worth knowing about are named as tags under the entity, which
/// is also where a component of this project's own with no fields at all ends up.
/// </para>
/// </remarks>
public static class DetailsPanel
{
    /// <summary>Draws the selection.</summary>
    public static void Draw()
    {
        if (EditorShell.Context is not { } ctx) return;

        ImGui.TextDisabled("DETAILS");
        ImGui.Separator();

        if (!EditorSelection.Any)
        {
            ImGui.Spacing();
            ImGui.TextDisabled("Nothing selected");
            return;
        }

        var entity = EditorSelection.Current;
        var name = ctx.Ecs.NameOf(entity) ?? $"Entity {entity.Index}";

        ImGui.SetNextItemWidth(-1f);
        var renamed = name;

        if (ImGui.InputText("##name", ref renamed, 128, ImGuiInputTextFlags.EnterReturnsTrue)
            && renamed.Length > 0)
        {
            ctx.Ecs.SetName(entity, renamed);
        }

        Tags(ctx, entity);

        ImGui.Spacing();

        if (!ImGui.BeginChild("##components", new Vector2(0f, 0f), ImGuiChildFlags.NavFlattened))
        {
            ImGui.EndChild();
            return;
        }

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is not { } schema) continue;
            if (schema.Fields.Count == 0) continue;

            // A card per component, tall enough for what is in it. What separates one from the next
            // is the gap and the step in fill, the same as everywhere else in the editor.
            //
            // Only under the editor's own look. The stock one is ImGui's decisions taken whole, and
            // a colour of ours pushed into it is exactly the kind of half-measure that makes a
            // theme look like two themes.
            var card = !EditorTheme.Current.Stock;

            if (card)
            {
                ImGui.PushStyleColor(ImGuiCol.ChildBg, EditorTheme.Alpha(
                    EditorTheme.Current.Hover,
                    EditorTheme.Current.PanelAlpha * 0.5f));
            }

            ImGui.BeginChild(
                $"##card{schema.Name}",
                new Vector2(0f, 0f),
                ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.NavFlattened);

            var open = ImGui.CollapsingHeader(schema.Name, ImGuiTreeNodeFlags.DefaultOpen);

            // A component's own menu, where taking it off lives. On the header, because that is
            // the thing the component is.
            if (ImGui.BeginPopupContextItem($"##menu{schema.Name}"))
            {
                if (ImGui.MenuItem("Remove", string.Empty, false, schema.CanAdd))
                {
                    schema.Remove(ctx.Ecs, entity);
                }

                ImGui.EndPopup();
            }

            if (open)
            {
                foreach (var field in schema.Fields)
                {
                    if (field.Hints.Hidden) continue;

                    Row(ctx, entity, schema, field);
                }

                foreach (var method in schema.Methods)
                {
                    if (ImGui.Button(method.Title)) method.Run(ctx.Ecs, entity);
                    ImGui.SameLine();
                }

                if (schema.Methods.Count > 0) ImGui.NewLine();
            }

            ImGui.EndChild();

            if (card) ImGui.PopStyleColor();

            ImGui.Spacing();
        }

        Add(ctx, entity);

        ImGui.EndChild();
    }

    /// <summary>
    /// What can be put on the entity that is not on it already.
    /// </summary>
    /// <remarks>
    /// Only what the generator emitted a way to add, which is every <c>[Behavior]</c> struct: the
    /// engine's own components need a byte-compatible mirror on this side and cannot be built from
    /// a name.
    /// </remarks>
    private static void Add(BehaviorContext ctx, Entity entity)
    {
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var room = ImGui.GetContentRegionAvail().X;

        if (ImGui.Button("Add Component", new Vector2(room, 26f))) ImGui.OpenPopup("##add");

        if (!ImGui.BeginPopup("##add")) return;

        var carried = new HashSet<string>();

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is { } schema) carried.Add(schema.Name);
        }

        foreach (var schema in ComponentSchemas.All)
        {
            if (!schema.CanAdd || carried.Contains(schema.Name)) continue;

            if (ImGui.MenuItem(schema.Name)) schema.Add(ctx.Ecs, entity);
        }

        ImGui.EndPopup();
    }

    /// <summary>
    /// What the entity is, as tags.
    /// </summary>
    /// <remarks>
    /// A component with nothing to edit still says something about the thing carrying it, and a row
    /// with no value in it says it badly. A tag is the shape that fits: a word, wrapped into as many
    /// lines as it takes.
    /// </remarks>
    private static void Tags(BehaviorContext ctx, Entity entity)
    {
        var room = ImGui.GetContentRegionAvail().X;
        var used = 0f;

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            var schema = ComponentSchemas.For(id);

            // Something of this project's own with no fields, or one of Bevy's worth naming.
            if (schema is { Fields.Count: > 0 }) continue;

            // What the engine works out for itself says nothing about the thing being edited.
            if (EditorEntity.IsDerived(ctx.Ecs, id)) continue;

            var label = schema?.Name ?? Short(ctx.Ecs.ComponentName(id));
            if (label.Length == 0) continue;

            var width = ImGui.CalcTextSize(label).X + (ImGui.GetStyle().FramePadding.X * 2f) + 8f;

            if (used > 0f && used + width < room) ImGui.SameLine();
            else used = 0f;

            used += width + ImGui.GetStyle().ItemSpacing.X;

            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.FrameBg));
            ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));
            ImGui.SmallButton(label);
            ImGui.PopStyleColor(2);
        }
    }

    /// <summary>One field, drawn as what it is.</summary>
    private static void Row(
        BehaviorContext ctx, Entity entity, ComponentSchema schema, ComponentField field)
    {
        var id = $"##{schema.Name}.{field.Name}";
        var value = field.Read(ctx.Ecs, entity);

        if (field.Hints.Header is { Length: > 0 } heading) ImGui.SeparatorText(heading);

        ImGui.PushID(id);
        ImGui.Columns(2, "##row", false);
        ImGui.SetColumnWidth(0, 110f);

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(field.Title);

        if (field.Hints.Tooltip is { Length: > 0 } why && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(why);
        }

        ImGui.NextColumn();
        ImGui.SetNextItemWidth(-1f);

        var editable = field.IsWritable;
        if (!editable) ImGui.BeginDisabled();

        switch (field.Kind)
        {
            case FieldKind.Bool:
            {
                var on = value is true;
                if (ImGui.Checkbox(id, ref on)) field.Write(ctx.Ecs, entity, on);
                break;
            }

            case FieldKind.Vec3:
            {
                var vector = value as Vec3? ?? default;
                var three = new Vector3(vector.X, vector.Y, vector.Z);

                if (ImGui.DragFloat3(id, ref three, 0.01f))
                {
                    field.Write(ctx.Ecs, entity, new Vec3(three.X, three.Y, three.Z));
                }

                break;
            }

            case FieldKind.Enum:
            {
                var options = field.Options;
                var said = value?.ToString() ?? string.Empty;
                var current = 0;

                for (var index = 0; index < options.Count; index++)
                {
                    if (options[index] == said) current = index;
                }

                if (options.Count > 0 && ImGui.Combo(id, ref current, [.. options], options.Count))
                {
                    field.Write(ctx.Ecs, entity, options[current]);
                }

                break;
            }

            case FieldKind.Int:
            {
                var number = Convert.ToInt32(value ?? 0, CultureInfo.InvariantCulture);

                var changed = field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most }
                    ? ImGui.SliderInt(id, ref number, (int)least, (int)most)
                    : ImGui.DragInt(id, ref number);

                if (changed) field.Write(ctx.Ecs, entity, number);
                break;
            }

            case FieldKind.Float:
            case FieldKind.Double:
            {
                var number = Convert.ToSingle(value ?? 0, CultureInfo.InvariantCulture);

                var changed = field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most }
                    ? ImGui.SliderFloat(id, ref number, (float)least, (float)most)
                    : ImGui.DragFloat(id, ref number, 0.01f);

                if (changed)
                {
                    field.Write(
                        ctx.Ecs,
                        entity,
                        field.Kind == FieldKind.Double ? (double)number : number);
                }

                break;
            }

            case FieldKind.Flags:
            {
                // Any number of a fixed set at once, so one box per name rather than one choice.
                var said = value?.ToString() ?? string.Empty;
                var chosen = said.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(part => part.Trim())
                    .ToHashSet();

                var changed = false;

                foreach (var option in field.Options)
                {
                    var on = chosen.Contains(option);

                    if (ImGui.Checkbox($"{option}##{id}", ref on))
                    {
                        if (on) chosen.Add(option);
                        else chosen.Remove(option);

                        changed = true;
                    }
                }

                if (changed) field.Write(ctx.Ecs, entity, string.Join(", ", chosen));
                break;
            }

            default:
            {
                var said = value?.ToString() ?? "-";
                ImGui.TextDisabled(said);
                break;
            }
        }

        if (!editable) ImGui.EndDisabled();

        if (field.Hints.Unit is { Length: > 0 } unit)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(unit);
        }

        ImGui.Columns(1);
        ImGui.PopID();
    }

    /// <summary>
    /// What a component is called, without the path it lives at.
    /// </summary>
    /// <remarks>
    /// Every part of the name loses its path, not just the last one, because a component's name is
    /// often another component's name inside it: cutting at the last <c>::</c> of
    /// <c>MeshMaterial3d&lt;StandardMaterial&gt;</c> leaves <c>StandardMaterial&gt;</c>.
    /// </remarks>
    private static string Short(string name)
    {
        var trimmed = new System.Text.StringBuilder();
        var start = 0;

        for (var index = 0; index <= name.Length; index++)
        {
            if (index < name.Length && name[index] is not ('<' or '>' or ',' or ' ')) continue;

            var part = name[start..index];
            var cut = part.LastIndexOf("::", StringComparison.Ordinal);

            trimmed.Append(cut >= 0 ? part[(cut + 2)..] : part);
            if (index < name.Length) trimmed.Append(name[index]);

            start = index + 1;
        }

        return trimmed.ToString();
    }
}
