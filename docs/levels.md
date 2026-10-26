# Levels

A level is blocked out as brushes, convex solids each made of planes with a material on every
face, and filled with entities, each a class and its keyvalues, as Quake's, Half-Life's and
Source's levels are. The file is map text in the Valve 220 format, which TrenchBroom draws and
saves with the Generic game and the Valve map format, so a level is drawn there and loaded here:

```csharp
var root = Levels.Load(ctx.Ecs, "levels/room.map");          // everything under one root
```

Every entity of the level stands under the root, so despawning the root takes the level away.
`Level.Load` reads the file alone, `Level.Parse` reads map text, and `Levels.Spawn` builds a level
read or made in code into the world. `level.load` does the same from the console and from `./bcs`,
so a level saved in TrenchBroom is tried in a running app as soon as it is saved.

## The file

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

The material is a name, which the face is drawn with as a modeling slot of that name is, so a face
of `wall` is drawn as a wall's prototype. The mapping is aligned to the world, a point on the face
falling on the material's texel along each axis over the scale and moved by the offset, and a
material's picture spans as many texels as the level has units a meter, so at a scale of one it
repeats once a meter. A brush's corners are found where three of its planes meet within the rest,
so a brush is always convex and two faces meet edge to edge, and one whose planes close nothing is
refused with the entity it belongs to.

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
`Levels.KeysOf` reads back, so a class the engine does not know still stands where the level put
it with everything the level said of it. `Levels.Register` says what a class makes of its entity,
here a light of the brightness its `light` key gives, in lumens, standing where the entity does:

```csharp
Levels.Register("light", (ecs, entity, source) =>
{
    var lumens = float.Parse(source["light"] ?? "800", System.Globalization.CultureInfo.InvariantCulture);
    ecs.SetParent(Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = lumens }), entity);
});
```

The engine registers `tree`, a tree of the `species` key's name and the `seed` key's seed, which
the library grows and dresses as any planted tree.
