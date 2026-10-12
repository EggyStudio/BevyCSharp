namespace Bevy;

/// <summary>
/// A unit of engine composition, which registers resources and systems on an <see cref="App"/>.
/// </summary>
/// <example>
/// <code>
/// public sealed class ScorePlugin : IPlugin
/// {
///     public void Build(App app)
///     {
///         app.World.InsertResource(new Score());
///         app.AddSystem(Stage.PostUpdate, world =&gt; world.Resource&lt;Score&gt;().Commit());
/// } }
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
