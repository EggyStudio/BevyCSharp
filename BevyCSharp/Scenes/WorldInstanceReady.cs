namespace Bevy;

/// <summary>
/// Posted when Bevy has spawned a scene asset under an entity, with any overrides on it applied.
/// </summary>
/// <param name="Entity">The entity the scene was spawned under.</param>
/// <remarks>
/// Bevy announces a spawned scene to an observer, which the bridge turns into this message at the
/// top of the next frame, after the scene's entities are in the world. An instance placed with
/// <see cref="SceneInstances.Spawn"/> has had its overrides applied by the time this is read, so a
/// system reacting to it sees the instance as the scene file describes it.
/// </remarks>
public readonly record struct WorldInstanceReady(Entity Entity);
