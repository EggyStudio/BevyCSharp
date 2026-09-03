using System.Reflection;

namespace Bevy;

/// <summary>
/// Finds every generated behavior registration in the loaded assemblies and runs it.
/// </summary>
/// <remarks>
/// <para>
/// The point of this plugin is that a consuming project needs no registration call, no
/// partial-class list and no startup boilerplate: drop a <c>[Behavior]</c> struct into any
/// referenced assembly and it gets scheduled.
/// </para>
/// <para>
/// It gets there two ways. The generator emits a module initializer per assembly, so most
/// registrations have already announced themselves in <see cref="BehaviorRegistry"/> by the
/// time this runs, which is both fast and safe under trimming. For an assembly that is loaded
/// but not touched yet, its initializer has not fired, so the plugin also scans for methods
/// tagged <see cref="GeneratedBehaviorRegistrationAttribute"/> and picks up whatever the
/// registry is missing.
/// </para>
/// </remarks>
public sealed class BehaviorsPlugin : IPlugin
{
    /// <summary>
    /// Directory of behavior scripts to compile at runtime.
    /// </summary>
    /// <remarks>
    /// Reserved. Compiling C# needs a compiler, and a game should not carry one to run, so this
    /// library provides the two halves that are engine business and leaves the compiling to
    /// whoever wants it: <see cref="App.EnableDynamicSystems"/> for a system that arrives after
    /// the loop started, and <see cref="App.RemoveSystemsBySource"/> for retiring the generation
    /// it replaces. <c>BevyCSharp.Editor</c> has a host built on those two.
    /// </remarks>
    public string? ScriptsDirectory { get; init; }

    /// <summary>Provenance tag applied to statically compiled behaviors.</summary>
    public string StaticSourceTag { get; init; } = "Static.Behaviors";

    /// <summary>
    /// Whether to scan loaded assemblies for registrations the registry has not seen.
    /// </summary>
    /// <remarks>
    /// On by default, because it is what makes a behavior library that nothing has touched yet
    /// still work. Turn it off for a trimmed or ahead-of-time compiled build, where the scan
    /// cannot find anything the module initializers did not already report.
    /// </remarks>
    public bool ScanLoadedAssemblies { get; init; } = true;

    /// <summary>How many registration methods the last <see cref="Build"/> invoked.</summary>
    public int RegistrationsFound { get; private set; }

    /// <inheritdoc/>
    public void Build(App app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var found = 0;
        using (new SystemRegistrationSourceScope(StaticSourceTag))
        {
            // Fast path: everything whose module initializer has already run.
            foreach (var register in BehaviorRegistry.Snapshot())
            {
                register(app);
                found++;
            }

            // Fallback: assemblies that are loaded but have not been touched, so their module
            // initializer has not fired. Anything already in the registry is skipped.
            if (ScanLoadedAssemblies) found += ScanForMissedRegistrations(app);
        }

        RegistrationsFound = found;

        if (ScriptsDirectory is not null)
        {
            Console.Error.WriteLine(
                "[BevyCSharp] BehaviorsPlugin.ScriptsDirectory is set, but this library does not "
                + "compile scripts; only compiled behaviors were registered. Drive a compiler "
                + "through App.EnableDynamicSystems and App.RemoveSystemsBySource, as "
                + "BevyCSharp.Editor does.");
        }
    }

    /// <summary>Invokes registrations found by scanning that the registry did not already have.</summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming", "IL2026:RequiresUnreferencedCode",
        Justification =
            "Best-effort fallback only. Every registration is also reported by a generated "
            + "module initializer, which is what a trimmed build relies on; finding nothing "
            + "here is correct rather than a failure.")]
    private static int ScanForMissedRegistrations(App app)
    {
        var found = 0;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic) continue;

            foreach (var method in FindRegistrations(assembly))
            {
                if (BehaviorRegistry.Contains(method)) continue;

                try
                {
                    method.Invoke(null, [app]);
                    found++;
                }
                catch (TargetInvocationException ex)
                {
                    throw new InvalidOperationException(
                        $"behavior registration '{method.DeclaringType?.Name}.{method.Name}' "
                        + $"failed: {ex.InnerException?.Message}", ex.InnerException);
                }
            }
        }

        return found;
    }

    /// <summary>
    /// True when <paramref name="method"/> is a generated behavior registration.
    /// </summary>
    /// <remarks>
    /// Defensive because this walks every assembly in the process, including ones this package
    /// knows nothing about. Reading attributes on an unrelated method can throw when one of
    /// them names a type the process cannot load (a test host or plugin loader produces exactly
    /// that), and it must not stop behavior discovery for everything else.
    /// </remarks>
    private static bool IsRegistration(MethodInfo method)
    {
        try
        {
            if (!method.IsDefined(typeof(GeneratedBehaviorRegistrationAttribute), false))
                return false;

            var parameters = method.GetParameters();
            return parameters.Length == 1 && parameters[0].ParameterType == typeof(App);
        }
        catch (Exception)
        {
            // Reading attributes can throw when an unrelated attribute on the method names a
            // type this process cannot load. That method is not ours; skip it.
            return false;
        }
    }

    /// <summary>Yields the generated registration methods in <paramref name="assembly"/>.</summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming", "IL2026:RequiresUnreferencedCode",
        Justification = "See ScanForMissedRegistrations; this path is a best-effort fallback.")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming", "IL2075:UnrecognizedReflectionPattern",
        Justification = "See ScanForMissedRegistrations; this path is a best-effort fallback.")]
    private static IEnumerable<MethodInfo> FindRegistrations(Assembly assembly)
    {
        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }
        catch (Exception)
        {
            yield break;
        }

        const BindingFlags flags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        foreach (var type in types)
        {
            if (type is null) continue;

            MethodInfo[] methods;
            try
            {
                methods = type.GetMethods(flags);
            }
            catch (Exception)
            {
                continue;
            }

            foreach (var method in methods)
            {
                if (!IsRegistration(method)) continue;
                yield return method;
            }
        }
    }
}

/// <summary>
/// Registers the engine's own resources. Added first by <see cref="DefaultPlugins"/>.
/// </summary>
/// <remarks>
/// <see cref="App"/> already inserts <see cref="Time"/>, <see cref="Input"/>,
/// <see cref="EcsWorld"/> and <see cref="EcsCommands"/> in its constructor, because the
/// internal frame systems need them before any plugin runs. This plugin exists so that
/// contract is explicit and so later engine wiring has an obvious home.
/// </remarks>
public sealed class EnginePlugin : IPlugin
{
    /// <inheritdoc/>
    public void Build(App app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.World.GetOrInsertResource(static () => new SystemToggleRegistry());
    }
}
