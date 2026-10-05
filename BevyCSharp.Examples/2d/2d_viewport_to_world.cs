// Bevy's 2d_viewport_to_world example, examples/2d/2d_viewport_to_world.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Demonstrates a 2D camera's viewport to world and world to viewport, drawing a circle where the
// cursor points and a smaller one where that point lands after going to the viewport and back,
// inside a viewport that moves and resizes. An offscreen run has no cursor and draws neither.
internal static class Example2dViewportToWorld
{
    private static Entity _camera;

    public static void Build(App app)
    {
        app.Startup(Setup, "2d_viewport_to_world.Setup");
        app.Update(Controls, "2d_viewport_to_world.Controls");
        app.Update(DrawCursor, "2d_viewport_to_world.DrawCursor");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var (width, height) = PhysicalSize();

        // A viewport three quarters of the window's size, in its middle.
        _camera = Render2d.SpawnCamera2d();
        ecs.Wrap<CameraRef>(_camera).Viewport = new Viewport((uint)(width * 0.125f), (uint)(height * 0.125f), (uint)(width * 0.75f), (uint)(height * 0.75f));

        Ui.SpawnText(
            "Move the mouse to see the circle follow your cursor.\nUse the arrow keys to move the camera.\n"
            + "Use the comma and period keys to zoom in and out.\nUse the WASD keys to move the viewport.\nUse the IJKL keys to resize the viewport.",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

        // A green rectangle to see the camera move by, and a near-black floor far larger than the
        // window behind it, to see where the viewport ends.
        foreach (var (w, h, color, z) in new[] { (40f, 20f, Scene.Srgb8(0, 128, 0), 0f), (50_000f, 50_000f, (0.01f, 0.01f, 0.01f, 1f), -200f) })
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, Transform.At(0f, 0f, z));
            Render2d.SetMesh(ecs, entity, Render.CreateMesh(MeshShape.Rectangle, w, h));
            Render2d.SetMaterial(ecs, entity, Render2d.CreateMaterial(new ColorMaterialSettings { Color = color }));
        }
    }

    private static (uint Width, uint Height) PhysicalSize()
    {
        var (width, height) = Window.Size();
        var scale = Window.Scale();
        return ((uint)(width * scale), (uint)(height * scale));
    }

    private static void Controls(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        var fspeed = 600f * ctx.Time.Delta;
        var uspeed = (uint)fspeed;
        var (windowWidth, windowHeight) = PhysicalSize();

        var transform = ecs.GetOrDefault<Transform>(_camera);
        var move = new Vec3(
            (input.KeyDown(Key.ArrowRight) ? fspeed : 0f) - (input.KeyDown(Key.ArrowLeft) ? fspeed : 0f),
            (input.KeyDown(Key.ArrowUp) ? fspeed : 0f) - (input.KeyDown(Key.ArrowDown) ? fspeed : 0f),
            0f);

        // Bevy zooms by the orthographic projection's scale. That sits in a variant beside its
        // scaling mode, which no wrapper types, and scaling the camera's transform zooms it the
        // same, the viewport's conversions included.
        var zoom = 1f;
        if (input.KeyDown(Key.Comma)) zoom *= MathF.Pow(4f, ctx.Time.Delta);
        if (input.KeyDown(Key.Period)) zoom *= MathF.Pow(0.25f, ctx.Time.Delta);
        if (move != Vec3.Zero || zoom != 1f)
            ecs.Set(_camera, transform with { Translation = transform.Translation + move, Scale = transform.Scale * zoom });

        var camera = ecs.Wrap<CameraRef>(_camera);
        if (camera.Viewport is not { } viewport) return;
        var (x, y, w, h) = (viewport.PhysicalPositionX, viewport.PhysicalPositionY, viewport.PhysicalSizeX, viewport.PhysicalSizeY);

        // Made again at three quarters of the window when the window has shrunk under it.
        if (w > windowWidth || h > windowHeight) (w, h) = ((uint)(windowWidth * 0.75f), (uint)(windowHeight * 0.75f));

        static uint Less(uint value, uint by) => value > by ? value - by : 0u;
        if (input.KeyDown(Key.W)) y = Less(y, uspeed);
        if (input.KeyDown(Key.S)) y += uspeed;
        if (input.KeyDown(Key.A)) x = Less(x, uspeed);
        if (input.KeyDown(Key.D)) x += uspeed;
        (x, y) = (Math.Min(x, windowWidth - w), Math.Min(y, windowHeight - h));

        if (input.KeyDown(Key.I)) h = Less(h, uspeed);
        if (input.KeyDown(Key.K)) h += uspeed;
        if (input.KeyDown(Key.J)) w = Less(w, uspeed);
        if (input.KeyDown(Key.L)) w += uspeed;
        (w, h) = (Math.Max(Math.Min(w, windowWidth - x), 20u), Math.Max(Math.Min(h, windowHeight - y), 20u));

        var resized = new Viewport(x, y, w, h);
        if (resized != viewport) camera.Viewport = resized;
    }

    private static void DrawCursor(BehaviorContext ctx)
    {
        var (cursorX, cursorY) = ctx.Input.MousePosition;
        if (!Render.TryRay(_camera, cursorX, cursorY, out var world, out _)) return;
        if (!Render.TryProject(_camera, world with { Z = 0f }, out var viewportX, out var viewportY)) return;
        if (!Render.TryRay(_camera, viewportX, viewportY, out var check, out _)) return;

        // The two should stand in the same place.
        Gizmos.Circle2d((world.X, world.Y), 10f, (1f, 1f, 1f, 1f));
        Gizmos.Circle2d((check.X, check.Y), 8f, (1f, 0f, 0f, 1f));
    }
}
