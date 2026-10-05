# Gizmos

Lines and shapes drawn for a frame, for debugging and for tools.

Debug drawing, for watching what a program is doing:

```csharp
Gizmos.Line(from, to, (0.3f, 0.8f, 1f, 1f));
Gizmos.Arrow(position, position + velocity, (1f, 0.4f, 0.2f, 1f));
Gizmos.Sphere(position, 0.35f, (1f, 0.85f, 0.2f, 1f));
Gizmos.Axes(transform, 1.5f);
```

`Line` and `Fade` draw a plain or a dying line, `Arrow` a line that has to say which way along it,
`Sphere`, `Circle`, `Ellipse`, `Arc`, `Rect`, `RoundedRect` and `Grid` a volume, a plane, an angle
or a floor, `Box`, `RoundedCuboid`, `Capsule`, `Cone`, `Cylinder`, `Torus` and `Frustum` the shapes
a collider or a radius of effect usually is, and `Axes` an orientation. Everything but a line takes
a `Quat`, because a shape with a flat side has to be told which way it faces.

```csharp
Gizmos.Arc(joint, facing, radius: 1.2f, angle: MathF.PI / 3f, (0.9f, 0.9f, 0.2f, 1f));
Gizmos.Box(bounds.Center, Quat.Identity, bounds.Size, (0.2f, 1f, 0.4f, 1f));
Gizmos.Grid(Vec3.Zero, Quat.Identity, across: 20, down: 20, spacing: 1f, (1f, 1f, 1f, 0.15f));
```

A curved shape takes a `resolution`, the segments it is drawn with, where a large circle needs
more to stay round and a small one drawn many times fewer, and zero leaves Bevy's own. An arrow
takes the length of its head and whether it has one at each end, for a span between two points.
A rounded box takes how round its corners are, a negative radius turning them inward, and a grid a
second spacing for cells longer one way than the other.

A gizmo lasts one frame, so anything that should stay on screen is asked for again every frame. That
makes them right for a value that changes. `Axes` colors itself red, green and blue for X, Y and Z,
scaled as the transform is, which is the quickest way to see whether something faces where it
should.

Lines that do not change are kept instead. `Gizmos.Record` keeps every shape asked for inside it in
an asset, and `Gizmos.Attach` has an entity draw that asset every frame, placed by the entity's
transform, with nothing asked for again:

```csharp
var outline = Gizmos.Record(() => Gizmos.Sphere(Vec3.Zero, 1f, (1f, 0.2f, 0.2f, 1f), resolution: 512));
Gizmos.Attach(ecs, entity, outline);
ecs.Wrap<GizmoRef>(entity).LineConfigWidth = 4f;
```

The entity's `GizmoRef` holds its own line width, style and depth bias, where a group's settings
apply to the shapes asked for each frame.

`inFront` decides whether the scene may hide a shape, and it is true everywhere except `Grid`. A
handle, an outline or a marker is drawn *about* the scene and has to be reachable; a grid, a path or
a wireframe is drawn *in* it and has to be behind what is in front of it. `Gizmos.Configure` sets
the line width, which render layers gizmos appear on, and whether they are drawn at all, for a debug
overlay bound to a key. Its `which` names a group, so a floor grid and a set of handles can be
turned on and off apart. The two a game draws in are the same split `inFront` chooses between,
which is why they line up with the two kinds of drawing already. `Lights` and `Bounds` are Bevy's
own groups for what it draws itself, and `All` is every group at once.

`Gizmos.SetDepthBias` moves a group toward the camera or away before the scene's depth is tested,
from -1, in front of everything, to 1, behind it. The in-front group sits at -1, and a key that
sets every group there shows the lot through walls.

Bevy draws two things of its own on request. `Gizmos.ShowLights` draws the shape of every light,
a point light's sphere, a spot light's cone, a directional light's arrow and a rectangle light's
rectangle, colored by the light, by its kind, by its entity or all alike. `Gizmos.ShowBounds` draws
the bounding box Bevy culls each entity by, which is where to look when something vanishes at the
edge of the screen. Without them, a light or an entity carrying `ShowLightGizmoRef` or
`ShowAabbGizmoRef` is drawn alone.

`Gizmos.SetLineStyle` decides what the line itself looks like. A dotted or dashed line tells one
meaning from another without spending a second color on it, so a path already walked can be drawn
against the one still to come, and the gap and the run of a dash are measured in line widths.
`joint` rounds, mitres or bevels the corners of a closed shape, which shows at the thick widths an
overlay meant to be read at a glance uses. `perspective` makes the width a size at the camera's
near plane rather than a size on screen, so a line further away is drawn thinner.

`Gizmos.Lines` draws a whole run of segments in one crossing, each with its own two ends and color,
and `GizmoSegment.Fading` gives one a second color so it can run out to nothing. It serves a
wireframe, a path or a grid, where the cost otherwise grows with the number of lines rather than
with the call. The editor's own floor grid is one of these. `Gizmos.Polyline`, `Gizmos.Triangle`
and `Gizmos.Tetrahedron` draw through a run of points the same way, a tetrahedron's six edges in one
call, from four corners or from a center and an edge's length.

Any other shape crosses on its own, which adds up for a scene drawing thousands of them a frame.
Inside a batch they are gathered and handed over together:

```csharp
using (Gizmos.Batch())
{
    foreach (var row in ctx.Ecs.Query<Collider>())
        Gizmos.Sphere(row.Position, row.Radius, (0f, 1f, 0f, 1f));
}
```

`Rect2d`, `RoundedRect2d`, `Circle2d`, `Ellipse2d`, `Line2d`, `Arrow2d`, `Arc2d` and `Grid2d` are
the same shapes for a 2D camera. They take a point on the XY plane and an angle about Z, because that is all a flat shape
can be turned by, and they go through Bevy's own flat calls rather than through the solid ones at
zero depth, which differ once a line has width.

`Gizmos.Text` writes a label in the world, and `Text2d` the same for a 2D camera, drawn as lines in
Bevy's stroke font, so it costs no font asset and no entity and lasts the frame it was asked for:

```csharp
Gizmos.Text($"{speed:0.0} m/s", position + Vec3.UnitY, Quat.Identity, 0.3f, (0f, -0.5f), (1f, 1f, 1f, 1f));
Gizmos.Text2d("spawn", (120f, -40f), 0f, 16f, (0f, 0f), (1f, 0.8f, 0f, 1f));
```

Its size is the height of a capital letter, and its anchor the point of its bounds that stands at
the position, from minus a half to a half on each axis, so `(0f, -0.5f)` puts the middle of its
bottom edge there. The font has the printable ASCII characters and a line break starts a new line.
Its lines are as thick as every gizmo's, so text a player reads belongs in `Ui` or on a `Text2d`
entity instead, which a real font shapes.

Gizmos are drawn by a plugin that comes with the window, so a windowless run refuses rather than
collecting shapes nothing will draw. Guard with `App.HasRenderer`.

---

Before this, [2D](2d.md).
Next, [The interface](ui.md).
Bevy's own examples of this, and which are written in C# here, are in
[EXAMPLES.md](../.github/EXAMPLES.md#gizmos). Its calls are each a line in the [cheatsheet](../CHEATSHEET.md#gizmos). The [guide's contents](../README.md#guide) list every page.
