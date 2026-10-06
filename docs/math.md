# Math

Bevy's math that a game reaches for beside its transforms, shapes as values and points sampled in
them.

## Shapes and points sampled in them

A shape is a value centered on the origin, measured as Bevy measures it, a `Cuboid` by half its
size along each axis and a `Sphere` by its radius. A game samples points in one or on its surface to
scatter what it spawns, each point as likely as any other, from a `Random` it seeds where the same
points are needed again:

```csharp
var crate = Cuboid.FromLength(2.9f);
var random = new Random(19878367);

Vec3 inside = crate.SampleInterior(random);
Vec3 onTheSurface = crate.SampleBoundary(random);    // a face chosen by its area, then a point across it
Vec3 inTheBall = new Sphere(3f).SampleInterior(random);
```

A box's surface is landed on face by face as often as each face's area says, and a ball's inside
is filled evenly, as many points in each shell as its share of the volume, so its middle is not
crowded. Bevy draws from its own generator, ChaCha8, so the same seed scatters other points here.
Drawing a shape is `Render.CreateMesh` with the shape's name and measures, its full size.
