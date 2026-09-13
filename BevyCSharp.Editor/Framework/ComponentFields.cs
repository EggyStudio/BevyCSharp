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
    /// How many places a number nobody is typing into is written to, at most.
    /// </summary>
    /// <remarks>
    /// As much as a box this narrow shows without the last digits running under its edge. The
    /// value itself is never rounded to it, so what is shown is short and what is held is whole.
    /// </remarks>
    internal const int Places = 3;

    /// <summary>The formats for every number of places, so a frame builds no strings.</summary>
    private static readonly string[] Shortly = ["%.0f", "%.1f", "%.2f", "%.3f"];

    /// <summary>
    /// How a number that is open for typing is written.
    /// </summary>
    /// <remarks>
    /// Seven figures, because that is what a single-precision number holds. ImGui fills the text
    /// box with the value put through the format the moment the box opens, so a field written at
    /// three places would offer 1.235 to somebody who came to correct 1.2345.
    /// </remarks>
    private const string Figures = "%.7g";

    /// <summary>
    /// What keeps a drag from rounding the value to the format it is shown in.
    /// </summary>
    /// <remarks>
    /// ImGui rounds a dragged value to whatever its format can print unless it is told not to, so
    /// without this a field shown to three places is a field that cannot hold 1.2345 even after
    /// somebody has typed it in.
    /// </remarks>
    private const ImGuiSliderFlags Whole = ImGuiSliderFlags.NoRoundToFormat;

    /// <summary>Which field is open for typing, while it is.</summary>
    private static string _opened = string.Empty;

    /// <summary>How the next field's number is written, which is who is about to read it.</summary>
    /// <param name="id">Which field is asking.</param>
    /// <param name="number">What the box holds, which decides how much of it there is to show.</param>
    private static string Written(string id, float number)
    {
        if (_opened == id) return Figures;

        if (Opening())
        {
            _opened = id;
            return Figures;
        }

        return Shortly[Needed(number)];
    }

    /// <summary>
    /// How many places one number needs, up to <see cref="Places"/>.
    /// </summary>
    /// <remarks>
    /// The fewest that says what the value is. Three zeroes after every whole number is three
    /// characters of nothing in a column that is already tight, and a tenth written as 1.200 says
    /// that two digits were measured which were not.
    /// </remarks>
    internal static int Needed(float number)
    {
        for (var places = 0; places < Places; places++)
        {
            if (MathF.Abs(number - MathF.Round(number, places)) < 0.0005f) return places;
        }

        return Places;
    }

    /// <summary>
    /// Whether the gesture this frame is the one that opens the next field for typing.
    /// </summary>
    /// <remarks>
    /// ImGui turns a drag box into a text box on a double click or a control click, and fills the
    /// text with the value as the format writes it, both inside the one call that draws the
    /// widget. The format has to be right before that call rather than after it, so the gesture is
    /// read here instead of being asked about afterwards. The rectangle is the one the widget is
    /// about to take, which is where the cursor is and how wide the row said it may be.
    /// </remarks>
    private static bool Opening()
    {
        var at = ImGui.GetCursorScreenPos();
        var size = new Vector2(ImGui.CalcItemWidth(), ImGui.GetFrameHeight());

        if (!ImGui.IsMouseHoveringRect(at, at + size)) return false;

        return ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)
            || (ImGui.GetIO().KeyCtrl && ImGui.IsMouseClicked(ImGuiMouseButton.Left));
    }

    /// <summary>Which rotation field is being turned, while it is being turned.</summary>
    /// <remarks>
    /// The angles are held here for as long as the box is held, because a rotation has more than
    /// one set of angles that describe it. Reading them back out of the quaternion on every frame
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

    /// <summary>
    /// Three number boxes on one row, each written to its own number of places.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What <c>DragFloat3</c> draws, except for the format. ImGui takes one format for the three
    /// boxes, and a vector whose Y is turning then writes X and Z as 0.000 while they are exactly
    /// nothing. Worse, the places change as Y passes each tenth, so the two boxes that are not
    /// changing are redrawn at a different width every frame and the numbers in them appear to
    /// shake. A number that is not moving has to look like it.
    /// </para>
    /// <para>
    /// The widths are ImGui's own arithmetic for a row of boxes, which is equal shares with the
    /// last one taking whatever the rounding left over.
    /// </para>
    /// </remarks>
    /// <param name="id">What the row is called, which each box takes a name under.</param>
    /// <param name="value">The three numbers, written back as they are dragged.</param>
    /// <param name="speed">How far a pixel of drag moves one of them.</param>
    /// <param name="active">Whether any of the three is being dragged or typed into.</param>
    /// <returns>Whether any of them changed.</returns>
    private static bool Vector(string id, ref Vector3 value, float speed, out bool active)
    {
        var inner = ImGui.GetStyle().ItemInnerSpacing.X;
        var full = ImGui.CalcItemWidth();

        var one = MathF.Max(1f, MathF.Floor((full - (inner * 2f)) / 3f));
        var last = MathF.Max(1f, MathF.Floor(full - ((one + inner) * 2f)));

        var changed = false;

        active = false;

        ImGui.BeginGroup();
        ImGui.PushID(id);

        for (var axis = 0; axis < 3; axis++)
        {
            if (axis > 0) ImGui.SameLine(0f, inner);

            ImGui.PushID(axis);
            ImGui.SetNextItemWidth(axis == 2 ? last : one);

            var number = axis switch { 0 => value.X, 1 => value.Y, _ => value.Z };
            var key = $"{id}.{axis}";

            if (ImGui.DragFloat("##n", ref number, speed, 0f, 0f, Written(key, number), Whole))
            {
                if (axis == 0) value.X = number;
                else if (axis == 1) value.Y = number;
                else value.Z = number;

                changed = true;
            }

            if (ImGui.IsItemActive()) active = true;
            else if (_opened == key) _opened = string.Empty;

            ImGui.PopID();
        }

        ImGui.PopID();
        ImGui.EndGroup();

        return changed;
    }

    /// <summary>
    /// Asks for a tick drawn in the text colour rather than in the accent.
    /// </summary>
    /// <remarks>
    /// The accent says what is selected or in force, and a box that has been ticked is neither. It
    /// is a value, like the number in the box on the row above, and a value is read in the colour
    /// everything else is read in. The slot the tick is drawn from is also where the theme keeps
    /// the accent, so this is pushed around the box rather than written into the style.
    /// </remarks>
    private static void Ticked() =>
        ImGui.PushStyleColor(ImGuiCol.CheckMark, EditorTheme.LiveText);

    /// <summary>
    /// Asks for a slider handle as round as the groove it runs in.
    /// </summary>
    /// <remarks>
    /// ImGui rounds a rectangle by at most half its shortest side, so a ten pixel handle in a
    /// groove twenty pixels tall cannot take the groove's own corner however large the rounding
    /// is. A square handle is a circle, which is the corner the ends of the groove are drawn with.
    /// <para>
    /// Square means the height ImGui gives a handle rather than the height of the groove, which is
    /// two pixels less at the top and two at the bottom. Asking for the groove's own height is
    /// asking for a handle wider than it is tall, which reads as an oval. Pushed rather than set
    /// once, because the number is a frame height and that is not known until there is a font to
    /// measure.
    /// </para>
    /// </remarks>
    private static void Knob() =>
        ImGui.PushStyleVar(ImGuiStyleVar.GrabMinSize, MathF.Max(1f, ImGui.GetFrameHeight() - 4f));

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

        // The name takes under a third and the value the rest, which holds at any width, because a
        // fixed column that fits at five hundred pixels leaves nothing for the value at three
        // hundred.
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

        // Written whole only while the box is open, which it is until ImGui says the widget is no
        // longer in use.
        if (_opened == id && !ImGui.IsItemActive()) _opened = string.Empty;

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

                Ticked();
                if (ImGui.Checkbox(id, ref on)) field.Write(ctx.Ecs, entity, on);
                ImGui.PopStyleColor();

                break;
            }

            case FieldKind.Vec3:
            {
                var vector = value as Vec3? ?? default;
                var three = new Vector3(vector.X, vector.Y, vector.Z);

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var moved = Vector(id, ref three, 0.01f, out _);

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

                var turned = Vector(id, ref degrees, 0.5f, out var holding);

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
                if (_turning == id && !holding) _turning = string.Empty;

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

                bool changed;

                if (field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most })
                {
                    Knob();
                    changed = ImGui.SliderInt(id, ref number, (int)least, (int)most);
                    ImGui.PopStyleVar();
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

                var format = Written(id, number);

                bool changed;

                if (field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most })
                {
                    Knob();
                    changed = ImGui.SliderFloat(id, ref number, (float)least, (float)most, format, Whole);
                    ImGui.PopStyleVar();
                }
                else
                {
                    changed = ImGui.DragFloat(id, ref number, 0.01f, 0f, 0f, format, Whole);
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

                if (ImGui.BeginPopup($"##pick{id}"))
                {
                    if (ImGui.MenuItem("Nothing")) field.Write(ctx.Ecs, entity, AssetHandle.None);

                    var kind = field.Hints.Asset ?? AssetKind.Mesh;

                    foreach (var file in EditorAssets.Every(Suits(field, kind)))
                    {
                        if (!ImGui.MenuItem(file)) continue;

                        // Loaded when it is chosen rather than when the list was built. A list that
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

                    // The name of the flag is part of the identifier, not only of the label. What
                    // ImGui hashes is what follows the two hashes, so every box in a set of flags
                    // sharing the field's id is a set of boxes ImGui cannot tell apart. It says so,
                    // in a window that takes the keyboard with it.
                    Ticked();

                    var set = ImGui.Checkbox($"{option}##{id}.{option}", ref on);

                    ImGui.PopStyleColor();

                    if (set)
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
