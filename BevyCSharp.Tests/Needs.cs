using Bevy;
using Xunit;

namespace Bevy.Tests;

/// <summary>
/// What a test needs of the bridge or the machine, each skipping the test with the reason when it
/// is missing.
/// </summary>
/// <remarks>
/// <para>
/// A test that returned early when it could not run was counted as passed, so a headless run
/// reported every picture test green without drawing anything. Skipping instead puts the number
/// that did not run, and why, in the run's summary, so a green suite on a bridge without the
/// renderer reads as what it is.
/// </para>
/// <para>
/// Each call throws xunit's skip, so it works only in a test marked <c>[SkippableFact]</c> or
/// <c>[SkippableTheory]</c>, and only on the test's own thread. Inside a system the engine runs,
/// the throw would be caught by the harness and reported as a failure, so a test asks before it
/// builds its engine, or after the run with what a system recorded.
/// </para>
/// </remarks>
internal static class Needs
{
    /// <summary>A bridge that draws, the render or the editor profile.</summary>
    internal static void Renderer() =>
        Skip.IfNot(App.HasRenderer, "needs a bridge built with the render or editor profile");

    /// <summary>The editor profile.</summary>
    internal static void Editor() =>
        Skip.IfNot(App.HasEditor, "needs a bridge built with the editor profile");

    /// <summary>A bridge without the editor profile, for a test of what a smaller build does.</summary>
    internal static void NoEditor() =>
        Skip.If(App.HasEditor, "is about a bridge without the editor profile");

    /// <summary>A headless bridge, for a test of what a build that cannot draw does.</summary>
    internal static void NoRenderer() =>
        Skip.If(App.HasRenderer, "is about a headless bridge");

    /// <summary>The variable a run sets to have a window opened, resized, minimized and moved.</summary>
    internal const string WindowStormVariable = "BCS_WINDOW_STORM";

    /// <summary>
    /// A window this test may open and storm, which the run asked for with
    /// <see cref="WindowStormVariable"/> set to 1, on a display that gives a minimized window back:
    /// X11 on Linux, which <c>xvfb-run</c> gives off any desktop, Windows or macOS.
    /// </summary>
    /// <remarks>
    /// Asked for rather than taken wherever there is a display, since the window is minimized and
    /// moved about whatever desktop the run has, which on a desk is somebody's. Wayland gives no
    /// program its minimized window back, so a run there is skipped as one with no display is.
    /// </remarks>
    internal static void WindowStorm()
    {
        Renderer();
        Skip.IfNot(
            Environment.GetEnvironmentVariable(WindowStormVariable) == "1",
            $"opens a window and storms it, which a run asks for with {WindowStormVariable}=1, under xvfb-run where it should stay off a desktop");
        Skip.If(
            OperatingSystem.IsLinux() && (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"))),
            "needs an X11 display, which gives a minimized window back where Wayland does not, as xvfb-run gives one");
    }

    /// <summary>A bridge that draws and a Slang compiler to compile shaders with.</summary>
    internal static void Shaders()
    {
        Renderer();
        Skip.IfNot(Bevy.Shaders.SlangAvailable, "needs slangc, which build/fetch-slang.sh puts in build/tools/slang");
    }

    /// <summary>
    /// Python, as <c>python3</c> where it is, as on Linux and macOS, or as <c>python</c>, as on
    /// Windows, answering the name it runs by.
    /// </summary>
    internal static string Python()
    {
        Skip.If(PythonName.Value is null, "needs python3 or python to run the build scripts");
        return PythonName.Value!;
    }

    private static readonly Lazy<string?> PythonName = new(() =>
    {
        foreach (var name in new[] { "python3", "python" })
        {
            try
            {
                using var python = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(name, "--version") { RedirectStandardOutput = true, RedirectStandardError = true });
                if (python is null) continue;
                python.WaitForExit(10_000);
                if (python.HasExited && python.ExitCode == 0) return name;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Not on the path.
            }
        }

        return null;
    });

    /// <summary>A system that says which files a process has open, Linux by its descriptors or Windows by its locks.</summary>
    internal static void OpenFiles() =>
        Skip.IfNot(OperatingSystem.IsLinux() || OperatingSystem.IsWindows(), "needs Linux's /proc/self/fd or Windows' locks to find a file held open");

    /// <summary>A folder beside the tests, such as the sample's assets, which a checkout has.</summary>
    internal static void Folder(string path, string what) =>
        Skip.IfNot(Directory.Exists(path), $"needs {what} at {path}");

    /// <summary>Meshlets running, as a system saw <see cref="Render.MeshletsActive"/>.</summary>
    internal static void Meshlets(bool active) =>
        Skip.IfNot(active, "needs a bridge built with --meshlet and a GPU with 64-bit texture atomics");

    /// <summary>Ray-traced lighting running, as a system saw <see cref="Render.RayTracingActive"/>.</summary>
    internal static void RayTracing(bool active) =>
        Skip.IfNot(active, "needs a bridge built with --solari and an adapter that traces rays");

    /// <summary>Ray queries in shaders, as a system saw <see cref="Bevy.Shaders.SupportsRayQueries"/>.</summary>
    internal static void RayQueries(bool supported) =>
        Skip.IfNot(supported, "needs an adapter with ray queries");
}
