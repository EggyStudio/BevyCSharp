using System.Text;

namespace Bevy;

/// <summary>
/// Writes down what Bevy's components look like, for the generator to turn into typed wrappers.
/// </summary>
/// <remarks>
/// <para>
/// A reflected component is reached by a string path, which fails when it is used rather than when
/// it is compiled, so a field Bevy renames breaks a program silently until the line runs. The
/// generator closes that gap by emitting a typed wrapper per component from a description checked
/// into the repository, and this writes that description from a running app, because only a
/// running app has Bevy's registry to read.
/// </para>
/// <para>
/// The description is lines of tab-separated words rather than JSON. The generator runs inside the
/// compiler on netstandard2.0, where a JSON reader would have to be carried into the analyzer, and
/// a sorted line per field diffs as a list of exactly what a Bevy upgrade changed.
/// </para>
/// </remarks>
internal static class ConsoleSchemaCommands
{
    /// <summary>The first line of the file, which says what it is and how it is made.</summary>
    private const string Header =
        "# Bevy's reflected components, for BevyCSharp.Generator to turn into Bevy.Reflected wrappers.\n"
        + "# Written by `./bcs command schema.dump <path>` against an editor build, which reflects the\n"
        + "# most. Regenerate it after upgrading Bevy, and read its diff as what Bevy changed.\n"
        + "# component <type path> <short name>\n"
        + "# field <type path> <name> <reflect path> <kind> <rust type> <options or asset kind>\n"
        + "# variant <type path> <enum field> <variant> <name> <reflect path> <kind> <rust type> <options or asset kind>\n";

    /// <summary>Writes the description of Bevy's components to a file.</summary>
    [Command("schema.dump", "Writes Bevy's components for the generator: schema.dump <path>")]
    internal static string Dump(string path)
    {
        var description = EcsWorld.DescribeReflected();
        if (description is null)
        {
            ConsoleHost.Fail("NO_WORLD", "Bevy's registry is read inside a frame.");
            return "Bevy's registry is read inside a frame";
        }

        var lines = Describe(description);
        var full = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, Header + string.Join('\n', lines) + '\n', new UTF8Encoding(false));

        var components = lines.Count(line => line.StartsWith("component\t", StringComparison.Ordinal));
        return $"wrote {components} components and {lines.Count - components} fields to {full}";
    }

    /// <summary>
    /// The lines of the description: each component, then its fields in the order Bevy declares
    /// them, with the components sorted by type path so a regeneration diffs only where Bevy did.
    /// </summary>
    /// <remarks>
    /// Built from the same schemas the inspector draws, with nothing a mirror covers taken out, so a
    /// wrapper and a row name every field alike. A field is written only when a wrapper can type it,
    /// which leaves out the JSON rows. A field of an enum's variant, there only while that variant
    /// is held, is a <c>variant</c> line naming the enum field and the variant, for the generator
    /// to make the variant a record, and its name is the part of its row's name after the
    /// variant's, empty for a variant wrapping one value. One inside a variant of an enum that is
    /// itself inside a variant names the inner enum's row and its variant, as an orthographic
    /// projection's fixed scaling mode holds its width, for the generator to make that enum a record
    /// of its own inside the outer one. A list of values with fields of their own is a line of the
    /// kind <c>List</c> naming its items' type, which an <c>item</c> line after the components
    /// describes with its own rows, their paths from the item, as a box shadow's list names its
    /// shadow style. The bridge's own components are left out, being its business rather than a
    /// game's.
    /// </remarks>
    internal static List<string> Describe(string description)
    {
        var schemas = ReflectedSchemas.Build(description, new HashSet<int>(), new HashSet<string>())
            .Where(schema => !schema.QualifiedName.StartsWith("bevy_csharp::", StringComparison.Ordinal))
            .OrderBy(schema => schema.QualifiedName, StringComparer.Ordinal);

        var lines = new List<string>();
        var items = new SortedDictionary<string, List<ComponentField>?>(StringComparer.Ordinal);
        foreach (var schema in schemas)
        {
            lines.Add($"component\t{schema.QualifiedName}\t{schema.Name}");
            Rows(lines, schema.QualifiedName, schema.Fields, items);
        }

        // The types lists hold as their items, each described once by its own rows after the
        // components, and the types the lists inside those hold in turn.
        while (items.FirstOrDefault(item => item.Value is null).Key is { } next)
        {
            var rows = ReflectedSchemas.ItemRows(description, next);
            items[next] = rows;
            foreach (var row in rows)
            {
                if (row.ItemType is { } inner) items.TryAdd(inner, null);
            }
        }

        foreach (var (item, rows) in items)
        {
            var cut = item.Split('<')[0].LastIndexOf("::", StringComparison.Ordinal);
            lines.Add($"item\t{item}\t{(cut < 0 ? item : item[(cut + 2)..])}");
            Rows(lines, item, rows!, items);
        }

        return lines;
    }

    /// <summary>
    /// The lines of one component's rows, or one item type's, and the item types the lists among
    /// them hold, added to <paramref name="items"/> for describing.
    /// </summary>
    private static void Rows(List<string> lines, string scope, IEnumerable<ComponentField> fields, IDictionary<string, List<ComponentField>?> items)
    {
        foreach (var field in fields)
        {
            if (field.ReflectPath is null) continue;

            // A list of records is written as one, its items' type in place of the options, and the
            // type described where it has a name, a tuple having no fields a record could name.
            if (field.ItemType is { } listed && !listed.StartsWith('(')) items.TryAdd(listed, null);
            var kind = field.ItemType is null ? field.Kind.ToString() : "List";
            var extra = field.ItemType ?? field.Kind switch
            {
                FieldKind.Enum => string.Join(',', field.Options),
                FieldKind.Asset => field.Hints.Asset ?? string.Empty,
                // A list of plain values names their kind, which the generator types each item by.
                FieldKind.List => field.ElementKind.ToString(),
                _ => string.Empty,
            };

            var conditions = field.Hints.Conditions;
            if (conditions.Count == 0)
            {
                lines.Add(string.Join('\t', "field", scope, field.Name, field.ReflectPath, kind, field.Type, extra));
            }
            else if (conditions.All(shown => shown is { Value: not null, Not: false }) && conditions[^1] is { Value: { } variant } condition)
            {
                // Named by the innermost variant it is under, so a field of a variant of an enum
                // inside another variant names that inner enum's row, which the generator finds
                // under the outer variant's value.
                var prefix = condition.Field.Length == 0 ? variant : condition.Field + "." + variant;
                if (!field.Name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var name = field.Name.Length == prefix.Length ? string.Empty : field.Name[(prefix.Length + 1)..];

                lines.Add(string.Join('\t', "variant", scope, condition.Field, variant, name, field.ReflectPath, kind, field.Type, extra));
            }
        }
    }
}
