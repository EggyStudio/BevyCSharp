namespace Bevy;

/// <summary>
/// What a component hook is told and may reach, Bevy's <c>HookContext</c> with what a Rust hook's
/// <c>DeferredWorld</c> gives it that C# can use.
/// </summary>
/// <remarks>
/// <para>
/// A hook runs inside Bevy, as the component goes on or comes off, while Bevy holds the world. So it
/// reaches the app's managed side, its resources, its messages and its command queue, and not
/// Bevy's world, whose calls (<see cref="EcsWorld"/>'s) answer that no world is loaned while it runs.
/// What it would change in Bevy's world it queues on <see cref="Cmd"/>, which the app applies at the
/// end of the frame's systems, as a Rust hook queues commands.
/// </para>
/// </remarks>
public readonly struct HookContext
{
    private readonly World _world;

    internal HookContext(Entity entity, int componentId, World world)
    {
        Entity = entity;
        ComponentId = componentId;
        _world = world;
    }

    /// <summary>The entity the component is going on or coming off.</summary>
    public Entity Entity { get; }

    /// <summary>The component's id in Bevy.</summary>
    public int ComponentId { get; }

    /// <summary>The app's command queue, for what the hook changes in the world, applied after the frame's systems.</summary>
    public EcsCommands Cmd => _world.Resource<EcsCommands>();

    /// <summary>The app's messages, for a message the hook sends, read the next frame as any is.</summary>
    public MessageBus Messages => _world.Resource<MessageBus>();

    /// <summary>One of the app's managed resources.</summary>
    /// <exception cref="InvalidOperationException">The app holds no such resource.</exception>
    public T Res<T>() where T : notnull => _world.Resource<T>();
}
