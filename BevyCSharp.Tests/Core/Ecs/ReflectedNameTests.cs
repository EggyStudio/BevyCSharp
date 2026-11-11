using Xunit;

namespace Bevy.Tests;

/// <summary>
/// Covers the wrappers of Bevy's reflected components keeping apart from the names the library
/// gives types in the Bevy namespace, in every project and not in Core alone.
/// </summary>
/// <remarks>
/// The generator compiles the wrappers in Core, whose compilation sees none of the projects above
/// it, so the names those give types in the Bevy namespace reach it in
/// <c>Core/Generated/bevy-taken-names.txt</c>. A wrapper of the same name as one of them would make
/// either ambiguous in code that uses both namespaces, as an example naming
/// <see cref="TileOrientation"/> found it before the list.
/// </remarks>
public sealed class ReflectedNameTests
{
    [Fact]
    public void NoWrapperSharesItsNameWithATypeOfTheLibrary()
    {
        var library = BevyNames(EngineAssemblies.Loaded());
        var shared = typeof(App).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "Bevy.Reflected" && !type.IsNested && library.Contains(type.Name))
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal);
        Assert.Empty(shared);
    }

    [Fact]
    public void EachListedNameIsATypeOfAProjectAboveCore()
    {
        var above = BevyNames(EngineAssemblies.Loaded().Where(assembly => assembly != typeof(App).Assembly && assembly != typeof(Interop.Native).Assembly));
        var listed = File.ReadLines(Path.Combine(CheatsheetTests.RepositoryRoot(), "BevyCSharp", "Core", "Generated", "bevy-taken-names.txt"))
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'));
        var stale = listed.Where(name => !above.Contains(name)).ToList();
        Assert.True(stale.Count == 0, $"bevy-taken-names.txt lists names no project above Core gives a type in Bevy: {string.Join(", ", stale)}");
    }

    // The names of the types at the top of the Bevy namespace in the assemblies.
    private static HashSet<string> BevyNames(IEnumerable<System.Reflection.Assembly> assemblies) =>
        assemblies.SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Namespace == "Bevy" && !type.IsNested)
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);
}
