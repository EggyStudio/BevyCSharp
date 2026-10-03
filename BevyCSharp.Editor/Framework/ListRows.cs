using System.Numerics;
using System.Runtime.CompilerServices;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A list field, drawn as a row per item with a grip to reorder it by, a button to take it out, and
/// a button under them to add one.
/// </summary>
/// <remarks>
/// <para>
/// Each item is drawn by the same switch every other field goes through, as a field of its own
/// whose read and write reach into the list, so a list of colors is a column of swatches and a list
/// of enums a column of choices with nothing written for either here.
/// </para>
/// <para>
/// Every change is a write of the whole list, which the row around it reads before and after and
/// records as one step in the history, so taking an item out and putting it back is an undo like
/// any other.
/// </para>
/// </remarks>
internal static class ListRows
{
    /// <summary>The item fields made for each list field, kept so one is not made per frame.</summary>
    private static readonly ConditionalWeakTable<ComponentField, List<ComponentField>> Items = [];

    /// <summary>The list being dragged and the place of the item held, while a grip is held.</summary>
    private static (string List, int Index)? _dragging;

    /// <summary>Draws a list field's items and the button that adds one.</summary>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">What the field belongs to.</param>
    /// <param name="field">The list field.</param>
    /// <param name="id">What the field's widgets are called, which each item takes a name under.</param>
    /// <param name="value">What the field holds.</param>
    internal static void Draw(
        BehaviorContext ctx, Entity entity, ComponentField field, string id, object? value)
    {
        var list = value as ListValue ?? ListValue.Empty;
        var size = ImGui.GetFrameHeight();
        var gap = ImGui.GetStyle().ItemInnerSpacing.X;
        var room = MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.RightInset);

        ImGui.PushID(id);

        if (field.ElementKind == FieldKind.Struct && field.Items is { } fields)
        {
            Records(ctx, entity, field, id, list, fields, size, gap);
            ImGui.PopID();
            return;
        }

        for (var i = 0; i < list.Count; i++)
        {
            ImGui.PushID(i);

            Grip(ctx, entity, field, id, list, i, size);
            ImGui.SameLine(0f, gap);

            ImGui.SetNextItemWidth(MathF.Max(1f, room - (size * 2f) - (gap * 2f)));
            ComponentFields.Item(ctx, entity, Item(field, i), $"{id}[{i}]", list[i]);

            ImGui.SameLine(0f, gap);
            if (Button("##remove", EditorIcons.Remove, size))
                field.Write(ctx.Ecs, entity, list.Without(i));

            ImGui.PopID();
        }

        if (Button("##add", EditorIcons.Add, size))
            field.Write(ctx.Ecs, entity, list.Adding(Blank(field.ElementKind, field.Options)));

        ImGui.PopID();
    }

    /// <summary>
    /// A list of items with fields of their own, each a fold holding its fields as rows, with the
    /// grip and the remove button in front of it.
    /// </summary>
    /// <remarks>
    /// The buttons go before the fold rather than after it, because a fold runs to the edge of the
    /// row and a button after it would sit past the panel's edge. Each fold starts open, as a
    /// struct's fold in a component does, and is named by the item's first text, so a list of
    /// loot reads as what drops rather than as numbered entries.
    /// </remarks>
    private static void Records(
        BehaviorContext ctx,
        Entity entity,
        ComponentField field,
        string id,
        ListValue list,
        ItemFields fields,
        float size,
        float gap)
    {
        for (var i = 0; i < list.Count; i++)
        {
            ImGui.PushID(i);

            Grip(ctx, entity, field, id, list, i, size);
            ImGui.SameLine(0f, gap);

            if (Button("##remove", EditorIcons.Remove, size))
            {
                field.Write(ctx.Ecs, entity, list.Without(i));
                ImGui.PopID();
                break;
            }

            ImGui.SameLine(0f, gap);

            var bound = list[i] is { } item ? fields.Bind(item) : null;

            var open = ImGui.TreeNodeEx(
                $"{Title(ctx, entity, fields, bound, i)}##item",
                ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.FramePadding | ImGuiTreeNodeFlags.DefaultOpen);

            if (open)
            {
                var at = i;
                if (bound is not null)
                    Fields(ctx, entity, bound, $"##{i}", item => field.Write(ctx.Ecs, entity, list.With(at, item)));
                ImGui.TreePop();
            }

            ImGui.PopID();
        }

        if (Button("##add", EditorIcons.Add, size))
            field.Write(ctx.Ecs, entity, list.Adding(fields.Create()));
    }

    /// <summary>
    /// What a folded item is called: its first piece of text, which is usually its name, or its type
    /// and its place when it has none.
    /// </summary>
    private static string Title(BehaviorContext ctx, Entity entity, ItemFields fields, ItemFields.Bound? bound, int index)
    {
        var named = bound?.Schema.Fields
            .Where(part => part.Kind == FieldKind.String)
            .Select(part => part.Read(ctx.Ecs, entity) as string)
            .FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));

        return named ?? $"{fields.Type} {index + 1}";
    }

    /// <summary>
    /// One item's fields as rows, handed back as a whole when any of them changed.
    /// </summary>
    /// <remarks>
    /// The rows edit a copy of the item, which <paramref name="changed"/> puts in the list or the
    /// map in place of the old one, so the row around it records the change as one step, as it
    /// records every edit of a list or a map.
    /// </remarks>
    internal static void Fields(
        BehaviorContext ctx,
        Entity entity,
        ItemFields.Bound bound,
        string id,
        Action<object> changed)
    {
        var parts = bound.Schema.Fields.Where(part => !part.Hints.Hidden).ToArray();
        var before = parts.Select(part => part.Read(ctx.Ecs, entity)).ToArray();

        // Measured here rather than taken from the list, since the fold indents what is under it.
        var across = MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.RightInset);

        if (EditorRows.Open("##fields", new Vector2(across, 0f)))
        {
            for (var k = 0; k < parts.Length; k++)
            {
                var part = parts[k];
                EditorRows.Line(part.Title, part.Hints.Tooltip, unit: part.Hints.Unit);
                ComponentFields.Item(ctx, entity, part, $"{id}.{part.Name}", before[k]);
            }

            EditorRows.Close();
        }

        // The asset each reference among the item's fields names, folded under the item's rows as
        // under a component's, so a loot entry's item is read and changed where the entry is.
        for (var k = 0; k < parts.Length; k++)
        {
            if (parts[k].Kind != FieldKind.Data || before[k] is not IDataRef { Id: not 0 } reference) continue;

            ImGui.PushID($"{id}.{parts[k].Name}.asset");
            EditorDataAssets.Opened(ctx, entity, parts[k], reference.Id);
            ImGui.PopID();
        }

        for (var k = 0; k < parts.Length; k++)
        {
            if (Equals(before[k], parts[k].Read(ctx.Ecs, entity))) continue;

            changed(bound.Value());
            return;
        }
    }

    /// <summary>
    /// The handle an item is dragged by, which takes another item dropped on it into its place.
    /// </summary>
    private static void Grip(
        BehaviorContext ctx, Entity entity, ComponentField field, string id, ListValue list, int index, float size)
    {
        var width = size * 0.6f;
        ImGui.InvisibleButton("##grip", new Vector2(width, size));

        var at = ImGui.GetItemRectMin();
        var draw = ImGui.GetWindowDrawList();
        var lit = ImGui.IsItemHovered() || ImGui.IsItemActive();
        var ink = ImGui.GetColorU32(lit ? ImGuiCol.Text : ImGuiCol.TextDisabled);

        // Three short strokes, the mark a thing to be dragged carries everywhere.
        for (var line = -1; line <= 1; line++)
        {
            var y = at.Y + (size * 0.5f) + (line * size * 0.18f);
            draw.AddLine(new Vector2(at.X + (width * 0.2f), y), new Vector2(at.X + (width * 0.8f), y), ink, 1.5f);
        }

        if (ImGui.BeginDragDropSource())
        {
            // The payload says only that an item of a list is moving. Which one is kept here, for
            // the reason the world list gives for its rows.
            _dragging = (id, index);
            ImGui.SetDragDropPayload(Payload, IntPtr.Zero, 0);
            ImGui.TextUnformatted(FieldNumbers.Say(list[index]));
            ImGui.EndDragDropSource();
        }

        if (_dragging is { } held && held.List == id && held.Index != index && ImGui.BeginDragDropTarget())
        {
            var payload = ImGui.AcceptDragDropPayload(Payload);
            if (Unsafe.As<ImGuiPayloadPtr, IntPtr>(ref payload) != IntPtr.Zero)
            {
                field.Write(ctx.Ecs, entity, list.Moving(held.Index, index));
                _dragging = null;
            }

            ImGui.EndDragDropTarget();
        }
    }

    /// <summary>What a list's items are dragged as.</summary>
    private const string Payload = "bcs.list.item";

    /// <summary>A square button showing an icon.</summary>
    internal static bool Button(string id, string icon, float size)
    {
        var pressed = ImGui.InvisibleButton(id, new Vector2(size, size));
        var at = ImGui.GetItemRectMin();
        var inset = size * 0.2f;

        EditorDraw.Icon(
            ImGui.GetWindowDrawList(), icon, at + new Vector2(inset), size - (inset * 2f), ImGui.IsItemHovered());

        return pressed;
    }

    /// <summary>
    /// A field standing for one item of a list, which reads that item and writes the list with it
    /// replaced.
    /// </summary>
    private static ComponentField Item(ComponentField list, int index)
    {
        var made = Items.GetOrCreateValue(list);

        while (made.Count <= index)
        {
            var at = made.Count;
            made.Add(new ComponentField(
                $"{list.Name}[{at}]",
                list.ElementKind,
                list.Type,
                (world, entity) => list.Read(world, entity) is ListValue items && at < items.Count
                    ? items[at]
                    : null,
                (world, entity, value) => list.Read(world, entity) is ListValue items
                    && at < items.Count
                    && list.Write(world, entity, items.With(at, value)),
                list.Options,
                list.Hints with { Label = null, Foldout = null, Header = null, Note = null, Wide = false }));
        }

        return made[index];
    }

    /// <summary>The value a new item starts as, for the kind the list holds.</summary>
    internal static object Blank(FieldKind kind, IReadOnlyList<string> options) => kind switch
    {
        FieldKind.Bool => false,
        FieldKind.Int => 0,
        FieldKind.Float => 0f,
        FieldKind.Double => 0d,
        FieldKind.String => string.Empty,
        FieldKind.Vec2 => default(Vec2),
        FieldKind.Vec3 => default(Vec3),
        FieldKind.Vec4 => default(Vec4),
        FieldKind.Quat => Quat.Identity,
        FieldKind.Color => Color.White,
        FieldKind.Enum or FieldKind.Flags => options.Count > 0 ? options[0] : 0,
        FieldKind.Entity => Entity.None,
        FieldKind.Asset => AssetHandle.None,
        _ => 0,
    };
}
