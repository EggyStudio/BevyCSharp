#:package Microsoft.CodeAnalysis.CSharp@5.0.0
#:property PublishAot=false

// What each of the engine's projects reaches of another's internals, which InternalsVisibleTo opens
// to it, the list build/norm/1.6.txt holds and each project file names under its openings.
//
//     dotnet build BevyCSharp.slnx
//     dotnet run build/norm/internals.cs              each opening, what it reaches, the counts
//     dotnet run build/norm/internals.cs -- --write   and each project file's comment from it
//
// Each project that an opening names is compiled from its own sources, with the engine's generator
// run over them, against the assemblies its last Debug build put beside it, and every name in it is
// asked which assembly declares it and whether that declaration is internal there. A build without
// an opening says the same only up to the first errors it stops at, and a comment written by hand
// misses what a later change reaches, so the list is the compiler's. A name inside an internal type
// is counted as the type, and an internal member of a public type as the type and the member, an
// internal constructor as "new" and the type.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "AGENTS.md")) || !Directory.Exists(Path.Combine(root, "build", "norm")))
{
    Console.Error.WriteLine("run this from the checkout's root, after a build");
    return 2;
}

var write = args.Contains("--write");
var generatorFile = Path.Combine(root, "BevyCSharp.Generator", "bin", "Debug", "netstandard2.0", "BevyCSharp.Generator.dll");
if (!File.Exists(generatorFile))
{
    Console.Error.WriteLine($"no {Path.GetRelativePath(root, generatorFile)}, so build the solution first");
    return 2;
}

var generators = new AnalyzerFileReference(generatorFile, new Loader()).GetGenerators(LanguageNames.CSharp);
var framework = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
var parse = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: ["DEBUG", "TRACE", "NET", "NET10_0", "NET10_0_OR_GREATER"]);

// The projects that may be opened to, each by the assembly its file names, and the openings each
// library project gives, by the assembly it opens to.
var projectFiles = Directory.GetFiles(Path.Combine(root, "BevyCSharp"), "*.csproj", SearchOption.AllDirectories)
    .Where(file => !Path.GetRelativePath(root, file).Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
    .Append(Path.Combine(root, "BevyCSharp.Cli", "BevyCSharp.Cli.csproj"))
    .Append(Path.Combine(root, "BevyCSharp.Scripting", "BevyCSharp.Scripting.csproj"))
    .ToDictionary(AssemblyOf, file => file, StringComparer.Ordinal);
var given = projectFiles
    .Where(project => project.Value.StartsWith(Path.Combine(root, "BevyCSharp") + Path.DirectorySeparatorChar, StringComparison.Ordinal))
    .SelectMany(project => Regex.Matches(File.ReadAllText(project.Value), "<InternalsVisibleTo Include=\"([^\"]+)\"").Select(m => (Provider: project.Key, Consumer: m.Groups[1].Value)))
    .ToHashSet();

var reached = new SortedDictionary<(string Provider, string Consumer), SortedSet<string>>();
foreach (var consumer in given.Select(pair => pair.Consumer).Distinct().Order(StringComparer.Ordinal))
{
    if (!projectFiles.TryGetValue(consumer, out var project))
    {
        Console.Error.WriteLine($"an opening to {consumer}, which no project here builds");
        return 1;
    }

    foreach (var ((provider, _), names) in Reached(consumer, project))
        reached[(provider, consumer)] = names;
}

var unused = given.Where(pair => !reached.ContainsKey(pair)).Order().ToList();
var missing = reached.Keys.Where(pair => !given.Contains(pair)).ToList();
foreach (var ((provider, consumer), names) in reached)
    Console.WriteLine($"{provider} opens to {consumer}\t{names.Count}\t{string.Join(", ", names)}");
Console.WriteLine($"{reached.Count} openings reached, {reached.Values.Sum(names => names.Count)} internal names reached through them");
foreach (var (provider, consumer) in unused) Console.WriteLine($"{provider} opens to {consumer} and nothing reaches through it");
foreach (var (provider, consumer) in missing) Console.WriteLine($"{consumer} reaches {provider}'s internals with no opening");

if (write)
{
    foreach (var (provider, file) in projectFiles)
    {
        var text = File.ReadAllText(file);
        var written = Regex.Replace(text, "(?:[ \\t]*<!--[^\\n]*-->\\r?\\n)?([ \\t]*)<InternalsVisibleTo Include=\"([^\"]+)\"/>", m =>
            reached.TryGetValue((provider, m.Groups[2].Value), out var names)
                ? $"{m.Groups[1].Value}<!-- {string.Join(", ", names)} -->\n{m.Groups[1].Value}<InternalsVisibleTo Include=\"{m.Groups[2].Value}\"/>"
                : m.Value);
        if (written != text) File.WriteAllText(file, written);
    }
}

return unused.Count == 0 && missing.Count == 0 ? 0 : 1;

// Every internal name of another engine assembly the project's sources reach, by that assembly.
Dictionary<(string Provider, string Consumer), SortedSet<string>> Reached(string consumer, string project)
{
    var folder = Path.GetDirectoryName(project)!;
    var built = Path.Combine(folder, "bin", "Debug", "net10.0");
    if (!File.Exists(Path.Combine(built, consumer + ".dll")))
        throw new InvalidOperationException($"no {consumer}.dll in {Path.GetRelativePath(root, built)}, so build the solution first");

    // The flat project compiles its top folder's files alone, the areas below it being projects
    // of their own, and each other project every file below its own folder.
    var flat = Path.GetFileName(project) == "BevyCSharp.csproj";
    var files = Directory.GetFiles(folder, "*.cs", flat ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories)
        .Where(file => Path.GetRelativePath(folder, file).Split(Path.DirectorySeparatorChar) is not ["bin" or "obj", ..])
        .Concat(Directory.GetFiles(Path.Combine(folder, "obj", "Debug", "net10.0"), "*.GlobalUsings.g.cs"))
        .ToList();
    var trees = files.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), parse, file)).ToList();
    var references = Directory.GetFiles(built, "*.dll")
        .Where(file => Path.GetFileNameWithoutExtension(file) != consumer)
        .Concat(framework)
        .GroupBy(Path.GetFileName)
        .Select(group => group.First())
        .Where(IsAssembly)
        .Select(file => (MetadataReference)MetadataReference.CreateFromFile(file));
    var compilation = CSharpCompilation.Create(consumer, trees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
    var additional = Regex.Matches(File.ReadAllText(project), "<AdditionalFiles Include=\"([^\"]+)\"")
        .Select(m => (AdditionalText)new FileText(Path.Combine(folder, m.Groups[1].Value)))
        .ToImmutableArray();
    CSharpGeneratorDriver.Create(generators, additional, parse).RunGeneratorsAndUpdateCompilation(compilation, out var generated, out _);

    var found = new Dictionary<(string, string), SortedSet<string>>();
    foreach (var tree in generated.SyntaxTrees)
    {
        var model = generated.GetSemanticModel(tree);
        foreach (var node in tree.GetRoot().DescendantNodes().Where(node => node is SimpleNameSyntax or BaseObjectCreationExpressionSyntax))
        {
            var info = model.GetSymbolInfo(node);
            var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
            if (symbol is IMethodSymbol { ReducedFrom: { } extension }) symbol = extension;
            if (symbol is null or IParameterSymbol or ILocalSymbol or ILabelSymbol or IRangeVariableSymbol or ITypeParameterSymbol) continue;
            if (symbol.ContainingAssembly is not { Name: var provider } || provider == consumer || !projectFiles.ContainsKey(provider) || !Internal(symbol)) continue;

            if (!found.TryGetValue((provider, consumer), out var names)) found[(provider, consumer)] = names = new SortedSet<string>(StringComparer.Ordinal);
            names.Add(Label(symbol));
        }
    }

    return found;
}

// A name as the list gives it, the outermost internal type holding it where one does.
static string Label(ISymbol symbol)
{
    INamedTypeSymbol? outer = null;
    for (var type = symbol as INamedTypeSymbol ?? symbol.ContainingType; type is not null; type = type.ContainingType)
        if (type.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal) outer = type;
    if (outer is not null) return Name(outer);
    return symbol is IMethodSymbol { MethodKind: MethodKind.Constructor } ? $"new {Name(symbol.ContainingType)}" : $"{Name(symbol.ContainingType)}.{symbol.Name}";
}

static string Name(INamedTypeSymbol type) => type.ContainingType is { } outer ? $"{Name(outer)}.{type.Name}" : type.Name;

static bool Internal(ISymbol symbol)
{
    for (var at = symbol; at is not null and not INamespaceSymbol; at = at.ContainingSymbol)
        if (at.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedAndInternal) return true;
    return false;
}

static bool IsAssembly(string file)
{
    try
    {
        AssemblyName.GetAssemblyName(file);
        return true;
    }
    catch (BadImageFormatException)
    {
        return false;
    }
}

static string AssemblyOf(string project) =>
    Regex.Match(File.ReadAllText(project), "<AssemblyName>([^<]+)</AssemblyName>") is { Success: true } m ? m.Groups[1].Value : Path.GetFileNameWithoutExtension(project);

// The generator is loaded into this process as an analyzer is into the compiler's.
sealed class Loader : IAnalyzerAssemblyLoader
{
    public void AddDependencyLocation(string fullPath) { }

    public Assembly LoadFromPath(string fullPath) => Assembly.LoadFrom(fullPath);
}

// A file the project gives its generator, as its AdditionalFiles items do.
sealed class FileText(string path) : AdditionalText
{
    public override string Path => path;

    public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(File.ReadAllText(path));
}
