// Bevy's rect_light example, examples/3d/rect_light.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// A simple 3D scene demonstrating rectangular area lights, Bevy's RectLight put on through
// reflection, each with its gizmo shown.
internal static class RectLight
{

    private static readonly List<Entity> Lights = [];
    private static MaterialSettings _floor = null!;
    private static AssetHandle _floorMaterial;
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
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 20f, 20f), _floorMaterial, Transform.Identity);
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, 1f), Render.CreateMaterial((1f, 1f, 1f, 1f)), Transform.At(0f, 1f, 0f));

            Spawn(ecs, Color.FromSrgb(1f, 0.3f, 0.2f), 100_000f, 2f, 1f, Transform.LookingAt(new Vec3(1f, 3f, 1f), Vec3.UnitY, Vec3.UnitY));
            Spawn(ecs, Color.FromSrgb(0.5f, 0.7f, 1f), 800_000f, 1.5f, 4f, new Transform(new Vec3(-2f, 1.5f, -3f), Quat.FromRotationY(MathF.PI), Vec3.One));

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-8f, 5f, 8f), Vec3.UnitY, Vec3.UnitY));
            ecs.Add(camera, new FreeCamera());

            var gray = Color.FromSrgb(0.9f, 0.9f, 0.9f);
            ecs.Add(Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f), Color = (gray.R, gray.G, gray.B, 1f) }, 18f), new RoughnessDisplay());
        });

        app.Update(ctx =>
        {
            if (ctx.Input.KeyPressed(Key.G))
            {
                _gizmos = !_gizmos;
                foreach (var light in Lights)
                {
                    if (_gizmos) ctx.Ecs.Insert<ShowLightGizmoRef>(light);
                    else ctx.Ecs.Wrap<ShowLightGizmoRef>(light).Remove();
                }
            }

            var delta = ctx.Input.KeyDown(Key.ArrowUp) ? 0.005f : ctx.Input.KeyDown(Key.ArrowDown) ? -0.005f : 0f;
            if (delta == 0f) return;

            _floor.Roughness = Math.Clamp(_floor.Roughness + delta, 0f, 1f);
            Render.WriteMaterial(_floorMaterial, _floor);
            foreach (var display in ctx.Ecs.EntitiesWith<RoughnessDisplay>()) Ui.SetText(display, Text());
        }, "rect_light.Controls");
    }

    private static void Spawn(EcsWorld ecs, Color color, float intensity, float width, float height, Transform at)
    {
        var light = ecs.Spawn();
        ecs.Add(light, at);
        var rect = ecs.Insert<RectLightRef>(light);
        (rect.Color, rect.Intensity, rect.Width, rect.Height, rect.Range) = (color, intensity, width, height, 20f);
        ecs.Insert<ShowLightGizmoRef>(light);
        Lights.Add(light);
    }

    private static string Text() =>
        FormattableString.Invariant($"Controls\nArrow Up/Down: Adjust floor roughness\nG: Toggle light gizmos\n\nRoughness: {_floor.Roughness:0.00}");
}

/// <summary>The text that says the floor's roughness.</summary>
[Behavior]
public partial struct RoughnessDisplay;
