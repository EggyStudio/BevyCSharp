using System.Reflection;
using System.Runtime.CompilerServices;

namespace Bevy;

/// <summary>
/// The engine's assemblies, the flat project's and each area's, loaded whole where an app is made or
/// a registry is read, so what each registers from its module initializer, its console commands,
/// its component schemas and its behaviors, is there before anything looks it up.
/// </summary>
/// <remarks>
/// A project's generated registrations run when its assembly is first touched, so without this a
/// scene file holding a body of the physics, read before anything touched the physics, would find no
/// schema for it, and the console would not know the levels' commands in a program that has loaded
/// no level. The names are kept here as text, in the lowest project that reads them, since Core
/// references none of the projects above it, and the norm test of the projects' layers holds the
/// list to the project files.
/// </remarks>
internal static class EngineAssemblies
{
    /// <summary>Every assembly of the engine, by name, the lowest first.</summary>
    internal static readonly string[] Names =
    [
        "BevyCSharp.Native", "BevyCSharp.Core", "BevyCSharp.Assets", "BevyCSharp.Audio", "BevyCSharp.Gizmos",
        "BevyCSharp.ImGui", "BevyCSharp.CliServer", "BevyCSharp.Rendering", "BevyCSharp.Render2d", "BevyCSharp.Animation",
        "BevyCSharp.GlobalIllumination", "BevyCSharp.Scenes", "BevyCSharp.Ui", "BevyCSharp.Physics", "BevyCSharp.Sky",
        "BevyCSharp.Modeling", "BevyCSharp.Diagnostics", "BevyCSharp.Vegetation", "BevyCSharp.Levels", "BevyCSharp",
    ];

    // A monitor, which the thread holding it enters again, as a module initializer reading a
    // registry does on the thread loading it.
    private static readonly object Gate = new();
    private static volatile bool _loaded;
    private static bool _loading;

    /// <summary>Loads each assembly of the engine the program has and runs its module initializer, once.</summary>
    /// <remarks>
    /// A module initializer that reads a registry on the thread loading it finds the loading under
    /// way and goes on with what is loaded so far, and another thread waits on the gate until every
    /// assembly is loaded, since the flag it reads first is set only once the last has.
    /// </remarks>
    internal static void Load()
    {
        if (_loaded) return;
        lock (Gate)
        {
            if (_loaded || _loading) return;
            _loading = true;
            try
            {
                foreach (var name in Names)
                {
                    try
                    {
                        var assembly = Assembly.Load(new AssemblyName(name));
                        RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
                    }
                    catch (Exception e) when (e is FileNotFoundException or FileLoadException or BadImageFormatException)
                    {
                        // A program built on some of the areas' packages and not the rest has fewer.
                    }
                }
            }
            finally
            {
                _loaded = true;
            }
        }
    }

    /// <summary>Whether <paramref name="assembly"/> is one of the engine's own.</summary>
    internal static bool Holds(Assembly assembly) => Names.Contains(assembly.GetName().Name);

    /// <summary>The engine's assemblies the program has, each loaded.</summary>
    internal static IEnumerable<Assembly> Loaded()
    {
        Load();
        return AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic && Holds(assembly));
    }
}
