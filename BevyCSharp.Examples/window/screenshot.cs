// Bevy's screenshot example, examples/window/screenshot.rs at v0.20.0, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Windowing;

// Saves a screenshot of the window to a numbered PNG beside the program each time Space is pressed,
// the pointer showing the platform's sign of work going on while one is being saved.
internal static class Screenshot
{
    private static readonly List<(Capture Capture, string Path)> Saving = [];
    private static int _counter;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Saving.Clear();
            _counter = 0;
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.5f, 0.3f)), Transform.Identity);
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.7f, 0.6f)), Transform.At(0f, 0.5f, 0f));
            ecs.SpawnPointLight(new Vec3(4f, 8f, 4f), shadows: true);
            ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
            Ui.SpawnText("Press <spacebar> to save a screenshot to disk", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "screenshot.Setup");

        app.Update(ctx =>
        {
            if (!ctx.Input.KeyPressed(Key.Space)) return;
            Saving.Add((Render.BeginCapture(), Path.Combine(AppContext.BaseDirectory, $"screenshot-{_counter}.png")));
            _counter++;
        }, "screenshot.ScreenshotOnSpacebar");

        // Each capture is written out as it arrives, and the pointer shows progress until none is
        // left to write.
        app.Update(_ =>
        {
            for (var i = Saving.Count - 1; i >= 0; i--)
            {
                if (!Render.TryReadCapture(Saving[i].Capture, out var picture) || picture is null) continue;
                File.WriteAllBytes(Saving[i].Path, picture.ToPng());
                Saving.RemoveAt(i);
            }

            if (Window.Entity() != Entity.None) Window.SetCursorShape(Saving.Count > 0 ? CursorShape.Progress : CursorShape.Default);
        }, "screenshot.ScreenshotSaving");
    }
}
