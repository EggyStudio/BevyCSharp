// Bevy's light_gizmos example, examples/gizmos/light_gizmos.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Gizmo;

// Draws the shape of each of four lights, a point, a spot, a directional and a rectangle light,
// as the camera circles them. D draws them over the scene, the left and right arrows change their
// width, A hides them, and C cycles how they are colored.
internal static class LightGizmos
{
    private static Entity _camera, _colorText;
    private static LightGizmoColoring _coloring;
    private static float _width;
    private static bool _enabled, _onTop;

    public static void Build(App app)
    {
        app.Startup(Setup, "light_gizmos.Setup");
        app.Update(RotateCamera, "light_gizmos.RotateCamera");
        app.Update(UpdateConfig, "light_gizmos.UpdateConfig");
    }

    private static (float R, float G, float B) Rgb((float R, float G, float B, float A) color) => (color.R, color.G, color.B);

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_coloring, _width, _enabled, _onTop) = (LightGizmoColoring.MatchLight, 2f, true, false);

        ecs.Mesh(Render.CreateMesh(MeshShape.Circle, 4f), Scene.Material((1f, 1f, 1f, 1f)), new Transform(Vec3.Zero, Quat.FromRotationX(-MathF.PI / 2f), Vec3.One));
        var cube = Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f);
        var blue = Scene.Material(Scene.Srgb8(124, 144, 255));
        foreach (var x in new[] { -2f, 0f, 2f }) ecs.Mesh(cube, blue, Transform.At(x, 0.5f, 0f));

        var point = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 1_000_000f, Range = 2f, Color = Rgb(Scene.Srgb8(0, 139, 139)), Shadows = true });
        ecs.Add(point, Transform.At(0f, 1.5f, 0f));

        var spot = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Spot,
            Intensity = 1_000_000f,
            Range = 3.5f,
            Color = Rgb(Scene.Srgb8(128, 0, 128)),
            OuterAngle = MathF.PI / 4f,
            InnerAngle = MathF.PI / 4f * 0.8f,
            Shadows = true,
        });
        ecs.Add(spot, Transform.LookingAt(new Vec3(4f, 2f, 0f), new Vec3(1.5f, 0f, 0f), Vec3.UnitY));

        // A twentieth of Bevy's default daylight.
        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 10_000f * 0.05f, Color = Rgb(Scene.Srgb8(255, 215, 0)), Shadows = true });
        ecs.Add(sun, Transform.LookingAt(new Vec3(-4f, 2f, 0f), new Vec3(-1.5f, 0f, 0f), Vec3.UnitY));

        var rect = ecs.Spawn();
        ecs.Add(rect, Transform.LookingAt(new Vec3(0f, 3f, -3f), Vec3.Zero, Vec3.UnitY));
        var orange = Scene.Srgb8(255, 165, 0);
        var light = ecs.Insert<RectLightRef>(rect);
        (light.Color, light.Intensity, light.Width, light.Height, light.Range) = (new Color(orange.R, orange.G, orange.B, 1f), 200_000f, 1.5f, 0.8f, 20f);

        _camera = ecs.Camera(Transform.LookingAt(new Vec3(-2.5f, 4.5f, 9f), Vec3.Zero, Vec3.UnitY));

        Ui.SpawnText(
            "Press 'D' to toggle drawing gizmos on top of everything else in the scene\n"
            + "Hold 'Left' or 'Right' to change the line width of the gizmos\n"
            + "Press 'A' to toggle drawing of the light gizmos\n"
            + "Press 'C' to cycle between the light gizmos coloring modes",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

        Gizmos.ShowLights(all: true, _coloring);
        var mode = Ui.SpawnText("Gizmo color mode: ", new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        _colorText = Ui.SpawnTextSpan(mode, ColorText(), new UiTextSettings(), (1f, 1f, 1f, 1f));
    }

    // What the coloring is, with the colors written as Bevy writes them, in sRGB hex.
    private static string ColorText() => _coloring switch
    {
        LightGizmoColoring.Manual => "Manual #808080",
        LightGizmoColoring.Varied => "Random from entity",
        LightGizmoColoring.MatchLight => "Match light color",
        _ => "Point #FF0000, Spot #008000, Directional #0000FF, Rect #800000",
    };

    private static void RotateCamera(BehaviorContext ctx)
    {
        var at = ctx.Ecs.GetOrDefault<Transform>(_camera);
        var turn = Quat.FromRotationY(ctx.Time.Delta / 2f);
        ctx.Ecs.Set(_camera, at with { Translation = turn * at.Translation, Rotation = turn * at.Rotation });
    }

    private static void UpdateConfig(BehaviorContext ctx)
    {
        var input = ctx.Input;

        if (input.KeyPressed(Key.D))
        {
            _onTop = !_onTop;
            Gizmos.SetDepthBias(_onTop ? -1f : 0f, GizmoGroup.All);
        }

        var widened = (input.KeyDown(Key.ArrowRight) ? 1f : 0f) - (input.KeyDown(Key.ArrowLeft) ? 1f : 0f);
        if (widened != 0f) _width = Math.Clamp(_width + widened * 5f * ctx.Time.Delta, 0f, 50f);
        if (input.KeyPressed(Key.A)) _enabled = !_enabled;
        if (widened != 0f || input.KeyPressed(Key.A)) Gizmos.Configure(width: MathF.Max(_width, 0.01f), enabled: _enabled, which: GizmoGroup.Lights);

        if (input.KeyPressed(Key.C))
        {
            _coloring = _coloring switch
            {
                LightGizmoColoring.Manual => LightGizmoColoring.Varied,
                LightGizmoColoring.Varied => LightGizmoColoring.MatchLight,
                LightGizmoColoring.MatchLight => LightGizmoColoring.ByKind,
                _ => LightGizmoColoring.Manual,
            };
            var gray = Scene.Srgb8(128, 128, 128);
            Gizmos.ShowLights(all: true, _coloring, gray);
            ctx.Ecs.Wrap<TextSpanRef>(_colorText).Value = ColorText();
        }
    }
}
