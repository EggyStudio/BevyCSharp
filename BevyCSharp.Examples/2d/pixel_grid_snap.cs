// Bevy's pixel_grid_snap example, examples/2d/pixel_grid_snap.rs at v0.20.0, by Bevy's contributors
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
    internal const uint ResWidth = 160, ResHeight = 90;

    // What the low-resolution camera draws, and what the window's camera draws, the canvas among it.
    private const uint PixelPerfectLayers = 1u << 0;
    private const uint HighResLayers = 1u << 1;

    public static void Build(App app) => app.Startup(Setup, "pixel_grid_snap.Setup");

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
        ecs.Add(inGame, new InGameCamera());

        // The canvas as a sprite in the window's world, and the camera that draws it there.
        var shown = ecs.Spawn();
        ecs.Add(shown, Transform.Identity);
        Render2d.SetSprite(ecs, shown, canvas);
        Render.SetLayers(ecs, shown, HighResLayers);
        ecs.Add(shown, new Canvas());
        var outerCamera = Render2d.SpawnCamera2d();
        ecs.Insert<MsaaRef>(outerCamera).Value = MsaaRef.ValueVariant.Off;
        Render.SetLayers(ecs, outerCamera, HighResLayers);
        ecs.Add(outerCamera, new OuterCamera());

        // A sprite in each world, the dark one on the canvas and the light one at the window's
        // resolution, and a black capsule on the canvas.
        foreach (var (image, y, layers) in new[] { ("pixel/bevy_pixel_dark.png", 20f, PixelPerfectLayers), ("pixel/bevy_pixel_light.png", -20f, HighResLayers) })
        {
            var sprite = ecs.Spawn();
            ecs.Add(sprite, Transform.At(-45f, y, 2f));
            Render2d.SetSprite(ecs, sprite, AssetServer.LoadImage(image, nearest));
            Render.SetLayers(ecs, sprite, layers);
            ecs.Add(sprite, new Rotate());
        }

        var capsule = ecs.Spawn();
        ecs.Add(capsule, new Transform(new Vec3(25f, 0f, 2f), Quat.Identity, new Vec3(32f)));
        Render2d.SetMesh(ecs, capsule, Render.CreateMesh(MeshShape.Capsule2d, 0.5f, 1f));
        Render2d.SetMaterial(ecs, capsule, Render2d.CreateMaterial(new ColorMaterialSettings { Color = (0f, 0f, 0f, 1f) }));
        Render.SetLayers(ecs, capsule, PixelPerfectLayers);
        ecs.Add(capsule, new Rotate());
    }
}

/// <summary>The sprite the canvas is drawn on in the window's world.</summary>
[Behavior]
public partial struct Canvas;

/// <summary>The camera that draws the pixel-perfect world into the canvas.</summary>
[Behavior]
public partial struct InGameCamera;

/// <summary>The window's camera, which draws the canvas scaled up.</summary>
[Behavior]
public partial struct OuterCamera
{
    /// <summary>
    /// The canvas scaled by a whole number, the largest that fits the window, so each of its pixels
    /// is the same number of the window's.
    /// </summary>
    /// <remarks>
    /// Bevy sets the camera's projection when the window is resized. Here the camera's own scale
    /// stands for the projection's, which sits beside a scaling mode no wrapper types, and is
    /// worked out from the window's size every frame, which also covers the first.
    /// </remarks>
    [OnUpdate]
    public void FitCanvas(BehaviorContext ctx, ref Transform transform)
    {
        var (width, height) = Window.Size();
        var fit = MathF.Max(MathF.Round(MathF.Min(width / (float)PixelGridSnap.ResWidth, height / (float)PixelGridSnap.ResHeight)), 1f);
        var scale = new Vec3(1f / fit, 1f / fit, 1f);
        if (transform.Scale != scale) transform.Scale = scale;
    }
}

/// <summary>A thing that turns, a radian a second, to show which world snaps to its pixels.</summary>
[Behavior]
public partial struct Rotate
{
    /// <summary>Turned about Z by the frame's time, as Bevy's <c>rotate</c> turns it.</summary>
    [OnUpdate]
    public void Turn(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationZ(ctx.Time.Delta) * transform.Rotation;
}
