using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Showcases wireframe rendering.
//
// Bevy's global wireframe is drawn here by giving every mesh not marked otherwise a wireframe of
// its own, so Z and X act on all of them at once. A wireframe's width and its quad topology are
// not bridged, so V and B do nothing and every line is a pixel wide over triangles.
internal static class Wireframe
{
    private static readonly (float R, float G, float B, float A) White = (1f, 1f, 1f, 1f);
    private static readonly (float R, float G, float B, float A) DeepPink = Scene.Srgb8(255, 20, 147);
    private static readonly (float R, float G, float B, float A) Lime = (0f, 1f, 0f, 1f);
    private static readonly (float R, float G, float B, float A) Red = (1f, 0f, 0f, 1f);

    private static bool _global = true;
    private static (float R, float G, float B, float A) _globalColor = White;
    private static (float R, float G, float B, float A) _toggleColor = Lime;

    private static Entity _orange, _plane, _toggle, _purple, _text;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);

            // The red cube has no wireframe, even under the global one.
            ecs.Mesh(cube, Scene.Material(Red), Transform.At(-1.5f, 0.5f, -1.5f));
            _orange = ecs.Mesh(cube, Scene.Material(Scene.Srgb8(255, 165, 0)), Transform.At(-0.5f, 0.5f, -0.5f));
            _toggle = ecs.Mesh(cube, Scene.Material(Lime), Transform.At(0.5f, 0.5f, 0.5f));
            _purple = ecs.Mesh(cube, Scene.Material(Scene.Srgb8(128, 0, 128)), Transform.At(1.5f, 0.5f, 1.5f));
            _plane = ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Scene.Material((0f, 0f, 1f, 1f)), Transform.Identity);

            ecs.PointLight(new Vec3(2f, 4f, 2f));
            ecs.Camera(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

            Apply();
        });

        app.Update(ctx =>
        {
            var changed = false;
            if (ctx.Input.KeyPressed(Key.Z)) { _global = !_global; changed = true; }
            if (ctx.Input.KeyPressed(Key.X)) { _globalColor = _globalColor == White ? DeepPink : White; changed = true; }
            if (ctx.Input.KeyPressed(Key.C)) { _toggleColor = _toggleColor == Lime ? Red : Lime; changed = true; }
            if (!changed) return;

            Apply();
            Ui.SetText(_text, Text());
        }, "wireframe.UpdateColors");
    }

    // The green and the purple cube always have one, in colors of their own; the orange cube has
    // the global one, and the plane the global one in black.
    private static void Apply()
    {
        Render.SetWireframe(_toggle, true, _toggleColor);
        Render.SetWireframe(_purple, true, (1f, 1f, 0f, 1f));
        Render.SetWireframe(_orange, _global, _globalColor);
        Render.SetWireframe(_plane, _global, (0f, 0f, 0f, 1f));
    }

    private static string Text() =>
        "Controls\n---------------\nZ - Toggle global\nX - Change global color\n"
        + "C - Change color of the green cube wireframe\n"
        + $"WireframeConfig\n-------------\nGlobal: {_global}\nColor: {(_globalColor == White ? "white" : "deep pink")}";
}
