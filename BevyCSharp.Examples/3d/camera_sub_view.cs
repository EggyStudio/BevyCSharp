// Bevy's camera_sub_view example, examples/3d/camera_sub_view.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates sub views, a camera drawing one part of a larger view, as one screen of several
// that show a single picture between them would. The same scene is drawn eight times, perspective
// on the left and orthographic on the right, whole, stretched, moving and in a wide window.
internal static class CameraSubView
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        var at = Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY);

        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
        ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));
        ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);

        // Each camera with the full view it draws a part of, where it starts in it, and how much
        // of it, or none for the whole view.
        var views = new (ExampleViewportsKind Place, bool Orthographic, (uint, uint)? Full, (float, float) Offset, (uint, uint) Size)[]
        {
            (ExampleViewportsKind.PerspectiveMain, false, null, (0f, 0f), (0, 0)),
            (ExampleViewportsKind.PerspectiveStretched, false, (10, 10), (5f, 0f), (5, 10)),
            (ExampleViewportsKind.PerspectiveMoving, false, (500, 500), (0f, 0f), (100, 100)),
            (ExampleViewportsKind.PerspectiveControl, false, (800, 800), (0f, 0f), (800, 400)),
            (ExampleViewportsKind.OrthographicMain, true, null, (0f, 0f), (0, 0)),
            (ExampleViewportsKind.OrthographicStretched, true, (2, 2), (0f, 0f), (1, 2)),
            (ExampleViewportsKind.OrthographicMoving, true, (500, 500), (0f, 0f), (100, 100)),
            (ExampleViewportsKind.OrthographicControl, true, (200, 200), (0f, 0f), (200, 100)),
        };

        for (var order = 0; order < views.Length; order++)
        {
            var (place, orthographic, full, offset, size) = views[order];
            var camera = ecs.SpawnCamera3d(at, new CameraSettings
            {
                Order = order,
                Projection = orthographic ? CameraProjection.Orthographic : CameraProjection.Perspective,
                Height = 6f,
            });

            if (full is var (fullWidth, fullHeight))
            {
                ecs.Wrap<CameraRef>(camera).SubCameraView = new SubCameraView(
                    fullWidth, fullHeight, new Vec2(offset.Item1, offset.Item2), size.Item1, size.Item2);
            }

            ecs.Add(camera, new ExampleViewports { Kind = place });
            if (place is ExampleViewportsKind.PerspectiveMoving or ExampleViewportsKind.OrthographicMoving) ecs.Add(camera, new MovingCameraMarker());
        }
    }, "camera_sub_view.Setup");
}

/// <summary>Where a camera's view sits in the window, as Bevy's <c>ExampleViewports</c> enum names them.</summary>
public enum ExampleViewportsKind
{
    PerspectiveMain,
    PerspectiveStretched,
    PerspectiveMoving,
    PerspectiveControl,
    OrthographicMain,
    OrthographicStretched,
    OrthographicMoving,
    OrthographicControl,
}

/// <summary>A camera, and where its view sits in the window.</summary>
[Behavior]
public partial struct ExampleViewports
{
    /// <summary>Where it sits.</summary>
    public ExampleViewportsKind Kind;

    /// <summary>
    /// Its viewport laid out from the window's size in physical pixels, a large one across the
    /// bottom four fifths for each projection and a row of small ones above them, so the views stay
    /// the same at any window size.
    /// </summary>
    [OnUpdate]
    public void ResizeViewports(BehaviorContext ctx)
    {
        var (width, height) = Window.Size();
        var scale = Window.Scale();
        var (physicalWidth, physicalHeight) = ((uint)(width * scale), (uint)(height * scale));

        var smallHeight = physicalHeight / 5;
        var smallWidth = physicalWidth / 8;
        var (largeWidth, largeHeight) = (smallWidth * 4, smallHeight * 4);
        var smallDim = Math.Min(smallHeight, smallWidth);

        var (x, y, w, h) = Kind switch
        {
            ExampleViewportsKind.PerspectiveMain => (0u, smallHeight, largeWidth, largeHeight),
            ExampleViewportsKind.PerspectiveStretched => (0u, 0u, smallDim, smallDim),
            ExampleViewportsKind.PerspectiveMoving => (smallWidth, 0u, smallDim, smallDim),
            ExampleViewportsKind.PerspectiveControl => (smallWidth * 2, 0u, smallDim * 2, smallDim),
            ExampleViewportsKind.OrthographicMain => (largeWidth, smallHeight, largeWidth, largeHeight),
            ExampleViewportsKind.OrthographicStretched => (smallWidth * 4, 0u, smallDim, smallDim),
            ExampleViewportsKind.OrthographicMoving => (smallWidth * 5, 0u, smallDim, smallDim),
            _ => (smallWidth * 6, 0u, smallDim * 2, smallDim),
        };
        Render.SetViewport(ctx.Entity, x, y, w, h);
    }
}

/// <summary>A camera whose part of its full view sweeps across it.</summary>
[Behavior]
public partial struct MovingCameraMarker
{
    /// <summary>The part it draws swept diagonally across, from a little before the top left to past the middle.</summary>
    [OnUpdate]
    public void MoveCameraView(BehaviorContext ctx)
    {
        var x = ctx.Time.Elapsed * 150f % 450f - 50f;
        var camera = ctx.Ecs.Wrap<CameraRef>(ctx.Entity);
        if (camera.SubCameraView is { } view) camera.SubCameraView = view with { Offset = new Vec2(x, x) };
    }
}
