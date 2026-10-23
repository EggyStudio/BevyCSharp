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
| `SelectEdges([(0, 4)])` | `select edge 0 4` | Edges by their two ends, which selects the ends |
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

A slot is drawn with the prototype material of its name, as [Grid prototyping](#grid-prototyping)
shows, until it is given a look, a color, a texture and the maps of the engine's material, each a
line of the recipe, the slot made where the mesh has none of its name:

| Call | Line | Does |
|---|---|---|
| `SlotColor("wall", 0.8f, 0.3f, 0.2f)` | `slot color wall 0.8 0.3 0.2` | Tints the texture, or the grid where there is none, in sRGB, an alpha below one after the three drawing the faces blended |
| `SlotTexture("wall", "textures/brick.png")` | `slot texture wall textures/brick.png` | Shows a picture in the grid's place, `none` taking it away |
| `SlotNormalMap("wall", "textures/brick_normal.png")` | `slot normal-map wall textures/brick_normal.png` | Gives the faces a normal map |
| `SlotOcclusionMap("wall", "textures/brick_ao.png")` | `slot occlusion-map wall textures/brick_ao.png` | Gives them an ambient occlusion map |
| `SlotMetallicRoughnessMap("wall", "textures/brick_mr.png")` | `slot metallic-roughness-map wall textures/brick_mr.png` | Gives them metalness in blue and roughness in green, as glTF lays them out |
| `SlotEmissive("lamp", 4f, 3f, 2f)` | `slot emissive lamp 4 3 2` | Makes them emissive, in linear numbers that may pass one |
| `SlotEmissiveMap("lamp", "textures/lamp.png")` | `slot emissive-map lamp textures/lamp.png` | Gives them an emissive map, emissive in white where no emissive color is given |
| `SlotMetallic("wall", 0f)` | `slot metallic wall 0` | Sets how metal they are, from zero to one |
| `SlotRoughness("wall", 0.8f)` | `slot roughness wall 0.8` | Sets how rough they are, from zero to one |
| `SlotAdd("trim")` | `slot add trim` | Adds a slot no face is in yet |
| `SlotRename("trim", "edge")` | `slot rename trim edge` | Gives a slot another name, its faces and its look kept |
| `SlotRemove("edge")` | `slot remove edge` | Takes a slot away, its faces given the first slot |

`LookOf("wall")` reads what a slot was given, and `Modeling.Materials(mesh)` makes a material for
each slot from its look, one a look while the app runs, which the meshes of `Modeling.Meshes` are
drawn with one for one. A path is relative to the asset root, and every texture repeats, since a
face's coordinates run as far past one as the face is meters wide. A model joined or cut with
another keeps its own look for a slot both have and takes the other's for a slot it lacks.

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

## Edges, cuts and mirrors

These act along edges and across planes, as Blender's bevel, loop cut, subdivide, bridge, fill,
dissolve, bisect, mirror, spin and smooth do. An edge is selected where both its ends are, so
selecting a face selects its edges and `select edge 0 4` selects one edge alone:

| Call | Line | Does |
|---|---|---|
| `Bevel(0.1f)`, `Bevel(0.1f, 4)` | `bevel 0.1`, `bevel 0.1 4` | Cuts the selected edges off ten centimeters into each face beside them, flat or round in four segments |
| `LoopCut(0.5f)` | `loopcut 0.5` | Cuts the ring of four sided faces across the selected edge in two, halfway along the edges it crosses |
| `Subdivide()`, `Subdivide(2)` | `subdivide`, `subdivide 2` | Cuts each edge of the selected faces in two, or three, and each face into a grid through the cuts |
| `Bridge()` | `bridge` | Takes away two selected faces, or two regions of them, and joins their rims with a band of faces |
| `Fill()` | `fill` | Closes each hole whose edges are selected with one face |
| `DissolveFaces()` | `dissolve` | Joins each region of selected faces into one face, its rim its corners |
| `DissolveEdges()` | `dissolve edges` | Takes away the selected edges, the faces either side joined |
| `Knife(Vec3.UnitY, at)` | `knife y at 0 1 0` | Cuts the selected faces in two where the plane through a point square to Y crosses them |
| `Mirror(Vec3.UnitX)` | `mirror x` | Copies the selected faces mirrored across the plane at X of zero, the vertices on it shared |
| `Spin(Vec3.UnitY, 360f, 16)` | `spin y 360 16` | Sweeps the selected edges a whole turn round the Y axis in sixteen steps |
| `Smooth(0.5f, 2)` | `smooth 0.5 2` | Moves each selected vertex halfway toward the mean of its neighbors, twice |

A bevel moves each edge of a face beside a beveled edge in along the face by the width, and fills
the strip between with faces across a round profile, a quarter circle where the faces meet square.
A corner where three beveled edges meet is closed by a patch, a triangle at one segment, and where
two meet the strips turn the corner into each other, so the top's four edges of a box beveled run
round it as one band. The strips and the patches are selected after. A cube of two meters with its
twelve edges beveled a quarter meter in two segments is 54 faces on 56 vertices, closed, and holds
less than the cube and more than the same cube beveled flat:

```csharp
var cube = new EditMesh()
    .Box(new Vec3(2f))
    .SelectAll()
    .Bevel(0.25f, 2);
```

A loop cut runs from the selected edge through each four sided face to the edge across it, both
ways, until it comes round, reaches a face of another count, which takes the new point as a corner,
or reaches a hole. The fraction is measured from the selected edge's end of the lower index, and the
loop's vertices are selected after. A box's upright edge cut at half makes a loop round its four
sides.

A subdivision cuts a four sided face into a grid, a triangle into triangles and a face of more
corners into a face for each corner round its middle. A box's six faces subdivided once are 24 faces
on 26 vertices. Neither the loop cut nor the subdivision changes the shape, and a face beside one
that was cut takes the new points along their shared edge, so the mesh stays closed. A face's own
texture coordinates are cut with it.

A bridge pairs the corners of the two rims so the band is shortest, and runs each face of it along
its rim the way the face it replaced did, so two boxes bridged top to bottom are one closed solid.
A dissolved region keeps its rim's corners where they are, and dissolving edges also takes away a
corner left between two edges in a straight line, so a loop cut's loop selected and dissolved puts
the box back. The knife cuts each selected face between the two points where the plane crosses its
outline, and a face beside it takes the point on their shared edge.

A mirror puts each selected vertex within a millimeter of the plane on it, shared by both halves,
and takes away a face lying in the plane, which would lie inside the whole, so half a model built
from the plane out is made whole, with no vertex twice:

```csharp
var block = new EditMesh()
    .Box(new Vec3(1f, 2f, 2f))        // the half from X of zero to one
    .SelectAll()
    .Mirror(Vec3.UnitX);              // the whole, from minus one to one
```

A spin turns the selected edges round an axis through the origin, or through the point after `at`,
and joins each step to the next with a face for each edge, so a profile beside the Y axis spun a
whole turn is a surface of revolution, a vertex on the axis staying where it is. Each face runs
along its edge the other way from the face the edge came from, so a profile's face facing the way
it turns makes a surface facing in, which `Flip()` turns out.

## Modifiers

A modifier is an operation kept in a list and run in order on a copy of the mesh each time it is
built, as Blender's modifier stack is, so what is drawn and collided with is the mesh with its
modifiers applied while the mesh's own vertices and faces, the base, stay as they are and are shaped
under them. Each runs on everything, whatever is selected:

| Line | Does |
|---|---|
| `mirror x`, `mirror x z at 0 0 0` | Mirrors across each axis's plane, through the origin unless a point is given, the vertices on it shared |
| `array 3 2 0 0` | Makes three copies, each two meters along X from the last |
| `array 5 along 0 0 0 4 0 0 4 0 4` | Makes five copies spaced evenly along the line through the points, the first where the mesh is |
| `bevel 0.1`, `bevel 0.1 2 angle 30` | Bevels every edge, or in two segments those whose faces meet at more than 30 degrees |
| `subdivision 2` | Subdivides twice by Catmull and Clark's rules, rounding the mesh toward a smooth surface |
| `solidify 0.1`, `solidify -0.1` | Gives the surface ten centimeters of thickness, outward or inward |
| `boolean subtract models/hole.model at 1 0 0` | Takes another model away, or adds it with `union` or keeps what both hold with `intersect`, a solid primitive's line in the path's place |
| `triangulate` | Cuts every face into triangles |

`AddModifier(line)` adds one at the end, and `MoveModifier(from, to)`, `RemoveModifier(index)` and
`ApplyModifier(index)` reorder the stack, take one out and bake one into the base, each a line of
the recipe as `modifier add mirror x`, `modifier move 1 0`, `modifier remove 0` and `modifier apply
0`. `Modifiers` lists the stack, and `Built()` answers a copy of the mesh with the stack applied:

```csharp
var tower = new EditMesh()
    .Box(new Vec3(1f, 2f, 2f))
    .AddModifier("mirror x")          // a whole box from the half
    .AddModifier("subdivision 2");    // rounded
var built = tower.Built();            // 98 vertices, the base still 8
```

A modifier is built once as it is added, so one that cannot run, a boolean with a model that is not
there among them, is refused then. A model joined or used in a boolean is taken as it is built, its
modifiers applied. A stack of a mirror, an array of four, a bevel in two segments and two levels of
subdivision on a box, 5,760 faces, builds in 13 ms on four cores
(`dotnet test --filter AStackOfFourBuildsWithinItsBudget`), the window building it again each time it
changes the base.

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
| `Union("stairs 16 0.2 0.3 1.2 at 0.2 0.2 1.4")` | `union stairs 16 0.2 0.3 1.2 at 0.2 0.2 1.4` | Adds a solid primitive made where its line places it, as `subtract` and `intersect` take one too |
| `Room(new Vec3(6f, 3f, 5f), 0.2f)` | `room 6 3 5 0.2` | Adds a box hollowed by walls, floor and ceiling two tenths thick |
| `Window(1f, 1.2f, 2f, 0.9f)` | `cut window 1 1.2 2 0.9` | Cuts a window through the wall selected, its middle two meters along, its sill at 0.9 |
| `Door(1f, 2.1f, 1.5f)` | `cut door 1 2.1 1.5` | Cuts a door from the wall's bottom, its middle a meter and a half along |
| `Ramp(1f, 3f, 2f)` | `ramp 1 3 2` | Adds a ramp a meter high over three, two wide, in a flight of stairs' words |

The faces the second solid left are selected after, so `material window` after a window
gives its sides a slot of their own, and a primitive's faces start in the mesh's first slot. A
flight of stairs standing on a floor and against a wall is joined with `union` rather than added
with `stairs`, since a solid added touches the floor and the wall as a second one, and a boolean
after it welds their edges together, four faces along each, which counts every one of them open. A window and a door are cut through the wall whose face was
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
slot named for a part is drawn with that part's material until it is given a look, which
`Modeling.Materials` follows, so a room sorted into slots is drawn by color with one line a slot:

<!-- compiled with:
var room = new EditMesh();
-->
```csharp
var meshes = Modeling.Meshes(room);
var materials = Modeling.Materials(room);
for (var slot = 0; slot < meshes.Length; slot++)
{
    if (meshes[slot] == AssetHandle.None) continue;          // a slot no face is in

    var part = ctx.Ecs.Spawn();
    ctx.Ecs.Add(part, Transform.Identity);
    Render.SetMesh(ctx.Ecs, part, meshes[slot]);
    Render.SetMaterial(ctx.Ecs, part, materials[slot]);
}
```

## Texture coordinates

A face's texture coordinates are its corners' places in meters, seen along the axis its normal is
nearest, which follow the face wherever it moves. The `uv` operations give the selected faces
coordinates of their own, which stay on their corners as they move, as a texture stuck to the face,
and are carried onto the pieces a cut, an inset or a boolean makes of the face:

| Call | Line | Does |
|---|---|---|
| `UvPlanar(Vec3.UnitY)` | `uv planar +y` | Projects along a direction, as a picture shone onto the faces from that side, `uv planar 0.3 0.5 0.8` along any |
| `UvBox()` | `uv box` | Projects each face along the axis its normal is nearest, the default written onto the faces |
| `UvCylinder(Vec3.UnitY)` | `uv cylinder y` | Wraps round an axis through the selection's middle, or `at` a point, in meters round and down |
| `UvSphere()` | `uv sphere` | Wraps round the selection's middle, or `at` a point, as a globe |
| `UvMove(0.5f, 0f)` | `uv move 0.5 0` | Moves the coordinates |
| `UvScale(2f)` | `uv scale 2` | Scales them about the selection's middle, two factors for the two coordinates, `each` about each face's own |
| `UvRotate(90f)` | `uv rotate 90` | Turns them about the middle, `each` as for a scale |
| `UvDensity(512f, 1024)` | `uv density 512 1024` | Scales them so a texture 1024 pixels wide shows 512 of its pixels a meter |
| `UvReset()` | `uv reset` | Puts the faces back on the default |

`model.uv` and the window read a density's width from the texture of the first selected face's
slot when the line gives one number, and the line written keeps the width, so the recipe makes the
same coordinates whether the texture has loaded or not. `UvsOf(face)` reads a face's coordinates
and `HasOwnUvs(face)` says whether they are its own. A wall of bricks shown at 512 pixels a meter
is these lines:

<!-- compiled with:
var hall = new EditMesh();
-->
```csharp
hall.SelectFacing(Vec3.UnitZ)
    .Material("wall")
    .SlotTexture("wall", "textures/brick.png")
    .SlotNormalMap("wall", "textures/brick_normal.png")
    .UvDensity(512f, 1024);
```

## From the terminal

Every line of a recipe is a command of the console's, the model's name after the verb, so a level is
blocked out from the terminal in a running app, each command answering with the model's counts:

```bash
./bcs open --feature-test --offscreen --frames 0
./bcs command model.room hall 6 3 5 0.2          # hall: 16 vertices, 24 edges, 12 faces, ...
./bcs command model.material hall wall
./bcs command model.select hall -z               # the front wall's outside
./bcs command model.cut hall window 1 1.2 2 0.9  # hall: 30 vertices, 47 edges, 19 faces, 4 faces ... selected
./bcs command model.material hall window
./bcs command model.show hall 0 0 -14            # drawn in its slots' prototype materials, and kept drawn
./bcs command model.info hall                    # bounds, volume, open edges, slots
./bcs command model.save hall models/hall.model
```

A slot's look and the faces' coordinates are commands as well, and a density given one number reads
the width of the slot's texture once it has loaded:

```bash
./bcs command model.slot hall texture wall textures/brick.png
./bcs command model.select hall slot wall
./bcs command model.uv hall density 512
```

A primitive, a join or a union makes the model where there is none of its name, and any other
operation on a name no model has is refused, so a mistyped name makes no model. `model.new`,
`model.free`, `model.list`, `model.recipe`, `model.undo`, `model.redo` and `model.load` do what
their names say, and `model.show` draws a model where a point says and keeps drawing it as the
commands change it. A game's own mesh answers to the commands once `Modeling.Register("level",
mesh)` names it, so a drive cuts a window in a level by a command and reads the counts.

## The window

`Modeling.DrawWindow` draws a window that shapes a mesh by hand, called every frame it is to be up
from a system of an app with the interface on (`Config.Gui`), with the camera the 3D view is drawn
by and where the mesh is drawn:

<!-- compiled with:
Entity camera = default;
var level = new EditMesh();
-->
```csharp
Modeling.DrawWindow(ctx, level, camera, Transform.At(0f, 0f, -14f));
```

The window lists the recipe with buttons taking its last line back and doing it again, the counts
and what is selected, the grid step, and each operation with its measures as fields and a button
that runs its line, so each change made in the window is a line of the recipe, as a command's is.
A line that cannot run says why under the selection's buttons.

In the 3D view, a click on a face selects it, by a ray from the camera tested against the mesh's
faces where the transform draws them, with shift adding the face and control taking it away. A drag
from a face along its normal extrudes the selection by whole grid steps once the button is let go,
the distance shown beside the pointer and the face where it would go outlined in cyan, and with alt
held it moves the selection as far instead. The face under the pointer is outlined in yellow and
the selection in orange. A drag that comes back to where it began does nothing.

The Modifiers group lists the stack with what it builds, and adds, moves, removes and applies
modifiers. The Edges group selects an edge by its two ends, and bevels, loop cuts, subdivides, bridges,
fills, dissolves, cuts with a plane, mirrors, spins and smooths with the measures typed beside each. The Texture group projects the selected faces' coordinates, along the view among the directions,
moves, scales and turns them, and draws them over their slot's texture, repeated as far as they
reach. The Slots group gives the slots their looks and adds, renames and takes away slots. The
window takes no focus as it appears, so the key that opened it moves into none of its fields, and
the program's keys stay its own until the window is clicked. A shortcut asks
`ImGuiRuntime.Typing`, which is true while a field has the caret.

[Blockout](../games/Blockout/assets/scripts/Blockout.cs), under `games/`, is a level made this way
from [one recipe](../games/Blockout/assets/levels/blockout.model), three rooms, a corridor, stairs
between two floors, two doors, a window and a lamp, cut from one block. It is walked through the
character controller on `Modeling.Collider`'s shape, made again each time the level changes, Tab
opens this window on the level while it is walked in, and F5 saves the recipe.
`blockout.autopilot` walks it up the stairs or round every room for a scripted run, and
`blockout.status` says where the walker stands and what lights the level.

`Modeling.WantsPointer` is true while the pointer is over a face or pressed on one, so a camera that
turns on a drag, as the editor's does with alt, begins one only where it is false, as it asks
`ImGuiRuntime.WantsMouse` over the interface. `Modeling.DrawPanel` draws the same contents where a
program has begun a window of its own, and `Modeling.Source` gives the edit mesh a mesh of
`Meshes` was built from. The editor's Model tab is the window on the entity selected, so once an
entity drawn with a model's mesh is selected, one `model.show` made for instance, and the tab is
open, a click on the scene picks the model's faces while the entity stays selected.

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
