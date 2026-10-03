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

    /// <summary>A bridge that draws and a Slang compiler to compile shaders with.</summary>
    internal static void Shaders()
    {
        Renderer();
        Skip.IfNot(Bevy.Shaders.SlangAvailable, "needs slangc, which build/fetch-slang.sh puts in build/tools/slang");
    }

    /// <summary>A sound device, as a test learned from whether anything played.</summary>
    internal static void SoundDevice(bool present) =>
        Skip.IfNot(present, "needs a sound device");

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
