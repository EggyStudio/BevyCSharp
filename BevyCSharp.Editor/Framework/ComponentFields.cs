using System.Globalization;
using System.Numerics;
using Bevy;
using ImGuiNET;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// One component field, drawn as whatever kind of thing it is.
/// </summary>
/// <remarks>
/// What a field knows about itself is <see cref="ComponentField"/>'s: its kind, its range, the
/// names of its options. This is the other half, which turns each of those into a widget and
/// writes back what comes out of it.
/// </remarks>
public static class ComponentFields
{
    /// <summary>
    /// A heading over the rows that belong to it, with a rule carried out to the end of the row.
    /// </summary>
    /// <remarks>
    /// ImGui's own runs its rule to the edge of the whole region, which is further right than the
    /// fields reach and close enough to the card's edge to read as a line that ran out of room.
    /// This ends where the fields end, so the row is inset by the same amount at both ends.
    /// </remarks>
    /// <param name="text">What the heading says.</param>
    private static void Heading(string text)
    {
        if (EditorTheme.Current.Stock)
        {
            ImGui.SeparatorText(text);
            return;
        }

        var style = ImGui.GetStyle();
        var thick = MathF.Max(1f, style.SeparatorTextBorderSize);

        var at = ImGui.GetCursorScreenPos();
        var word = ImGui.CalcTextSize(text);

        var width = MathF.Max(
            word.X + (style.SeparatorTextPadding.X * 2f),
            ImGui.GetContentRegionAvail().X - DetailsPanel.Inset);

        var height = MathF.Max(word.Y + (style.SeparatorTextPadding.Y * 2f), thick);

        ImGui.Dummy(new Vector2(width, height));

        var draw = ImGui.GetWindowDrawList();
        var rule = MathF.Floor(at.Y + (height * 0.5f));
        var color = ImGui.GetColorU32(ImGuiCol.Separator);

        var from = at.X + style.SeparatorTextPadding.X;
        var to = from + word.X + style.ItemSpacing.X;

        if (from - style.ItemSpacing.X > at.X)
        {
            draw.AddLine(
                new Vector2(at.X, rule),
                new Vector2(from - style.ItemSpacing.X, rule),
                color,
                thick);
        }

        if (to < at.X + width)
        {
            draw.AddLine(new Vector2(to, rule), new Vector2(at.X + width, rule), color, thick);
        }

        draw.AddText(
            new Vector2(from, at.Y + style.SeparatorTextPadding.Y),
            ImGui.GetColorU32(ImGuiCol.Text),
            text);
    }

    /// <summary>One field, drawn as what it is.</summary>
    internal static void Row(
        BehaviorContext ctx, Entity entity, ComponentSchema schema, ComponentField field)
    {
        var id = $"##{schema.Name}.{field.Name}";
        var value = field.Read(ctx.Ecs, entity);

        if (field.Hints.Header is { Length: > 0 } heading) Heading(heading);

        ImGui.PushID(id);

        // Short of the card's edge on the right by the same air it keeps on the left, or the last
        // field in every row runs into the side of the card it is drawn in.
        var across = new Vector2(
            MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.Inset),
            0f);

        if (!EditorRows.Open("##row", across))
        {
            ImGui.PopID();
            return;
        }

        EditorRows.Line(field.Title, field.Hints.Tooltip);

        var editable = field.IsWritable;
        if (!editable) ImGui.BeginDisabled();

        Edit(ctx, entity, field, id, value);

        // Written whole only while the box is open, which it is until ImGui says the widget is no
        // longer in use.
        FieldNumbers.Close(id);

        if (!editable) ImGui.EndDisabled();

        if (field.Hints.Unit is { Length: > 0 } unit)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(unit);
        }

        EditorRows.Close();
        ImGui.PopID();
    }

    /// <summary>
    /// The widget one field is edited through, chosen by what kind of value it holds.
    /// </summary>
    /// <remarks>
    /// Each arm reads what is there, offers it, and writes back only when the widget says it
    /// changed. Nothing here decides where the widget goes. The row it sits in has already set
    /// the column and the width.
    /// </remarks>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">What the field belongs to.</param>
    /// <param name="field">Which field.</param>
    /// <param name="id">What to call the widget, which is unique to the field.</param>
    /// <param name="value">What the field holds.</param>
    private static void Edit(
        BehaviorContext ctx, Entity entity, ComponentField field, string id, object? value)
    {
        switch (field.Kind)
        {
            case FieldKind.Bool:
            {
                var on = value is true;

                if (EditorWidgets.Ticked(id, ref on)) field.Write(ctx.Ecs, entity, on);

                break;
            }

            case FieldKind.Vec3:
            {
                var vector = value as Vec3? ?? default;
                var three = new Vector3(vector.X, vector.Y, vector.Z);

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var moved = FieldNumbers.Vector(id, ref three, 0.01f, out _);

                ImGui.PopFont();

                if (moved)
                {
                    field.Write(ctx.Ecs, entity, new Vec3(three.X, three.Y, three.Z));
                }

                break;
            }

            case FieldKind.Quat:
            {
                var turn = value as Quat? ?? Quat.Identity;

                // Degrees about X, Y and Z, which is the only way anybody reads or writes a
                // rotation. Four numbers that must stay on the unit sphere are not something to
                // type into, and three that say pitch, turn and roll are.
                var degrees = FieldNumbers.Turning(id, turn);

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var turned = FieldNumbers.Vector(id, ref degrees, 0.5f, out var holding);

                ImGui.PopFont();

                if (turned)
                {
                    FieldNumbers.Turned(id, degrees);

                    field.Write(
                        ctx.Ecs,
                        entity,
                        Quat.FromEuler(
                            degrees.X * FieldNumbers.Radians,
                            degrees.Y * FieldNumbers.Radians,
                            degrees.Z * FieldNumbers.Radians));
                }

                // Let go of the angles the moment the box is let go of, so the rotation is what the
                // world says again rather than what was last typed.
                FieldNumbers.Settle(id, holding);

                break;
            }

            case FieldKind.Enum:
            {
                var options = field.Options;
                var said = value?.ToString() ?? string.Empty;

                EditorWidgets.Choice(
                    id, said, options, chosen => field.Write(ctx.Ecs, entity, chosen));

                break;
            }

            case FieldKind.Int:
            {
                var number = Convert.ToInt32(value ?? 0, CultureInfo.InvariantCulture);

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                bool changed;

                if (field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most })
                {
                    var low = (int)least;
                    var high = (int)most;
                    var held = number;

                    changed = EditorWidgets.Sliding(
                        id,
                        high > low ? (float)(number - low) / (high - low) : 0f,
                        () => ImGui.SliderInt(id, ref held, low, high));

                    number = held;
                }
                else
                {
                    changed = ImGui.DragInt(id, ref number);
                }

                ImGui.PopFont();

                if (changed) field.Write(ctx.Ecs, entity, number);
                break;
            }

            case FieldKind.Float:
            case FieldKind.Double:
            {
                var number = Convert.ToSingle(value ?? 0, CultureInfo.InvariantCulture);

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var format = FieldNumbers.Written(id, number);

                bool changed;

                if (field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most })
                {
                    var low = (float)least;
                    var high = (float)most;
                    var held = number;

                    changed = EditorWidgets.Sliding(
                        id,
                        high > low ? (number - low) / (high - low) : 0f,
                        () => ImGui.SliderFloat(id, ref held, low, high, format, FieldNumbers.Whole));

                    number = held;
                }
                else
                {
                    changed = ImGui.DragFloat(id, ref number, 0.01f, 0f, 0f, format, FieldNumbers.Whole);
                }

                ImGui.PopFont();

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
                // when the list opens rather than when the row is drawn, because a field that
                // listed the project every frame would read the disk sixty times a second.
                var held = value?.ToString() ?? "none";

                if (ImGui.Button(Short(held), new Vector2(-1f, 0f))) ImGui.OpenPopup($"##pick{id}");

                if (EditorWidgets.Flyout($"##pick{id}"))
                {
                    RoundedRows.Rows(() =>
                    {
                        if (ImGui.MenuItem("Nothing")) field.Write(ctx.Ecs, entity, AssetHandle.None);

                        RoundedRows.Row();

                        var kind = field.Hints.Asset ?? AssetKind.Mesh;

                        foreach (var file in EditorAssets.Every(Suits(field, kind)))
                        {
                            var chosen = ImGui.MenuItem(file);

                            RoundedRows.Row();

                            // Loaded when it is chosen rather than when the list was built. A list
                            // that loaded everything it offered would load the project to ask a
                            // question.
                            if (chosen) field.Write(ctx.Ecs, entity, AssetServer.Load(kind, file));
                        }
                    });

                    EditorWidgets.EndFlyout();
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

                if (EditorWidgets.Flyout($"##pick{id}"))
                {
                    RoundedRows.Rows(() =>
                    {
                        if (ImGui.MenuItem("Nothing")) field.Write(ctx.Ecs, entity, Entity.None);

                        RoundedRows.Row();

                        foreach (var other in ctx.Ecs.All())
                        {
                            if (ctx.Ecs.NameOf(other) is not { Length: > 0 } called) continue;
                            if (EditorEntity.IsInterface(ctx.Ecs, other)) continue;

                            if (ImGui.MenuItem(called)) field.Write(ctx.Ecs, entity, other);

                            RoundedRows.Row(other == held);
                        }
                    });

                    EditorWidgets.EndFlyout();
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

                    // The name of the flag is part of the identifier, not only of the label. What
                    // ImGui hashes is what follows the two hashes, so every box in a set of flags
                    // sharing the field's id is a set of boxes ImGui cannot tell apart. It says so,
                    // in a window that takes the keyboard with it.
                    if (EditorWidgets.Ticked($"{option}##{id}.{option}", ref on))
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
                var said = FieldNumbers.Say(value);

                // The same face for a number nobody can edit, so a column of them lines up
                // whether or not it happens to be writable.
                var figures = value is float or double or Vec3 or Quat;

                if (figures) ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                ImGui.TextDisabled(said);

                if (figures) ImGui.PopFont();
                break;
            }
        }
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
    /// often another component's name inside it, so cutting at the last <c>::</c> of
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
