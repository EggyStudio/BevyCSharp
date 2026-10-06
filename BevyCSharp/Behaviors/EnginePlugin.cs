namespace Bevy;

/// <summary>
/// Registers the engine's own resources. Added first by <see cref="DefaultPlugins"/>.
/// </summary>
/// <remarks>
/// <see cref="App"/> already inserts <see cref="Time"/>, <see cref="Input"/>,
/// <see cref="EcsWorld"/> and <see cref="EcsCommands"/> in its constructor, because the
/// internal frame systems need them before any plugin runs. This plugin exists so that
/// contract is explicit and so later engine wiring has an obvious home.
/// </remarks>
public sealed class EnginePlugin : IPlugin
{
    /// <inheritdoc/>
    public void Build(App app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.World.GetOrInsertResource(static () => new SystemToggleRegistry());
    }
}
