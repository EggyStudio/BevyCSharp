// Bevy's pcss example, examples/3d/pcss.rs at v0.19.1, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates percentage-closer soft shadows, a palm tree's shadow sharp at its trunk and soft at
// its leaves, from a directional, point or spot light, filtered each frame alone or over several.
internal static class Pcss
{
    private enum LightType { Directional, Point, Spot }

    private enum ShadowFilter { NonTemporal, Temporal }


    // The size of the light, which is how wide a shadow's soft edge grows, and the rest of Bevy's
    // constants for it.
    private const float LightRadius = 10f;
    private const float PointLightIntensity = 1_000_000_000f;
    private const float PointLightRange = 110f;
    private const float DirectionalShadowDepthBias = 0.2f;
    private const float PointShadowDepthBias = 0.35f;
    private const float ShadowMapNearZ = 50f;

    private static readonly Transform LightAt = new(new Vec3(57.693f, 34.334f, -6.422f), new Quat(0.6539259f, -0.34646285f, 0.36505926f, -0.5648683f), Vec3.One);

    private static LightType _lightType;
    private static ShadowFilter _filter;
    private static bool _soft;
    private static Entity _camera, _light;
    private static RadioButtons<LightType>? _lightButtons;
    private static RadioButtons<ShadowFilter>? _filterButtons;
    private static RadioButtons<bool>? _softButtons;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_lightType, _filter, _soft) = (LightType.Directional, ShadowFilter.NonTemporal, true);

            var at = new Vec3(-12.912f, 4.466f, -10.624f) * 0.7f;
            _camera = ecs.Camera(new Transform(at, Quat.FromEuler(-0.175f, -134.76f / 180f * MathF.PI, 0f), Vec3.One));
            Render.SetPostProcessing(_camera, new PostSettings { Msaa = 1 });
            Shaders.SetPrepass(_camera, depth: true, motion: true);
            Render.SetShadowFiltering(_camera, ShadowFiltering.Gaussian);
            Render.SetSkybox(_camera, AssetServer.Load(AssetKind.Image, "environment_maps/sky_skybox.ktx2"), 500f);

            _light = SpawnLight(ecs);

            var column = RadioButtons<LightType>.Column();
            _lightButtons = new RadioButtons<LightType>(ecs, column, "Light Type",
                [(LightType.Directional, "Directional"), (LightType.Point, "Point"), (LightType.Spot, "Spot")], _lightType);
            _filterButtons = new RadioButtons<ShadowFilter>(ecs, column, "Shadow Filter",
                [(ShadowFilter.Temporal, "Temporal"), (ShadowFilter.NonTemporal, "Non-Temporal")], _filter);
            _softButtons = new RadioButtons<bool>(ecs, column, "Soft Shadows", [(true, "On"), (false, "Off")], _soft);
        });

        app.SpawnGltf("models/PalmTree/PalmTree.gltf");

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;

            if (_lightButtons!.Pressed(out var lightType) && lightType != _lightType)
            {
                _lightType = lightType;
                _lightButtons.Select(ecs, _lightType);
                ecs.Despawn(_light);
                _light = SpawnLight(ecs);
            }

            if (_filterButtons!.Pressed(out var filter) && filter != _filter)
            {
                _filter = filter;
                _filterButtons.Select(ecs, _filter);
                if (_filter == ShadowFilter.Temporal)
                {
                    Render.SetShadowFiltering(_camera, ShadowFiltering.Temporal);
                    ecs.Insert<TemporalAntiAliasingRef>(_camera);
                }
                else
                {
                    Render.SetShadowFiltering(_camera, ShadowFiltering.Gaussian);
                    ecs.Wrap<TemporalAntiAliasingRef>(_camera).Remove();
                }
            }

            if (_softButtons!.Pressed(out var soft) && soft != _soft)
            {
                _soft = soft;
                _softButtons.Select(ecs, _soft);
                ecs.Despawn(_light);
                _light = SpawnLight(ecs);
            }
        }, "pcss.Update");
    }

    // A light of the chosen kind, made again whenever the kind or its softness changes, as Bevy's
    // example builds its light component again.
    private static Entity SpawnLight(EcsWorld ecs)
    {
        var light = Render.SpawnLight(_lightType switch
        {
            LightType.Point => new LightSettings { Kind = LightKind.Point, Intensity = PointLightIntensity, Range = PointLightRange, Radius = LightRadius, ShadowDepthBias = PointShadowDepthBias },
            LightType.Spot => new LightSettings { Kind = LightKind.Spot, Intensity = PointLightIntensity, Range = PointLightRange, Radius = LightRadius, ShadowDepthBias = DirectionalShadowDepthBias, OuterAngle = MathF.PI / 4f },
            _ => new LightSettings { Kind = LightKind.Directional, ShadowDepthBias = DirectionalShadowDepthBias },
        });
        ecs.Add(light, LightAt);

        if (_lightType == LightType.Point) ecs.Wrap<PointLightRef>(light).ShadowMapNearZ = ShadowMapNearZ;
        else if (_lightType == LightType.Spot) ecs.Wrap<SpotLightRef>(light).ShadowMapNearZ = ShadowMapNearZ;
        if (_soft) Render.SetSoftShadows(light, LightRadius);
        return light;
    }
}
