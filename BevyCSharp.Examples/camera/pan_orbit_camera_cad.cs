// Bevy's pan_orbit_camera_cad example, examples/camera/pan_orbit_camera_cad.rs at v0.20.0, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Cameras;

// The pan-orbit camera inspecting an engine model as a CAD program would. P moves between a
// perspective and an orthographic projection over a dolly zoom, C between an orbit that keeps the
// camera upright and one that does not, 1 to 6 turn it to face along an axis, E pulls the model's
// parts apart along their height and back, and Z lets a zoom pass through a surface it reaches.
internal static class PanOrbitCameraCad
{
    private const float ExplodeSeconds = 2f;

    private static Entity _camera;
    private static Entity _zoomStatus;
    private static Entity _root;
    private static bool _orthographic;
    private static (bool Toggled, float Start, float StartAmount)? _explode;
    private static float _explodeAmount;
    private static readonly Dictionary<Entity, float> StartHeights = [];

    public static void Configure(Config config) =>
        (config.CameraControllers, config.MeshPicking) = (CameraControllers.PanOrbit, true);

    public static void Build(App app)
    {
        app.Startup(Setup, "pan_orbit_camera_cad.Setup");

        // Twice its size, and every material of the model kept from being too glossy for the
        // environment map, as Bevy's example clamps them.
        app.SpawnGltf("PlaneEngine/scene.gltf", (ctx, root) =>
        {
            _root = root;
            ctx.Ecs.Set(root, new Transform(Vec3.Zero, Quat.Identity, new Vec3(2f, 2f, 2f)));
            foreach (var part in ctx.Ecs.Descendants(root))
            {
                var material = Render.MaterialOf(ctx.Ecs, part);
                if (material == AssetHandle.None || !Render.TryReadMaterial(material, out var settings)) continue;
                settings.Roughness = Math.Clamp(settings.Roughness, 0.3f, 1f);
                Render.WriteMaterial(material, settings);
            }
        });

        app.Update(TogglesAndTurns, "pan_orbit_camera_cad.Inputs");
        app.Update(Explode, "pan_orbit_camera_cad.Explode");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_orthographic, _explode, _explodeAmount, _root) = (false, null, 0f, Entity.None);
        StartHeights.Clear();

        Render.SetAmbientLight((0f, 0f, 0f), 0f);

        var at = new Vec3(2f, 2f, 2f);
        _camera = ecs.SpawnCamera3d(Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY));
        Render.SetPostProcessing(_camera, new PostSettings
        {
            Hdr = true,
            Tonemapper = Tonemapper.AcesFitted,
            Bloom = true,
            AntiAlias = AntiAliasPass.Smaa,
            Msaa = 1,
        });
        Render.SetAmbientOcclusion(_camera, AmbientOcclusionQuality.High);
        Render.SetEnvironmentMap(
            _camera,
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"),
            AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"),
            5000f);

        var orbit = ecs.Insert<PanOrbitCameraRef>(_camera);
        orbit.OrbitConstraint = new OrbitConstraint.Free();
        orbit.LastAnchorDepth = -at.Length;
        // Near enough for the ambient occlusion to work in an orthographic view.
        orbit.OrthographicScaleToNearClip = 1000f;

        var column = Ui.SpawnNode(new UiSettings { Margin = Sides.All(Length.Px(20f)), Direction = UiDirection.Column, Camera = _camera });
        var help = Ui.SpawnText(
            "Left Mouse  - Pan\n"
                + "Right Mouse - Orbit\n"
                + "Scroll      - Zoom\n"
                + "Z           - Toggle passing thought surface on minimal zoom\n"
                + "P           - Toggle projection\n"
                + "C           - Toggle orbit constraint\n"
                + "E           - Toggle explode\n"
                + "1-6         - Switch direction",
            new UiSettings(), 20f);
        _zoomStatus = Ui.SpawnText(ZoomStatus(false), new UiSettings(), 20f);
        ecs.SetParent(help, column);
        ecs.SetParent(_zoomStatus, column);
    }

    private static string ZoomStatus(bool zoomThrough) => $"Zoom Through: {(zoomThrough ? "true" : "false")}";

    private static void TogglesAndTurns(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var orbit = ctx.Ecs.Wrap<PanOrbitCameraRef>(_camera);

        if (input.KeyPressed(Key.P))
        {
            _orthographic = !_orthographic;
            Render.DollyZoom(_camera, _orthographic ? CameraProjection.Orthographic : CameraProjection.Perspective);
        }

        // Upright again, turned to the axis nearest the way it faces.
        if (input.KeyPressed(Key.C))
        {
            if (orbit.OrbitConstraint is OrbitConstraint.Free)
            {
                orbit.OrbitConstraint = new OrbitConstraint.Fixed(0, 1, 0, false);
                Render.LookTo(_camera, ctx.Ecs.GetOrDefault<Transform>(_camera).Rotation * -Vec3.UnitZ);
            }
            else
            {
                orbit.OrbitConstraint = new OrbitConstraint.Free();
            }
        }

        Vec3[] directions = [Vec3.UnitX, Vec3.UnitZ, -Vec3.UnitX, -Vec3.UnitZ, Vec3.UnitY, -Vec3.UnitY];
        Key[] keys = [Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6];
        for (var i = 0; i < keys.Length; i++)
        {
            if (input.KeyPressed(keys[i])) Render.LookTo(_camera, directions[i]);
        }

        if (input.KeyPressed(Key.Z))
        {
            var through = !orbit.ZoomLimitsZoomThroughObjects;
            orbit.ZoomLimitsZoomThroughObjects = through;
            Ui.SetText(_zoomStatus, ZoomStatus(through));
        }
    }

    // Each part rises by twice the top of its box over two seconds, eased as a stylesheet's ease,
    // and comes down again on the next press.
    private static void Explode(BehaviorContext ctx)
    {
        var now = ctx.Time.Elapsed;
        if (ctx.Input.KeyPressed(Key.E))
        {
            var toggled = _explode is not { } last || !last.Toggled;
            _explode = (toggled, now, _explodeAmount);
        }

        if (_explode is not { } explode || _root == Entity.None) return;

        var goal = explode.Toggled ? 1f : 0f;
        var t = Math.Clamp((now - explode.Start) / ExplodeSeconds, 0f, 1f);
        _explodeAmount = explode.StartAmount + (goal - explode.StartAmount) * Ease(t);

        var ecs = ctx.Ecs;
        foreach (var part in ecs.Descendants(_root))
        {
            if (Render.MeshOf(ecs, part) == AssetHandle.None) continue;
            if (!StartHeights.TryGetValue(part, out var start))
            {
                if (ecs.Get<AabbRef>(part) is not { } box) continue;
                StartHeights[part] = start = box.Center.Y + box.HalfExtents.Y;
            }

            var transform = ecs.GetOrDefault<Transform>(part);
            transform.Translation = new Vec3(transform.Translation.X, _explodeAmount * start * 2f, transform.Translation.Z);
            ecs.Set(part, transform);
        }
    }

    // Bevy's cubic Bézier easing through (0.25, 0.1) and (0.25, 1), the y of the curve at the x
    // given, the x found by Newton's method within Bevy's eight steps and its error.
    private static float Ease(float x)
    {
        static float Bezier(float a, float b, float t) => 3f * (1f - t) * (1f - t) * t * a + 3f * (1f - t) * t * t * b + t * t * t;
        static float Slope(float a, float b, float t) => 3f * (1f - t) * (1f - t) * a + 6f * (1f - t) * t * (b - a) + 3f * t * t * (1f - b);

        var (t, y) = (x, 0f);
        for (var i = 0; i < 8; i++)
        {
            y = Bezier(0.1f, 1f, t);
            var error = Bezier(0.25f, 0.25f, t) - x;
            if (MathF.Abs(error) <= 1e-5f) break;
            t -= error / Slope(0.25f, 0.25f, t);
        }

        return y;
    }
}
