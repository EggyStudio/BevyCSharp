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
    /// <summary>One degree, in radians.</summary>
    private const float Radians = MathF.PI / 180f;

    /// <summary>
    /// How a number somebody can edit is written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// As many digits as it takes and no more: zero is <c>0</c> rather than <c>0.000</c>, and a
    /// tenth is <c>1.2</c> rather than <c>1.200</c>. Three zeroes after every whole number is
    /// three characters of nothing in a column that is already tight.
    /// </para>
    /// <para>
    /// Seven figures, because that is what a single-precision number holds. It matters for more
    /// than reading: ImGui rounds a dragged value to whatever its format can print, so a field
    /// written as three decimal places is a field that cannot hold 1.2345 even if somebody types
    /// it in. Asking for fewer digits than the number has is asking the editor to quietly lose
    /// them.
    /// </para>
    /// </remarks>
    private const string Figures = "%.7g";

    /// <summary>Which rotation field is being turned, while it is being turned.</summary>
    /// <remarks>
    /// The angles are held here for as long as the box is held, because a rotation has more than
    /// one set of angles that describe it: reading them back out of the quaternion on every frame
    /// of a drag means the numbers jump to a different decomposition halfway through, and a box
    /// whose value changes while it is being dragged cannot be dragged.
    /// </remarks>
    private static string _turning = string.Empty;

    /// <summary>The angles that field is being turned to, in degrees.</summary>
    private static Vector3 _turned;

    /// <summary>The angles to show for a rotation: the ones being typed, or the world's own.</summary>
    /// <param name="id">Which field is asking.</param>
    /// <param name="turn">What the world says it is rotated by.</param>
    private static Vector3 Turning(string id, Quat turn)
    {
        if (_turning == id) return _turned;

        var euler = turn.ToEuler();

        return new Vector3(Tidy(euler.X), Tidy(euler.Y), Tidy(euler.Z));
    }

    /// <summary>One angle in degrees, with the noise of the round trip taken off it.</summary>
    /// <remarks>
    /// A rotation that is exactly none comes back out of the quaternion as a few millionths of a
    /// degree, sometimes negative, which a box then shows as -0.000. That is not a rotation and
    /// reads as a fault.
    /// </remarks>
    private static float Tidy(float radians)
    {
        var degrees = radians / Radians;

        return MathF.Abs(degrees) < 0.0005f ? 0f : degrees;
    }

    /// <summary>
    /// A value as a person reads it, rather than as a round trip through binary writes it.
    /// </summary>
    /// <remarks>
    /// Four places at most, and none of them trailing zeroes. A number that came from a rotation
    /// or a division prints seventeen digits by default, and sixteen of them are the difference
    /// between what a float can hold and what was meant.
    /// </remarks>
    private static string Say(object? value) => value switch
    {
        null => "-",
        float number => Digits(number),
        double number => Digits((float)number),
        Vec3 vector => $"{Digits(vector.X)}, {Digits(vector.Y)}, {Digits(vector.Z)}",
        Quat turn => Say(turn.ToEuler() * (1f / Radians)),
        _ => value.ToString() ?? "-",
    };

    /// <summary>One number, to four places at most.</summary>
    private static string Digits(float number) =>
        MathF.Round(number, 4).ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>One field, drawn as what it is.</summary>
    internal static void Row(
        BehaviorContext ctx, Entity entity, ComponentSchema schema, ComponentField field)
    {
        var id = $"##{schema.Name}.{field.Name}";
        var value = field.Read(ctx.Ecs, entity);

        if (field.Hints.Header is { Length: > 0 } heading) ImGui.SeparatorText(heading);

        ImGui.PushID(id);

        // Short of the card's edge on the right by the same air it keeps on the left, or the last
        // field in every row runs into the side of the card it is drawn in.
        var across = new Vector2(
            MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.Inset),
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

        // The name takes under a third and the value the rest, which holds at any width: a fixed
        // column that fits at five hundred pixels leaves nothing for the value at three hundred.
        //
        // Weighted towards the value, because a name that runs out of room is still readable from
        // its first half and a number that runs out of room is a different number.
        ImGui.TableSetupColumn("##name", ImGuiTableColumnFlags.WidthStretch, 0.3f);
        ImGui.TableSetupColumn("##value", ImGuiTableColumnFlags.WidthStretch, 0.7f);

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

        Edit(ctx, entity, field, id, value);

        if (!editable) ImGui.EndDisabled();

        if (field.Hints.Unit is { Length: > 0 } unit)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(unit);
        }

        ImGui.EndTable();
        ImGui.PopID();
    }

    /// <summary>
    /// The widget one field is edited through, chosen by what kind of value it holds.
    /// </summary>
    /// <remarks>
    /// Each arm reads what is there, offers it, and writes back only when the widget says it
    /// changed. Nothing here decides where the widget goes: the row it sits in has already set
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
                if (ImGui.Checkbox(id, ref on)) field.Write(ctx.Ecs, entity, on);
                break;
            }

            case FieldKind.Vec3:
            {
                var vector = value as Vec3? ?? default;
                var three = new Vector3(vector.X, vector.Y, vector.Z);

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var moved = ImGui.DragFloat3(id, ref three, 0.01f, 0f, 0f, Figures);

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
                var degrees = Turning(id, turn);

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var turned = ImGui.DragFloat3(id, ref degrees, 0.5f, 0f, 0f, Figures);

                ImGui.PopFont();

                if (turned)
                {
                    _turning = id;
                    _turned = degrees;

                    field.Write(
                        ctx.Ecs,
                        entity,
                        Quat.FromEuler(
                            degrees.X * Radians,
                            degrees.Y * Radians,
                            degrees.Z * Radians));
                }

                // Let go of the angles the moment the box is let go of, so the rotation is what the
                // world says again rather than what was last typed.
                if (_turning == id && !ImGui.IsItemActive()) _turning = string.Empty;

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

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var changed = field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most }
                    ? ImGui.SliderInt(id, ref number, (int)least, (int)most)
                    : ImGui.DragInt(id, ref number);

                ImGui.PopFont();

                if (changed) field.Write(ctx.Ecs, entity, number);
                break;
            }

            case FieldKind.Float:
            case FieldKind.Double:
            {
                var number = Convert.ToSingle(value ?? 0, CultureInfo.InvariantCulture);

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var changed = field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most }
                    ? ImGui.SliderFloat(id, ref number, (float)least, (float)most, Figures)
                    : ImGui.DragFloat(id, ref number, 0.01f, 0f, 0f, Figures);

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
                var said = Say(value);

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
