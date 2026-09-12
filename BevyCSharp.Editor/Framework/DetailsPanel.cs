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

        // How many were picked, when it is more than one. The rest of the panel is about the last
        // of them, and without this a drag that took a dozen things looks like a click that took
        // one: the other eleven are only visible in the list, which may not be on screen.
        if (EditorSelection.Count > 1)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"({EditorSelection.Count} selected)");
        }

        ImGui.Spacing();

        if (!EditorSelection.Any)
        {
            ImGui.TextDisabled("Nothing selected");
            return;
        }

        var entity = EditorSelection.Current;
        var name = ctx.Ecs.NameOf(entity) ?? $"Entity {entity.Index}";

        ImGui.SetNextItemWidth(-1f - EditorShell.DockRoom());
        var renamed = name;

        if (ImGui.InputText("##name", ref renamed, 128, ImGuiInputTextFlags.EnterReturnsTrue)
            && renamed.Length > 0)
        {
            ctx.Ecs.SetName(entity, renamed);
        }

        ImGui.Spacing();

        // One scrolling region and nothing nested inside it.
        //
        // A card used to be a child window of its own, and a child window whose height is worked
        // out from its contents, inside a region that scrolls, is the thing that made this panel
        // jump while it was being scrolled: the height it reports depends on what is visible, and
        // what is visible depends on the height. Drawn as a rectangle behind a group instead, it
        // is one region with one scroll and no argument.
        // No fill of its own: the panel's card is the surface this scrolls over, and the component
        // cards drawn into it are the step above it.
        ImGui.PushStyleColor(ImGuiCol.ChildBg, 0u);

        // The room the button at the bottom keeps for itself, taken out of the scrolling region
        // rather than scrolled with it: adding a component is not something to go looking for at
        // the end of a long list of what is already there.
        var button = ImGui.GetFrameHeight() + (ImGui.GetStyle().ItemSpacing.Y * 2f);
        var room = ImGui.GetContentRegionAvail().Y - button;

        var open = ImGui.BeginChild(
            "##components",
            new Vector2(0f, MathF.Max(1f, room)),
            ImGuiChildFlags.NavFlattened);

        ImGui.PopStyleColor();

        if (!open)
        {
            ImGui.EndChild();
            return;
        }

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is not { } schema) continue;
            if (schema.Fields.Count == 0) continue;

            Component(ctx, entity, schema);
        }

        // What the thing is, under what can be edited about it: a tag says what something carries,
        // which is worth knowing and never worth the room at the top.
        Tags(ctx, entity);

        ImGui.EndChild();

        Add(ctx, entity);
    }

    /// <summary>How much air a component's card keeps inside its own edge.</summary>
    private const float Inset = 8f;

    /// <summary>One component, as a card with its fields in it.</summary>
    /// <remarks>
    /// The fill is drawn behind the group rather than around it, through a split in the draw list:
    /// the content goes on the upper channel, the rectangle on the lower one, and the merge puts
    /// the rectangle underneath. That is how a card is drawn without a window to hold it.
    /// </remarks>
    private static void Component(BehaviorContext ctx, Entity entity, ComponentSchema schema)
    {
        var theme = EditorTheme.Current;
        var draw = ImGui.GetWindowDrawList();
        var padding = ImGui.GetStyle().WindowPadding;

        // Where the card's two edges go, taken before anything is drawn: the room left on the row
        // is the room inside the scrollbar, which is what the card has to fit between.
        //
        // Measured rather than worked back from the group, because the group's own rectangle starts
        // where its first item does. Reaching left of that to make room for the indent puts the
        // card's corner outside the region it is drawn in, where it is clipped square.
        var left = ImGui.GetCursorScreenPos().X;
        var right = left + ImGui.GetContentRegionAvail().X;

        draw.ChannelsSplit(2);
        draw.ChannelsSetCurrent(1);

        ImGui.BeginGroup();

        ImGui.Dummy(new Vector2(0f, padding.Y * 0.5f));
        ImGui.Indent(Inset);

        // The expander wears no fill of its own. The card behind it is the expander grown
        // downwards: one rounded shape, the width of the header, holding the header and everything
        // it opened. A pill for the header inside a card for the component is two surfaces saying
        // the same thing, and the inner one always looked stuck on.
        if (!theme.Stock)
        {
            ImGui.PushStyleColor(ImGuiCol.Header, 0u);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, EditorTheme.LiveHover);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, EditorTheme.LiveHover);
        }

        var open = ImGui.CollapsingHeader(schema.Name, ImGuiTreeNodeFlags.DefaultOpen);

        if (!theme.Stock) ImGui.PopStyleColor(3);

        // A component's own menu, where taking it off lives. On the header, because that is the
        // thing the component is.
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

            // Wrapped rather than run off the edge: a row of buttons as wide as the panel is a row
            // whose last button cannot be pressed.
            var room = ImGui.GetContentRegionAvail().X;
            var used = 0f;

            foreach (var method in schema.Methods)
            {
                var width = ImGui.CalcTextSize(method.Title).X + (ImGui.GetStyle().FramePadding.X * 2f);

                if (used > 0f && used + width < room) ImGui.SameLine();
                else used = 0f;

                used += width + ImGui.GetStyle().ItemSpacing.X;

                if (ImGui.Button(method.Title)) method.Run(ctx.Ecs, entity);
            }
        }

        ImGui.Unindent(Inset);
        ImGui.Dummy(new Vector2(0f, padding.Y * 0.5f));

        ImGui.EndGroup();

        if (!theme.Stock)
        {
            var from = ImGui.GetItemRectMin();
            var to = ImGui.GetItemRectMax();

            draw.ChannelsSetCurrent(0);
            draw.AddRectFilled(
                new Vector2(left, from.Y),
                new Vector2(right, to.Y),
                ImGui.GetColorU32(EditorTheme.LiveGroup),
                ImGui.GetStyle().ChildRounding);
        }

        draw.ChannelsMerge();

        ImGui.Spacing();
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

        var room = ImGui.GetContentRegionAvail().X;

        if (ImGui.Button("Add Component", new Vector2(room, ImGui.GetFrameHeight())))
        {
            ImGui.OpenPopup("##add");
        }

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
        var any = false;

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            var schema = ComponentSchemas.For(id);

            // Something of this project's own with no fields to edit. Not the engine's: a mesh, a
            // material and a visibility are on everything that is drawn, so naming them says
            // nothing about the thing being looked at and crowds out what does.
            if (schema is null) continue;
            if (schema.Fields.Count > 0) continue;

            var label = schema.Name;
            if (label.Length == 0) continue;

            var width = ImGui.CalcTextSize(label).X + (ImGui.GetStyle().FramePadding.X * 2f) + 8f;

            if (used > 0f && used + width < room) ImGui.SameLine();
            else used = 0f;

            used += width + ImGui.GetStyle().ItemSpacing.X;

            if (!any)
            {
                any = true;

                ImGui.Spacing();
                ImGui.TextDisabled("ALSO CARRIES");
                ImGui.Spacing();

                // Said before the first one, so a thing carrying nothing extra says nothing at all
                // rather than showing a heading over an empty row.
                used = width + ImGui.GetStyle().ItemSpacing.X;
            }

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

        // Short of the card's edge on the right by the same air it keeps on the left, or the last
        // field in every row runs into the side of the card it is drawn in.
        var across = new Vector2(
            MathF.Max(1f, ImGui.GetContentRegionAvail().X - Inset),
            0f);

        if (!ImGui.BeginTable(
                "##row",
                2,
                ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.NoSavedSettings,
                across))
        {
            ImGui.PopID();
            return;
        }

        // The name takes a third and the value the rest, which holds at any width: a fixed column
        // that fits at five hundred pixels leaves nothing for the value at three hundred.
        ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch, 0.38f);
        ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch, 0.62f);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(field.Title);

        if (field.Hints.Tooltip is { Length: > 0 } why && ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(why);
        }

        ImGui.TableNextColumn();
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

            case FieldKind.Asset:
            {
                // What it holds, and a list of what it could hold instead. The files are asked for
                // when the list opens rather than when the row is drawn: a field that listed the
                // project every frame would read the disk sixty times a second.
                var held = value?.ToString() ?? "none";

                if (ImGui.Button(Short(held), new Vector2(-1f, 0f))) ImGui.OpenPopup($"##pick{id}");

                if (ImGui.BeginPopup($"##pick{id}"))
                {
                    if (ImGui.MenuItem("Nothing")) field.Write(ctx.Ecs, entity, AssetHandle.None);

                    var kind = field.Hints.Asset ?? AssetKind.Mesh;

                    foreach (var file in EditorAssets.Every(Suits(field, kind)))
                    {
                        if (!ImGui.MenuItem(file)) continue;

                        // Loaded when it is chosen rather than when the list was built: a list that
                        // loaded everything it offered would load the project to ask a question.
                        field.Write(ctx.Ecs, entity, AssetServer.Load(kind, file));
                    }

                    ImGui.EndPopup();
                }

                break;
            }

            case FieldKind.Entity:
            {
                var held = value as Entity? ?? Entity.None;

                var name = held.IsNone
                    ? "none"
                    : ctx.Ecs.NameOf(held) ?? $"Entity {held.Index}";

                if (ImGui.Button(name, new Vector2(-1f, 0f))) ImGui.OpenPopup($"##pick{id}");

                if (ImGui.BeginPopup($"##pick{id}"))
                {
                    if (ImGui.MenuItem("Nothing")) field.Write(ctx.Ecs, entity, Entity.None);

                    foreach (var other in ctx.Ecs.All())
                    {
                        if (ctx.Ecs.NameOf(other) is not { Length: > 0 } called) continue;
                        if (EditorEntity.IsInterface(ctx.Ecs, other)) continue;

                        if (ImGui.MenuItem(called)) field.Write(ctx.Ecs, entity, other);
                    }

                    ImGui.EndPopup();
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

                    // The name of the flag is part of the identifier, not only of the label: what
                    // ImGui hashes is what follows the two hashes, so every box in a set of flags
                    // sharing the field's id is a set of boxes ImGui cannot tell apart. It says so,
                    // in a window that takes the keyboard with it.
                    if (ImGui.Checkbox($"{option}##{id}.{option}", ref on))
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

        ImGui.EndTable();
        ImGui.PopID();
    }

    /// <summary>Which files suit a field: what it asked for, or what its kind usually holds.</summary>
    private static IReadOnlyCollection<string> Suits(ComponentField field, string kind)
    {
        if (field.Hints.Extensions is { Length: > 0 } asked)
        {
            return asked.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return [.. EditorAssets.ExtensionsFor(kind)];
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
