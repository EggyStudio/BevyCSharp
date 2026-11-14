using System.Reflection;

namespace Bevy;

/// <summary>
/// What a program's own build sets as the default of a setting its program left alone.
/// </summary>
/// <remarks>
/// <para>
/// A game from the package built in Debug watches its assets, so a texture, a model, a scene or a
/// script changed on disk shows in the running game, and one built in Release watches nothing. The
/// generator emits a module initializer into a program's executable built in Debug that calls
/// <see cref="ProgramBuiltInDebug"/>, and the CLR runs it before the program's first line, so the
/// default is known before any <see cref="Config"/> is read. A library, a test project and a
/// program built in Release emit nothing.
/// </para>
/// <para>
/// A program built in Debug reads and watches its project's assets folder where its config names no
/// <see cref="Config.AssetRoot"/>, since the build copies the folder beside the program and only a
/// build changes the copy, so a file saved in the project would never be read again. Its compiled
/// shaders are then cached under the project's build folder, so the assets folder holds only what
/// was put there. A program built in Release, a published one and one moved off the machine it was
/// built on read the copy beside them.
/// </para>
/// <para>
/// The program's own config decides over it either way, through <see cref="Config.WatchAssets"/>,
/// and the default reaches the program alone. A test that loads a game's assembly into its own
/// process runs that assembly's initializer as well, and the call names an assembly the process did
/// not start from, so it is passed over.
/// </para>
/// </remarks>
public static class BuildDefaults
{
    /// <summary>Whether the program's own build asks for its assets to be watched where its config says nothing.</summary>
    public static bool WatchAssets { get; private set; }

    /// <summary>The project's assets folder, which a program built in Debug reads where its config names no root, or nothing.</summary>
    public static string? ProjectAssets { get; internal set; }

    /// <summary>The folder under the project's build folder that a program reading <see cref="ProjectAssets"/> caches its compiled shaders in, or nothing.</summary>
    public static string? ShaderCache { get; internal set; }

    /// <summary>
    /// Records that <paramref name="program"/> was built in Debug, where it is the assembly the
    /// process started from, with the project it was built from. Called from a module initializer
    /// the generator emits, and safe to call twice.
    /// </summary>
    /// <param name="program">The assembly the generated initializer stands in.</param>
    /// <param name="project">The project's folder, or nothing where the build did not say it.</param>
    /// <param name="intermediate">The project's build folder, <c>obj</c>, under the project's folder where it is relative.</param>
    public static void ProgramBuiltInDebug(Assembly program, string? project = null, string? intermediate = null)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (program != Assembly.GetEntryAssembly()) return;

        WatchAssets = true;
        if (string.IsNullOrEmpty(project)) return;

        ProjectAssets = Path.Combine(project, "assets");
        ShaderCache = Path.Combine(project, string.IsNullOrEmpty(intermediate) ? "obj" : intermediate, ".slang-cache");
    }
}
