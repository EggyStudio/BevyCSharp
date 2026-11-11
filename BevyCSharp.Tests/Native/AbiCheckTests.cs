using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Bevy.Interop;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// The loader's check, which resolves the bridge and compares its ABI version with this assembly's,
/// runs before the first call into the bridge, whichever class of entry points makes it.
/// </summary>
/// <remarks>
/// Each area's entry points stand in a class of their own, and a class's static constructor runs
/// before the first call to any of its methods, so every class that declares an entry point runs
/// the check from its own. A class that did not would call into a library the loader had not
/// compared, where a stale bridge reads a struct of another layout instead of failing with the
/// loader's message.
/// </remarks>
public sealed class AbiCheckTests
{
    [Fact]
    public void EveryClassOfEntryPointsRunsTheCheckBeforeItsFirstCall()
    {
        var missing = EntryPoints().Select(entry => entry.DeclaringType!).Distinct()
            .Where(type => !RunsTheCheck(type))
            .Select(type => type.Name)
            .Order(StringComparer.Ordinal);
        Assert.Empty(missing);
    }

    /// <summary>The library's entry points into the bridge, each declared to import it by name.</summary>
    internal static IEnumerable<MethodInfo> EntryPoints()
    {
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly;
        return EngineAssemblies.Loaded().SelectMany(assembly => assembly.GetTypes())
            .SelectMany(type => type.GetMethods(all))
            .Where(method => method.GetCustomAttribute<LibraryImportAttribute>()?.LibraryName == Native.Library);
    }

    /// <summary>Whether a type's static constructor calls the loader's check.</summary>
    /// <remarks>
    /// Every byte of the constructor's code is read as though a call began there. A byte that only
    /// reads as one resolves to no method or to another, so only a real call to the check counts.
    /// </remarks>
    private static bool RunsTheCheck(Type type)
    {
        var check = typeof(NativeLoader).GetMethod(nameof(NativeLoader.Initialize), BindingFlags.Static | BindingFlags.NonPublic);
        if (type.TypeInitializer?.GetMethodBody()?.GetILAsByteArray() is not { } code) return false;
        for (var at = 0; at + 5 <= code.Length; at++)
            if (code[at] == OpCodes.Call.Value && Called(type.Module, BitConverter.ToInt32(code, at + 1)) == check)
                return true;
        return false;
    }

    private static MethodBase? Called(Module module, int token)
    {
        try
        {
            return module.ResolveMethod(token);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
