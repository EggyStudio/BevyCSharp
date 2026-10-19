// Bevy's specular_tint example, examples/3d/specular_tint.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// A black, mirror-smooth sphere that shows only what it reflects of an environment map, its
// reflection tinted by one color or by a noise texture, chosen by two buttons at the bottom left,
// while the camera goes round it.
internal static class SpecularTint
{
    // Radians the camera goes round each frame.
    private const float RotationSpeed = 0.005f;

    // Degrees the tint's hue moves at each choice of the solid tint.
    private const float HueShiftSpeed = 0.2f;

    private enum TintType
    {
        Solid,
        Map,
    }

    private static Entity _camera;
    private static AssetHandle _material, _noise;
    private static TintType _tintType;
    private static float _hue;

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app)
    {
        (_tintType, _hue) = (TintType.Solid, 0f);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Theme.BasicExampleTheme(Color.White);
            _noise = AssetServer.Load(AssetKind.Image, "textures/AlphaNoise.png");
            Render.SetAmbientLight((0f, 0f, 0f), 0f);

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-2f, 0f, 3.5f), Vec3.Zero, Vec3.UnitY));
            Render.SetPostProcessing(_camera, new PostSettings { Hdr = true });
            var specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
            Render.SetSkybox(_camera, specular, 3000f);

            // Bright, so the tint shows well in what the sphere reflects.
            Render.SetEnvironmentMap(_camera, AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"), specular, 25000f);

            // Black, so it shows only its reflection, and no metal, since a metal reflects in its
            // own color and ignores the reflectance a tint modulates.
            _material = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (0f, 0f, 0f, 1f),
                Reflectance = 1f,
                SpecularTint = Color.FromHsl(_hue, 1f, 0.5f),
                Metallic = 0f,
                Roughness = 0f,
            });
            ecs.SpawnMesh(
                Render.CreateMesh(MeshShape.UvSphere, 0.5f, 32f, 18f),
                _material,
                new Transform(Vec3.Zero, Quat.FromRotationX(MathF.PI * 0.5f), Vec3.One));

            var row = Radio.FeathersOptionButtons(ecs, Radio.MainUiNode(), "Toggle specular tint",
                [(TintType.Solid, "SOLID"), (TintType.Map, "MAP")], 0);
            Ui.SelfUpdate(row, UiWidgetKind.RadioGroup);
            ecs.Observe<ValueChange<Entity>>(on => ToggleSpecularMap(on.Event.Value));
        }, "specular_tint.Setup");

        app.Update(ctx =>
        {
            var transform = ctx.Ecs.GetOrDefault<Transform>(_camera);
            ctx.Ecs.Set(_camera, Transform.LookingAt(Quat.FromRotationY(RotationSpeed) * transform.Translation, Vec3.Zero, Vec3.UnitY));
        }, "specular_tint.RotateCamera");
    }

    // A choice of the solid tint takes the map away and moves the hue on, and a choice of the map
    // spreads its reflectance over the whole range and leaves the color to the map.
    private static void ToggleSpecularMap(Entity button)
    {
        if (!Radio.OptionValue<TintType>(button, out var selection)) return;

        _tintType = selection;
        if (!Render.TryReadMaterial(_material, out var settings)) return;

        if (_tintType == TintType.Solid)
        {
            settings.Reflectance = 1f;
            settings.SpecularTintTexture = AssetHandle.None;
            _hue += HueShiftSpeed;
            settings.SpecularTint = Color.FromHsl(_hue, 1f, 0.5f);
        }
        else
        {
            // A full alpha in a specular map counts as half, so two spreads it over the whole range,
            // and the tint is white, since the map is multiplied by it.
            settings.Reflectance = 2f;
            settings.SpecularTint = (1f, 1f, 1f, 1f);
            settings.SpecularTintTexture = _noise;
        }

        Render.WriteMaterial(_material, settings);
    }
}
