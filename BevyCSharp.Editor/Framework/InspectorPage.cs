using System.Globalization;
using System.Text;
using Bevy;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// What is selected, drawn as rows that can be edited.
/// </summary>
/// <remarks>
/// <para>
/// A component's fields come from the schema the generator emitted, so nothing here names a type
/// and nothing reflects at runtime. What a field is drawn as follows from its kind: a checkbox for
/// a flag, a field for a number, three across for a vector, a button that offers the list for a
/// choice.
/// </para>
/// <para>
/// Written again only when what it says has changed. An edit in flight is left alone: the row
/// somebody is typing into is not rewritten under them, which is the whole of what the old
/// framework needed a settling period and a generation counter for.
/// </para>
/// </remarks>
public static class InspectorPage
{
    /// <summary>The fields on screen, by the id their row carries.</summary>
    private static readonly Dictionary<string, ComponentField> Rows = [];

    private static string _written = string.Empty;
    private static Entity _shown = Entity.None;

    /// <summary>Draws the selection, or says there is none.</summary>
    public static void Draw(BehaviorContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        if (!EditorSelection.Any)
        {
            Rows.Clear();
            _shown = Entity.None;
            Fill("<div class=\"empty\">Nothing selected</div>");
            return;
        }

        var entity = EditorSelection.Current;
        var markup = new StringBuilder();

        Rows.Clear();

        markup.Append("<div class=\"group\"><header>")
            .Append(EditorShell.Escape(ctx.Ecs.NameOf(entity) ?? $"Entity {entity.Index}"))
            .Append("</header></div>");

        foreach (var id in ctx.Ecs.ComponentsOf(entity))
        {
            if (ComponentSchemas.For(id) is not { } schema)
            {
                // A component with no schema is still worth naming: it says what the thing is,
                // even where nothing about it can be edited.
                markup.Append("<div class=\"group dim\"><header>")
                    .Append(EditorShell.Escape(Short(ctx.Ecs.ComponentName(id))))
                    .Append("</header></div>");

                continue;
            }

            markup.Append("<div class=\"group\"><header>")
                .Append(EditorShell.Escape(schema.Name))
                .Append("</header>");

            foreach (var field in schema.Fields)
            {
                if (field.Hints.Hidden) continue;

                // A heading, a gap or a line before the row, if the field asked for one. What
                // separates a group of values from the next is the field's own declaration.
                if (field.Hints.Header is { Length: > 0 } heading)
                {
                    markup.Append("<div class=\"heading\">")
                        .Append(EditorShell.Escape(heading))
                        .Append("</div>");
                }
                else if (field.Hints.Separator)
                {
                    markup.Append("<div class=\"rule\"></div>");
                }

                Field(markup, ctx, entity, schema, field);
            }

            markup.Append("</div>");
        }

        var drawn = markup.ToString();

        // The whole panel is one comparison, because a panel that is the same panel costs a string
        // compare and a panel that is not has to be parsed either way.
        if (drawn == _written && entity == _shown) return;

        _written = drawn;
        _shown = entity;
        Fill(drawn);
    }

    /// <summary>One field, drawn as what it is.</summary>
    private static void Field(
        StringBuilder markup,
        BehaviorContext ctx,
        Entity entity,
        ComponentSchema schema,
        ComponentField field)
    {
        var id = $"f-{schema.Name}-{field.Name}";
        Rows[id] = field;

        var value = field.Read(ctx.Ecs, entity);
        var editable = field.IsWritable;

        markup.Append(field.Hints.Unit is { Length: > 0 } ? "<div class=\"row united\"><label" : "<div class=\"row\"><label")
            .Append(field.Hints.Tooltip is { Length: > 0 } why
                ? $" title=\"{EditorShell.Escape(why)}\">"
                : ">")
            .Append(EditorShell.Escape(field.Title))
            .Append("</label>");

        switch (field.Kind)
        {
            case FieldKind.Bool:
                // The state is on the element and the stylesheet draws the tick, so what a checked
                // box looks like is a rule rather than a character chosen here.
                markup.Append($"<div class=\"check\" id=\"{id}\" data-on=\"")
                    .Append(value is true ? "1" : "0")
                    .Append("\"></div>");
                break;

            case FieldKind.Vec3:
                var vector = value as Vec3? ?? default;
                markup.Append("<div class=\"triple\">");
                Number(markup, $"{id}-x", vector.X, editable);
                Number(markup, $"{id}-y", vector.Y, editable);
                Number(markup, $"{id}-z", vector.Z, editable);
                markup.Append("</div>");
                break;

            case FieldKind.Enum:
            case FieldKind.Flags:
                markup.Append($"<div class=\"button wide\" id=\"{id}\">")
                    .Append(EditorShell.Escape(value?.ToString() ?? "-"))
                    .Append("</div>");
                break;

            case FieldKind.Int:
            case FieldKind.Float:
            case FieldKind.Double:
                var number = Convert.ToDouble(value ?? 0, CultureInfo.InvariantCulture);

                // A field that said what it runs between is a bar with the number beside it, which
                // is what a range means everywhere else.
                if (field.Hints is { HasRange: true, Minimum: { } least, Maximum: { } most })
                {
                    var step = field.Hints.Step ?? (field.Kind == FieldKind.Int ? 1.0 : 0.001);

                    markup.Append("<div class=\"ranged\">")
                        .Append($"<input class=\"bar\" id=\"{id}\" type=\"range\"")
                        .Append($" min=\"{least.ToString(CultureInfo.InvariantCulture)}\"")
                        .Append($" max=\"{most.ToString(CultureInfo.InvariantCulture)}\"")
                        .Append($" step=\"{step.ToString(CultureInfo.InvariantCulture)}\"")
                        .Append($" value=\"{number.ToString("0.###", CultureInfo.InvariantCulture)}\" />")
                        .Append($"<span class=\"readout\">{number.ToString("0.##", CultureInfo.InvariantCulture)}</span>")
                        .Append("</div>");

                    break;
                }

                Number(markup, id, number, editable);
                break;

            default:
                markup.Append("<span class=\"field dim\">")
                    .Append(EditorShell.Escape(value?.ToString() ?? "-"))
                    .Append("</span>");
                break;
        }

        // What the number is measured in, in a column of its own after the value.
        if (field.Hints.Unit is { Length: > 0 } unit)
        {
            markup.Append("<span class=\"unit\">").Append(EditorShell.Escape(unit)).Append("</span>");
        }

        markup.Append("</div>");
    }

    /// <summary>A number, typed into when it can be.</summary>
    private static void Number(StringBuilder markup, string id, double value, bool editable)
    {
        var said = value.ToString("0.###", CultureInfo.InvariantCulture);

        if (!editable)
        {
            markup.Append($"<span class=\"field dim\" id=\"{id}\">{said}</span>");
            return;
        }

        markup.Append($"<input class=\"field\" id=\"{id}\" type=\"text\" value=\"{said}\" />");
    }

    /// <summary>Takes what a row says and writes it back to the component.</summary>
    /// <remarks>
    /// Called when a field is finished with rather than while it is being typed in, because a
    /// number half typed is not a number and writing it back would fight the typing.
    /// </remarks>
    public static void Accepted(BehaviorContext ctx, string id)
    {
        if (!EditorSelection.Any) return;

        var entity = EditorSelection.Current;

        // A vector's parts each carry the field's id with an axis after it.
        var axis = id.Length > 2 && id[^2] == '-' ? id[^1] : '\0';
        var key = axis is 'x' or 'y' or 'z' ? id[..^2] : id;

        if (!Rows.TryGetValue(key, out var field) || !field.IsWritable) return;

        var element = Dom.Element(id);
        if (!element.Exists) return;

        var typed = Dom.GetValue(element).Trim();

        if (!double.TryParse(typed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
        {
            // Left as it was, and put back, so a field that was typed nonsense into says what the
            // value actually is rather than keeping the nonsense on screen.
            _written = string.Empty;
            return;
        }

        var written = field.Kind switch
        {
            FieldKind.Vec3 => Vector(ctx, entity, field, axis, (float)number),
            FieldKind.Int => field.Write(ctx.Ecs, entity, (int)number),
            FieldKind.Float => field.Write(ctx.Ecs, entity, (float)number),
            FieldKind.Double => field.Write(ctx.Ecs, entity, number),
            _ => false,
        };

        // Drawn again either way: what the component made of the value is what should be on screen,
        // and a component is free to clamp or round what it was given.
        if (written) _written = string.Empty;
    }

    /// <summary>Changes one part of a vector, keeping the other two.</summary>
    private static bool Vector(
        BehaviorContext ctx, Entity entity, ComponentField field, char axis, float number)
    {
        var current = field.Read(ctx.Ecs, entity) as Vec3? ?? default;

        var changed = axis switch
        {
            'x' => new Vec3(number, current.Y, current.Z),
            'y' => new Vec3(current.X, number, current.Z),
            'z' => new Vec3(current.X, current.Y, number),
            _ => current,
        };

        return field.Write(ctx.Ecs, entity, changed);
    }

    /// <summary>Turns a flag on or off when its box is clicked.</summary>
    public static bool Clicked(BehaviorContext ctx, string id)
    {
        if (!EditorSelection.Any) return false;
        if (!Rows.TryGetValue(id, out var field)) return false;
        if (field.Kind != FieldKind.Bool || !field.IsWritable) return false;

        var entity = EditorSelection.Current;
        var now = field.Read(ctx.Ecs, entity) is true;

        if (field.Write(ctx.Ecs, entity, !now)) _written = string.Empty;

        return true;
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
        var trimmed = new StringBuilder();
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

    private static void Fill(string markup)
    {
        _written = markup;
        EditorShell.Fill("inspector", markup);
    }
}
