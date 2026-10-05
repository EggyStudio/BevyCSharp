using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Builds a mesh from each of Bevy's flat shapes and draws it in a color of its own, with a second
// row of the bands along the insides of their outlines. R turns them, and Space shows their edges.
internal static class Shapes2d
{
    private const float XExtent = 1000f, YExtent = 150f, Thickness = 5f;

    private static readonly List<Entity> Meshes = [];
    private static bool _rotating;

    public static void Build(App app)
    {
        Meshes.Clear();
        _rotating = false;

        app.Startup(Setup, "2d_shapes.Setup");
        app.Update(ToggleWireframes, "2d_shapes.ToggleWireframes");
        app.Update(Rotate, "2d_shapes.Rotate");
    }

    private static void Setup(BehaviorContext ctx)
    {
        Render2d.SpawnCamera2d();

        AssetHandle[] shapes =
        [
            Render.CreateMesh(MeshShape.Circle, 50f),
            Render.CreateMesh(MeshShape.CircularSector, 50f, 1f),
            Render.CreateMesh(MeshShape.CircularSegment, 50f, 1.25f),
            Render.CreateMesh(MeshShape.Ellipse, 25f, 50f),
            Render.CreateMesh(MeshShape.Annulus, 25f, 50f),
            Render.CreateMesh(MeshShape.Capsule2d, 25f, 50f),
            Render.CreateMesh(MeshShape.Rhombus, 75f, 100f),
            Render.CreateMesh(MeshShape.Rectangle, 50f, 100f),
            Render.CreateMesh(MeshShape.RegularPolygon, 50f, 6f),
            Render.CreateMesh(MeshShape.Triangle, 100f),

            // A segment and a line through three points are lines rather than shapes, so they are
            // built point by point as Bevy builds them.
            Render.CreateMesh(new MeshData { Positions = [new(-50f, 50f, 0f), new(50f, -50f, 0f)], Topology = MeshTopology.Lines }),
            Render.CreateMesh(new MeshData { Positions = [new(-50f, 50f, 0f), new(0f, -50f, 0f), new(50f, 50f, 0f)], Topology = MeshTopology.LineStrip }),
        ];
        Row(ctx.Ecs, shapes, shapes.Length, YExtent / 2f);

        AssetHandle[] rings =
        [
            Render.CreateMesh(MeshShape.Ring(MeshShape.Circle), 50f, c: Thickness),
            Render.CreateMesh(MeshShape.Ring(MeshShape.CircularSector), 50f, 1f, Thickness),
            Render.CreateMesh(MeshShape.Ring(MeshShape.CircularSegment), 50f, 1.25f, Thickness),
            Render.CreateMesh(MeshShape.Ring(MeshShape.Ellipse), 25f, 50f, Thickness),

            // The same as the annulus above, a disc with a disc of half its radius cut out.
            Render.CreateMesh(MeshShape.Annulus, 25f, 50f),
            Render.CreateMesh(MeshShape.Ring(MeshShape.Capsule2d), 25f, 50f, Thickness),
            Render.CreateMesh(MeshShape.Ring(MeshShape.Rhombus), 75f, 100f, Thickness),
            Render.CreateMesh(MeshShape.Ring(MeshShape.Rectangle), 50f, 100f, Thickness),
            Render.CreateMesh(MeshShape.Ring(MeshShape.RegularPolygon), 50f, 6f, Thickness),
            Render.CreateMesh(MeshShape.Ring(MeshShape.Triangle), 100f, c: Thickness),
        ];

        // Spaced for two more than there are, so the row ends short of the one above.
        Row(ctx.Ecs, rings, rings.Length + 2, -YExtent / 2f);

        Ui.SpawnText("Press 'R' to pause/resume rotation\nPress 'Space' to toggle wireframes",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // Spreads the meshes from one side to the other, their colors evenly around the rainbow.
    private static void Row(EcsWorld ecs, AssetHandle[] meshes, int count, float y)
    {
        for (var i = 0; i < meshes.Length; i++)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(-XExtent / 2f + i / (float)(count - 1) * XExtent, y, 0f));
            Render2d.SetMesh(ecs, entity, meshes[i]);
            Render2d.SetMaterial(ecs, entity, Render2d.CreateMaterial(new ColorMaterialSettings { Color = Scene.Hsl(360f * i / count, 0.95f, 0.7f) }));
            Meshes.Add(entity);
        }
    }

    // Bevy's global wireframe, in its Wireframe2dConfig resource, drawn over every mesh.
    private static void ToggleWireframes(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.Space)) return;
        var config = ctx.Ecs.Resource<Wireframe2dConfigRef>() ?? ctx.Ecs.InsertResource<Wireframe2dConfigRef>();
        config.Global = !config.Global;
    }

    private static void Rotate(BehaviorContext ctx)
    {
        if (ctx.Input.KeyPressed(Key.R)) _rotating = !_rotating;
        if (!_rotating) return;

        var turn = Quat.FromRotationZ(ctx.Time.Delta / 2f);
        foreach (var entity in Meshes)
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(entity);
            ctx.Ecs.Set(entity, at with { Rotation = turn * at.Rotation });
        }
    }
}
