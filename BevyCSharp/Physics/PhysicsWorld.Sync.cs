namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>What a body made from components was made from, so a change to any of it makes it again.</summary>
    /// <param name="Body">The body's settings.</param>
    /// <param name="Collider">The collider's settings.</param>
    /// <param name="Scale">The entity's scale, which sizes the collider.</param>
    /// <param name="Placed">
    /// Where a static body was put, which nothing else moves it from, or nothing for a body that
    /// moves or follows its entity.
    /// </param>
    private readonly record struct MadeFrom(RigidBody Body, Collider Collider, Vec3 Scale, Transform? Placed);

    /// <summary>The bodies made from components, each with what it was made from.</summary>
    private readonly Dictionary<Entity, MadeFrom> _fromComponents = [];

    /// <summary>
    /// Makes, remakes and takes away the bodies of entities carrying a <see cref="RigidBody"/> and a
    /// <see cref="Collider"/>, so the simulation holds what the world says.
    /// </summary>
    /// <param name="ecs">The world. Only valid inside a system.</param>
    /// <remarks>
    /// <para>
    /// <see cref="PhysicsPlugin"/> calls this before each step, paused or not, so a body put on an
    /// entity in the inspector exists by the next step and one edited there is made again with
    /// what was changed. A game stepping on its own schedule calls it before its own
    /// <see cref="Step"/>.
    /// </para>
    /// <para>
    /// A body is made again when its components change, when its entity's scale does, and for a
    /// static body when its entity is moved, since a static body is put where it is once and never
    /// follows. A dynamic body is not made again for moving, which it does itself. A collider
    /// fitted to a mesh that has not loaded waits for it, and the body comes when the mesh does.
    /// An entity that has a body from <see cref="Add"/> already keeps that one.
    /// </para>
    /// </remarks>
    public void Sync(EcsWorld ecs)
    {
        ArgumentNullException.ThrowIfNull(ecs);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Gone first, an entity or one of its two components, which takes its body with it.
        List<Entity>? gone = null;
        foreach (var entity in _fromComponents.Keys)
        {
            if (!ecs.IsAlive(entity) || !ecs.Has<RigidBody>(entity) || !ecs.Has<Collider>(entity))
                (gone ??= []).Add(entity);
        }

        if (gone is not null)
        {
            foreach (var entity in gone)
            {
                if (_bodies.ContainsKey(entity)) Remove(entity);
                _fromComponents.Remove(entity);
            }
        }

        foreach (var row in ecs.Query<Collider>(markChanged: false))
        {
            var entity = row.Entity;
            if (!ecs.TryGet<RigidBody>(entity, out var body)) continue;

            var at = ecs.GetOrDefault<Transform>(entity);
            var made = new MadeFrom(body, row.Component, at.Scale, body.Kind == BodyKind.Static ? at : null);

            if (_fromComponents.TryGetValue(entity, out var was))
            {
                if (was == made) continue;
            }
            else if (_bodies.ContainsKey(entity))
            {
                // Added in code, which a level's components do not overrule.
                continue;
            }

            if (!Colliders.TryFit(ecs, entity, out var fit)) continue;

            // A dynamic body made again keeps going the way it was going rather than starting from
            // rest, so an edit while it falls does not stop it in the air.
            (Vec3 Linear, Vec3 Angular)? moving = null;
            if (_bodies.TryGetValue(entity, out var old))
            {
                if (old.Kind == BodyKind.Dynamic && body.Kind == BodyKind.Dynamic) moving = Velocity(entity);
                Remove(entity);
            }

            var material = body.Friction > 0f || body.Bounce > 0f
                ? new PhysicsMaterial(body.Friction > 0f ? body.Friction : 1f, body.Bounce)
                : (PhysicsMaterial?)null;

            Add(entity, fit.Shape, body.Kind, at, body.Mass > 0f ? body.Mass : 1f, body.Sensor, material);
            if (moving is { } velocity) SetVelocity(entity, velocity.Linear, velocity.Angular);
            _fromComponents[entity] = made;
        }
    }
}
