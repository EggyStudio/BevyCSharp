using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Showcases wireframe rendering for 2D meshes.
//
// Bevy's global wireframe and its color are a resource. Here they are given to the mesh that
// follows them, so Z and X act on it as they would on every unmarked mesh.
internal static class Wireframe2dExample
{
    private static readonly Color White = new(1f, 1f, 1f, 1f);
    private static readonly Color Red = new(1f, 0f, 0f, 1f);
    private static readonly Color Green = ToColor(Scene.Srgb8(0, 128, 0));

    private static bool _global, _circleGreen;
    private static Color _globalColor;
    private static Entity _rectangle, _circle, _text;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_global, _globalColor, _circleGreen) = (true, White, true);
            var black = Render2d.CreateMaterial(new ColorMaterialSettings { Color = (0f, 0f, 0f, 1f) });

            Entity Spawn(AssetHandle mesh, float x)
            {
                var entity = ecs.Spawn();
                ecs.Add(entity, Transform.At(x, 0f, 0f));
                Render2d.SetMesh(ecs, entity, mesh);
                Render2d.SetMaterial(ecs, entity, black);
                return entity;
            }

            // The triangle never has a wireframe, the rectangle has the global one, and the circle
            // always has one in a color of its own.
            Spawn(Render.CreateMesh(MeshShape.Triangle, 100f), -150f);
            _rectangle = Spawn(Render.CreateMesh(MeshShape.Rectangle, 100f, 100f), 0f);
            _circle = Spawn(Render.CreateMesh(MeshShape.Circle, 50f), 150f);
            ecs.Insert<Wireframe2dRef>(_circle);
            ecs.Insert<Wireframe2dColorRef>(_circle).Color = Green;

            Render2d.SpawnCamera2d();
            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
            Apply(ecs);
        }, "wireframe_2d.Setup");

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var changed = false;
            if (input.KeyPressed(Key.Z)) { _global = !_global; changed = true; }
            if (input.KeyPressed(Key.X)) { _globalColor = _globalColor == White ? Red : White; changed = true; }
            if (input.KeyPressed(Key.C))
            {
                _circleGreen = !_circleGreen;
                ctx.Ecs.Wrap<Wireframe2dColorRef>(_circle).Color = _circleGreen ? Green : Red;
            }

            if (!changed) return;
            Apply(ctx.Ecs);
            Ui.SetText(_text, Text());
        }, "wireframe_2d.UpdateColors");
    }

    // The rectangle stands for every mesh the global wireframe reaches.
    private static void Apply(EcsWorld ecs)
    {
        if (!_global)
        {
            ecs.Get<Wireframe2dRef>(_rectangle)?.Remove();
            return;
        }

        ecs.Insert<Wireframe2dRef>(_rectangle);
        (ecs.Get<Wireframe2dColorRef>(_rectangle) ?? ecs.Insert<Wireframe2dColorRef>(_rectangle)).Color = _globalColor;
    }

    private static Color ToColor((float R, float G, float B, float A) linear) => new(linear.R, linear.G, linear.B, linear.A);

    private static string Text() =>
        "Controls\n---------------\nZ - Toggle global\nX - Change global color\nC - Change color of the circle wireframe\n"
        + $"Wireframe2dConfig\n-------------\nGlobal: {_global.ToString().ToLowerInvariant()}\n"
        + $"Color: {(_globalColor == White ? "Srgba { red: 1.0, green: 1.0, blue: 1.0, alpha: 1.0 }" : "Srgba { red: 1.0, green: 0.0, blue: 0.0, alpha: 1.0 }")}";
}
