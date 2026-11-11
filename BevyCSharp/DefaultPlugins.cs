namespace Bevy;

/// <summary>
/// The plugins every BevyCSharp app needs, which wire the engine's resources and discover
/// behaviors.
/// </summary>
/// <remarks>
/// Unlike Bevy's own <c>DefaultPlugins</c>, this group is small. Windowing, rendering, assets
/// and input backends are all Bevy's job, and are installed inside the native bridge according
/// to the <see cref="Config"/> you pass to <see cref="App"/>.
/// </remarks>
public sealed class DefaultPlugins : IPluginGroup
{
    /// <inheritdoc/>
    public IEnumerable<(IPlugin Plugin, int Order)> GetPlugins()
    {
        yield return (new EnginePlugin(), 0);
        yield return (new BehaviorsPlugin(), 100);

        // Inert unless Config.Serve or BCS_SERVE asks for it, so every app can be controlled from a
        // terminal and none pays for the possibility. Last, so the commands it serves are the ones
        // everything else has already registered.
        yield return (new CliPlugin(), 200);
    }
}
