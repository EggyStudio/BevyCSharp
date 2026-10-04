using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates depth of field, a circuit board and its parts seen close up through a lens focused
// on one of them, the board lit by a lightmap baked into an HDR image.
internal static class DepthOfField
{

    // How far a held key moves the focus and the aperture each frame.
    private const float FocalDistanceSpeed = 0.05f;
    private const float ApertureSpeed = 0.01f;
    private const float MinFocalDistance = 0.01f;
    private const float MinAperture = 0.05f;

    // Bevy's own sensor, the Super 35 cinema format, and its default field of view, from which the
    // help text works out the focal length the way Bevy's calculate_focal_length does.
    private const float SensorHeight = 0.01866f;
    private const float FieldOfView = MathF.PI / 4f;

    private static readonly HashSet<Entity> Tweaked = [];
    private static Entity _camera, _text, _scene = Entity.None;
    private static float _focalDistance, _aperture;
    private static DepthOfFieldMode _mode;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            Tweaked.Clear();
            (_focalDistance, _aperture, _mode) = (7f, 1f / 8f, DepthOfFieldMode.Bokeh);

            _camera = ctx.Ecs.Camera(Transform.LookingAt(new Vec3(0f, 4.5f, 8.25f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(_camera, new PostSettings { Hdr = true, Tonemapper = Tonemapper.TonyMcMapface, Bloom = true });
            ApplyFocus();

            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.SpawnGltf("models/DepthOfFieldExample/DepthOfFieldExample.glb", (_, root) => _scene = root);

        app.Update(ctx =>
        {
            TweakScene(ctx.Ecs);

            var input = ctx.Input;
            var distance = input.KeyDown(Key.ArrowDown) ? -FocalDistanceSpeed : input.KeyDown(Key.ArrowUp) ? FocalDistanceSpeed : 0f;
            var stops = input.KeyDown(Key.ArrowLeft) ? -ApertureSpeed : input.KeyDown(Key.ArrowRight) ? ApertureSpeed : 0f;
            var changed = distance != 0f || stops != 0f;
            _focalDistance = MathF.Max(_focalDistance + distance, MinFocalDistance);
            _aperture = MathF.Max(_aperture + stops, MinAperture);

            if (input.KeyPressed(Key.Space))
            {
                _mode = _mode switch
                {
                    DepthOfFieldMode.Bokeh => DepthOfFieldMode.Gaussian,
                    DepthOfFieldMode.Gaussian => DepthOfFieldMode.None,
                    _ => DepthOfFieldMode.Bokeh,
                };
                changed = true;
            }

            if (!changed) return;
            ApplyFocus();
            Ui.SetText(_text, Text());
        }, "depth_of_field.Update");
    }

    private static void ApplyFocus() => Render.SetEffects(_camera, new EffectSettings
    {
        DepthOfField = _mode,
        FocalDistance = _focalDistance,
        Aperture = _aperture,
        MaxDepth = 14f,
    });

    // Once the model is in, its sun casts shadows and its board takes the light baked for it, as
    // Bevy's tweak_scene does for each light and named mesh as it appears.
    private static void TweakScene(EcsWorld ecs)
    {
        if (_scene == Entity.None) return;

        foreach (var entity in Descendants(ecs, _scene))
        {
            if (Tweaked.Contains(entity)) continue;

            if (ecs.Get<DirectionalLightRef>(entity) is { } sun)
            {
                sun.ShadowMapsEnabled = true;
                Tweaked.Add(entity);
            }
            else if (ecs.Get<GltfMeshNameRef>(entity)?.Value == "CircuitBoard")
            {
                var material = Render.MaterialOf(ecs, entity);
                if (Render.TryReadMaterial(material, out var settings))
                {
                    settings.LightmapExposure = 10000f;
                    Render.WriteMaterial(material, settings);
                }

                ecs.Insert<LightmapRef>(entity).Image = AssetServer.Load(AssetKind.Image, "models/DepthOfFieldExample/CircuitBoardLightmap.hdr");
                Tweaked.Add(entity);
            }
        }
    }

    private static IEnumerable<Entity> Descendants(EcsWorld ecs, Entity root)
    {
        foreach (var child in ecs.ChildrenOf(root))
        {
            yield return child;
            foreach (var below in Descendants(ecs, child)) yield return below;
        }
    }

    private static string Text()
    {
        if (_mode == DepthOfFieldMode.None) return "Mode: Off (Press Space to change)";

        var focalLength = 0.5f * SensorHeight / MathF.Tan(0.5f * FieldOfView);
        var mode = _mode == DepthOfFieldMode.Bokeh ? "Bokeh" : "Gaussian";
        return string.Join('\n',
            FormattableString.Invariant($"Focal distance: {_focalDistance:0.00} m (Press Up/Down to change)"),
            FormattableString.Invariant($"Aperture F-stops: f/{_aperture:0.000} (Press Left/Right to change)"),
            FormattableString.Invariant($"Sensor height: {SensorHeight * 1000f:0.00}mm"),
            FormattableString.Invariant($"Focal length: {focalLength * 1000f:0.00}mm"),
            $"Mode: {mode} (Press Space to change)");
    }
}
