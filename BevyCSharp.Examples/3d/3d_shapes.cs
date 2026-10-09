// Bevy's 3d_shapes example, examples/3d/3d_shapes.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Here we use shape primitives to generate meshes for 3d objects as well as attaching a runtime-
// generated patterned texture to each 3d object.
//
// Bevy's front row of solids and middle row of extrusions are here, and Tab moves every shape back
// a row. Its segment and polyline, the convex polygon at the end of its extrusions and its rear
// row of extruded rings are shapes the bridge does not build.
internal static class Example3dShapes
{
    private const float ShapesXExtent = 14f, ExtrusionXExtent = 14f, ZExtent = 8f;

    // Bevy's wireframe plugin, which Bevy's example adds for Space to draw the shapes' edges.
    public static void Configure(Config config) => config.Wireframes = true;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var debug = Render.CreateMaterial(new MaterialSettings { BaseColorTexture = UvDebugTexture() });

            var shapes = new[]
            {
                Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f),
                Render.CreateMesh(MeshShape.Tetrahedron, 1f),
                Render.CreateMesh(MeshShape.Capsule, 0.5f, 1f),
                Render.CreateMesh(MeshShape.Torus, 0.5f, 1.5f),
                Render.CreateMesh(MeshShape.Cylinder, 0.5f, 1f),
                Render.CreateMesh(MeshShape.Cone, 0.5f, 1f),
                Render.CreateMesh(MeshShape.ConicalFrustum, 0.25f, 0.5f, 0.5f),
                Render.CreateMesh(MeshShape.Sphere, 0.5f),
                Render.CreateMesh(MeshShape.UvSphere, 0.5f, 32f, 18f),
            };

            // Bevy's default of each flat shape, a unit deep.
            var extrusions = new[]
            {
                Render.CreateMesh(MeshShape.Extrusion(MeshShape.Rectangle), 1f, 1f, 1f),
                Render.CreateMesh(MeshShape.Extrusion(MeshShape.Capsule2d), 0.5f, 1f, 1f),
                Render.CreateMesh(MeshShape.Extrusion(MeshShape.Annulus), 0.5f, 1f, 1f),
                Render.CreateMesh(MeshShape.Extrusion(MeshShape.Circle), 0.5f, 1f, 1f),
                Render.CreateMesh(MeshShape.Extrusion(MeshShape.Ellipse), 1f, 0.5f, 1f),
                Render.CreateMesh(MeshShape.Extrusion(MeshShape.RegularPolygon), 0.5f, 6f, 1f),
                Render.CreateMesh(MeshShape.Extrusion(MeshShape.Triangle), 1f, 1f, 1f),
            };

            void Row(AssetHandle[] meshes, float extent, int row)
            {
                for (var i = 0; i < meshes.Length; i++)
                {
                    var x = -extent / 2f + i / (float)(meshes.Length - 1) * extent;
                    var shape = ctx.Ecs.SpawnMesh(meshes[i], debug, new Transform(new Vec3(x, 2f, Shape.Z(row)), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
                    ctx.Ecs.Add(shape, new Shape { Row = row });
                }
            }

            Row(shapes, ShapesXExtent, 0);
            Row(extrusions, ExtrusionXExtent, 1);

            var light = Render.SpawnLight(new LightSettings
            {
                Kind = LightKind.Point,
                Intensity = 10_000_000f,
                Range = 100f,
                Shadows = true,
                ShadowDepthBias = 0.2f,
            });
            ctx.Ecs.Add(light, Transform.At(8f, 16f, 8f));

            // Bevy's silver, #c0c0c0.
            ctx.Ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 50f, 50f), Render.CreateMaterial(Color.FromSrgb8(192, 192, 192)), Transform.Identity);

            ctx.Ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 7f, 14f), new Vec3(0f, 1f, 0f), Vec3.UnitY));

            Ui.SpawnText(
                "Press 'R' to pause/resume rotation\nPress 'Tab' to cycle through rows\nPress 'Space' to toggle wireframes",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.R)) Shape.Paused = !Shape.Paused;

            // Front to rear, middle to front and rear to middle.
            if (ctx.Input.KeyPressed(Key.Tab))
            {
                foreach (var row in ctx.Ecs.Query<Shape>())
                {
                    row.Component.Row = row.Component.Row switch { 0 => 2, 1 => 0, _ => 1 };
                    var at = ctx.Ecs.GetOrDefault<Transform>(row.Entity);
                    at.Translation.Z = Shape.Z(row.Component.Row);
                    ctx.Ecs.Set(row.Entity, at);
                }
            }

            if (!ctx.Input.KeyPressed(Key.Space)) return;

            Shape.Wireframes = !Shape.Wireframes;
            foreach (var row in ctx.Ecs.Query<Shape>(markChanged: false)) Render.SetWireframe(row.Entity, Shape.Wireframes);
        }, "3d_shapes.Keys");
    }

    // An eight by eight test pattern, each row the palette turned one color further.
    private static AssetHandle UvDebugTexture()
    {
        const int Size = 8;
        byte[] palette =
        [
            255, 102, 159, 255, 255, 159, 102, 255, 236, 255, 102, 255, 121, 255, 102, 255, 102, 255,
            198, 255, 102, 198, 255, 255, 121, 102, 255, 255, 236, 102, 255, 255,
        ];

        var pixels = new byte[Size * Size * 4];
        for (var y = 0; y < Size; y++)
        {
            palette.CopyTo(pixels, Size * y * 4);
            palette = [.. palette[^4..], .. palette[..^4]];
        }

        return Render.CreateImage(pixels, Size, Size);
    }
}

/// <summary>One of the shapes, which turn about Y until R pauses them.</summary>
[Behavior]
public partial struct Shape
{
    public static bool Paused;
    public static bool Wireframes;

    /// <summary>Which row it stands in, the front, the middle or the rear.</summary>
    public int Row;

    /// <summary>How far forward a row stands, the front four units toward the camera and the rear four away.</summary>
    public static float Z(int row) => row switch { 0 => 4f, 1 => 0f, _ => -4f };

    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform)
    {
        if (!Paused) transform.Rotation = Quat.FromRotationY(ctx.Time.Delta / 2f) * transform.Rotation;
    }
}
