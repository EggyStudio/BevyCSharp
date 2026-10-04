# Physics

Rigid bodies, colliders, joints, contacts and characters, simulated by BepuPhysics.

Rigid bodies are simulated with [BepuPhysics v2](https://github.com/bepu/bepuphysics2) on the
managed side, in `Bevy.Physics`, which comes with the library in the way the interface does, so a
game has physics by referencing BevyCSharp and nothing else. Nothing new crosses to the engine for
it, since a body's pose reaches Bevy as the `Transform` write any system makes.

```csharp
app.AddPlugin(new PhysicsPlugin());

// In a behavior:
var physics = ctx.Res<PhysicsWorld>();
physics.Add(floor, PhysicsShape.Box(new Vec3(20f, 1f, 20f)), BodyKind.Static, floorTransform);
physics.Add(crate, PhysicsShape.Box(new Vec3(1f)), BodyKind.Dynamic, crateTransform, mass: 5f);

physics.ApplyImpulse(crate, new Vec3(0f, 20f, 0f));
if (physics.Raycast(eye, forward, 50f) is { } hit) Select(hit.Entity);
```

The simulation steps once per `FixedUpdate`, so Bevy's fixed timestep does the accumulating and a
slow frame is caught up in whole steps, which keeps a run the same on every machine. A dynamic body
is written back to its entity every step. A kinematic one follows its entity's transform, with the
velocity of how far it moved, so a moving platform pushes what stands on it. A static one never
moves. Boxes, spheres, capsules and cylinders are the shapes, sized in world units and not scaled
with the entity, and a level's floors and walls are a mesh shape made from triangles, such as a
mesh `Render.TryReadMesh` reads back once it has loaded:

```csharp
if (Render.TryReadMesh(levelMesh, out var triangles))
    physics.Add(level, PhysicsShape.Mesh(triangles!), BodyKind.Static, levelTransform);
```

A level holds its bodies as components, a `RigidBody` saying how one moves and a `Collider` saying
what it collides as, which the editor's inspector edits, its viewport draws, and a scene file
carries. The plugin makes the body once an entity has both, makes it again when either changes, or
when a static body's entity is moved or rescaled, and takes it away with them:

```csharp
ctx.Ecs.Add(wall, new RigidBody { Kind = BodyKind.Static });
ctx.Ecs.Add(wall, new Collider { Shape = ColliderShape.Box });     // fitted to the mesh it is drawn with
ctx.Ecs.Add(coin, new RigidBody { Kind = BodyKind.Static, Sensor = true });
ctx.Ecs.Add(coin, new Collider { Shape = ColliderShape.Sphere });
```

A collider is sized in the entity's own units and scaled with it, so a cube stretched into a wall
collides as one. One left at a size of zero takes the bounds of the mesh the entity is drawn with,
and `Hull` and `Mesh` are the drawn mesh itself, the second for a static floor or wall.
`Colliders.TryFit` says what one comes to and `Colliders.Draw` draws it as a gizmo, which is how the
editor shows them. A body added in code on an entity carrying the two is the game's and is left
alone.

A game's pause is a state the simulation knows nothing of, so `PhysicsWorld.Paused` holds it
still. Every body keeps its pose and its velocity and goes on from them when the pause is lifted,
and no contact starts or ends meanwhile:

```csharp
[OnEnter(Pause.On)] public static void Hold(BehaviorContext ctx) => ctx.Res<PhysicsWorld>().Paused = true;
[OnExit(Pause.On)]  public static void Go(BehaviorContext ctx)   => ctx.Res<PhysicsWorld>().Paused = false;
```

A `CharacterController` beside a dynamic body makes it a character, walked at the velocity a game
asks for rather than pushed about. Before every step it is walked toward `Move` along the ground it
stands on. It slides along a wall it meets, rides over a low edge and climbs a step up to
`StepHeight`, stands still on a slope up to `MaxSlope` and slides off a steeper one, and leaves the
ground at `Jump` where it stands on any. Each step writes back whether it stands on ground and which
way that faces. The body stays upright whatever its entity's rotation, so the game turns the entity
to face the way it walks:

```csharp
ctx.Ecs.Add(player, new RigidBody { Kind = BodyKind.Dynamic, Mass = 70f });
ctx.Ecs.Add(player, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(0.7f, 1.8f, 0.7f), Offset = new Vec3(0f, 0.9f, 0f) });
ctx.Ecs.Add(player, new CharacterController());

[OnUpdate]
public void Walk(BehaviorContext ctx, ref CharacterController body)
{
    body.Move = new Vec3(ctx.Input.KeyDown(Key.D) ? 4f : 0f, 0f, 0f);
    if (body.Grounded && ctx.Input.KeyPressed(Key.Space)) body.Jump = 5f;
}
```

A triangle collides from the side Bevy draws its face on. A rock or an odd crate that has to tumble
is a convex hull of its points instead, such as a model's own vertices, solid where a mesh shape is
a surface:

```csharp
if (Render.TryReadMesh(rockMesh, out var rock))
    physics.Add(boulder, PhysicsShape.Hull(rock!), BodyKind.Dynamic, boulderTransform, mass: 40f);
```

The hull turns about its own center while its entity keeps its origin, so a model exported standing
on its base rests with its base on the ground. A body belongs to its entity, so despawning the entity removes it, and the
simulation's memory and threads are released with the app. F7 in the sample drops crates onto its
ground, around the turning cube as a kinematic body, and `./bcs command sample.crates 12` does the
same on a running sample.

Each body can have a material of its own, how hard it is to slide and how much it bounces, given as
it is added or changed later:

```csharp
physics.Add(ball, PhysicsShape.Sphere(0.5f), BodyKind.Dynamic, ballTransform, material: new PhysicsMaterial(Friction: 0.6f, Bounce: 0.8f));
physics.SetMaterial(floor, new PhysicsMaterial(Friction: 0.05f));   // ice
```

Two bodies touching slide with the geometric mean of their frictions and bounce as the bouncier
does. Bepu's contacts are stiff springs that take a bounce's speed out, so a bounce is given back
after the step, along the surface's normal, to a body that struck it faster than a fifth of a unit
a second.

Bodies that start or stop touching are reported on the message bus, and a body added as a sensor
reports what enters it without pushing it, which is a trigger volume:

```csharp
physics.Add(door, PhysicsShape.Box(new Vec3(2f, 3f, 1f)), BodyKind.Static, doorway, sensor: true);

foreach (var contact in ctx.Read<ContactStarted>())
    if (contact.A == door || contact.B == door) Open();
```

A pair counts as separated once it has gone a few steps without touching, so a body settling onto
another, which hops clear of it by a millimeter as it lands, is not reported as leaving and landing
again.

Joints hold two moving bodies together: a ball joint for a shoulder or a pendulum, a hinge for a
door or a wheel, a weld for a part bolted on, and a distance range for a rope or a rod.

```csharp
var hinge = physics.Connect(frame, door, Joint.Hinge(
    anchorA: new Vec3(0.5f, 0f, 0f), axisA: Vec3.UnitY,
    anchorB: new Vec3(-0.5f, 0f, 0f), axisB: Vec3.UnitY));

physics.Disconnect(hinge);            // or remove either body, which takes its joints with it
```

A joint is solved between two velocities, so both bodies move, and a body pinned to the world is
joined to a kinematic one that stays put. Two bodies a joint holds do not collide with each other,
so a hinge's pin can pass through its wheel. A hinge can turn itself, as a fan or a driven wheel
does, and stop at an angle each way, as a door does:

```csharp
var fan = physics.Connect(mount, blades, Joint.Hinge(Vec3.Zero, Vec3.UnitY, Vec3.Zero, Vec3.UnitY)
    .WithMotor(degreesPerSecond: 360f, torque: 50f));
physics.SetMotor(fan, 0f, 50f);       // stops it, holding it where it is

physics.Connect(frame, door, Joint.Hinge(hingeOnFrame, Vec3.UnitY, hingeOnDoor, Vec3.UnitY)
    .WithLimits(lowestDegrees: 0f, highestDegrees: 100f));
```

A limit is measured from how the two are turned when they are joined, so a door joined closed opens
from closed.

---

Before this, [Audio](audio.md).
Next, [Input](input.md).
The [guide's contents](../README.md#guide) list every page.
