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
    /// <param name="Character">Whether a dynamic body was made a character, by a <see cref="CharacterController"/> beside it.</param>
    private readonly record struct MadeFrom(RigidBody Body, Collider Collider, Vec3 Scale, Transform? Placed, bool Character);

    /// <summary>The bodies made from components, each with what it was made from.</summary>
    private readonly Dictionary<Entity, MadeFrom> _fromComponents = [];

    /// <summary>The world's change tick at the last sync, from which the next reads what changed.</summary>
    private uint _syncTick;

    /// <summary>Entities whose collider fits a mesh that had not loaded at the last sync.</summary>
    private readonly HashSet<Entity> _waitingForMesh = [];

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
    /// follows. A dynamic body is not made again for moving, which it does itself, and is made
    /// again as a character when a <see cref="CharacterController"/> is put beside it or as a plain
    /// body when the controller is taken away. A collider
    /// fitted to a mesh that has not loaded waits for it, and the body comes when the mesh does.
    /// An entity that has a body from <see cref="Add"/> already keeps that one.
    /// </para>
    /// <para>
    /// Only what changed is read. The world lists the bodies and colliders added or changed since
    /// the last sync, and the bodies already made are asked after together, whether each still has
    /// both components and whether its transform was written, so a step in which nothing changed
    /// costs a few calls into the bridge rather than a few a body.
    /// </para>
    /// </remarks>
    public void Sync(EcsWorld ecs)
    {
        ArgumentNullException.ThrowIfNull(ecs);
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Read in a handful of calls rather than a few a body, since a level holds thousands of
        // bodies and almost none of them change in a step. What was added or changed since the
        // last sync is listed by the world, and the bodies it holds are asked after in one call a
        // component, so a step where nothing changed reads nothing a body.
        var since = _syncTick;
        var candidates = new HashSet<Entity>(_waitingForMesh);
        _waitingForMesh.Clear();

        var bodyTick = since;
        foreach (var entity in ecs.ChangedSince<RigidBody>(ref bodyTick)) candidates.Add(entity);
        var colliderTick = since;
        foreach (var entity in ecs.ChangedSince<Collider>(ref colliderTick)) candidates.Add(entity);

        // A controller added makes a body a character. One the step wrote back to is listed as
        // well, and found to make no difference below.
        var characterTick = since;
        foreach (var entity in ecs.ChangedSince<CharacterController>(ref characterTick)) candidates.Add(entity);
        _syncTick = characterTick;

        // A controller taken away makes it a plain body again, which nothing above lists, and
        // characters are few enough to ask after one at a time.
        foreach (var entity in _characters.Keys)
        {
            if (!ecs.Has<CharacterController>(entity)) candidates.Add(entity);
        }

        if (_fromComponents.Count > 0)
        {
            var held = _fromComponents.Keys.ToArray();
            var bodies = new bool[held.Length];
            var colliders = new bool[held.Length];
            var moved = new bool[held.Length];

            ecs.HasMany<RigidBody>(held, bodies);
            ecs.HasMany<Collider>(held, colliders);
            ecs.HasMany<Transform>(held, moved, since);

            for (var i = 0; i < held.Length; i++)
            {
                var entity = held[i];

                // Gone, an entity or one of its two components, which takes its body with it.
                if (!bodies[i] || !colliders[i])
                {
                    if (_bodies.ContainsKey(entity)) Remove(entity);
                    _fromComponents.Remove(entity);
                    candidates.Remove(entity);
                    continue;
                }

                // A transform written since, which a static body was put at once and does not
                // follow, and whose scale sizes every body's collider. A dynamic body's own
                // write back is among these, and is found unchanged below.
                if (moved[i]) candidates.Add(entity);
            }
        }

        foreach (var entity in candidates) Make(ecs, entity);
    }

    /// <summary>Makes an entity's body from its components, again where they changed what it is.</summary>
    private void Make(EcsWorld ecs, Entity entity)
    {
        if (!ecs.TryGet<RigidBody>(entity, out var body) || !ecs.TryGet<Collider>(entity, out var collider)) return;

        var at = ecs.GetOrDefault<Transform>(entity);
        var character = body.Kind == BodyKind.Dynamic && ecs.Has<CharacterController>(entity);
        var made = new MadeFrom(body, collider, at.Scale, body.Kind == BodyKind.Static ? at : null, character);

        if (_fromComponents.TryGetValue(entity, out var was))
        {
            if (was == made) return;
        }
        else if (_bodies.ContainsKey(entity))
        {
            // Added in code, which a level's components do not overrule.
            return;
        }

        if (!Colliders.TryFit(ecs, entity, out var fit))
        {
            // The mesh it fits has not loaded, so it is asked after again next time.
            _waitingForMesh.Add(entity);
            return;
        }

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
        if (character) MakeCharacter(entity, fit, at);
        if (moving is { } velocity) SetVelocity(entity, velocity.Linear, character ? default : velocity.Angular);
        _fromComponents[entity] = made;
    }
}
