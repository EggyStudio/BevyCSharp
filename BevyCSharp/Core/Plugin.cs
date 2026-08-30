namespace Bevy;

/// <summary>
/// A unit of engine composition: registers resources and systems on an <see cref="App"/>.
/// </summary>
/// <example>
/// <code>
/// public sealed class ScorePlugin : IPlugin
/// {
///     public void Build(App app)
///     {
///         app.World.InsertResource(new Score());
///         app.AddSystem(Stage.PostUpdate, world =&gt; world.Resource&lt;Score&gt;().Commit());
///     }
/// }
/// </code>
/// </example>
public interface IPlugin
{
    /// <summary>Registers this plugin's contributions on <paramref name="app"/>.</summary>
    void Build(App app);

    /// <summary>
    /// Plugin types that must already be registered. Validated before <see cref="Build"/>
    /// runs, so a misordered <c>AddPlugin</c> fails with a clear error instead of a null
    /// reference deep inside another plugin.
    /// </summary>
    IReadOnlyCollection<Type> Dependencies => [];
}

/// <summary>An ordered collection of plugins added together.</summary>
public interface IPluginGroup
{
    /// <summary>The plugins in this group, with their relative ordering.</summary>
    IEnumerable<(IPlugin Plugin, int Order)> GetPlugins();
}

/// <summary>Raised when a plugin is added before a plugin it depends on.</summary>
public sealed class PluginOrderException : Exception
{
    /// <summary>The plugin that could not be built.</summary>
    public string PluginName { get; }

    /// <summary>The dependency that was missing.</summary>
    public string MissingDependency { get; }

    /// <summary>Creates the exception for a missing dependency.</summary>
    public PluginOrderException(string pluginName, string missingDependency)
        : base($"Plugin '{pluginName}' requires '{missingDependency}' to be added first. "
               + $"Move the AddPlugin call for '{missingDependency}' above it.")
    {
        PluginName = pluginName;
        MissingDependency = missingDependency;
    }
}

/// <summary>
/// The plugins every BevyCSharp app needs: engine resource wiring plus behavior discovery.
/// </summary>
/// <remarks>
/// Unlike Bevy's own <c>DefaultPlugins</c>, this group is small. Windowing, rendering, assets
/// and input backends are all Bevy's job, and are installed inside the native bridge according
/// to the <see cref="Config"/> you pass to <see cref="App"/>.
/// </remarks>
public sealed class DefaultPlugins : IPluginGroup
{
    /// <summary>
    /// Directory scanned for hot-reloadable behavior scripts, or <see langword="null"/> to
    /// use only the behaviors compiled into loaded assemblies.
    /// </summary>
    public string? ScriptsDirectory { get; init; }

    /// <inheritdoc/>
    public IEnumerable<(IPlugin Plugin, int Order)> GetPlugins()
    {
        yield return (new EnginePlugin(), 0);
        yield return (new BehaviorsPlugin { ScriptsDirectory = ScriptsDirectory }, 100);
    }
}
