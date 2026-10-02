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
        + "# field <type path> <name> <reflect path> <kind> <rust type> <options or asset kind>\n";

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
    /// which leaves out the JSON rows and the rows of an enum variant, since a variant's fields are
    /// there only while that variant is held. The bridge's own components are left out too, being
    /// its business rather than a game's.
    /// </remarks>
    internal static List<string> Describe(string description)
    {
        var schemas = ReflectedSchemas.Build(description, new HashSet<int>(), new HashSet<string>())
            .Where(schema => !schema.QualifiedName.StartsWith("bevy_csharp::", StringComparison.Ordinal))
            .OrderBy(schema => schema.QualifiedName, StringComparer.Ordinal);

        var lines = new List<string>();
        foreach (var schema in schemas)
        {
            lines.Add($"component\t{schema.QualifiedName}\t{schema.Name}");

            foreach (var field in schema.Fields)
            {
                if (field.ReflectPath is null || field.Hints.Conditions.Count > 0) continue;

                var extra = field.Kind switch
                {
                    FieldKind.Enum => string.Join(',', field.Options),
                    FieldKind.Asset => field.Hints.Asset ?? string.Empty,
                    _ => string.Empty,
                };

                lines.Add(string.Join('\t',
                    "field", schema.QualifiedName, field.Name, field.ReflectPath, field.Kind,
                    field.Type, extra));
            }
        }

        return lines;
    }
}
