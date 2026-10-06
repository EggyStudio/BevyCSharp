// Bevy's projection_zoom example, examples/camera/projection_zoom.rs at v0.19.1, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using System.Text.Json.Nodes;
using Bevy;

namespace BevyCSharp.Examples.Cameras;

// Shows how to zoom orthographic and perspective cameras, the wheel scaling the one and widening
// or narrowing the other, and Space switching between them.
internal static class ProjectionZoom
{
    // An orthographic projection holds its scaling mode, an enum inside a variant, which a wrapper
    // does not type, so the projection is read and written as JSON.
    private const string Projection = "bevy_camera::projection::Projection";
    private const float OrthographicHeight = 5f;
    private const float OrthographicZoomSpeed = 0.2f;
    private const float PerspectiveZoomSpeed = 0.05f;
    private static readonly (float Min, float Max) OrthographicZoom = (0.1f, 10f);
    private static readonly (float Min, float Max) PerspectiveZoom = (MathF.PI / 5f, MathF.PI - 0.2f);

    private static Entity _camera;
    private static bool _orthographic;
    private static float _scale, _fieldOfView;

    // The orthographic projection as Bevy writes it, kept to switch back to with its scale changed.
    private static JsonNode? _orthographicJson;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_orthographic, _scale, _fieldOfView, _orthographicJson) = (true, 1f, PerspectiveZoom.Min, null);

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(5f, 5f, 5f), Vec3.Zero, Vec3.UnitY),
                new CameraSettings { Projection = CameraProjection.Orthographic, Height = OrthographicHeight });
            ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 5f, 5f), Render.CreateMaterial(new MaterialSettings { BaseColor = Color.FromSrgb(0.3f, 0.5f, 0.3f), DoubleSided = true }), Transform.Identity);
            ecs.SpawnPointLight(new Vec3(3f, 8f, 5f));

            Ui.SpawnText("Scroll mouse wheel to zoom in/out\nSpace: switch between orthographic and perspective projections",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        }, "projection_zoom.Setup");

        app.SpawnGltf("models/animated/Fox.glb", (ctx, fox) => ctx.Ecs.Set(fox, new Transform(Vec3.Zero, Quat.Identity, new Vec3(0.025f))));

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            _orthographicJson ??= ecs.GetReflected(_camera, Projection) is { } json ? JsonNode.Parse(json) : null;

            if (ctx.Input.KeyPressed(Key.Space))
            {
                _orthographic = !_orthographic;
                if (_orthographic) _scale = 1f;
                else _fieldOfView = PerspectiveZoom.Min;
                Apply(ecs);
            }

            var wheel = ctx.Input.WheelY;
            if (wheel == 0f) return;

            if (_orthographic) _scale = Math.Clamp(_scale * (1f - wheel * OrthographicZoomSpeed), OrthographicZoom.Min, OrthographicZoom.Max);
            else _fieldOfView = Math.Clamp(_fieldOfView - wheel * PerspectiveZoomSpeed, PerspectiveZoom.Min, PerspectiveZoom.Max);
            Apply(ecs);
        }, "projection_zoom.SwitchAndZoom");
    }

    private static void Apply(EcsWorld ecs)
    {
        if (!_orthographic)
        {
            Render.SetPerspective(_camera, _fieldOfView * 180f / MathF.PI, 0.1f, 1000f);
            return;
        }

        if (_orthographicJson?["Orthographic"] is not JsonObject orthographic) return;
        orthographic["scale"] = _scale;
        ecs.SetReflected(_camera, Projection, string.Empty, _orthographicJson.ToJsonString());
    }
}
