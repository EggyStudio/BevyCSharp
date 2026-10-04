using System.Globalization;
using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates how shadow biases affect shadows in a 3D scene: a row of spheres running far into
// the distance over a plane, lit by a directional or a point light whose depth and normal biases
// change with the number keys.
internal static class ShadowBiases
{
    private const string PointLightType = "bevy_light::point_light::PointLight";
    private const string DirectionalLightType = "bevy_light::directional_light::DirectionalLight";

    // Bevy's own defaults for each kind of light.
    private const float PointDepthDefault = 0.08f, PointNormalDefault = 0.6f;
    private const float DirectionalDepthDefault = 0.02f, DirectionalNormalDefault = 1.8f;

    // Bevy's light_consts, a very large cinema light and ambient daylight.
    private const float CinemaLumens = 1_000_000f, DaylightLux = 10_000f;

    private static Entity _lights, _point, _directional, _camera, _text;
    private static Vec3 _lightAt;
    private static bool _pointOn;
    private static ShadowFiltering _filter;
    private static float _pointDepth, _pointNormal, _directionalDepth, _directionalNormal;

    public static void Build(App app)
    {
        (_lightAt, _pointOn, _filter) = (new Vec3(5f, 5f, 0f), false, ShadowFiltering.Hardware2x2);
        (_pointDepth, _pointNormal) = (PointDepthDefault, PointNormalDefault);
        (_directionalDepth, _directionalNormal) = (DirectionalDepthDefault, DirectionalNormalDefault);

        app.Startup(Setup, "shadow_biases.Setup");
        app.Update(Adjust, "shadow_biases.Adjust");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        const float depth = 300f, height = 2f, radius = 0.25f;

        var white = Render.CreateMaterial(new MaterialSettings { BaseColor = (1f, 1f, 1f, 1f), Roughness = 1f });
        var sphere = Render.CreateMesh(MeshShape.Sphere, radius);

        // Both lights hang from one entity that the arrow keys move, and only one is on at a time.
        _lights = ecs.Spawn();
        ecs.Add(_lights, Transform.LookingAt(_lightAt, Vec3.Zero, Vec3.UnitY));

        _point = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Point,
            Intensity = 0f,
            Range = depth,
            Shadows = true,
            ShadowDepthBias = PointDepthDefault,
            ShadowNormalBias = PointNormalDefault,
        });
        _directional = Render.SpawnLight(new LightSettings
        {
            Kind = LightKind.Directional,
            Intensity = DaylightLux,
            Shadows = true,
            ShadowDepthBias = DirectionalDepthDefault,
            ShadowNormalBias = DirectionalNormalDefault,
        });
        foreach (var light in new[] { _point, _directional })
        {
            ecs.Add(light, Transform.Identity);
            ecs.SetParent(light, _lights);
        }

        _camera = ecs.Camera(Transform.LookingAt(new Vec3(-1f, 1f, 1f), new Vec3(-1f, 1f, 0f), Vec3.UnitY));
        ecs.Add(_camera, new FreeCamera());
        Render.SetShadowFiltering(_camera, _filter);

        // Every other sphere raised, so the shadows fall at two distances from what casts them.
        for (var z = -(int)depth; z <= 0; z += 2)
            ecs.Mesh(sphere, white, Transform.At(0f, z % 4 == 0 ? height : radius, z));

        ecs.Mesh(Render.CreateMesh(MeshShape.Plane, 2f * depth, 2f * depth), white, Transform.Identity);

        var panel = Ui.SpawnNode(new UiSettings { Absolute = true, Padding = Sides.All(Length.Px(5f)), Color = (0f, 0f, 0f, 0.75f) });
        _text = Ui.SpawnText(Describe(), new UiSettings());
        ecs.SetParent(_text, panel);
    }

    private static void Adjust(BehaviorContext ctx)
    {
        var input = ctx.Input;
        var ecs = ctx.Ecs;
        var changed = false;

        if (input.KeyPressed(Key.F))
        {
            _filter = _filter switch
            {
                ShadowFiltering.Hardware2x2 => ShadowFiltering.Gaussian,
                ShadowFiltering.Gaussian => ShadowFiltering.Temporal,
                _ => ShadowFiltering.Hardware2x2,
            };
            Render.SetShadowFiltering(_camera, _filter);
            changed = true;
        }

        if (input.KeyPressed(Key.L))
        {
            _pointOn = !_pointOn;
            ecs.SetReflected(_point, PointLightType, ".intensity", Number(_pointOn ? CinemaLumens : 0f));
            ecs.SetReflected(_directional, DirectionalLightType, ".illuminance", Number(_pointOn ? 0f : DaylightLux));
            changed = true;
        }

        var offset = Vec3.Zero;
        if (input.KeyPressed(Key.ArrowLeft)) offset.X -= 1f;
        if (input.KeyPressed(Key.ArrowRight)) offset.X += 1f;
        if (input.KeyPressed(Key.ArrowUp)) offset.Z -= 1f;
        if (input.KeyPressed(Key.ArrowDown)) offset.Z += 1f;
        if (input.KeyPressed(Key.PageDown)) offset.Y -= 1f;
        if (input.KeyPressed(Key.PageUp)) offset.Y += 1f;
        if (offset != Vec3.Zero)
        {
            _lightAt += offset;
            ecs.Set(_lights, Transform.LookingAt(_lightAt, Vec3.Zero, Vec3.UnitY));
            changed = true;
        }

        const float depthStep = 0.01f, normalStep = 0.1f;
        var biased = false;
        void Step(Key down, Key up, ref float value, float step)
        {
            if (input.KeyPressed(down)) { value -= step; biased = true; }
            if (input.KeyPressed(up)) { value += step; biased = true; }
        }

        Step(Key.Digit1, Key.Digit2, ref _pointDepth, depthStep);
        Step(Key.Digit3, Key.Digit4, ref _pointNormal, normalStep);
        Step(Key.Digit5, Key.Digit6, ref _directionalDepth, depthStep);
        Step(Key.Digit7, Key.Digit8, ref _directionalNormal, normalStep);
        if (input.KeyPressed(Key.R))
        {
            (_pointDepth, _pointNormal) = (PointDepthDefault, PointNormalDefault);
            (_directionalDepth, _directionalNormal) = (DirectionalDepthDefault, DirectionalNormalDefault);
            biased = true;
        }
        if (input.KeyPressed(Key.Z))
        {
            (_pointDepth, _pointNormal, _directionalDepth, _directionalNormal) = (0f, 0f, 0f, 0f);
            biased = true;
        }

        if (biased)
        {
            ecs.SetReflected(_point, PointLightType, ".shadow_depth_bias", Number(_pointDepth));
            ecs.SetReflected(_point, PointLightType, ".shadow_normal_bias", Number(_pointNormal));
            ecs.SetReflected(_directional, DirectionalLightType, ".shadow_depth_bias", Number(_directionalDepth));
            ecs.SetReflected(_directional, DirectionalLightType, ".shadow_normal_bias", Number(_directionalNormal));
        }

        if (changed || biased) Ui.SetText(_text, Describe());
    }

    private static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    private static string Describe() => string.Create(CultureInfo.InvariantCulture,
        $"""
        Controls:
        R / Z - reset biases to default / zero
        L     - switch between directional and point lights [{(_pointOn ? "PointLight" : "DirectionalLight")}]
        F     - switch directional light filter methods [{_filter}]
        1/2   - change point light depth bias [{_pointDepth:0.00}]
        3/4   - change point light normal bias [{_pointNormal:0.0}]
        5/6   - change direction light depth bias [{_directionalDepth:0.00}]
        7/8   - change direction light normal bias [{_directionalNormal:0.0}]
        left/right/up/down/pgup/pgdown - adjust light position (looking at 0,0,0) [{_lightAt.X:0.0}, {_lightAt.Y:0.0}, {_lightAt.Z:0.0}]

        """);
}
