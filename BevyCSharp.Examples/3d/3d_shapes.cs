using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Here we use shape primitives to generate meshes for 3d objects as well as attaching a runtime-
// generated patterned texture to each 3d object.
//
// Bevy's front row, the solids, is all here. Its segment and polyline, and its two rows of
// extrusions, are shapes the bridge does not build.
internal static class Example3dShapes
{
    private const float ShapesXExtent = 14f;

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
                Render.CreateMesh(MeshShape.Torus, 0.5f, 1f),
                Render.CreateMesh(MeshShape.Cylinder, 0.5f, 1f),
                Render.CreateMesh(MeshShape.Cone, 0.5f, 1f),
                Render.CreateMesh(MeshShape.ConicalFrustum, 0.25f, 0.5f, 0.5f),
                Render.CreateMesh(MeshShape.Sphere, 0.5f),
            };

            for (var i = 0; i < shapes.Length; i++)
            {
                var x = -ShapesXExtent / 2f + i / (float)(shapes.Length - 1) * ShapesXExtent;
                var shape = ctx.Ecs.Mesh(shapes[i], debug, new Transform(new Vec3(x, 2f, 0f), Quat.FromRotationX(-MathF.PI / 4f), Vec3.One));
                ctx.Ecs.Add(shape, new Shape());
            }

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
            ctx.Ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 50f, 50f), Scene.Material(Scene.Srgb8(192, 192, 192)), Transform.Identity);

            ctx.Ecs.Camera(Transform.LookingAt(new Vec3(0f, 7f, 14f), new Vec3(0f, 1f, 0f), Vec3.UnitY));

            Ui.SpawnText(
                "Press 'R' to pause/resume rotation\nPress 'Space' to toggle wireframes",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.R)) Shape.Paused = !Shape.Paused;
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

    [OnUpdate]
    public void Rotate(BehaviorContext ctx, ref Transform transform)
    {
        if (!Paused) transform.Rotation = Quat.FromRotationY(ctx.Time.Delta / 2f) * transform.Rotation;
    }
}
