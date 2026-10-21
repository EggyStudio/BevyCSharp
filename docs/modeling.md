# Modeling

An `EditMesh` is a mesh shaped by operations, as a modeling program shapes one, for blocking out
a level or a prop from code. It starts empty, takes primitives, and is changed through a selection,
each call returning the mesh so the next follows on:

```csharp
var room = new EditMesh()
    .Box(new Vec3(8f, 3f, 6f))
    .SelectFacing(Vec3.UnitY)
    .Move(new Vec3(0f, 1f, 0f));      // the ceiling a meter higher
```

Each operation writes a line to the mesh's recipe, so those three calls are these three lines, and
the lines run from an empty mesh make it again:

```text
box 8 3 6
select +y
move 0 1 0
```

The recipe is the mesh's history, its undo and its file, as [The recipe](#the-recipe) shows, and
each section here gives an operation's line beside its call.

A vertex is a point, and a face is a loop of three or more vertices, wound counter-clockwise seen
from the side it faces. A face of any count is kept whole, so a box's side is one face of four
corners and a staircase's side is one face of many, and it is cut into triangles only when it is
drawn. Edges are read from the faces, each pair of corners that follow one another round a face.

What a mesh holds is read from it:

<!-- compiled with:
var room = new EditMesh();
-->
```csharp
Console.WriteLine($"{room.VertexCount} vertices, {room.EdgeCount} edges, {room.FaceCount} faces");
Console.WriteLine($"{room.Volume} cubic meters, {room.OpenEdges} open edges");
var (low, high) = room.Bounds;
```

`Volume` is the space a closed mesh holds, positive when its faces face out. `OpenEdges` counts the
edges not shared by two faces running along them in opposite directions, the edge of a hole, an
edge three faces meet at, or one beside a face turned the wrong way, and `IsClosed` is true where
there are none, which a solid needs for its volume and its collider to mean anything.

## Primitives

Each primitive is added to what the mesh holds and selected, so the call after it acts on it. Each
is placed by its lowest corner, at the origin unless `at` says otherwise, or by its middle where
`centered` is set:

```csharp
var mesh = new EditMesh()
    .Box(new Vec3(2f, 3f, 4f))                                   // from (0, 0, 0) to (2, 3, 4)
    .Box(new Vec3(1f), at: new Vec3(5f, 0f, 0f))                 // its corner at (5, 0, 0)
    .Box(new Vec3(1f), at: new Vec3(0f, 5f, 0f), centered: true); // its middle at (0, 5, 0)
```

### Box

`Box(size)`, or `box X Y Z`, eight vertices and six faces of four corners, as wide along X, as tall
along Y and as deep along Z as `size` says. A box of two by three by four holds 24 cubic meters. A
primitive's line ends in `at X Y Z` where it is placed elsewhere and `center` where it is placed by
its middle, as `box 1 1 1 at 0 5 0 center`.

### Plane

`Plane(width, depth)`, or `plane X Z`, one face of four corners lying flat and facing up, as wide
along X and as deep along Z as given. Its four edges are open, as a floor's are before walls meet it.

### Cylinder and cone

`Cylinder(radius, height, sides)`, or `cylinder RADIUS HEIGHT SIDES`, stands along Y, its round side
made of `sides` flat sides, and `Cone(radius, height, sides)`, or `cone RADIUS HEIGHT SIDES`, stands
on a base of as many sides with its point over the middle.
Their corners stand half a side round from the axes, so a cylinder of four sides is a box with its
sides facing along X and Z, and one of eight has a flat side facing each way along them.

```csharp
var pillar = new EditMesh().Cylinder(0.4f, 3f, 8);   // 16 vertices, 24 edges, 10 faces
var spire = new EditMesh().Cone(1f, 2f, 6);           // 7 vertices, 12 edges, 7 faces
```

### Wedge

`Wedge(size)`, or `wedge X Y Z`, is a box cut through from one bottom edge to the opposite top edge. It is nothing tall
at its lowest Z and its full height at its highest, so it rises along Z as stairs do, and a wedge
laid over a flight of stairs makes their ramp.

### Stairs

`Stairs(steps, rise, run, width)`, or `stairs STEPS RISE RUN WIDTH`, is a flight rising along Z, each step `rise` high and `run` deep,
as wide along X as `width`. It is closed underneath, a back face as tall as the flight at its far end
and a floor under it, so it is a solid that stands on the ground and against a wall:

```csharp
var flight = new EditMesh().Stairs(12, 0.25f, 0.3f, 2f);   // three meters up over 3.6
```

Its two sides are faces of one outline each, a step's corner at a time. A flight of four steps has
twenty vertices, thirty edges and twelve faces, and holds the sum of its steps, each as tall as it
stands.

### Arch

`Arch(radius, thickness, depth, sides)`, or `arch RADIUS THICKNESS DEPTH SIDES`, is half a ring standing on its two feet, its curve made of
`sides` flat sides and its opening along Z. It is twice its radius wide and its radius tall, so it
stands over a doorway as wide as its inside, `radius - thickness` either side of its middle.

## The selection

Operations act on what is selected, a set of vertices and a set of faces. Selecting a face selects
its corners, so moving the vertices selected moves the faces selected, and an edge is selected where
both its ends are.

| Call | Line | Selects |
|---|---|---|
| `SelectAll()`, `SelectNone()` | `select all`, `select none` | Everything, or nothing |
| `SelectInvert()` | `select invert` | What was not selected, faces with their corners |
| `SelectFaces([0, 3])` | `select face 0 3` | Faces by their indices |
| `SelectFacing(Vec3.UnitY)` | `select +y` | The faces facing nearest a way along an axis, here up |
| `SelectInside(corner, opposite)` | `select box 0 0 0 4 3 4` | The vertices inside a box, and the faces all of whose corners are |
| `SelectSlot("wall")` | `select slot wall` | The faces in a material slot |

Each but the first three takes a `SelectionMode` after it, `Replace` unless it says `Add` or
`Remove`, which a line says after `select`, as `select add -y`. `SelectFacing` sorts a face by which of the six directions along the axes its normal is
nearest, so every face is a floor, a ceiling or one of four walls. A slope rising exactly as far as
it runs, at forty-five degrees, is taken as a floor, where a character walks it.

```csharp
var hall = new EditMesh()
    .Box(new Vec3(10f, 3f, 4f))
    .SelectFacing(Vec3.UnitX)
    .SelectFacing(-Vec3.UnitX, SelectionMode.Add);   // both ends of the hall
```

## Moving, turning and scaling

`Move(by)` moves the selected vertices, `Rotate(axis, degrees)` turns them about an axis and
`Scale(factor)` stretches them, each along one axis or all three, the lines `move X Y Z`,
`rotate y 90` (or an axis's three numbers, `rotate 1 0 1 45`) and `scale 2` or `scale 1 2 1`, each
ending in `at X Y Z` where a pivot is given. A turn or a scale is about the
middle of the box holding the selection unless a pivot is given, so a part turned a quarter in place
stays on the grid it stood on, and a negative scale mirrors the selection with its faces still
facing out.

```csharp
var tower = new EditMesh()
    .Box(new Vec3(4f, 12f, 4f))
    .SelectFacing(Vec3.UnitY)
    .Scale(new Vec3(0.5f, 1f, 0.5f))                          // the top drawn in, a taper
    .SelectAll()
    .Rotate(Vec3.UnitY, 45f, pivot: Vec3.Zero);               // the whole turned about the origin
```

## The grid

A mesh holds a grid step, a meter unless `Grid(step)`, or `grid STEP`, sets a power of two of one
from a sixteenth to sixty-four. A measure typed is taken as given, so a move of a fifth of a meter moves a fifth of a
meter whatever the step. `Snap()`, or `snap`, puts each selected vertex on the nearest point of the grid,
which mends what turns and scales left off it:

```csharp
var crate = new EditMesh()
    .Box(new Vec3(1f))
    .Rotate(Vec3.UnitY, 30f)
    .Grid(0.25f)
    .Snap();
```

## Material slots

Each face has a material slot, a name, the first of which, `default`, every new face takes.
`Material("wall")`, or `material wall`, puts the selected faces in the slot of that name, made where there is none, so a
mesh's faces are sorted into floors, walls and trims before what each is drawn with is decided:

```csharp
var cell = new EditMesh()
    .Box(new Vec3(6f, 3f, 6f))
    .Material("wall")
    .SelectFacing(-Vec3.UnitY)
    .Material("floor");
```

## Changing the faces

These change which vertices and faces there are, on what is selected:

| Call | Line | Does |
|---|---|---|
| `Extrude(1f)` | `extrude 1` | Pushes the selected faces out a meter along their normals as one region, sides filled |
| `Extrude(new Vec3(0f, 3f, 0f))` | `extrude 0 3 0` | Pushes them by one vector, as walls pushed up from a floor's edge |
| `Inset(0.25f)` | `inset 0.25` | Makes each a smaller face a quarter in from its edges, with a band round it |
| `Delete()`, `Delete(fill: true)` | `delete`, `delete fill` | Takes them away, or the selected vertices with their faces, leaving the hole or closing it |
| `Merge(0.01f)` | `merge 0.01` | Joins selected vertices within a centimeter into one |
| `Flip()` | `flip` | Turns the faces to face the other way |
| `Triangulate()` | `triangulate` | Cuts each into triangles, as the drawing cuts it |
| `Duplicate()` | `duplicate` | Copies them in place with vertices of their own, the copy selected |
| `Join("models/pillar.model")` | `join models/pillar.model` | Adds another model made from its file, selected |

An extrude moves the region's faces together, so faces side by side stay joined and each goes the
whole distance along its own normal, and selects them after, so a second extrude pushes them
further. A box whose top is pushed up a meter has twelve vertices and ten faces, and a window's
recess is an inset followed by an extrude inward:

```csharp
var wall = new EditMesh()
    .Box(new Vec3(4f, 3f, 0.3f))
    .SelectFacing(Vec3.UnitZ)
    .Inset(0.8f)
    .Extrude(-0.2f);                 // a recess twenty centimeters deep
```

`Shade(true, 45f)`, or `shade smooth 45`, draws the selected faces smoothed into the faces beside
them that meet them at under 45 degrees, thirty where no angle is given, so a cylinder's sides are
round and its ends flat, and `shade flat` draws them flat again.

## The recipe

`Recipe` is the list of lines, `Undo()` takes the last back by making the mesh again from the lines
before it, and `Redo()` does it again. `Run(line)` runs a line as its call would, so a line typed is
an operation done, and `Save(path)` writes the recipe as a file of its own, `*.model`, which
`EditMesh.Load(path)` makes the mesh from, reading it as the game reads its assets:

```csharp
var room = new EditMesh().Box(new Vec3(8f, 3f, 6f)).SelectFacing(Vec3.UnitY).Extrude(1f);
room.Undo();                                     // the box again, its top selected
room.Run("extrude 2");
room.Save("models/room.model");                  // under the asset root

var again = EditMesh.Load("models/room.model");
```

The file starts with the line `model 1`, its form and the form's version, and skips blank lines and
what follows a `#`, so a model is a short text a reader follows and a diff reads line by line:

```text
model 1
# a room with a raised ceiling
box 8 3 6
select +y
extrude 2
```

A line that is not an operation is named in the error with its number. The words are the same in
3DEngine's, so a recipe written in either engine makes the same mesh in the other, its faces
numbered alike.

## Booleans and openings

A solid is added to another, taken away from it or kept where the two overlap, the faces where they
meet cut and the pieces joined back into faces where they lie in one plane and one slot:

| Call | Line | Does |
|---|---|---|
| `Union("models/tower.model", at)` | `union models/tower.model at 4 0 0` | Adds another model's solid, moved, the faces inside the two taken away |
| `Subtract("models/arch.model", at)` | `subtract models/arch.model at 2 0 0` | Takes another model's solid away |
| `Intersect("models/block.model", at)` | `intersect models/block.model` | Keeps what both hold |
| `Cut("box 1 1 2 at 1.5 1 -0.5")` | `cut box 1 1 2 at 1.5 1 -0.5` | Takes away a solid primitive made where its line places it |
| `Room(new Vec3(6f, 3f, 5f), 0.2f)` | `room 6 3 5 0.2` | Adds a box hollowed by walls, floor and ceiling two tenths thick |
| `Window(1f, 1.2f, 2f, 0.9f)` | `cut window 1 1.2 2 0.9` | Cuts a window through the wall selected, its middle two meters along, its sill at 0.9 |
| `Door(1f, 2.1f, 1.5f)` | `cut door 1 2.1 1.5` | Cuts a door from the wall's bottom, its middle a meter and a half along |
| `Ramp(1f, 3f, 2f)` | `ramp 1 3 2` | Adds a ramp a meter high over three, two wide, in a flight of stairs' words |

The faces the second solid left are selected after, so `material window` after a window
gives its sides a slot of their own. A window and a door are cut through the wall whose face was
selected first, measured from its left edge, seen from in front, and its bottom, over every selected
face in its plane, so a wall a window cut into pieces is measured whole when its pieces are
selected. Each is as deep as a ray from the face finds the wall's other side, so a room's front wall
is cut through and its back wall is not:

```csharp
var room = new EditMesh()
    .Room(new Vec3(6f, 3f, 5f), 0.2f)
    .SelectFacing(-Vec3.UnitZ)                   // the front wall's outside first
    .Door(1f, 2.1f, 1.5f)
    .Material("door");
```

```text
room 6 3 5 0.2
select -z
cut door 1 2.1 1.5
material door
```

The booleans are csg.js's binary space partition, each solid split by the other's planes. Two faces
in one plane are cut as the partition cuts them, a face of one solid lying in a face of the other
kept or taken by the way it faces, which can leave a sliver or a face where one was not meant, so a
solid cutting an opening goes a little past the faces it cuts through, as a window and a door go a
centimeter past each face of the wall. A point is in a plane within the mesh's epsilon, a hundredth
of a millimeter unless `Epsilon(distance)`, or `epsilon D`, says another, and both solids are
closed, as a boolean needs. What it makes is closed too, its faces joined so a window through a
wall leaves the wall's front in two faces round the hole, since a face of one loop cannot go round
one.

## Grid prototyping

A level blocked out reads by color and by a grid of a meter, as the prototype textures of the
engines of the 2000s read. `Images.Grid()` makes the image of one meter's cell, its edges a line and
a finer line every tenth of a meter, 1024 pixels a meter with its smaller levels, repeating, and the
texture coordinates of a built face are in meters, so the grid shows a meter on every face and runs
on across faces. `Modeling.Prototype(name)` is the grid tinted by the color of a part of a level:

| Name | Color |
|---|---|
| `floor` | Gray |
| `wall` | A pale warm gray |
| `ceiling` | A pale blue |
| `stairs` | Orange |
| `door` | Green |
| `window` | Blue |
| `hazard` | Red |

Any other name takes the grid's own gray, and `Modeling.Prototype(color)` any color. A material
slot named for a part is drawn with that part's material, so a room sorted into slots is drawn by
color with one line a slot:

<!-- compiled with:
var room = new EditMesh();
-->
```csharp
var meshes = Modeling.Meshes(room);
for (var slot = 0; slot < meshes.Length; slot++)
{
    if (meshes[slot] == AssetHandle.None) continue;          // a slot no face is in

    var part = ctx.Ecs.Spawn();
    ctx.Ecs.Add(part, Transform.Identity);
    Render.SetMesh(ctx.Ecs, part, meshes[slot]);
    Render.SetMaterial(ctx.Ecs, part, Modeling.Prototype(room.Slots[slot]));
}
```

## Drawing and colliding

`Modeling.Build(mesh)` builds the mesh into a `MeshData` for each material slot, each face cut into
triangles by ear clipping, so a concave face such as a flight's side is cut inside its outline.
`Modeling.Meshes(mesh)` makes a mesh from each the first time and writes it again in place once the
edit mesh has changed, so an entity drawn with one follows the edits:

<!-- compiled with:
Entity floor = default;
var mesh = new EditMesh();
var grid = Render.CreateMaterial(0.6f, 0.6f, 0.6f);
-->
```csharp
var meshes = Modeling.Meshes(mesh);              // in a system, each frame if it likes
Render.SetMesh(ctx.Ecs, floor, meshes[0]);
Render.SetMaterial(ctx.Ecs, floor, grid);
```

A face's texture coordinates are its corners' places in meters, seen along the axis its normal is
nearest, so a texture of a meter's grid shows a meter on every face and carries on across faces
meeting along a line, and `Meshes` works out from them the tangents a normal map needs.

`Modeling.Collider(mesh)` is the mesh's triangles as a shape a static body collides as, so a level
blocked out is walked on:

<!-- compiled with:
using Bevy.Physics;
Entity level = default;
var mesh = new EditMesh();
var physics = new PhysicsWorld(new PhysicsSettings());
-->
```csharp
physics.Add(level, Modeling.Collider(mesh), BodyKind.Static, Transform.Identity);
```

---

Before this, [Drawing](drawing.md).
Next, [Materials and textures](materials.md).
Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#modeling). The
[guide's contents](../README.md#guide) list every page.
