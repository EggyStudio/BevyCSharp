# Physics

Rigid bodies, colliders, joints, contacts and characters, simulated by BepuPhysics.

Rigid bodies are simulated with [BepuPhysics v2](https://github.com/bepu/bepuphysics2) on the
managed side, in `Bevy.Physics`, which comes with the library in the way the interface does, so a
game has physics by referencing BevyCSharp and nothing else. Nothing new crosses to the engine for
it, since a body's pose reaches Bevy as the `Transform` write any system makes.

<!-- compiled with:
private static void Select(Entity entity) { }
Entity floor = default, crate = default;
Transform floorTransform = Transform.Identity, crateTransform = Transform.Identity;
Vec3 eye = default, forward = -Vec3.UnitZ;
-->
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
is written back to its entity every step. A kinematic one follows its entity's transform at the
speed and rate of turning the entity moves at, so a moving platform pushes what it meets and
carries what stands on it at its own pace, turning or not, whether the game moves it once a frame
or once a fixed step and at any frame rate. An entity put somewhere rather than moved there, as a
level begun again puts its platforms back, is said to be with `physics.MarkPlaced(platform)`, or
goes further in a frame than `PhysicsSettings.PlaceBeyond`, and its body is put there at rest
rather than swept through what lies between. A dynamic body is put somewhere new the same way with
`physics.Place(ecs, entity, position)`, as a character's respawn is. A static one never moves.
Boxes, spheres, capsules and cylinders are the shapes, sized in world units and not scaled with the
entity, and a level's floors and walls are a mesh shape made from triangles, such as a
mesh `Render.TryReadMesh` reads back once it has loaded:

<!-- compiled with:
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
AssetHandle levelMesh = default;
Entity level = default;
Transform levelTransform = Transform.Identity;
-->
```csharp
if (Render.TryReadMesh(levelMesh, out var triangles))
    physics.Add(level, PhysicsShape.Mesh(triangles!), BodyKind.Static, levelTransform);
```

A level holds its bodies as components, a `RigidBody` saying how one moves and a `Collider` saying
what it collides as, which the editor's inspector edits, its viewport draws, and a scene file
carries. The plugin makes the body once an entity has both, makes it again when either changes, or
when a static body's entity is moved or rescaled, and takes it away with them:

<!-- compiled with:
Entity wall = default, coin = default;
-->
```csharp
ctx.Ecs.Add(wall, new RigidBody { Kind = BodyKind.Static });
ctx.Ecs.Add(wall, new Collider { Shape = ColliderShape.Box });     // fitted to the mesh it is drawn with
ctx.Ecs.Add(coin, new RigidBody { Kind = BodyKind.Static, Sensor = true });
ctx.Ecs.Add(coin, new Collider { Shape = ColliderShape.Sphere });
```

A collider is sized in the entity's own units and scaled with it, so a cube stretched into a wall
collides as one. One left at a size of zero takes the bounds of the mesh the entity is drawn with,
and `Hull` and `Mesh` are the drawn meshes themselves, the second for a static floor or wall. Those
two take the meshes of the entities under it as well, placed as they are under it, so a `Mesh`
collider on the entity that places a level's model is the shape of its floors and walls once the
model has spawned and loaded.
`Colliders.TryFit` says what one comes to and `Colliders.Draw` draws it as a gizmo, which is how the
editor shows them. A body added in code on an entity carrying the two is the game's and is left
alone.

A game's pause is a state the simulation knows nothing of, so `PhysicsWorld.Paused` holds it
still. Every body keeps its pose and its velocity and goes on from them when the pause is lifted,
and no contact starts or ends meanwhile:

<!-- compiled with:
public enum Pause { Off, On }
-->
```csharp
[OnEnter(Pause.On)] public static void Hold(BehaviorContext ctx) => ctx.Res<PhysicsWorld>().Paused = true;
[OnExit(Pause.On)]  public static void Go(BehaviorContext ctx)   => ctx.Res<PhysicsWorld>().Paused = false;
```

A `CharacterController` beside a dynamic body makes it a character, walked at the velocity a game
asks for rather than pushed about. Before every step it is walked toward `Move` along the ground it
stands on. It slides along a wall it meets, rides over a low edge and climbs a step up to
`StepHeight`, stands still on a slope up to `MaxSlope` and slides off a steeper one, and leaves the
ground at `Jump` where it stands on any. It stands as tall as `Height`, with its feet where they
are, so a game crouches it by lowering the height and stands it by raising it again, or by setting
zero, its collider's height. A character has the height once there is room overhead for it, so one
crouched under a ledge stands as it walks out. With `Fly` set it flies at `Move`, up and down as
well, held against gravity, and walls, floors and ceilings still stop it, as a game's creative mode
needs. Each step writes back whether it stands on ground and which way that faces. The body stays
upright whatever its entity's rotation, so the game turns the entity to face the way it walks:

<!-- compiled with:
Entity player = default;
-->
```csharp
ctx.Ecs.Add(player, new RigidBody { Kind = BodyKind.Dynamic, Mass = 70f });
ctx.Ecs.Add(player, new Collider { Shape = ColliderShape.Capsule, Size = new Vec3(0.7f, 1.8f, 0.7f), Offset = new Vec3(0f, 0.9f, 0f) });
ctx.Ecs.Add(player, new CharacterController());

[OnUpdate]
public void Walk(BehaviorContext ctx, ref CharacterController body)
{
    body.Move = new Vec3(ctx.Input.KeyDown(Key.D) ? 4f : 0f, 0f, 0f);
    if (body.Grounded && ctx.Input.KeyPressed(Key.Space)) body.Jump = 5f;
    body.Height = ctx.Input.KeyDown(Key.ControlLeft) ? 1f : 0f;
}
```

A triangle collides from the side Bevy draws its face on. A rock or an odd crate that has to tumble
is a convex hull of its points instead, such as a model's own vertices, solid where a mesh shape is
a surface:

<!-- compiled with:
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
AssetHandle rockMesh = default;
Entity boulder = default;
Transform boulderTransform = Transform.Identity;
-->
```csharp
if (Render.TryReadMesh(rockMesh, out var rock))
    physics.Add(boulder, PhysicsShape.Hull(rock!), BodyKind.Dynamic, boulderTransform, mass: 40f);
```

The hull turns about its own center while its entity keeps its origin, so a model exported standing
on its base rests with its base on the ground. A body belongs to its entity, so despawning the entity removes it, and the
simulation's memory and threads are released with the app. The feature test's panel drops crates and
balls onto its hub's ground from its spawn page, around the turning cube as a kinematic body, and
`./bcs command feature.crates 12` does the same on a running one.

Each body can have a material of its own, how hard it is to slide and how much it bounces, given as
it is added or changed later:

<!-- compiled with:
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
Entity ball = default, floor = default;
Transform ballTransform = Transform.Identity;
-->
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

<!-- compiled with:
private static void Open() { }
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
Entity door = default;
Transform doorway = Transform.Identity;
-->
```csharp
physics.Add(door, PhysicsShape.Box(new Vec3(2f, 3f, 1f)), BodyKind.Static, doorway, sensor: true);

foreach (var contact in ctx.Read<ContactStarted>())
    if (contact.A == door || contact.B == door) Open();
```

A pair counts as separated once it has gone a few steps without touching, so a body settling onto
another, which hops clear of it by a millimeter as it lands, is not reported as leaving and landing
again.

A body is on one of 32 layers, layer 0 to begin with, and which layers collide with which is a table
the game sets, every one with every other to begin with. Bodies on layers that do not collide pass
through each other and report nothing, a sensor included, so a trigger on a layer only the player's
collides with reports the player alone, and the player's shots pass through the player:

<!-- compiled with:
private static void Damage(Entity entity) { }
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
const int Player = 1, Shots = 2;
Entity bullet = default, gun = default;
Vec3 muzzle = default, aim = -Vec3.UnitZ;
-->
```csharp
physics.SetLayersCollide(Player, Shots, false);
physics.SetLayer(bullet, Shots);                 // or RigidBody.Layer, for a body made from components
if (physics.Raycast(muzzle, aim, 100f, from: gun) is { } hit) Damage(hit.Entity);
```

A character stands only on what its layer collides with, and a ray cast from a body passes through
that body and sees what its layer collides with, where a ray from nowhere sees every layer. Every
ray passes through sensors, so a wheel's ray finds the road under a trigger volume. A pair
asleep is tested again once either changes, so a crate resting on a floor falls through once the
floor's layer stops colliding with its own.

A body meets what is within a tenth of a unit of it as a step starts, so one fast enough crosses a
thin wall within a step, a ball of 40 units a second through a wall a fifth of a unit thick. One a
game knows is fast is swept over each step instead, `physics.SetContinuous(ball, true)` or
`RigidBody.Continuous`, at the cost of a sweep for each pair it nears. A contact still stops it over a
step rather than at once, so a wall has to be a fifth of a unit thick at 100 units a second and half a
unit at 300, and a shot faster than that is a ray cast each frame rather than a body.

A contact that starts says where and how hard the two met. `Point` is their deepest contact in the
world, `Normal` the way `A` is pushed, and `Speed` how fast they closed along it, for a sound as loud
as the hit or the damage it does. The speed is read as the two approach as well as as they touch,
since the solver slows a pair in the step before it touches, so a crate dropped onto the floor meets
it at the speed its fall gave it, and one placed on the floor meets it at nothing:

<!-- compiled with:
AssetHandle thud = default;
-->
```csharp
foreach (var contact in ctx.Read<ContactStarted>())
    if (contact.Speed > 2f) Audio.Play(thud, new AudioSettings { Volume = MathF.Min(1f, contact.Speed / 10f) });
```

How hard two touching bodies press is asked of the pair, as a pressure plate asks of what stands on
it. `physics.ContactImpulse(plate, crate)` is the push the last step gave them along the normals of
their contacts, which divided by the step is the force between them, a crate's weight whether it
rests or is dragged across, since the friction is not counted. A pair asleep goes on being answered
with what it pressed as it fell asleep, since nothing between them changes while it sleeps.

Joints hold two moving bodies together: a ball joint for a shoulder or a pendulum, a hinge for a
door or a wheel, a weld for a part bolted on, a distance range for a rope or a rod, and a slider
for a drawer or a lift.

<!-- compiled with:
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
Entity frame = default, door = default;
-->
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

<!-- compiled with:
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
Entity mount = default, blades = default, frame = default, door = default;
Vec3 hingeOnFrame = default, hingeOnDoor = default;
-->
```csharp
var fan = physics.Connect(mount, blades, Joint.Hinge(Vec3.Zero, Vec3.UnitY, Vec3.Zero, Vec3.UnitY)
    .WithMotor(degreesPerSecond: 360f, torque: 50f));
physics.SetMotor(fan, 0f, 50f);       // stops it, holding it where it is

physics.Connect(frame, door, Joint.Hinge(hingeOnFrame, Vec3.UnitY, hingeOnDoor, Vec3.UnitY)
    .WithLimits(lowestDegrees: 0f, highestDegrees: 100f));
```

A limit is measured from how the two are turned when they are joined, so a door joined closed opens
from closed.

A ball joint can be kept within a cone, as a shoulder or a link of a chain is, swinging no further
than an angle from an axis on the first body and twisting about it no further than another, each
measured from how the two are turned when joined. A distance joint's range changes while it holds,
as a winch reels a rope in a little each frame:

<!-- compiled with:
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
Entity torso = default, arm = default, hook = default, crate = default;
Vec3 shoulder = default;
float length = 3f;
-->
```csharp
physics.Connect(torso, arm, Joint.Ball(shoulder, Vec3.Zero)
    .WithCone(axisA: -Vec3.UnitY, swingDegrees: 60f, twistDegrees: 20f));

var rope = physics.Connect(hook, crate, Joint.Distance(Vec3.Zero, Vec3.Zero, 0f, 3f));
physics.SetDistance(rope, 0f, length -= 0.5f * ctx.Time.Delta);
```

A slider keeps the second body on a line through its middle and from turning against the first, a
drawer, a sliding door or a lift on its frame. It stops at the ends of its travel, measured from
where it was joined, and drives itself along the line where it has a drive, which pushes with no
more than its force and holds it still at a speed of nothing:

<!-- compiled with:
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
Entity frame = default, car = default;
-->
```csharp
var lift = physics.Connect(frame, car, Joint.Slider(Vec3.UnitY)
    .WithTravel(minimum: 0f, maximum: 3f)
    .WithDrive(unitsPerSecond: 2f, force: 2000f));
physics.SetDrive(lift, -2f, 2000f);              // back down
var floor = physics.SliderPosition(lift);        // how far up it is
```

A level describes a joint as an entity of its own with a `JointBetween` naming the two bodies'
entities, so the editor places a door's hinge where it stands and a scene file carries it. The
entity's place is where the two are joined and its up direction is a hinge's axis, a slider's line
and the middle of a ball joint's cone, and the limits, the motor and the drive are fields of the
component. The joint is made once both bodies are, again when the component changes or a body is
made again, and taken away with the entity. One naming a static body is written to the log once and
left until its component changes. `physics.JointOf` answers the joint made, for a game to drive:

<!-- compiled with:
PhysicsWorld physics = ctx.Res<PhysicsWorld>();
Entity hinge = default, post = default, door = default, fanMount = default;
-->
```csharp
ctx.Ecs.Add(hinge, Transform.At(0.05f, 1f, 0f));
ctx.Ecs.Add(hinge, new JointBetween { Kind = JointKind.Hinge, A = post, B = door, MinAngle = 0f, MaxAngle = 100f });

if (physics.JointOf(fanMount) is { } fan) physics.SetMotor(fan, 0f, 50f);
```

---

Before this, [Audio](audio.md).
Next, [Input](input.md).
Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#physics). The [guide's contents](../README.md#guide) list every page.
