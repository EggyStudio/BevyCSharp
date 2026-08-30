using System.Reflection;
using System.Runtime.InteropServices;

namespace Bevy.Interop;

/// <summary>
/// Finds and loads <c>bevy_csharp</c>, and refuses to continue against a mismatched ABI.
/// </summary>
/// <remarks>
/// When the package is consumed normally, NuGet's <c>runtimes/{rid}/native/</c> layout means
/// the default resolver already finds the library and the extra probing here never fires. The
/// probe paths exist for the cases NuGet does not cover: running straight out of the repo
/// against a local <c>cargo build</c>, and single-file or otherwise relocated layouts.
/// </remarks>
internal static class NativeLoader
{
    private static readonly object Gate = new();
    private static bool _initialized;

    /// <summary>
    /// Installs the resolver and verifies the ABI. Safe to call repeatedly; only the first
    /// call does work.
    /// </summary>
    internal static void Initialize()
    {
        if (_initialized) return;
        lock (Gate)
        {
            if (_initialized) return;
            _initialized = true;

            NativeLibrary.SetDllImportResolver(typeof(NativeLoader).Assembly, Resolve);
            VerifyAbi();
        }
    }

    /// <summary>Fails fast if the native library predates or postdates this assembly.</summary>
    private static void VerifyAbi()
    {
        int actual;
        try
        {
            actual = Native.bcs_abi_version();
        }
        catch (DllNotFoundException ex)
        {
            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"Could not load the native Bevy bridge ('{Native.Library}'). Probed: "
                + string.Join(", ", CandidateDirectories()) + ". "
                + "If you are working in the BevyCSharp repo, run build/build-native.sh first.",
                ex);
        }

        if (actual != Native.ExpectedAbiVersion)
        {
            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"Native Bevy bridge reports ABI version {actual}, but this build of BevyCSharp "
                + $"expects {Native.ExpectedAbiVersion}. The managed and native halves of the "
                + "package are out of sync - reinstall the package or rebuild the native crate.");
        }
    }

    /// <summary>Platform-specific file names to try for the bridge.</summary>
    private static string[] FileNames()
    {
        if (OperatingSystem.IsWindows()) return ["bevy_csharp.dll"];
        if (OperatingSystem.IsMacOS()) return ["libbevy_csharp.dylib"];
        return ["libbevy_csharp.so"];
    }

    /// <summary>Directories searched for the bridge, in priority order.</summary>
    private static IEnumerable<string> CandidateDirectories()
    {
        var baseDirectory = AppContext.BaseDirectory;
        yield return baseDirectory;

        var rid = RuntimeInformation.RuntimeIdentifier;
        yield return Path.Combine(baseDirectory, "runtimes", rid, "native");

        // In a single-file app Location is empty and BaseDirectory above already covers it.
        var location = GetAssemblyLocation();
        var assemblyDirectory = string.IsNullOrEmpty(location) ? null : Path.GetDirectoryName(location);
        if (!string.IsNullOrEmpty(assemblyDirectory))
        {
            yield return assemblyDirectory;
            yield return Path.Combine(assemblyDirectory, "runtimes", rid, "native");
        }

        // Repo-local development against `cargo build --release`.
        var probe = baseDirectory;
        for (var depth = 0; depth < 8 && !string.IsNullOrEmpty(probe); depth++)
        {
            yield return Path.Combine(probe, "native", "target", "release");
            probe = Path.GetDirectoryName(probe.TrimEnd(Path.DirectorySeparatorChar));
        }
    }

    /// <summary>This assembly's path on disk, or empty in a single-file app.</summary>
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "SingleFile", "IL3000:AvoidAssemblyLocationInSingleFile",
        Justification =
            "An empty result is handled: AppContext.BaseDirectory is probed first and is what a "
            + "single-file app actually needs.")]
    private static string GetAssemblyLocation() => Assembly.GetExecutingAssembly().Location;

    /// <summary>Resolves <see cref="Native.Library"/> against the candidate directories.</summary>
    private static IntPtr Resolve(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != Native.Library) return IntPtr.Zero;

        foreach (var directory in CandidateDirectories())
        {
            foreach (var fileName in FileNames())
            {
                var path = Path.Combine(directory, fileName);
                if (File.Exists(path) && NativeLibrary.TryLoad(path, out var handle))
                    return handle;
            }
        }

        // Fall back to the platform loader's own search rules.
        return NativeLibrary.TryLoad(libraryName, assembly, searchPath, out var fallback)
            ? fallback
            : IntPtr.Zero;
    }
}
