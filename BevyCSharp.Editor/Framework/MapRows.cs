using System.Runtime.CompilerServices;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// A map field, drawn as a row per entry with its key and its value side by side, a button to take
/// it out, and a button under them to add one.
/// </summary>
/// <remarks>
/// <para>
/// The key and the value are each drawn by the same switch every field goes through, as fields of
/// their own whose writes reach into the map, so a map of names to colors is a column of text boxes
/// beside a column of swatches.
/// </para>
/// <para>
/// A key changed to one another entry already has is refused by the field's write, so the entry
/// keeps its key rather than overwriting the other. A new entry is given a key no entry has, so
/// pressing add twice adds two.
/// </para>
/// </remarks>
internal static class MapRows
{
    /// <summary>The key and value fields made for each map field and entry, kept across frames.</summary>
    private static readonly ConditionalWeakTable<ComponentField, List<(ComponentField Key, ComponentField Value)>>
        Made = [];

    /// <summary>Draws a map field's entries and the button that adds one.</summary>
    internal static void Draw(
        BehaviorContext ctx, Entity entity, ComponentField field, string id, object? value)
    {
        var map = value as MapValue ?? MapValue.Empty;
        var size = ImGui.GetFrameHeight();
        var gap = ImGui.GetStyle().ItemInnerSpacing.X;
        var room = MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.RightInset);

        // The key takes the smaller share, being a name to find the value by.
        var keyWidth = MathF.Floor((room - size - (gap * 2f)) * 0.4f);
        var valueWidth = MathF.Max(1f, room - size - keyWidth - (gap * 2f));

        ImGui.PushID(id);

        for (var i = 0; i < map.Count; i++)
        {
            ImGui.PushID(i);
            var (key, held) = Entry(field, i);

            ImGui.SetNextItemWidth(keyWidth);
            ComponentFields.Item(ctx, entity, key, $"{id}[{i}].key", map[i].Key);

            ImGui.SameLine(0f, gap);
            ImGui.SetNextItemWidth(valueWidth);
            ComponentFields.Item(ctx, entity, held, $"{id}[{i}].value", map[i].Value);

            ImGui.SameLine(0f, gap);
            if (ListRows.Button("##remove", EditorIcons.Remove, size))
                field.Write(ctx.Ecs, entity, map.Without(i));

            ImGui.PopID();
        }

        if (ListRows.Button("##add", EditorIcons.Add, size))
        {
            field.Write(
                ctx.Ecs,
                entity,
                map.Adding(Unused(field, map), ListRows.Blank(field.ElementKind, field.Options)));
        }

        ImGui.PopID();
    }

    /// <summary>The key and value fields standing for one entry.</summary>
    private static (ComponentField Key, ComponentField Value) Entry(ComponentField map, int index)
    {
        var made = Made.GetOrCreateValue(map);

        while (made.Count <= index)
        {
            var at = made.Count;
            var hints = map.Hints with { Label = null, Foldout = null, Header = null, Note = null, Wide = false };

            made.Add((
                new ComponentField(
                    $"{map.Name}[{at}].key",
                    map.KeyKind,
                    map.Type,
                    (world, entity) => map.Read(world, entity) is MapValue entries && at < entries.Count
                        ? entries[at].Key
                        : null,
                    (world, entity, value) => map.Read(world, entity) is MapValue entries
                        && at < entries.Count
                        && map.Write(world, entity, entries.Rekeyed(at, value)),
                    hints: hints with { Minimum = null, Maximum = null }),
                new ComponentField(
                    $"{map.Name}[{at}].value",
                    map.ElementKind,
                    map.Type,
                    (world, entity) => map.Read(world, entity) is MapValue entries && at < entries.Count
                        ? entries[at].Value
                        : null,
                    (world, entity, value) => map.Read(world, entity) is MapValue entries
                        && at < entries.Count
                        && map.Write(world, entity, entries.With(at, value)),
                    map.Options,
                    hints)));
        }

        return made[index];
    }

    /// <summary>A key no entry has yet, for the kind the map is keyed by.</summary>
    private static object Unused(ComponentField map, MapValue entries)
    {
        var keys = entries.Select(entry => entry.Key).ToHashSet();

        switch (map.KeyKind)
        {
            case FieldKind.String:
                for (var n = 1; ; n++)
                {
                    var key = n == 1 ? "key" : $"key {n}";
                    if (!keys.Contains(key)) return key;
                }

            case FieldKind.Int:
                var whole = 0L;
                var plain = System.Globalization.CultureInfo.InvariantCulture;
                while (keys.Any(key => Convert.ToInt64(key, plain) == whole)) whole++;
                return whole;

            default:
                return ListRows.Blank(map.KeyKind, []);
        }
    }
}
