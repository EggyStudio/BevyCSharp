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

    private enum Viewport
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

    private static readonly List<(Entity Camera, Viewport Place)> Cameras = [];
    private static readonly List<Entity> Moving = [];

    public static void Build(App app)
    {
        Cameras.Clear();
        Moving.Clear();

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var at = Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY);

            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));
            ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);

            // Each camera with the full view it draws a part of, where it starts in it, and how much
            // of it, or none for the whole view.
            var views = new (Viewport Place, bool Orthographic, (uint, uint)? Full, (float, float) Offset, (uint, uint) Size)[]
            {
                (Viewport.PerspectiveMain, false, null, (0f, 0f), (0, 0)),
                (Viewport.PerspectiveStretched, false, (10, 10), (5f, 0f), (5, 10)),
                (Viewport.PerspectiveMoving, false, (500, 500), (0f, 0f), (100, 100)),
                (Viewport.PerspectiveControl, false, (800, 800), (0f, 0f), (800, 400)),
                (Viewport.OrthographicMain, true, null, (0f, 0f), (0, 0)),
                (Viewport.OrthographicStretched, true, (2, 2), (0f, 0f), (1, 2)),
                (Viewport.OrthographicMoving, true, (500, 500), (0f, 0f), (100, 100)),
                (Viewport.OrthographicControl, true, (200, 200), (0f, 0f), (200, 100)),
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

                Cameras.Add((camera, place));
                if (place is Viewport.PerspectiveMoving or Viewport.OrthographicMoving) Moving.Add(camera);
            }
        }, "camera_sub_view.Setup");

        app.Update(ctx =>
        {
            // Sweeps the view diagonally across, from just before its top left to past its middle.
            var x = ctx.Time.Elapsed * 150f % 450f - 50f;
            foreach (var camera in Moving)
            {
                var wrapped = ctx.Ecs.Wrap<CameraRef>(camera);
                if (wrapped.SubCameraView is { } view) wrapped.SubCameraView = view with { Offset = new Vec2(x, x) };
            }
        }, "camera_sub_view.MoveCameraView");

        app.Update(_ => ResizeViewports(), "camera_sub_view.ResizeViewports");
    }

    // A large view each across the bottom four fifths and a row of small ones above them, laid out
    // from the window's size in physical pixels as Bevy's are.
    private static void ResizeViewports()
    {
        var (width, height) = Window.Size();
        var scale = Window.Scale();
        var (physicalWidth, physicalHeight) = ((uint)(width * scale), (uint)(height * scale));

        var smallHeight = physicalHeight / 5;
        var smallWidth = physicalWidth / 8;
        var (largeWidth, largeHeight) = (smallWidth * 4, smallHeight * 4);
        var smallDim = Math.Min(smallHeight, smallWidth);

        foreach (var (camera, place) in Cameras)
        {
            var (x, y, w, h) = place switch
            {
                Viewport.PerspectiveMain => (0u, smallHeight, largeWidth, largeHeight),
                Viewport.PerspectiveStretched => (0u, 0u, smallDim, smallDim),
                Viewport.PerspectiveMoving => (smallWidth, 0u, smallDim, smallDim),
                Viewport.PerspectiveControl => (smallWidth * 2, 0u, smallDim * 2, smallDim),
                Viewport.OrthographicMain => (largeWidth, smallHeight, largeWidth, largeHeight),
                Viewport.OrthographicStretched => (smallWidth * 4, 0u, smallDim, smallDim),
                Viewport.OrthographicMoving => (smallWidth * 5, 0u, smallDim, smallDim),
                _ => (smallWidth * 6, 0u, smallDim * 2, smallDim),
            };
            Render.SetViewport(camera, x, y, w, h);
        }
    }
}
