namespace Bevy;

/// <summary>An event that happens to one entity, so observers of that entity are told of it.</summary>
/// <remarks>
/// Bevy's <c>EntityEvent</c>. Triggered with <see cref="EcsWorld.Trigger{TEvent}(TEvent)"/>, it reaches
/// the observers watching every such event and then the ones watching <see cref="Entity"/>.
/// </remarks>
public interface IEntityEvent
{
    /// <summary>The entity it happened to.</summary>
    Entity Entity { get; }
}
