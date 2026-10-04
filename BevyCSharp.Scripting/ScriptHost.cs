using System.Reflection;
using System.Runtime.Loader;
using Bevy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bevy.Scripting;

/// <summary>
/// Compiles behavior scripts from a directory and swaps them in while the app runs.
/// </summary>
/// <remarks>
/// <para>
/// A script is an ordinary <c>.cs</c> file holding an ordinary <c>[Behavior]</c> struct. Nothing
/// about it is special, because it is compiled with the same source generator the rest of the
/// project uses, so what it gets is the same runner, the same attributes and the same scheduling.
/// The only difference is when it is compiled.
/// </para>
/// <para>
/// Each build goes into a collectible load context of its own, and each generation registers under
/// a tag of its own. Reloading is therefore three steps, which are to compile the new one, retire
/// the old tag and drop the old context. A generation that fails to compile changes nothing, so a
/// half-typed file leaves the running one alone rather than taking the app down with it.
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

    /// <summary>
    /// How many components the last build moved onto the types it declared them as again, or put
    /// back on entities a scene loaded before there were types for them.
    /// </summary>
    public int Carried { get; private set; }

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

        // What the running generation's components are described by, before the new one's
        // descriptions take their names.
        var before = ReloadedComponents.Before();

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

            // What the script's own code threw, which a registration run by reflection or a
            // module initializer hands back wrapped in an exception that says only that it threw.
            var cause = e;
            while (cause is System.Reflection.TargetInvocationException or TypeInitializationException && cause.InnerException is { } inner)
                cause = inner;

            LastError = $"loading {name} failed: {cause.GetType().Name}: {cause.Message}";
            return false;
        }

        // The entities carrying the last generation's components carry the new one's instead, so
        // a level's walls stay walls and its player keeps what it carried. Only while the app
        // runs, since before then nothing can carry anything and the world is not on loan.
        // A scene loaded before the scripts were holds their components as the file had them,
        // and those go on their entities now that there are types for them.
        Carried = app.IsRunning
            ? ReloadedComponents.Carry(app.World.Resource<EcsWorld>(), before) + ReloadedComponents.Revive(app.World.Resource<EcsWorld>())
            : 0;

        // Only once the new generation is in. Retiring the old one first would leave a frame
        // with neither, and a failure above would leave the app with nothing at all.
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

        // The same generators the compiled projects use, so a script gets the same runner rather
        // than a second, subtly different way of being a behavior, and a state it declares on its
        // enum is declared as the game's own would be.
        var driver = CSharpGeneratorDriver.Create(
            new Bevy.Generator.BehaviorGenerator().AsSourceGenerator(),
            new Bevy.Generator.StateGenerator().AsSourceGenerator());

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
    /// The generator emits one tagged entry point per assembly. Finding it by attribute rather than
    /// by name lets each generation carry its own copy without colliding.
    /// </remarks>
    private int Register(Assembly assembly)
    {
        // The module initializers first, which declare the assembly's schemas, states and
        // commands. The runtime runs them only once something in the module is touched, and a
        // script of components alone has no registration below to touch it, so its components
        // would have no schema for a level to be read with.
        System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);

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
