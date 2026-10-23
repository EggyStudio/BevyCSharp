using System.Text.RegularExpressions;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The bridge's Slang modules, read as files, giving their names plainly, as 3DEngine's modules do,
/// with no namespace for a shader to name before them, and each name of one module unlike every
/// other module's, so a shader importing two meets no name twice.
/// </summary>
public sealed partial class SlangModuleTests
{
    private static readonly string Modules = Path.Combine(CheatsheetTests.RepositoryRoot(), "native", "bevy_csharp", "src", "render");

    private static IEnumerable<string> Files() => Directory.GetFiles(Modules, "bcs*.slang").Order();

    /// <summary>The code of a module, its comments left out.</summary>
    private static IEnumerable<string> Code(string file) =>
        File.ReadAllLines(file).Select(line => line.Contains("//", StringComparison.Ordinal) ? line[..line.IndexOf("//", StringComparison.Ordinal)] : line);

    [Fact]
    public void NoModuleDeclaresANamespace()
    {
        var files = Files().ToList();
        Assert.True(files.Count >= 10, $"{files.Count} modules found in {Modules}");

        var declaring = files.Where(file => Code(file).Any(line => Namespace().IsMatch(line))).Select(Path.GetFileName);
        Assert.Empty(declaring);
    }

    [Fact]
    public void NoTwoModulesDeclareOneName()
    {
        var names = Files()
            .SelectMany(file => Code(file).Select(line => Declared().Match(line)).Where(match => match.Success)
                .Select(match => (Name: match.Groups["name"].Value, File: Path.GetFileName(file))))
            .Distinct()
            .GroupBy(declared => declared.Name)
            .Where(group => group.Select(declared => declared.File).Distinct().Count() > 1)
            .Select(group => $"{group.Key} in {string.Join(", ", group.Select(declared => declared.File))}");

        Assert.Empty(names);
    }

    [GeneratedRegex(@"\bnamespace\b")]
    private static partial Regex Namespace();

    // A public name at a module's top level, a type, a function, a constant or a global, at the
    // start of its line, which a struct's own fields, indented, are not.
    [GeneratedRegex(@"^(?:\[\[[^\]]*\]\]\s*)?public\s+(?:struct|enum|static const \w+|[\w<>, :]+?)\s+(?<name>\w+)\s*(?:[(;<=]|$)")]
    private static partial Regex Declared();
}
