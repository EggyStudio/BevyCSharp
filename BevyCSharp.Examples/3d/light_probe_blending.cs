using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Demonstrates blending between reflection probes. A mirror sphere moves between two rooms, each
// lit by a probe captured in it, and is reflecting a blend of the two while it crosses where their
// falloffs overlap. The probes' bounds, falloffs and parallax correction are drawn as gizmos.
internal static class LightProbeBlending
{
    private const float SphereMovementSpeed = 0.3f;
    private const float RoomSideLength = 10f;
    private const float RoomSeparation = 11f;
    private const float LightProbeSideLength = 15f;
    private const float LightProbeFalloff = 0.5f;
    private const float ParallaxSideLength = RoomSideLength / LightProbeSideLength * 0.5f + 0.01f;
    private const float OrbitSpeedInclination = 0.003f, OrbitSpeedAzimuth = 0.004f, ZoomSpeed = 0.15f;
    private const float LightProbeIntensity = 500f;

    private static Entity _camera, _sphere, _prism, _help;
    private static readonly List<Entity> Probes = [];
    private static float _radius, _inclination, _azimuth;
    private static bool _gizmos, _sphereShown, _orbit;
    private static RadioButtons<bool>? _gizmoButtons, _objectButtons, _cameraButtons;

    public static void Build(App app)
    {
        Probes.Clear();
        (_radius, _inclination, _azimuth) = (3f, 7f * MathF.PI / 4f, MathF.PI / 4f);
        (_gizmos, _sphereShown, _orbit) = (true, true, true);

        app.Startup(Setup, "light_probe_blending.Setup");
        app.SpawnGltf("light_probe_blending/two_rooms.glb");
        app.Update(MoveSphereAndOrbit, "light_probe_blending.MoveSphere");
        app.Update(HandleButtons, "light_probe_blending.HandleButtons");
        app.Update(DrawGizmos, "light_probe_blending.DrawGizmos");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var mirror = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Metallic = 1f, Reflectance = 1f, Roughness = 0f });

        _camera = ecs.Camera(Transform.Identity);
        Render.SetPostProcessing(_camera, new PostSettings { Hdr = true });

        _sphere = ecs.Mesh(Render.CreateMesh(MeshShape.Sphere, 0.5f), mirror, Transform.Identity);

        // A long mirror bar under both rooms, shown in place of the sphere.
        _prism = ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 4f, 2f, 20f), mirror, Transform.At(0f, -4f, -5.5f));
        ecs.Wrap<VisibilityRef>(_prism).Value = Visibility.Hidden;

        // A probe for each room, each a cube larger than its room turned over so it faces in, and
        // faded across half its size into the other.
        foreach (var (room, z) in new[] { (1, 0f), (2, -RoomSeparation) })
        {
            var probe = ecs.Spawn();
            ecs.Add(probe, new Transform(new Vec3(0f, 0f, z), Quat.FromRotationX(MathF.PI), new Vec3(1f, -1f, 1f) * LightProbeSideLength));
            ecs.Insert<ParallaxCorrectionRef>(probe).Value = new ParallaxCorrection.Custom(new Vec3(ParallaxSideLength));
            Render.SetReflectionProbe(
                probe,
                AssetServer.Load(AssetKind.Image, $"light_probe_blending/diffuse_room{room}.ktx2"),
                AssetServer.Load(AssetKind.Image, $"light_probe_blending/specular_room{room}.ktx2"),
                LightProbeIntensity,
                new Vec3(LightProbeFalloff));
            Probes.Add(probe);
        }

        var column = RadioButtons<bool>.Column();
        _gizmoButtons = new RadioButtons<bool>(ecs, column, "Gizmos", [(true, "On"), (false, "Off")], _gizmos);
        _objectButtons = new RadioButtons<bool>(ecs, column, "Object to Show", [(true, "Sphere"), (false, "Prism")], _sphereShown);
        _cameraButtons = new RadioButtons<bool>(ecs, column, "Camera Mode", [(true, "Orbit"), (false, "Free")], _orbit);

        // Empty until a setting changes, as Bevy's is.
        _help = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
    }

    // The sphere eases from the first room to the second and back, and the orbiting camera follows.
    private static void MoveSphereAndOrbit(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var u = ctx.Time.Elapsed * SphereMovementSpeed % 2f;
        var x = u <= 1f ? u : 2f - u;
        var eased = x * x * (3f - 2f * x);
        var sphere = new Vec3(0f, 0f, -RoomSeparation * eased);
        ecs.Set(_sphere, Transform.At(sphere.X, sphere.Y, sphere.Z));

        if (!_orbit) return;
        var input = ctx.Input;
        if (input.MouseDown(MouseButton.Left))
        {
            _azimuth -= input.MouseDeltaX * OrbitSpeedAzimuth;
            _inclination += input.MouseDeltaY * OrbitSpeedInclination;
        }
        _radius = MathF.Max(_radius - ZoomSpeed * input.WheelY, 0.01f);

        var offset = _radius * new Vec3(
            MathF.Sin(_inclination) * MathF.Cos(_azimuth),
            MathF.Cos(_inclination),
            MathF.Sin(_inclination) * MathF.Sin(_azimuth));
        ecs.Set(_camera, Transform.LookingAt(offset + sphere, sphere, Vec3.UnitY));
    }

    private static void HandleButtons(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var changed = false;

        if (_gizmoButtons!.Pressed(out var gizmos) && gizmos != _gizmos)
        {
            (_gizmos, changed) = (gizmos, true);
            _gizmoButtons.Select(ecs, gizmos);
        }

        if (_objectButtons!.Pressed(out var sphereShown) && sphereShown != _sphereShown)
        {
            _sphereShown = sphereShown;
            ecs.Wrap<VisibilityRef>(_sphere).Value = sphereShown ? Visibility.Inherited : Visibility.Hidden;
            ecs.Wrap<VisibilityRef>(_prism).Value = sphereShown ? Visibility.Hidden : Visibility.Inherited;
            _objectButtons.Select(ecs, sphereShown);
        }

        if (_cameraButtons!.Pressed(out var orbit) && orbit != _orbit)
        {
            _orbit = orbit;
            if (orbit)
            {
                // Taken up again from where the free camera left it.
                ecs.Remove<FreeCamera>(_camera);
                var relative = ecs.GetOrDefault<Transform>(_camera).Translation - ecs.GetOrDefault<Transform>(_sphere).Translation;
                _radius = relative.Length;
                var flat = MathF.Sqrt(relative.X * relative.X + relative.Z * relative.Z);
                _inclination = MathF.Atan2(flat / _radius, relative.Y / _radius);
                _azimuth = MathF.Atan2(relative.Z / flat, relative.X / flat);
            }
            else
            {
                ecs.Add(_camera, new FreeCamera());
            }

            _cameraButtons.Select(ecs, orbit);
            changed = true;
        }

        if (changed) Ui.SetText(_help, HelpText());
    }

    // Each probe's bounds in tan, the bounds its falloff starts at in crimson, and its parallax
    // correction in blue, drawn over the scene.
    private static void DrawGizmos(BehaviorContext ctx)
    {
        if (!_gizmos) return;

        var (tan, crimson, blue) = (Scene.Srgb8(210, 180, 140), Scene.Srgb8(220, 20, 60), Scene.Srgb8(100, 149, 237));
        foreach (var probe in Probes)
        {
            var at = ctx.Ecs.GetOrDefault<Transform>(probe);
            Gizmos.Box(at.Translation, at.Rotation, at.Scale, tan);
            Gizmos.Box(at.Translation, at.Rotation, at.Scale * (1f - LightProbeFalloff), crimson);
            Gizmos.Box(at.Translation, at.Rotation, at.Scale * ParallaxSideLength, blue);
        }
    }

    private static string HelpText()
    {
        var text = _orbit
            ? "Click and drag to orbit the camera\nUse the mouse wheel to zoom the camera\n"
            : "Click and drag to rotate the camera\nUse WASDEQ to move the camera\n";
        text += "\n";
        if (_gizmos) text += "Gizmos:\nTan: Light probe bounds\nRed: Light probe falloff bounds\nBlue: Parallax correction bounds";
        return text;
    }
}
