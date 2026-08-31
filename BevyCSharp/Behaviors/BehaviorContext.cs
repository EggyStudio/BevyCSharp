namespace Bevy;

/// <summary>
/// Everything a behavior method needs, handed to it on each invocation.
/// </summary>
/// <remarks>
/// <para>
/// The context resolves its resources once, when it is built, rather than on every property
/// read. For a per-entity method that is the difference between one dictionary lookup per
/// system run and one per entity.
/// </para>
/// <para>
/// <see cref="Entity"/> is only meaningful inside an instance method, where it identifies the
/// entity whose component <c>this</c> is bound to. In a static method it is
/// <see cref="Bevy.Entity.None"/>.
/// </para>
/// <para>
/// <b>On worker threads,</b> <see cref="Ecs"/> is unusable, because the world is only loaned
/// to the main thread. <see cref="Cmd"/>, <see cref="Time"/> and <see cref="Input"/> are safe to
/// read from anywhere.
/// </para>
/// </remarks>
/// <seealso cref="BehaviorAttribute"/>
public sealed class BehaviorContext
{
    /// <summary>The managed resource world.</summary>
    public World World { get; }

    /// <summary>Direct, immediate access to Bevy's entities and components.</summary>
    /// <remarks>Main thread only; see the remarks on <see cref="BehaviorContext"/>.</remarks>
    public EcsWorld Ecs { get; }

    /// <summary>Deferred structural changes, applied at the end of <see cref="Stage.PostUpdate"/>.</summary>
    public EcsCommands Cmd { get; }

    /// <summary>Messages broadcast between systems.</summary>
    public MessageBus Messages { get; }

    /// <summary>This frame's timing.</summary>
    public Time Time { get; }

    /// <summary>This frame's input.</summary>
    public Input Input { get; }

    /// <summary>
    /// The entity being processed, for an instance method. <see cref="Bevy.Entity.None"/> in a
    /// static one.
    /// </summary>
    public Entity Entity { get; set; }

    /// <summary>Resolves every engine resource from <paramref name="world"/>.</summary>
    /// <exception cref="InvalidOperationException">An engine resource is missing.</exception>
    public BehaviorContext(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        World = world;
        Ecs = world.Resource<EcsWorld>();
        Cmd = world.Resource<EcsCommands>();
        Time = world.Resource<Time>();
        Input = world.Resource<Input>();
        Messages = world.Resource<MessageBus>();
    }

    /// <summary>
    /// Builds a context from resources the caller already resolved.
    /// </summary>
    /// <remarks>
    /// Used by the generated runners, which resolve once per system and then hand a context to
    /// each worker thread rather than re-resolving per chunk.
    /// </remarks>
    public BehaviorContext(World world, EcsWorld ecs, EcsCommands cmd, Time time, Input input)
    {
        World = world;
        Ecs = ecs;
        Cmd = cmd;
        Time = time;
        Input = input;
        Messages = world.Resource<MessageBus>();
    }

    /// <summary>Gets a required resource.</summary>
    /// <exception cref="InvalidOperationException">The resource is not registered.</exception>
    public T Res<T>() where T : notnull => World.Resource<T>();

    /// <summary>Gets a resource, reporting whether it was found.</summary>
    public bool TryRes<T>(out T value) where T : notnull => World.TryGetResource(out value);

    /// <summary>
    /// The current value of <typeparamref name="TState"/>.
    /// </summary>
    /// <remarks>
    /// Main thread only, like <see cref="Ecs"/>: it reads the live value out of the world.
    /// </remarks>
    public TState State<TState>() where TState : struct, Enum => StateRegistry.Current<TState>();

    /// <summary>
    /// Asks Bevy to move <typeparamref name="TState"/> to <paramref name="value"/>.
    /// </summary>
    /// <remarks>
    /// Queued rather than immediate, so every system in this frame still sees the old state.
    /// The change lands at Bevy's next transition point.
    /// </remarks>
    public void SetState<TState>(TState value) where TState : struct, Enum =>
        StateRegistry.Set(value);

    /// <summary>Broadcasts a message for every reader to see next frame.</summary>
    /// <remarks>Safe from a parallel behavior method, like <see cref="Cmd"/>.</remarks>
    public void Send<TMessage>(TMessage message) where TMessage : notnull =>
        Messages.Send(message);

    /// <summary>The messages of type <typeparamref name="TMessage"/> sent during the previous frame.</summary>
    public ReadOnlySpan<TMessage> Read<TMessage>() where TMessage : notnull =>
        Messages.Read<TMessage>();

    /// <summary>Asks the engine to shut down after this frame.</summary>
    public void Exit() => App.RequestExit();
}
