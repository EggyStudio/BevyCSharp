// Bevy's specular_tint example, examples/3d/specular_tint.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// A black, mirror-smooth sphere that shows only what it reflects of an environment map, its
// reflection tinted by one color or by a noise texture, chosen by two buttons at the bottom left,
// while the camera goes round it.
//
// Bevy draws the buttons with Feathers. Here they are the examples' own row of radio buttons, as
// Bevy's examples drew them before Feathers.
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
    private static RadioButtons<TintType>? _buttons;

    public static void Build(App app)
    {
        (_tintType, _hue, _buttons) = (TintType.Solid, 0f, null);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
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

            _buttons = new RadioButtons<TintType>(ecs, RadioButtons<TintType>.Column(), "Toggle specular tint",
                [(TintType.Solid, "SOLID"), (TintType.Map, "MAP")], _tintType);
        }, "specular_tint.Setup");

        app.Update(ctx =>
        {
            var transform = ctx.Ecs.GetOrDefault<Transform>(_camera);
            ctx.Ecs.Set(_camera, Transform.LookingAt(Quat.FromRotationY(RotationSpeed) * transform.Translation, Vec3.Zero, Vec3.UnitY));
        }, "specular_tint.RotateCamera");

        app.Update(ToggleSpecularMap, "specular_tint.ToggleSpecularMap");
    }

    // A choice of the solid tint takes the map away and moves the hue on, and a choice of the map
    // spreads its reflectance over the whole range and leaves the color to the map.
    private static void ToggleSpecularMap(BehaviorContext ctx)
    {
        // A button reads as pressed for as long as it is held, and Bevy's radio group says nothing
        // of the one already chosen.
        if (_buttons is null || !_buttons.Pressed(out var selection) || selection == _tintType) return;

        _tintType = selection;
        _buttons.Select(ctx.Ecs, selection);
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
