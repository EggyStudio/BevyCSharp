namespace Bevy;

/// <summary>
/// The one-line entry point: build a fully wired app and run it.
/// </summary>
/// <remarks>
/// This is the whole of the setup a normal project needs. Every <c>[Behavior]</c> struct in
/// the loaded assemblies is discovered and scheduled automatically, so a game can be nothing
/// but behavior files plus this call.
/// </remarks>
/// <example>
/// <code>
/// // Program.cs
/// using Bevy;
///
/// BevyApp.Run();
/// </code>
/// </example>
public static class BevyApp
{
    /// <summary>
    /// Creates an app with <see cref="DefaultPlugins"/>, runs it, and disposes it.
    /// </summary>
    /// <param name="config">Startup configuration; <see cref="Config.Default"/> when omitted.</param>
    /// <returns>The process exit code; 0 for a clean shutdown.</returns>
    public static int Run(Config? config = null)
    {
        using var app = new App(config);
        app.AddPlugins(new DefaultPlugins());
        return app.Run();
    }

    /// <summary>
    /// Creates an app with <see cref="DefaultPlugins"/>, lets <paramref name="configure"/> add
    /// to it, then runs it.
    /// </summary>
    /// <param name="configure">Callback for extra plugins, systems and resources.</param>
    /// <param name="config">Startup configuration; <see cref="Config.Default"/> when omitted.</param>
    /// <returns>The process exit code; 0 for a clean shutdown.</returns>
    public static int Run(Action<App> configure, Config? config = null)
    {
        ArgumentNullException.ThrowIfNull(configure);

        using var app = new App(config);
        app.AddPlugins(new DefaultPlugins());
        configure(app);
        return app.Run();
    }

    /// <summary>
    /// Builds an app with <see cref="DefaultPlugins"/> without running it.
    /// </summary>
    /// <remarks>
    /// Useful in tests, where you want to inspect what got registered before starting the loop.
    /// The caller owns the returned app and must dispose it.
    /// </remarks>
    public static App Build(Config? config = null)
    {
        var app = new App(config);
        try
        {
            app.AddPlugins(new DefaultPlugins());
            return app;
        }
        catch
        {
            app.Dispose();
            throw;
        }
    }
}
