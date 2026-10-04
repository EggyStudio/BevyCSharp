using System.Globalization;
using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// A simple 3D scene demonstrating rectangular area lights, Bevy's RectLight put on through
// reflection, each with its gizmo shown.
internal static class RectLight
{
    private const string Light = "bevy_light::rect_light::RectLight";
    private const string Gizmo = "bevy_light::gizmos::ShowLightGizmo";

    private static readonly List<Entity> Lights = [];
    private static MaterialSettings _floor = null!;
    private static AssetHandle _floorMaterial;
    private static Entity _text;
    private static bool _gizmos = true;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Lights.Clear();
            _gizmos = true;

            _floor = new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Metallic = 1f, Roughness = 0.6f };
            _floorMaterial = Render.CreateMaterial(_floor);
            ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 20f, 20f), _floorMaterial, Transform.Identity);
            ecs.Mesh(Render.CreateMesh(MeshShape.Sphere, 1f), Scene.Material((1f, 1f, 1f, 1f)), Transform.At(0f, 1f, 0f));

            Spawn(ecs, Color.FromSrgb(1f, 0.3f, 0.2f), 100_000f, 2f, 1f, Transform.LookingAt(new Vec3(1f, 3f, 1f), Vec3.UnitY, Vec3.UnitY));
            Spawn(ecs, Color.FromSrgb(0.5f, 0.7f, 1f), 800_000f, 1.5f, 4f, new Transform(new Vec3(-2f, 1.5f, -3f), Quat.FromRotationY(MathF.PI), Vec3.One));

            var camera = ecs.Camera(Transform.LookingAt(new Vec3(-8f, 5f, 8f), Vec3.UnitY, Vec3.UnitY));
            ecs.Add(camera, new FreeCamera());

            var grey = Color.FromSrgb(0.9f, 0.9f, 0.9f);
            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f), Color = (grey.R, grey.G, grey.B, 1f) }, 18f);
        });

        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.G))
            {
                _gizmos = !_gizmos;
                foreach (var light in Lights)
                {
                    if (_gizmos) ctx.Ecs.InsertReflected(light, Gizmo);
                    else ctx.Ecs.RemoveReflected(light, Gizmo);
                }
            }

            var delta = ctx.Input.KeyDown(Key.ArrowUp) ? 0.005f : ctx.Input.KeyDown(Key.ArrowDown) ? -0.005f : 0f;
            if (delta == 0f) return;

            _floor.Roughness = Math.Clamp(_floor.Roughness + delta, 0f, 1f);
            Render.WriteMaterial(_floorMaterial, _floor);
            Ui.SetText(_text, Text());
        }, "rect_light.Controls");
    }

    private static void Spawn(EcsWorld ecs, Color color, float intensity, float width, float height, Transform at)
    {
        var light = ecs.Spawn();
        ecs.Add(light, at);
        ecs.InsertReflected(light, Light);
        ecs.SetReflectedColor(light, Light, ".color", color);
        ecs.SetReflected(light, Light, ".intensity", intensity.ToString(CultureInfo.InvariantCulture));
        ecs.SetReflected(light, Light, ".width", width.ToString(CultureInfo.InvariantCulture));
        ecs.SetReflected(light, Light, ".height", height.ToString(CultureInfo.InvariantCulture));
        ecs.SetReflected(light, Light, ".range", "20.0");
        ecs.InsertReflected(light, Gizmo);
        Lights.Add(light);
    }

    private static string Text() =>
        FormattableString.Invariant($"Controls\nArrow Up/Down: Adjust floor roughness\nG: Toggle light gizmos\n\nRoughness: {_floor.Roughness:0.00}");
}
