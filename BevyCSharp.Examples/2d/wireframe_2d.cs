// Bevy's wireframe_2d example, examples/2d/wireframe_2d.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Showcases wireframe rendering for 2D meshes, Bevy's global wireframe and its color set through
// its Wireframe2dConfig resource, and a mesh's own through the components on it.
internal static class Wireframe2dExample
{
    private static readonly Color White = new(1f, 1f, 1f, 1f);
    private static readonly Color Red = new(1f, 0f, 0f, 1f);
    private static readonly Color Green = Color.FromSrgb(0f, 128f / 255f, 0f);

    private static bool _circleGreen;
    private static Entity _circle, _text;

    // Bevy's 2D wireframe plugin, which Bevy's example adds.
    public static void Configure(Config config) => config.Wireframes = true;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _circleGreen = true;
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
            ecs.Insert<NoWireframe2dRef>(Spawn(Render.CreateMesh(MeshShape.Triangle, 100f), -150f));
            Spawn(Render.CreateMesh(MeshShape.Rectangle, 100f, 100f), 0f);
            _circle = Spawn(Render.CreateMesh(MeshShape.Circle, 50f), 150f);
            ecs.Insert<Wireframe2dRef>(_circle);
            ecs.Insert<Wireframe2dColorRef>(_circle).Color = Green;

            // Every mesh not marked otherwise, in white.
            var config = ecs.Resource<Wireframe2dConfigRef>() ?? ecs.InsertResource<Wireframe2dConfigRef>();
            (config.Global, config.DefaultColor) = (true, White);

            Render2d.SpawnCamera2d();
            _text = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "wireframe_2d.Setup");

        app.Update(ctx =>
        {
            var input = ctx.Input;
            if (ctx.Ecs.Resource<Wireframe2dConfigRef>() is not { } config) return;

            var (global, color) = (config.Global, config.DefaultColor);
            var srgb = color.ToSrgb();
            Ui.SetText(_text,
                "Controls\n---------------\nZ - Toggle global\nX - Change global color\nC - Change color of the circle wireframe\n"
                + $"Wireframe2dConfig\n-------------\nGlobal: {global.ToString().ToLowerInvariant()}\n"
                + FormattableString.Invariant($"Color: Srgba {{ red: {srgb.X:0.0###}, green: {srgb.Y:0.0###}, blue: {srgb.Z:0.0###}, alpha: {srgb.W:0.0###} }}"));

            if (input.KeyPressed(Key.Z)) config.Global = !global;
            if (input.KeyPressed(Key.X)) config.DefaultColor = color == White ? Red : White;
            if (input.KeyPressed(Key.C))
            {
                _circleGreen = !_circleGreen;
                ctx.Ecs.Wrap<Wireframe2dColorRef>(_circle).Color = _circleGreen ? Green : Red;
            }
        }, "wireframe_2d.UpdateColors");
    }
}
