namespace Bevy.Physics;

/// <summary>
/// Adds a <see cref="PhysicsWorld"/>, makes the bodies the world holds as components, and steps it
/// once per fixed step.
/// </summary>
/// <remarks>
/// Each fixed step first runs <see cref="PhysicsWorld.Sync"/>, which makes, remakes and takes away
/// the bodies of entities carrying a <see cref="RigidBody"/> and a <see cref="Collider"/>, then
/// steps unless <see cref="PhysicsWorld.Paused"/> is set.
/// </remarks>
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
            world =>
            {
                // The bodies a level holds as components first, paused or not, so one put on an
                // entity or edited is there for the step.
                var physics = world.Resource<PhysicsWorld>();
                var ecs = world.Resource<EcsWorld>();

                // Under a name of its own in a frame profile, which counts it inside the step's
                // time as well, since it runs in the step's system.
                var syncing = FrameProfile.On ? System.Diagnostics.Stopwatch.GetTimestamp() : 0L;
                physics.Sync(ecs);
                if (FrameProfile.On) FrameProfile.Ran("Physics.Step: sync", System.Diagnostics.Stopwatch.GetTimestamp() - syncing);

                if (!physics.Paused) physics.Step(ecs, world.Resource<Time>().FixedDelta, world.Resource<MessageBus>());
            },
            "Physics.Step"));
    }
}
