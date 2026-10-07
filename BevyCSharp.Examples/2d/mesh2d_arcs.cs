// Bevy's mesh2d_arcs example, examples/2d/mesh2d_arcs.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Demonstrates UV mappings of the CircularSector and CircularSegment primitives, and draws the
// bounding boxes and circles of the primitives. Eight copies of the Bevy logo are cut as sectors
// and eight as segments, each larger than the last up to a whole circle. A shape's image is mapped
// at an angle as well as turned by its transform, so the logo stays upright as each sector turns to
// start at the top, and charges forward in each segment turned a quarter turn.
internal static class Mesh2dArcs
{
    private const int NumSlices = 8;
    private const float SpacingX = 100f;
    private const float OffsetX = SpacingX * (NumSlices - 1) / 2f;

    internal static readonly Color Red = Color.FromSrgb(1f, 0f, 0f);
    internal static readonly Color Blue = Color.FromSrgb(0f, 0f, 1f);

    public static void Build(App app) => app.Startup(Setup, "mesh2d_arcs.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var material = Render2d.CreateMaterial(new ColorMaterialSettings { Texture = AssetServer.Load(AssetKind.Image, "branding/icon.png") });

        var camera = Render2d.SpawnCamera2d();
        ecs.Wrap<CameraRef>(camera).ClearColor = new ClearColorConfig.Custom(Color.FromSrgb(0.5f, 0.5f, 0.5f));

        for (var i = 0; i < NumSlices; i++)
        {
            var fraction = (i + 1) / (float)NumSlices;

            // Turned so the sectors appear clockwise from north, in the transform and in the mesh's
            // mapping onto the image both.
            var sector = CircularSector.FromTurns(40f, fraction);
            var sectorAngle = -sector.HalfAngle;
            var sectorMesh = Render.CreateMesh(MeshShape.UvAngle(MeshShape.CircularSector), sector.Radius, sector.HalfAngle, sectorAngle);
            var sectorEntity = Spawn(ecs, sectorMesh, material, new Vec3(SpacingX * i - OffsetX, 50f, 0f), sectorAngle);
            ecs.Add(sectorEntity, new SectorBounds { Shape = sector });

            // Bevy charging forward, which turns the shape and its image a quarter turn. The angle
            // is the one the vertices are mapped onto the image at rather than the image's own, so
            // it is the turn the other way.
            var segment = CircularSegment.FromTurns(40f, fraction);
            var segmentAngle = -MathF.PI / 2f;
            var segmentMesh = Render.CreateMesh(MeshShape.UvAngle(MeshShape.CircularSegment), segment.Radius, segment.HalfAngle, -segmentAngle);
            var segmentEntity = Spawn(ecs, segmentMesh, material, new Vec3(SpacingX * i - OffsetX, -50f, 0f), segmentAngle);
            ecs.Add(segmentEntity, new SegmentBounds { Shape = segment });
        }
    }

    private static Entity Spawn(EcsWorld ecs, AssetHandle mesh, AssetHandle material, Vec3 at, float angle)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, new Transform { Translation = at, Rotation = Quat.FromRotationZ(angle), Scale = Vec3.One });
        Render2d.SetMesh(ecs, entity, mesh);
        Render2d.SetMaterial(ecs, entity, material);
        return entity;
    }

    // Bevy's draw_bounds, the box about a shape in red and the circle about it in blue, where its
    // entity places it.
    internal static void DrawBounds(IBounded2d shape, Transform transform)
    {
        var isometry = Isometry2d.FromTransform(transform);

        var box = shape.AabbAt(isometry);
        var size = box.HalfSize * 2f;
        Gizmos.Rect2d((box.Center.X, box.Center.Y), size.X, size.Y, Red);

        var circle = shape.BoundingCircleAt(isometry);
        Gizmos.Circle2d((circle.Center.X, circle.Center.Y), circle.Radius, Blue);
    }
}

/// <summary>A sector whose bounds are drawn where its entity is, Bevy's <c>DrawBounds</c> of a <c>CircularSector</c>.</summary>
[Behavior]
public partial struct SectorBounds
{
    /// <summary>The sector the entity's mesh was cut as.</summary>
    public CircularSector Shape;

    /// <summary>Draws the sector's box and circle each frame, as Bevy's <c>draw_bounds</c> does.</summary>
    [OnUpdate]
    public readonly void Draw(BehaviorContext ctx, ref Transform transform) => Mesh2dArcs.DrawBounds(Shape, transform);
}

/// <summary>A segment whose bounds are drawn where its entity is, Bevy's <c>DrawBounds</c> of a <c>CircularSegment</c>.</summary>
[Behavior]
public partial struct SegmentBounds
{
    /// <summary>The segment the entity's mesh was cut as.</summary>
    public CircularSegment Shape;

    /// <summary>Draws the segment's box and circle each frame, as Bevy's <c>draw_bounds</c> does.</summary>
    [OnUpdate]
    public readonly void Draw(BehaviorContext ctx, ref Transform transform) => Mesh2dArcs.DrawBounds(Shape, transform);
}
