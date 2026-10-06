namespace Bevy;

/// <summary>An event as an observer is handed it, with the world it happened in.</summary>
/// <typeparam name="TEvent">The event.</typeparam>
/// <remarks>
/// Bevy's <c>On</c>. The event is handed by reference, so an observer that changes it, as armor
/// lessens an attack's damage, passes the changed event to the observers after it and to the
/// entities it propagates to.
/// </remarks>
public sealed class On<TEvent>
{
    private TEvent _event;

    internal On(World world, TEvent value, Entity entity)
    {
        World = world;
        _event = value;
        Entity = entity;
    }

    /// <summary>The event, to read or to change for the observers after this one.</summary>
    public ref TEvent Event => ref _event;

    /// <summary>
    /// The entity the event is at now, which is the entity it happened to or, as it propagates, the
    /// ancestor it has reached. <see cref="Entity.None"/> for an event that happened to no entity.
    /// </summary>
    public Entity Entity { get; internal set; }

    /// <summary>The world it happened in.</summary>
    public World World { get; }

    /// <summary>The world's entities and components.</summary>
    public EcsWorld Ecs => World.Resource<EcsWorld>();

    /// <summary>A context over the world, as a system has, with the entity the event is at.</summary>
    public BehaviorContext Context => new(World) { Entity = Entity };

    /// <summary>Whether the event goes on to the entity's parent after this entity's observers.</summary>
    internal bool Propagating { get; private set; } = true;

    /// <summary>Lets the event go on up to the parent, or, given false, stops it here.</summary>
    /// <remarks>Read only for an <see cref="IPropagatingEvent"/>, and only after every observer of this entity has run.</remarks>
    public void Propagate(bool propagate) => Propagating = propagate;
}
