// Bevy's light_probe_blending example, examples/3d/light_probe_blending.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Demonstrates blending between reflection probes. A mirror sphere moves between two rooms, each
// lit by a probe captured in it, and is reflecting a blend of the two while it crosses where their
// falloffs overlap. The probes' bounds, falloffs and parallax correction are drawn as gizmos.
internal static class LightProbeBlending
{
    internal const float SphereMovementSpeed = 0.3f;
    private const float RoomSideLength = 10f;
    internal const float RoomSeparation = 11f;
    private const float LightProbeSideLength = 15f;
    private const float LightProbeFalloff = 0.5f;
    private const float ParallaxSideLength = RoomSideLength / LightProbeSideLength * 0.5f + 0.01f;
    internal const float OrbitSpeedInclination = 0.003f, OrbitSpeedAzimuth = 0.004f, ZoomSpeed = 0.15f;
    private const float LightProbeIntensity = 500f;

    private static Entity _camera;
    private static readonly List<Entity> Probes = [];
    private static bool _gizmos, _sphereShown, _orbit;
    private static RadioButtons<bool>? _gizmoButtons, _objectButtons, _cameraButtons;

    public static void Build(App app)
    {
        Probes.Clear();
        (_gizmos, _sphereShown, _orbit) = (true, true, true);

        app.Startup(Setup, "light_probe_blending.Setup");
        app.SpawnGltf("light_probe_blending/two_rooms.glb");
        app.Update(HandleButtons, "light_probe_blending.HandleButtons");
        app.Update(DrawGizmos, "light_probe_blending.DrawGizmos");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var mirror = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Metallic = 1f, Reflectance = 1f, Roughness = 0f });

        _camera = ecs.SpawnCamera3d(Transform.Identity);
        Render.SetPostProcessing(_camera, new PostSettings { Hdr = true });
        ecs.Add(_camera, new OrbitCamera { Radius = 3f, Inclination = 7f * MathF.PI / 4f, Azimuth = MathF.PI / 4f });

        ecs.Add(ecs.SpawnMesh(Render.CreateMesh(MeshShape.Sphere, 0.5f), mirror, Transform.Identity), new ReflectiveSphere());

        // A long mirror bar under both rooms, shown in place of the sphere.
        var prism = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 4f, 2f, 20f), mirror, Transform.At(0f, -4f, -5.5f));
        ecs.Wrap<VisibilityRef>(prism).Value = Visibility.Hidden;
        ecs.Add(prism, new ReflectivePrism());

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
        ecs.Add(Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) }), new ProbeBlendingHelpText());
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
            foreach (var sphere in ecs.EntitiesWith<ReflectiveSphere>()) ecs.Wrap<VisibilityRef>(sphere).Value = sphereShown ? Visibility.Inherited : Visibility.Hidden;
            foreach (var prism in ecs.EntitiesWith<ReflectivePrism>()) ecs.Wrap<VisibilityRef>(prism).Value = sphereShown ? Visibility.Hidden : Visibility.Inherited;
            _objectButtons.Select(ecs, sphereShown);
        }

        if (_cameraButtons!.Pressed(out var orbit) && orbit != _orbit)
        {
            _orbit = orbit;
            if (orbit)
            {
                // Taken up again from where the free camera left it.
                ecs.Remove<FreeCamera>(_camera);
                var sphere = ecs.EntitiesWith<ReflectiveSphere>() is [var first, ..] ? ecs.GetOrDefault<Transform>(first).Translation : Vec3.Zero;
                var relative = ecs.GetOrDefault<Transform>(_camera).Translation - sphere;
                var radius = relative.Length;
                var flat = MathF.Sqrt(relative.X * relative.X + relative.Z * relative.Z);
                ecs.Add(_camera, new OrbitCamera
                {
                    Radius = radius,
                    Inclination = MathF.Atan2(flat / radius, relative.Y / radius),
                    Azimuth = MathF.Atan2(relative.Z / flat, relative.X / flat),
                });
            }
            else
            {
                ecs.Remove<OrbitCamera>(_camera);
                ecs.Add(_camera, new FreeCamera());
            }

            _cameraButtons.Select(ecs, orbit);
            changed = true;
        }

        if (changed) foreach (var help in ecs.EntitiesWith<ProbeBlendingHelpText>()) Ui.SetText(help, HelpText());
    }

    // Each probe's bounds in tan, the bounds its falloff starts at in crimson, and its parallax
    // correction in blue, drawn over the scene.
    private static void DrawGizmos(BehaviorContext ctx)
    {
        if (!_gizmos) return;

        var (tan, crimson, blue) = (Color.FromSrgb8(210, 180, 140), Color.FromSrgb8(220, 20, 60), Color.FromSrgb8(100, 149, 237));
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

/// <summary>The mirrored sphere, which eases from one room to the other and back.</summary>
[Behavior]
public partial struct ReflectiveSphere
{
    /// <summary>Moved along Z between the rooms by a smooth step ping-ponged over time, first of the two Bevy chains.</summary>
    [OnUpdate]
    public void MoveSphere(BehaviorContext ctx, ref Transform transform)
    {
        var u = ctx.Time.Elapsed * LightProbeBlending.SphereMovementSpeed % 2f;
        var x = u <= 1f ? u : 2f - u;
        transform.Translation.Z = -LightProbeBlending.RoomSeparation * x * x * (3f - 2f * x);
    }
}

/// <summary>The long mirrored bar shown in place of the sphere.</summary>
[Behavior]
public partial struct ReflectivePrism;

/// <summary>A camera orbiting the sphere, where it is by its distance and two angles.</summary>
[Behavior]
public partial struct OrbitCamera
{
    /// <summary>Its distance from the sphere.</summary>
    public float Radius;

    /// <summary>Its angle down from straight above the sphere, in radians.</summary>
    public float Inclination;

    /// <summary>Its angle round the sphere, in radians.</summary>
    public float Azimuth;

    /// <summary>
    /// Turned by a drag with the left button and drawn in by the wheel, placed by its angles about
    /// the sphere and looking at it, after the sphere has moved.
    /// </summary>
    [OnUpdate]
    [After("ReflectiveSphere.MoveSphere")]
    public void OrbitCameraSystem(BehaviorContext ctx, ref Transform transform)
    {
        if (ctx.Ecs.EntitiesWith<ReflectiveSphere>() is not [var first, ..]) return;
        var sphere = ctx.Ecs.GetOrDefault<Transform>(first).Translation;

        var input = ctx.Input;
        if (input.MouseDown(MouseButton.Left))
        {
            Azimuth -= input.MouseDeltaX * LightProbeBlending.OrbitSpeedAzimuth;
            Inclination += input.MouseDeltaY * LightProbeBlending.OrbitSpeedInclination;
        }

        Radius = MathF.Max(Radius - LightProbeBlending.ZoomSpeed * input.WheelY, 0.01f);
        var offset = Radius * new Vec3(MathF.Sin(Inclination) * MathF.Cos(Azimuth), MathF.Cos(Inclination), MathF.Sin(Inclination) * MathF.Sin(Azimuth));
        transform = Transform.LookingAt(offset + sphere, sphere, Vec3.UnitY);
    }
}

/// <summary>
/// The help text, Bevy's <c>HelpText</c> under another name since color_grading's shares the
/// namespace.
/// </summary>
[Behavior]
public partial struct ProbeBlendingHelpText;
