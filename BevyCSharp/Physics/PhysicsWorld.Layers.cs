using BepuPhysics;
using BepuPhysics.Collidables;

namespace Bevy.Physics;

public sealed partial class PhysicsWorld
{
    /// <summary>
    /// Puts an entity's body on one of 32 layers, 0 to 31, which decides what it collides with, as
    /// <see cref="SetLayersCollide"/> says. Every body is on layer 0 to begin with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bodies on layers that do not collide pass through each other and report no contact, a sensor
    /// included, so a sensor on a layer only the player's collides with reports the player alone. A
    /// character stands only on what its layer collides with, and a ray cast from a body
    /// (<see cref="Raycast(Vec3, Vec3, float, Entity)"/>) sees what that body's layer collides with.
    /// </para>
    /// <para>
    /// A pair that sleeps is not tested again until something wakes it, so a body whose layer
    /// changes is woken, and a static wakes the bodies resting within its bounds, as a crate asleep
    /// on a floor falls through once the floor's layer stops colliding with its own. The layer is the
    /// body's, so a body made again starts on layer 0, and one made from <see cref="RigidBody"/> takes
    /// its <see cref="RigidBody.Layer"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="KeyNotFoundException">The entity has no body.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The layer is not one of the 32.</exception>
    public void SetLayer(Entity entity, int layer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(layer);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(layer, CollisionLayers.Count);

        var body = _bodies[entity];
        if (_contacts.Layers.Of(Packed(body)) == layer) return;

        _contacts.Layers.Set(Packed(body), layer);
        WakeAround(body);
    }

    /// <summary>The layer an entity's body is on.</summary>
    /// <exception cref="KeyNotFoundException">The entity has no body.</exception>
    public int LayerOf(Entity entity) => _contacts.Layers.Of(Packed(_bodies[entity]));

    /// <summary>
    /// Whether bodies on layer <paramref name="a"/> collide with bodies on layer <paramref name="b"/>,
    /// both ways, as the player's shots pass through the player and the enemies through each other.
    /// Every layer collides with every other to begin with.
    /// </summary>
    /// <remarks>The bodies asleep on either layer are woken by a change, so their sleeping pairs are tested again.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A layer is not one of the 32.</exception>
    public void SetLayersCollide(int a, int b, bool collide)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(a);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(a, CollisionLayers.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(b);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(b, CollisionLayers.Count);
        if (_contacts.Layers.Collide(a, b) == collide) return;

        _contacts.Layers.SetCollide(a, b, collide);

        // Every pair the change touches has a body on one of the two layers, a static never
        // sleeping, so waking those bodies is enough.
        foreach (var body in _bodies.Values)
        {
            if (body.Kind == BodyKind.Static) continue;

            var layer = _contacts.Layers.Of(Packed(body));
            if (layer == a || layer == b) Wake(_simulation.Bodies[body.Moving]);
        }
    }

    /// <summary>Whether bodies on two layers collide.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A layer is not one of the 32.</exception>
    public bool LayersCollide(int a, int b)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(a);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(a, CollisionLayers.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(b);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(b, CollisionLayers.Count);
        return _contacts.Layers.Collide(a, b);
    }

    // Wakes a body, or for a static the bodies whose bounds meet its own. A sleeping body's bounds
    // are kept in the broad phase's tree of statics, which is where they are looked for.
    private void WakeAround(Body body)
    {
        if (body.Kind != BodyKind.Static)
        {
            Wake(_simulation.Bodies[body.Moving]);
            return;
        }

        var bounds = _simulation.Statics[body.Fixed].BoundingBox;
        var sleepers = new Sleepers { Found = [] };
        _simulation.BroadPhase.GetOverlaps(bounds.Min, bounds.Max, _pool, ref sleepers);
        foreach (var handle in sleepers.Found)
        {
            if (_simulation.Bodies.BodyExists(handle)) Wake(_simulation.Bodies[handle]);
        }
    }

    private struct Sleepers : BepuUtilities.IBreakableForEach<CollidableReference>
    {
        public List<BodyHandle> Found;

        public readonly bool LoopBody(CollidableReference collidable)
        {
            if (collidable.Mobility != CollidableMobility.Static) Found.Add(collidable.BodyHandle);
            return true;
        }
    }
}
