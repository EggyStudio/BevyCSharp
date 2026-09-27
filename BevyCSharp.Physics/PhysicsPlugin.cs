namespace Bevy.Physics;

/// <summary>
/// Adds a <see cref="PhysicsWorld"/> and steps it once per fixed step.
/// </summary>
/// <example>
/// <code>
/// app.AddPlugin(new PhysicsPlugin());
///
/// // Later, in a behavior:
/// var physics = ctx.Res&lt;PhysicsWorld&gt;();
/// physics.Add(crate, PhysicsShape.Box(new Vec3(1f)), BodyKind.Dynamic, ctx.Ecs.GetOrDefault&lt;Transform&gt;(crate));
/// </code>
/// </example>
public sealed class PhysicsPlugin(PhysicsSettings? settings = null) : IPlugin
{
    /// <inheritdoc/>
    public void Build(App app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // A resource, so the world disposes it with the app, and Bepu's pools and threads go with
        // it rather than being left to the collector.
        app.World.InsertResource(new PhysicsWorld(settings));

        app.AddSystem(Stage.FixedUpdate, new SystemDescriptor(
            world => world.Resource<PhysicsWorld>().Step(world.Resource<EcsWorld>(), world.Resource<Time>().FixedDelta),
            "Physics.Step"));
    }
}
