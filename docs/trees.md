# Trees

A tree is grown from a species and a seed and built into meshes, its stems as tubes and its leaves
as cards, so a forest is a few numbers and no models. `Trees.Grow` grows a tree's skeleton,
the points its stems pass through, `Trees.Build` makes the stems into an edit mesh and
`Trees.BuildLeaves` makes the leaves, and the bark and the leaves are drawn with materials made
from the species:

<!-- compiled with:
Entity stems = default;
Entity crown = default;
-->
```csharp
var species = TreeSpecies.QuakingAspen;
var skeleton = Trees.Grow(species, seed: 7);                  // the same tree for the same seed

Render.SetMesh(ctx.Ecs, stems, Modeling.Meshes(Trees.Build(skeleton))[0]);
Render.SetMaterial(ctx.Ecs, stems, Trees.BarkMaterial(species));
Render.SetMesh(ctx.Ecs, crown, Render.CreateMesh(Trees.BuildLeaves(skeleton, species, seed: 7)));
Render.SetMaterial(ctx.Ecs, crown, Trees.LeafMaterial(species));
```

The growth runs on the thread that calls it and touches nothing of the app's, so it runs on a
worker while a level loads, and the two meshes are made in a system as any mesh is.

## Planting and forests

A tree in a game or a level is an entity with a `Tree` component, its species by a registered name
and a seed, which the library grows and dresses itself, so a level keeps a name and a number and no
mesh. `Trees.Plant` makes one at a place, and `Trees.PlantForest` spreads a forest over a disc:

```csharp
Trees.Plant(ctx.Ecs, "quaking-aspen", seed: 12, new Vec3(4f, 0f, -10f));
Trees.PlantForest(ctx.Ecs, "black-tupelo", seed: 3, middle: new Vec3(0f, 0f, -60f), radius: 20f, count: 25);
```

Each species grows eight variants on a worker the first time a tree of it is asked for, and every
tree shares the variant its seed picks, turned and sized by the seed under a child of its entity,
so a forest of a hundred trees grows eight and the entity's own transform stays where it was put.
A planted tree is drawn in three levels of detail, `TreeDetail.Full` near the camera,
`TreeDetail.Reduced` from two and a half of its heights away and a far level from six, each
dithered into the next by Bevy's visibility range, so no tree pops. The reduced level has half the
tube sides, leaves out the stems thinner than a twenty-fifth of the trunk, and draws every other
leaf the square root of two larger at its place, so the crown covers as much with half the cards.
The far level is the tree's sixteen thickest stems with a third of their sides and the branches and
leaves each one carries pictured on two crossed cards, which a camera of the library's own draws
once a variant into an atlas as the trees are planted, with a normal map of the leaves it pictures.
A tree is dressed once its variant's pictures are taken, some frames after it has grown. Each
level's shadow is drawn the same way, measured from the scene's camera. `Trees.Build` and
`Trees.BuildLeaves` take the level for a tree built by hand.

The trunk and each limb twelve centimeters thick or more stand as capsules the player stops at,
through the `RigidBody` and `Collider` components the physics plugin makes bodies of. The presets
are registered as `quaking-aspen`, `black-tupelo`, `weeping-willow`, `california-black-oak`, `bush`
and `fern`, and a game registers its own:

```csharp
Trees.Register("tall-aspen", TreeSpecies.QuakingAspen with { Scale = 20f });
```

The feature test's meadow, south of its hub, is ringed by aspens and oaks with a forest of tupelos
and aspens behind it, which `./bcs open --feature-test` opens.

## The presets

`TreeSpecies` carries the four species of Weber and Penn's paper of 1995 with the values of its
appendix, and a bush and a fern of the library's own on the same controls, each with the bark and
leaves of the library's texture collection:

| Preset | Height | Bark and leaves |
|---|---|---|
| `TreeSpecies.QuakingAspen` | 13 meters, three either way | `silver-birch-bark`, `silver-birch-leaves` |
| `TreeSpecies.BlackTupelo` | 23 meters, five either way | `broadleaf-bark`, `red-alder-leaves` |
| `TreeSpecies.WeepingWillow` | 12 meters, four either way | `weeping-willow-bark`, `weeping-willow-leaves` |
| `TreeSpecies.CaliforniaBlackOak` | 10 meters, ten either way | `broadleaf-bark`, `shingle-oak-leaves` |
| `TreeSpecies.Bush` | a meter and a half | `hornbeam-bark`, `hornbeam-leaves` |
| `TreeSpecies.Fern` | a meter of fronds | `fern-leaves`, drawn as fronds |

A species is a record, so one is made from another with `with`:

```csharp
var tallerAspen = TreeSpecies.QuakingAspen with { Scale = 18f, BaseSize = 0.5f };
```

## The controls

The species sets the crown's outline, eight of them from `TreeShape.Conical` to
`TreeShape.TendFlame` and the paper's pruning envelope as `TreeShape.Envelope`, how tall the tree is
and how much one varies from the next, how much of the trunk is bare and how many trunks it parts
into there, how thick the trunk is for its height and how it flares at the ground, and for the trunk
and each level of branches under it a `TreeLevel`, the angle a stem leaves its parent at, the turn
round the parent from one to the next, how it bends, how often it forks, how many it carries and how
long it grows.

The paper draws each stem from those numbers, and here the branches are grown inside them by space
colonization after Runions, Lane and Prusinkiewicz (2007), with buds and light after Palubicki and
others (2009). A stem's tip carries it to the length the paper gives it while light reaches the tip,
and a bud further out than the first level breaks only where it perceives room, attractors scattered
through the crown's outline, and enough light. Each new point shades the space under it, so the
crown's inside, under its outside, stays open, and the paper's controls are the buds' tendencies:

| Control | What the growth does with it |
|---|---|
| `Shape`, `BaseSize` and the first level's `Length` | How long the first branches are by where they leave the trunk, and with their angles and bends the outline the attractors are scattered through |
| `DownAngle`, `Rotate` | The way a bud points when its stem sets it, leaning out from the stem and turned round it from the bud before |
| `Curve`, `CurveBack`, `CurveVariation` | The turn a stem takes at each internode as it lengthens, a share of the whole bend at each, or of each half's |
| `AttractionUp` | A turn toward upright at each internode from the second level of branches on, by the stem's angle from upright |
| `Splits`, `SplitAngle`, `BaseSplits` | The chance a stem's tip forks at an internode, so a stem forks as often as `Splits` on average, each half turning back toward the way the one grew as the paper's do, and the trunk parting at the crown's foot |
| `Branches` | How many buds a stem sets along its length for the next level, the first level's all breaking and the rest where they find room and light |
| `Length` | The most a stem grows, its tip stopping there or sooner where the light runs out |
| `PruneRatio` and the pruning envelope | Where a branch stops, at the envelope's edge, by the ratio as a chance |
| `Ratio`, `Taper`, `Flare` | How thick the trunk is and how a stem thins, by the pipe model, each point's cross section that of the stems it carries, and how much wider the trunk's foot is |
| `Leaves`, `LeafScale`, `LeafLevel`, `LeafBend` | The leaves along each twig, their length, the angles they leave it at and how far they turn to face up and out |

## Light, walls and neighbors

A tree grows round boxes given to it, a wall or a neighbor's crown in the tree's own space, which
no stem grows into and which take the sky from the side next to them, so the tree stands away from
them toward the light:

```csharp
var wall = (Low: new Vec3(1f, 0f, -6f), High: new Vec3(1.6f, 18f, 6f));
var besideTheWall = Trees.Grow(TreeSpecies.QuakingAspen, seed: 5, [wall]);
```

## Leaves

A twig is a stem that carries no other, and the paper's tree carries `Leaves` leaves on every stem
of its last level. A grown tree has fewer twigs than the paper draws, where light and room stopped
its buds, so the paper's count is shared among the twigs there are, more toward each twig's tip, at
most 30,000 a tree, each leaf drawn larger where the count is cut so the crown covers what the
paper's covers, as the paper's quality factor does. Each leaf is a card of its own picture, cut out
by its opacity, its stalk on the twig or a little off it, as a leaf on the twig's finer twiglets
stands, turned by `LeafBend` to face up and away from the trunk.

Where `Leaves` is below zero, each stem of the last level is itself a leaf, a frond drawn as a strip
along its whole length as wide as `LeafScale`, so a fern's fronds bend as their stems do.

A leaf deep in a crown takes less of the sky than one on its rim. The leaves' area is gathered into
a grid over the crown, and each leaf takes the share of the sky that reaches it through the leaves
round it by Beer and Lambert's law, drawn as a gray its vertices carry, from the leaf's whole color
where the sky reaches it to half of it deep in a dense crown and never darker, since the shadow map
darkens where the sun does not reach. The presets' deepest leaves keep between half and seven
tenths of their color, and the leaves on a crown's upper rim about nine tenths on average.

The crown is lit as a volume as well. Each leaf carries the way out of the crown at it, the way its
leaves thin fastest, and `Trees.LeafMaterial` lights the leaf's diffuse light by its normal bent half
way toward it, so the side of the crown toward the sun stands out from the far side as the crown's
shape decides, more than three times as far as the leaves' own normals light it in a grazing sun. Its specular
and the light let through it are lit by the leaf's own normal, so a crown seen from below keeps its
color rather than mirroring the sky, which a normal bent out of the crown points up at. Lit so twice a
pixel, the leaves cost more than the standard material's lighting, 7.5 ms of a forest of 24 tupelos'
opaque pass at 1920 by 1080 against 5.2 lit once on an RTX 4070 Laptop GPU, as
`TheLeavesLitAsACrownCostIsReadInAForest` prints it, and are drawn forward whatever the camera.

`Trees.LeafMaterial(species, fall)` draws the leaves from both sides with light showing through
them by their translucency, and turns them toward the species' `FallColor` by a share from summer to
fall, so a game easing the share over a season turns its forest:

```csharp
var autumn = Trees.LeafMaterial(TreeSpecies.QuakingAspen, fall: 0.8f);   // in a system
```

## Bark and leaf textures

A species names the sets of the library's texture collection its bark and its leaves are drawn
with, `BarkSet` and `LeafSet`, which `TreeTextures` finds by name, each an albedo, a normal map and a
surface of occlusion, roughness and translucency in the formats a desktop GPU samples as they are. A
leaf set's atlas comes with the box of each leaf in it, so a card shows one leaf, and its smaller
levels keep the share of each box a card sees past the cut, so a crown keeps its leaves as it
stands further off, until a leaf is about a texel of the level it is drawn from. Where the
collection has no set of the name a species gives, the bark is drawn from ridged noise in its
`BarkColor` and the leaves from a generated atlas of its `LeafShape` in its `LeafColor`.

A set's three images load with the samplers they are meant for, and its surface is the occlusion,
the metallic-roughness and the diffuse transmission texture at once, as `Trees.BarkMaterial` binds
them:

```csharp
if (TreeTextures.Find("weeping-willow-bark") is { } set)               // in a system
{
    var (albedo, normal, surface) = set.Load();
    var bark = Render.CreateMaterial(new MaterialSettings
    {
        BaseColorTexture = albedo,
        NormalMap = normal,
        OcclusionTexture = surface,
        MetallicRoughnessTexture = surface,
        Roughness = 1f,
        Metallic = 1f,
    });
}
```

A game adds a folder of its own sets with `TreeTextures.Add(directory)`, made by the import the
library's are made by, and a set of the same name stands in place of the library's.

A bark set carries its depth too, and `Trees.BarkMaterial` gives the bark depth by Bevy's parallax
occlusion, its grooves a fortieth of a stem's circumference deep through twelve layers, so its ridges
hide the grooves behind them as the eye moves round a trunk. The leaves take none. On an RTX 4070
Laptop GPU a forest of 24 tupelos at 1920 by 1080 draws its opaque pass a tenth to a half of a
millisecond slower with the depth than without, as `TheDepthsCostIsReadInAForest` prints it.

## The skeleton

The skeleton is the points the stems pass through, each with the point it grew from and its radius,
so a level may keep it beside the seed and build the same tree where another machine's floating
point would grow it a little differently. `Trees.Build` makes each stem one
[tube](modeling.md#tube) through its points, a stem starting on its parent's middle so the joint is
closed with no boolean, every face in the `bark` slot.

---

Before this, [Modeling](modeling.md).
Next, [Materials and textures](materials.md).
Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#trees). The
[guide's contents](../README.md#guide) list every page.
