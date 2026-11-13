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

    /// <summary>
    /// Records that <paramref name="program"/> was built in Debug, where it is the assembly the
    /// process started from. Called from a module initializer the generator emits; safe to call
    /// twice.
    /// </summary>
    /// <param name="program">The assembly the generated initializer stands in.</param>
    public static void ProgramBuiltInDebug(Assembly program)
    {
        ArgumentNullException.ThrowIfNull(program);
        if (program == Assembly.GetEntryAssembly()) WatchAssets = true;
    }
}
