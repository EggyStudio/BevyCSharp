using System.Reflection;
using System.Runtime.Loader;
using Bevy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace BevyCSharp.Editor.Framework;

/// <summary>
/// Compiles behavior scripts from a directory and swaps them in while the app runs.
/// </summary>
/// <remarks>
/// <para>
/// A script is an ordinary <c>.cs</c> file holding an ordinary <c>[Behavior]</c> struct. Nothing
/// about it is special: it is compiled with the same source generator the rest of the project
/// uses, so what it gets is the same runner, the same attributes and the same scheduling. The
/// only difference is when it is compiled.
/// </para>
/// <para>
/// Each build goes into a collectible load context of its own, and each generation registers
/// under a tag of its own. Reloading is therefore: compile the new one, retire the old tag, drop
/// the old context. A generation that fails to compile changes nothing, so a half-typed file
/// leaves the running one alone rather than taking the editor down with it.
/// </para>
/// <para>
/// The app has to have had <see cref="App.EnableDynamicSystems"/> called before it started, or
/// there is nowhere for a late system to go.
/// </para>
/// </remarks>
public sealed class ScriptHost(App app, string directory)
{
    private ScriptLoadContext? _loaded;
    private string? _tag;
    private int _generation;

    /// <summary>Where the scripts are.</summary>
    public string Directory { get; } = directory;

    /// <summary>How many behaviors the last successful build registered.</summary>
    public int Registered { get; private set; }

    /// <summary>What went wrong with the last build, or null when it worked.</summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Compiles the directory and puts the result in place of whatever was there.
    /// </summary>
    /// <returns>Whether the build succeeded. A failure leaves the previous generation running.</returns>
    public bool Reload()
    {
        var sources = Sources();
        if (sources.Length == 0)
        {
            LastError = $"no .cs files under {Directory}";
            return false;
        }

        var name = $"Scripts.Generation{++_generation}";

        if (!Compile(sources, name, out var image, out var error))
        {
            LastError = error;
            return false;
        }

        var context = new ScriptLoadContext(name);
        int registered;

        try
        {
            using var stream = new MemoryStream(image!);
            registered = Register(context.LoadFromStream(stream));
        }
        catch (Exception e)
        {
            context.Unload();
            LastError = $"loading {name} failed: {e.Message}";
            return false;
        }

        // Only once the new generation is in. Retiring the old one first would leave a frame
        // with neither, and a failure above would leave the editor with nothing at all.
        Retire();

        _loaded = context;
        _tag = name;
        Registered = registered;
        LastError = null;
        return true;
    }

    /// <summary>Retires the running generation and unloads it.</summary>
    public void Retire()
    {
        if (_tag is not null) app.RemoveSystemsBySource(_tag);

        _loaded?.Unload();
        _loaded = null;
        _tag = null;
    }

    /// <summary>The script files, in a fixed order so a build is reproducible.</summary>
    private string[] Sources() => System.IO.Directory.Exists(Directory)
        ? [.. System.IO.Directory.GetFiles(Directory, "*.cs", SearchOption.AllDirectories).Order()]
        : [];

    /// <summary>Builds the scripts into an assembly image, generators and all.</summary>
    private static bool Compile(string[] sources, string name, out byte[]? image, out string? error)
    {
        image = null;
        error = null;

        // The usings the projects get from ImplicitUsings, plus Bevy itself. A script is meant to
        // be a short file someone edits while the editor runs, and starting every one with the
        // same handful of lines is ceremony that teaches nothing.
        const string Preamble =
            "global using System;\n"
            + "global using System.Collections.Generic;\n"
            + "global using System.Linq;\n"
            + "global using Bevy;\n";

        var trees = sources
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path))
            .Prepend(CSharpSyntaxTree.ParseText(Preamble, path: "<preamble>"))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            name,
            trees,
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // The same generator the compiled projects use, so a script gets the same runner rather
        // than a second, subtly different way of being a behavior.
        var driver = CSharpGeneratorDriver.Create(
            new Bevy.Generator.BehaviorGenerator().AsSourceGenerator());

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var generated, out _);

        using var output = new MemoryStream();
        var result = generated.Emit(output);

        if (!result.Success)
        {
            error = string.Join(
                Environment.NewLine,
                result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .Take(10));
            return false;
        }

        image = output.ToArray();
        return true;
    }

    /// <summary>Everything a script is compiled against: this app, and what it already loaded.</summary>
    private static MetadataReference[] References() => [.. AppDomain.CurrentDomain
        .GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
        .Select(a => MetadataReference.CreateFromFile(a.Location))];

    /// <summary>
    /// Invokes the generated registration in a freshly loaded assembly.
    /// </summary>
    /// <remarks>
    /// The generator emits one tagged entry point per assembly. Finding it by attribute rather
    /// than by name is what lets each generation carry its own copy without colliding.
    /// </remarks>
    private int Register(Assembly assembly)
    {
        var found = 0;

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public
                                                   | BindingFlags.NonPublic))
            {
                if (method.GetCustomAttribute<GeneratedBehaviorRegistrationAttribute>() is null)
                    continue;

                using (new SystemRegistrationSourceScope(assembly.GetName().Name!))
                    method.Invoke(null, [app]);

                found++;
            }
        }

        return found;
    }

    /// <summary>One generation's assembly, in a context that can be dropped.</summary>
    private sealed class ScriptLoadContext(string name)
        : AssemblyLoadContext(name, isCollectible: true);
}
