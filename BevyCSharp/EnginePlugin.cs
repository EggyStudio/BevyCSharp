namespace Bevy;

/// <summary>
/// Registers the engine's own resources and each of the library's areas' part in the app and its
/// frame. Added first by <see cref="DefaultPlugins"/>.
/// </summary>
/// <remarks>
/// <see cref="App"/> already inserts <see cref="Time"/>, <see cref="Input"/>,
/// <see cref="EcsWorld"/> and <see cref="EcsCommands"/> in its constructor, because the
/// internal frame systems need them before any plugin runs. This plugin exists so that
/// contract is explicit and an engine system added later has one place to be added in. Each area
/// forgets what it kept for the app before, adds its systems and puts its work in the frame in
/// its place, so an app made without this plugin draws and plays without the areas' upkeep.
/// </remarks>
public sealed class EnginePlugin : IPlugin
{
    /// <inheritdoc/>
    public void Build(App app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.World.GetOrInsertResource(static () => new SystemToggleRegistry());

        // Built here rather than added, so the app counts the one plugin a game added, and this one
        // is added once, so each area's is built once.
        IPlugin[] areas =
        [
            new AssetWork(), new AudioWork(), new RenderWork(), new ImGuiWork(), new PhysicsWork(),
            new SceneWork(), new AnimationWork(), new ModelingWork(), new TreeWork(), new FieldWork(),
            new SkyWork(), new UiWork(), new DiagnosticsWork(),
        ];
        foreach (var area in areas) area.Build(app);
    }
}
