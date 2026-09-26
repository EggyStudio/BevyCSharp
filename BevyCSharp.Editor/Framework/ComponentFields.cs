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
    /// <summary>How wide the box beside a bar is, when a field asks for one.</summary>
    private const float Boxed = 64f;

    /// <summary>What one pixel of a drag on a field is worth, which the field may say.</summary>
    /// <param name="field">Which field.</param>
    private static float Pixel(ComponentField field) =>
        field.Hints.Step is { } step and > 0d ? (float)step : 0.01f;

    /// <summary>
    /// Whether a field is drawn at all, as its conditions say.
    /// </summary>
    /// <remarks>
    /// A condition names another field of the same component and either a value it has to have or,
    /// with none, that it has to read as true. A condition naming a field that is not there is
    /// ignored rather than obeyed, because a row that vanishes on account of a typo is worse than
    /// a row that stays.
    /// </remarks>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">What the field belongs to.</param>
    /// <param name="schema">The component it is part of, which holds the field it names.</param>
    /// <param name="field">Which field.</param>
    private static bool Shown(
        BehaviorContext ctx, Entity entity, ComponentSchema schema, ComponentField field)
    {
        if (field.Hints.Hidden) return false;
        if (field.Hints.Conditions.Count == 0) return true;

        foreach (var (named, wanted, not) in field.Hints.Conditions)
        {
            var other = Named(schema, named);
            if (other is null) continue;

            var held = other.Read(ctx.Ecs, entity);

            var met = wanted is { Length: > 0 }
                ? string.Equals(held?.ToString(), wanted, StringComparison.Ordinal)
                : held is true;

            if (met == not) return false;
        }

        return true;
    }

    /// <summary>One field of a component by name, or nothing when it names none.</summary>
    /// <param name="schema">The component to look in.</param>
    /// <param name="name">What the field is called.</param>
    private static ComponentField? Named(ComponentSchema schema, string name)
    {
        foreach (var field in schema.Fields)
        {
            if (field.Name == name) return field;
        }

        return null;
    }

    /// <summary>
    /// A sentence above a field, in the color of what it is.
    /// </summary>
    /// <remarks>
    /// Wrapped, because this is a sentence rather than a label and the panel it is in is narrow.
    /// </remarks>
    /// <param name="text">What it says.</param>
    /// <param name="kind">How much it matters.</param>
    private static void Note(string text, NoteKind kind)
    {
        var theme = EditorTheme.Current;

        var color = kind switch
        {
            NoteKind.Warning => theme.Warn,
            NoteKind.Error => theme.Bad,
            NoteKind.Heading => EditorTheme.LiveText,
            _ => theme.Dim,
        };

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.PushTextWrapPos(0f);

        ImGui.TextUnformatted(text);

        ImGui.PopTextWrapPos();
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Puts what a field's widget just did on the undo stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Told by what the field holds rather than by what the widget returned, because every arm of
    /// the switch writes through the field itself and one of them writes a rotation the box was
    /// never given. What changed is the difference between what was there before the widget was
    /// drawn and what is there after it.
    /// </para>
    /// <para>
    /// Under the field's own name as the key, so a drag along a handle or a number typed one
    /// character at a time is one edit rather than one per frame. The key on an edit exists for
    /// that.
    /// </para>
    /// </remarks>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">What the field belongs to.</param>
    /// <param name="schema">The component it is part of, which names the edit.</param>
    /// <param name="field">Which field.</param>
    /// <param name="id">What the field is called, which makes a run of edits one edit.</param>
    /// <param name="before">What it held before the widget was drawn.</param>
    private static void Recorded(
        BehaviorContext ctx,
        Entity entity,
        ComponentSchema schema,
        ComponentField field,
        string id,
        object? before)
    {
        var after = field.Read(ctx.Ecs, entity);

        if (Equals(before, after)) return;

        // Only what can be put back. A field that reads as nothing has no value to write, and an
        // undo that writes nothing is one that says it did something and did not.
        if (before is { } was && after is { } now)
        {
            EditorHistory.Record(
                $"{schema.Name}.{field.Title}",
                world => field.Write(world, entity, was),
                world => field.Write(world, entity, now),
                id);
        }

        // And whatever the field asked to have run once it had changed, which is how a component
        // keeps something worked out from a field in step with it.
        foreach (var wanted in field.Hints.Changed)
        {
            foreach (var method in schema.Methods)
            {
                if (method.Name == wanted) method.Run(ctx.Ecs, entity);
            }
        }
    }

    /// <summary>One field, drawn as what it is.</summary>
    internal static void Row(
        BehaviorContext ctx, Entity entity, ComponentSchema schema, ComponentField field)
    {
        if (!Shown(ctx, entity, schema, field)) return;

        var id = $"##{schema.Name}.{field.Name}";
        var value = field.Read(ctx.Ecs, entity);

        // What the rest of the selection holds, so the row can say they disagree and an edit can
        // reach all of them. One entity selected is the usual case and costs nothing here.
        var others = Others(ctx, entity, schema);
        var differs = Differs(ctx, others, field, value);

        if (field.Hints.Header is { Length: > 0 } heading)
        {
            EditorSurface.Heading(heading, DetailsPanel.Inset);
        }

        // What the field asked for above itself, in the order a person reads them.
        if (field.Hints.Space) ImGui.Spacing();
        if (field.Hints.Separator) EditorTheme.Divide();
        if (field.Hints.Note is { Length: > 0 } note) Note(note, field.Hints.NoteKind);

        ImGui.PushID(id);

        // Short of the card's edge on the right by the same air it keeps on the left, or the last
        // field in every row runs into the side of the card it is drawn in.
        var across = new Vector2(
            MathF.Max(1f, ImGui.GetContentRegionAvail().X - DetailsPanel.Inset),
            0f);

        // A field that asked for the whole row gets it, with its name on the line above rather than
        // in a column beside it. Anything a name column would leave no room for needs that, such as
        // a sentence, a path or a color.
        var wide = field.Hints.Wide;

        if (wide)
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted(field.Title);

            if (field.Hints.Tooltip is { Length: > 0 } says && ImGui.IsItemHovered())
            {
                EditorWidgets.Tip(says);
            }

            ImGui.SetNextItemWidth(across.X);
        }
        else
        {
            if (!EditorRows.Open("##row", across))
            {
                ImGui.PopID();
                return;
            }

            EditorRows.Line(field.Title, field.Hints.Tooltip, differs);
        }

        var editable = field.IsWritable;
        if (!editable) ImGui.BeginDisabled();

        Edit(ctx, entity, field, id, value);

        // Written whole only while the box is open, which it is until ImGui says the widget is no
        // longer in use.
        FieldNumbers.Close(id);

        if (editable) Recorded(ctx, entity, schema, field, id, value);

        // And the same value onto everything else selected.
        if (editable) Spread(ctx, entity, others, field, value);

        if (!editable) ImGui.EndDisabled();

        if (field.Hints.Unit is { Length: > 0 } unit)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(unit);
        }

        if (!wide) EditorRows.Close();

        ImGui.PopID();
    }

    /// <summary>
    /// Everything else selected that carries the same component.
    /// </summary>
    /// <remarks>
    /// The panel is about one entity and a selection is often several, so an edit made here is
    /// meant for all of them. Anything in the selection without the component is left out rather
    /// than given one, because a person editing a field asked to change a value and not to put a
    /// component on something.
    /// </remarks>
    private static List<Entity> Others(BehaviorContext ctx, Entity entity, ComponentSchema schema)
    {
        var others = new List<Entity>();

        if (EditorSelection.Count <= 1) return others;

        foreach (var other in EditorSelection.All)
        {
            if (other == entity || !ctx.Ecs.IsAlive(other)) continue;

            foreach (var id in ctx.Ecs.ComponentsOf(other))
            {
                if (ComponentSchemas.For(id) != schema) continue;

                others.Add(other);
                break;
            }
        }

        return others;
    }

    /// <summary>Whether any of them holds something other than what the panel is showing.</summary>
    private static bool Differs(
        BehaviorContext ctx, List<Entity> others, ComponentField field, object? value)
    {
        foreach (var other in others)
        {
            if (!Same(field.Read(ctx.Ecs, other), value)) return true;
        }

        return false;
    }

    /// <summary>
    /// Copies what one field now holds onto the rest of the selection, if it changed.
    /// </summary>
    /// <remarks>
    /// Worked out by asking what the field holds after the widget ran rather than by every widget
    /// reporting it, so a kind of field added later is covered without being told to be. A widget
    /// that changed nothing leaves the two equal and nothing is written, which matters because a
    /// write is a change the history records.
    /// </remarks>
    /// <param name="ctx">This frame.</param>
    /// <param name="entity">The one the panel is showing.</param>
    /// <param name="others">The rest of the selection carrying the same component.</param>
    /// <param name="field">Which field.</param>
    /// <param name="before">What it held before the widget ran.</param>
    /// <returns>How many others were written.</returns>
    internal static int Spread(
        BehaviorContext ctx,
        Entity entity,
        IReadOnlyList<Entity> others,
        ComponentField field,
        object? before)
    {
        if (others.Count == 0 || !field.IsWritable) return 0;

        var now = field.Read(ctx.Ecs, entity);
        if (Same(now, before)) return 0;

        var written = 0;

        foreach (var other in others)
        {
            if (!ctx.Ecs.IsAlive(other)) continue;

            field.Write(ctx.Ecs, other, now);
            written++;
        }

        return written;
    }

    /// <summary>Whether two field values are the same, for a value that may be null.</summary>
    /// <remarks>
    /// By <see cref="object.Equals(object?, object?)"/>, which is right for every kind a field
    /// holds here, since each is a struct or a string and compares by value.
    /// </remarks>
    private static bool Same(object? left, object? right) => Equals(left, right);

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

                // Three numbers that are a color are a color, and nobody reads one as numbers.
                if (field.Hints.Color)
                {
                    var shade = new Vector4(three, 1f);

                    if (EditorWidgets.Swatch(id, ref shade))
                    {
                        field.Write(ctx.Ecs, entity, new Vec3(shade.X, shade.Y, shade.Z));
                    }

                    break;
                }

                ImGui.PushFont(ImGuiRuntime.Face(EditorShell.Figures));

                var moved = FieldNumbers.Vector(id, ref three, Pixel(field), out _);

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

                // Let go of the angles the moment the box is let go of, so the rotation shows what
                // the world says again rather than what was last typed.
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

                    // What the bar says about its own number. A box beside it is for a value that
                    // has to be typed exactly as well as dragged roughly, and nothing at all is
                    // for one where the position is the whole of the answer.
                    var readout = field.Hints.Readout;

                    // What the bar writes on itself, which the bar draws rather than ImGui, so it
                    // can be kept out from under the handle.
                    var inside = readout == SliderReadout.Number
                        ? FieldNumbers.Say(number)
                        : string.Empty;

                    if (readout == SliderReadout.Box)
                    {
                        ImGui.SetNextItemWidth(
                            MathF.Max(40f, ImGui.CalcItemWidth() - Boxed - ImGui.GetStyle().ItemSpacing.X));
                    }

                    changed = EditorWidgets.Sliding(
                        id,
                        high > low ? (number - low) / (high - low) : 0f,
                        () => ImGui.SliderFloat(id, ref held, low, high, format, FieldNumbers.Whole),
                        inside);

                    number = held;

                    if (readout == SliderReadout.Box)
                    {
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(Boxed);

                        if (ImGui.DragFloat(
                                $"{id}.box",
                                ref number,
                                Pixel(field),
                                low,
                                high,
                                FieldNumbers.Written($"{id}.box", number),
                                FieldNumbers.Whole))
                        {
                            changed = true;
                        }

                        FieldNumbers.Close($"{id}.box");
                    }
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
                FieldPickers.Asset(ctx, entity, field, id, value);
                break;

            case FieldKind.Entity:
                FieldPickers.Link(ctx, entity, field, id, value);
                break;

            case FieldKind.Flags:
                FieldPickers.Flags(ctx, entity, field, id, value);
                break;

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
}
