using System.Reflection;

namespace Bevy;

/// <summary>
/// Finds every generated behavior registration in the loaded assemblies and runs it.
/// </summary>
/// <remarks>
/// <para>
/// The point of this plugin is that a consuming project needs no registration call, no
/// partial-class list and no startup boilerplate. Drop a <c>[Behavior]</c> struct into any
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
    /// <summary>Provenance tag applied to statically compiled behaviors.</summary>
    public string StaticSourceTag { get; init; } = "Static.Behaviors";

    /// <summary>
    /// Whether to scan loaded assemblies for registrations the registry has not seen.
    /// </summary>
    /// <remarks>
    /// On by default, because it makes a behavior library that nothing has touched yet still work.
    /// Turn it off for a trimmed or ahead-of-time compiled build, where the scan cannot find
    /// anything the module initializers did not already report.
    /// </remarks>
    public bool ScanLoadedAssemblies { get; init; } = true;

    /// <summary>The assemblies whose behaviors are registered, or none for every assembly loaded.</summary>
    /// <remarks>
    /// Every loaded assembly by default, since a game asks for its own behaviors and its libraries'.
    /// A process that loads an app's assembly without being that app names its own, as a test
    /// suite that loads the editor's to test its panels does, since the editor's behaviors would
    /// otherwise bring the editor up inside every app the suite runs.
    /// </remarks>
    public IReadOnlyCollection<Assembly>? Assemblies { get; init; }

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
                // A script's, which its host registers, as the scan below leaves them too.
                if (register.Method.DeclaringType?.Assembly.IsCollectible == true) continue;
                if (Assemblies is { } only && register.Method.DeclaringType?.Assembly is { } from && !only.Contains(from)) continue;

                register(app);
                found++;
            }

            // Fallback: assemblies that are loaded but have not been touched, so their module
            // initializer has not fired. Anything already in the registry is skipped.
            if (ScanLoadedAssemblies) found += ScanForMissedRegistrations(app, Assemblies);
        }

        RegistrationsFound = found;
    }

    /// <summary>Invokes registrations found by scanning that the registry did not already have.</summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming", "IL2026:RequiresUnreferencedCode",
        Justification =
            "Best-effort fallback only. Every registration is also reported by a generated "
            + "module initializer, which a trimmed build relies on; finding nothing "
            + "here is correct rather than a failure.")]
    private static int ScanForMissedRegistrations(App app, IReadOnlyCollection<Assembly>? only)
    {
        var found = 0;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            // A collectible assembly is a script, which the host that loaded it registers, and one
            // a host has retired may still be in the process until the runtime unloads it, so
            // finding it here would run a generation nobody asked for beside the current one.
            if (assembly.IsDynamic || assembly.IsCollectible) continue;
            if (only is not null && !only.Contains(assembly)) continue;

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
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming", "IL2065:UnrecognizedReflectionPattern",
        Justification = "The same call as IL2075 names, as the iterator the compiler makes of this method reports it, which a native publish of a game found.")]
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
            // An assembly that cannot be enumerated at all carries nothing of ours, and a scan
            // that is a fallback in the first place has nothing to report about it.
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
                // Same again for one type. What cannot be reflected over is not a behavior this
                // scan is looking for.
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
