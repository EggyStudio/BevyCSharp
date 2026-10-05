// Bevy's pixel_grid_snap example, examples/2d/pixel_grid_snap.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Shows how to draw a pixel-perfect world at a low resolution and scale it up to the window, so
// what turns in it snaps to its coarse pixels, beside a sprite drawn at the window's own resolution
// that turns smoothly.
internal static class PixelGridSnap
{
    // The game's own resolution.
    private const uint ResWidth = 160, ResHeight = 90;

    // What the low-resolution camera draws, and what the window's camera draws, the canvas among it.
    private const uint PixelPerfectLayers = 1u << 0;
    private const uint HighResLayers = 1u << 1;

    private static readonly List<Entity> Rotating = [];
    private static Entity _outerCamera;

    public static void Build(App app)
    {
        Rotating.Clear();
        app.Startup(Setup, "pixel_grid_snap.Setup");
        app.Update(Rotate, "pixel_grid_snap.Rotate");
        app.Update(ctx => FitCanvas(ctx.Ecs), "pixel_grid_snap.FitCanvas");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Bevy's example samples every image by its nearest pixel, as these settings do by
        // default, the canvas included.
        var nearest = new TextureSettings();

        // The canvas the pixel-perfect world is drawn into, by a camera drawn before the window's.
        var canvas = Render.CreateTarget(ResWidth, ResHeight);
        Render.SetSampler(canvas, nearest);
        var inGame = Render2d.SpawnCamera2d(order: -1);
        Render.SetCameraTarget(inGame, canvas);
        ecs.Wrap<CameraRef>(inGame).ClearColor = new ClearColorConfig.Custom(Color.FromSrgb(0.5f, 0.5f, 0.5f));
        ecs.Insert<MsaaRef>(inGame).Value = MsaaRef.ValueVariant.Off;
        Render.SetLayers(ecs, inGame, PixelPerfectLayers);

        // The canvas as a sprite in the window's world, and the camera that draws it there.
        var shown = ecs.Spawn();
        ecs.Add(shown, Transform.Identity);
        Render2d.SetSprite(ecs, shown, canvas);
        Render.SetLayers(ecs, shown, HighResLayers);
        _outerCamera = Render2d.SpawnCamera2d();
        ecs.Insert<MsaaRef>(_outerCamera).Value = MsaaRef.ValueVariant.Off;
        Render.SetLayers(ecs, _outerCamera, HighResLayers);

        // A sprite in each world, the dark one on the canvas and the light one at the window's
        // resolution, and a black capsule on the canvas.
        foreach (var (image, y, layers) in new[] { ("pixel/bevy_pixel_dark.png", 20f, PixelPerfectLayers), ("pixel/bevy_pixel_light.png", -20f, HighResLayers) })
        {
            var sprite = ecs.Spawn();
            ecs.Add(sprite, Transform.At(-45f, y, 2f));
            Render2d.SetSprite(ecs, sprite, AssetServer.LoadImage(image, nearest));
            Render.SetLayers(ecs, sprite, layers);
            Rotating.Add(sprite);
        }

        var capsule = ecs.Spawn();
        ecs.Add(capsule, new Transform(new Vec3(25f, 0f, 2f), Quat.Identity, new Vec3(32f)));
        Render2d.SetMesh(ecs, capsule, Render.CreateMesh(MeshShape.Capsule2d, 0.5f, 1f));
        Render2d.SetMaterial(ecs, capsule, Render2d.CreateMaterial(new ColorMaterialSettings { Color = (0f, 0f, 0f, 1f) }));
        Render.SetLayers(ecs, capsule, PixelPerfectLayers);
        Rotating.Add(capsule);
    }

    private static void Rotate(BehaviorContext ctx)
    {
        var turn = Quat.FromRotationZ(ctx.Time.Delta);
        foreach (var entity in Rotating)
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(entity);
            ctx.Ecs.Set(entity, at with { Rotation = turn * at.Rotation });
        }
    }

    // The canvas is scaled by a whole number, the largest that fits the window, so each of its
    // pixels is the same number of the window's. Bevy sets the window camera's projection when the
    // window is resized. Here the camera's own scale stands for the projection's, which sits beside
    // a scaling mode no wrapper types, and is worked out from the window's size every frame, which
    // also covers the first.
    private static void FitCanvas(EcsWorld world)
    {
        var (width, height) = Window.Size();
        var fit = MathF.Round(MathF.Min(width / (float)ResWidth, height / (float)ResHeight));
        if (fit < 1f) fit = 1f;

        var camera = world.GetOrDefault<Transform>(_outerCamera);
        var scale = new Vec3(1f / fit, 1f / fit, 1f);
        if (camera.Scale != scale) world.Set(_outerCamera, camera with { Scale = scale });
    }
}
