# Levels

A level is blocked out as brushes, convex solids each made of planes with a material on every
face, and filled with entities, each a class and its keyvalues, as Quake's, Half-Life's and
Source's levels are. The file is map text in the Valve 220 format, which TrenchBroom draws and
saves with the Generic game and the Valve map format, so a level is drawn there and loaded here:

```csharp
var root = LevelSpawner.Load(ctx.Ecs, "levels/room.map");    // everything under one root
```

Every entity of the level stands under the root, so despawning the root takes the level away, and
the root stands where `place` puts it, the file's origin, as the feature test stands its hub,
`games/FeatureTest/assets/levels/feature-test.map`, on its ground. `Level.Parse` reads map text,
`Level.Load` a file off the disk, and `LevelSpawner.Spawn` builds a level read or made in code into
the world. `level.load` does the same from the console and from `./bcs`, so a level saved in
TrenchBroom is tried in a running app as soon as it is saved.

The format, the brushes and the shapes below are the Levels module's, a library both engines share,
checked out under `modules/Levels` and named in the namespace `Levels`, which the package carries
beside the engine. It gives an entity's brushes out as a mesh a material and a convex hull a brush,
and `LevelSpawner` is this engine's binding of it, drawing the meshes as their materials, making
their collision and spawning the entities by class.

## The file

The repository's `BevyCSharp/Levels/TrenchBroom` folder holds a game config and the engine's classes
for TrenchBroom, beside the module's base classes in `modules/Levels/TrenchBroom/Levels.fgd`, with
the prototype materials' pictures it shows, and its README says how to install them.
`LevelSpawner.WritePrototypePictures`, or `level.textures` from the console and `./bcs`, writes the
pictures again into a game's folder.

A level's coordinates are TrenchBroom's, Z up and in units. The `worldspawn` entity's
`_units_per_meter` key says how many units make a meter, and a level without it has 32, so
TrenchBroom's 32 unit grid is a meter and its 8 unit grid a quarter of one. The engine turns a
point into meters with Y up, the file's (x, y, z) at (x, z, -y) over the units a meter, a turn that
keeps every face facing the way it did.

A face is written as three points of its plane, clockwise seen from outside the brush, then its
material and two axes across the world with an offset each, a rotation and two scales:

```
( -128 -128 0 ) ( -128 -127 0 ) ( -127 -128 0 ) floor [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1
```

A face's material is laid over it by its picture's texels, a point falling on the texel its axis
gives over the face's scale and moved by its offset, so a picture of 512 texels at a scale of a
sixteenth spans 32 units, a meter, the same in TrenchBroom and in the engine. A material under
`proto/`, as `proto/floor`, is drawn as the prototype of the rest of its name, the grid in Unreal's
level prototyping palette (the modeling page's grid prototyping), over a picture of 512 texels. A
material with a picture of its own under the asset root's `textures` folder, a PNG or a JPEG of its
name, is drawn with it over its own pixels, and any other name is drawn as the modeling slot of that
name over a picture of the level's units a meter, so at a scale of one it repeats once a meter. A
brush's corners are found where three of its planes meet within the rest, so a brush is always
convex and two faces meet edge to edge, and one whose planes close nothing is refused with the
entity it belongs to.

A key may come more than once, as an output naming its target does when a button opens two doors:

```
"OnPressed" "door,Open,,0.5,1"
```

The keys stay in the file's order, `LevelEntity.All` reads every value of one, and the indexer
the first.

`Level.Write` writes the canonical text, laid out as TrenchBroom lays it out, so a level the
engine wrote reads and writes back byte for byte, and one TrenchBroom wrote reads back the same
brushes, entities and keys in the same order. `Level.Save` writes it to a file.

## Entities and classes

The `worldspawn`'s brushes are the level's own, built into one mesh a material and colliding as
their triangles. A brush entity's brushes are built the same way under its own entity, which stands
at its `origin` key where it has one, the point a door turns about, and at the middle of its
brushes otherwise. A point entity stands at its `origin`, turned by its `angle`, a yaw in degrees,
or its `angles`, the pitch, the yaw and the roll, so its forward, minus Z, looks where TrenchBroom
draws its arrow.

Every entity carries a `LevelKeys` component, its class, its `targetname` and every key, which
`LevelSpawner.KeysOf` reads back, so a class the engine does not know still stands where the level
put it with everything the level said of it. `LevelSpawner.Register` says what a class makes of its
entity, here a light of the brightness its `light` key gives, in lumens, standing where the entity
does:

```csharp
LevelSpawner.Register("light", (ecs, entity, source) =>
{
    var lumens = float.Parse(source["light"] ?? "800", System.Globalization.CultureInfo.InvariantCulture);
    ecs.SetParent(Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = lumens }), entity);
});
```

The engine registers `tree`, a tree of the `species` key's name and the `seed` key's seed, which
the library grows and dresses as any planted tree.

## Building a level in code

`LevelBrush.Box`, `LevelBrush.Wedge` and `LevelBrush.Arch` make brushes as a level editor's tools
do, a box, a ramp rising along X and a ring cut into straight segments, and `Level.Doorway` a wall
along X with a rounded opening through it, each face mapped as a new face is, along the world's axes
at a sixteenth's scale. `LevelBrush.Turned` turns a brush about the vertical and `LevelBrush.Moved`
moves it, each mapping its faces again from the world, so a wall another way or elsewhere is one of
these turned and moved, as the feature test's level is built:

<!-- compiled with:
using Levels;
-->
```csharp
var level = new Level();
var world = new LevelEntity();
world["classname"] = "worldspawn";
world.Brushes.Add(LevelBrush.Box(new LevelPoint(-256, -256, -16), new LevelPoint(256, 256, 0), "proto/floor"));
world.Brushes.AddRange(Level.Doorway(new LevelPoint(-128, 240, 0), new LevelPoint(128, 256, 160), 64, 128, "proto/wall", arch: "proto/stairs"));
world.Brushes.AddRange(Level.Doorway(new LevelPoint(-128, 240, 0), new LevelPoint(128, 256, 160), 64, 128, "proto/wall")
    .Select(brush => brush.Turned(90, new LevelPoint(0, 0, 0))));
level.Entities.Add(world);
level.Save("levels/room.map");
```
