using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Showcases wireframe rendering.
//
// Bevy's global wireframe and its default topology are a resource, and here they are what is given
// to every mesh not marked otherwise, so Z, X and B act on all of them at once. A wireframe's own
// width and topology are Bevy's WireframeLineWidth and WireframeTopology, put on through their
// wrappers.
internal static class Wireframe
{

    private static readonly (float R, float G, float B, float A) White = (1f, 1f, 1f, 1f);
    private static readonly (float R, float G, float B, float A) DeepPink = Scene.Srgb8(255, 20, 147);
    private static readonly (float R, float G, float B, float A) Lime = (0f, 1f, 0f, 1f);
    private static readonly (float R, float G, float B, float A) Red = (1f, 0f, 0f, 1f);

    private static bool _global = true;
    private static bool _quads;
    private static float _width = 3f;
    private static (float R, float G, float B, float A) _globalColor = White;
    private static (float R, float G, float B, float A) _toggleColor = Lime;

    private static Entity _orange, _plane, _toggle, _purple, _text;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
            (_global, _quads, _width, _globalColor, _toggleColor) = (true, false, 3f, White, Lime);

            // The red cube has no wireframe, even under the global one.
            ecs.Mesh(cube, Scene.Material(Red), Transform.At(-1.5f, 0.5f, -1.5f));
            _orange = ecs.Mesh(cube, Scene.Material(Scene.Srgb8(255, 165, 0)), Transform.At(-0.5f, 0.5f, -0.5f));
            _toggle = ecs.Mesh(cube, Scene.Material(Lime), Transform.At(0.5f, 0.5f, 0.5f));
            _purple = ecs.Mesh(cube, Scene.Material(Scene.Srgb8(128, 0, 128)), Transform.At(1.5f, 0.5f, 1.5f));
            _plane = ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Scene.Material((0f, 0f, 1f, 1f)), Transform.Identity);

            // The purple cube's wireframe is its own, wider and drawn over quads.
            Render.SetWireframe(_purple, true, (1f, 1f, 0f, 1f));
            ecs.Insert<WireframeLineWidthRef>(_purple).Width = 3f;
            ecs.Insert<WireframeTopologyRef>(_purple).Value = WireframeTopologyRef.ValueVariant.Quads;

            ecs.PointLight(new Vec3(2f, 4f, 2f));
            ecs.Camera(Transform.LookingAt(new Vec3(-2f, 2.5f, 5f), Vec3.Zero, Vec3.UnitY));
            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

            Apply(ecs);
        });

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var changed = false;
            if (input.KeyPressed(Key.Z)) { _global = !_global; changed = true; }
            if (input.KeyPressed(Key.X)) { _globalColor = _globalColor == White ? DeepPink : White; changed = true; }
            if (input.KeyPressed(Key.C)) { _toggleColor = _toggleColor == Lime ? Red : Lime; changed = true; }
            if (input.KeyPressed(Key.B)) { _quads = !_quads; changed = true; }
            if (input.KeyPressed(Key.V))
            {
                _width = _width switch { <= 2f => 3f, <= 4f => 5f, <= 7f => 10f, _ => 2f };
                ctx.Ecs.Wrap<WireframeLineWidthRef>(_purple).Width = _width;
                changed = true;
            }

            if (!changed) return;

            Apply(ctx.Ecs);
            Ui.SetText(_text, Text());
        }, "wireframe.UpdateColors");
    }

    // The green cube always has one in a color of its own. The orange cube has the global one, and
    // the plane the global one in black, each drawn as the global topology says.
    private static void Apply(EcsWorld ecs)
    {
        Render.SetWireframe(_toggle, true, _toggleColor);
        Render.SetWireframe(_orange, _global, _globalColor);
        Render.SetWireframe(_plane, _global, (0f, 0f, 0f, 1f));

        foreach (var entity in new[] { _toggle, _orange, _plane })
        {
            ecs.Insert<WireframeTopologyRef>(entity).Value = _quads ? WireframeTopologyRef.ValueVariant.Quads : WireframeTopologyRef.ValueVariant.Triangles;
        }
    }

    private static string Text() =>
        "Controls\n---------------\nZ - Toggle global\nX - Change global color\n"
        + "C - Change color of the green cube wireframe\n"
        + FormattableString.Invariant($"V - Line width (current: {_width:0.0}px)\n")
        + $"B - Toggle topology (current: {(_quads ? "Quads" : "Triangles")})\n"
        + $"WireframeConfig\n-------------\nGlobal: {_global}\nColor: {(_globalColor == White ? "white" : "deep pink")}";
}
