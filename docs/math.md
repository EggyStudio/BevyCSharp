# Math

Bevy's math that a game reaches for beside its transforms, shapes as values, points sampled in them,
and the volumes about them that tell what may touch.

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

## Bounding volumes and casts

A shape in the plane, a `Rectangle`, `Circle`, `Triangle2d`, `Segment2d`, `Capsule2d` or
`RegularPolygon`, gives its bounds where an `Isometry2d` places it, a box along the axes or a circle
about it, as Bevy's `Bounded2d` does. Each is an `IBounded2d`, so a game holding a mix asks each
alike. A box is cheap and tight on a shape that stays square to the axes, and a circle stays the
same as its shape turns:

```csharp
var placed = Isometry2d.FromTransform(transform);          // turned about Z, moved in X and Y
Aabb2d box = shape.AabbAt(placed);
BoundingCircle circle = shape.BoundingCircleAt(placed);
if (box.Intersects(otherBox) || circle.Intersects(otherBox)) Check(entity);
```

A ray is tested against a volume as far as a distance, and a box or a circle is swept along one,
each answering how far along it first meets the volume, zero from inside, or null where it misses:

```csharp
var cast = new RayCast2d(Ray2d.Toward(origin, direction), maxDistance);
if (cast.AabbIntersectionAt(box) is { } distance) Hit(cast.Ray.At(distance));

var sweep = new BoundingCircleCast(new BoundingCircle(Vec2.Zero, 15f), cast);
float? wall = sweep.CircleCollisionAt(circle);             // how far the ball rolls first
```

The tests are Bevy's own, the slab test for a box and the closest approach for a circle, and a
triangle with a wide angle is held by the circle on the side across that angle rather than the
larger one through its corners, as Bevy's is.
