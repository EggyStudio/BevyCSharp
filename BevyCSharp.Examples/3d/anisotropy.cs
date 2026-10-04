using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates anisotropy, the highlight stretched along a surface as on brushed metal, on a
// barn lamp loaded from glTF and on a sphere, under a directional light, a point light or an
// environment map, with each material's anisotropy turned on and off.
internal static class Anisotropy
{
    private const string VisibilityType = "bevy_camera::visibility::Visibility";
    private static readonly Vec3 CameraStart = new(-0.4f, 0f, 0f);

    private enum LightMode
    {
        Directional,
        Point,
        EnvironmentMap,
    }

    private static Entity _camera, _light, _lamp, _sphere, _text;
    private static LightMode _mode;
    private static bool _anisotropic, _sphereShown;
    private static float _turned;

    // Each mesh's material and the same material with no anisotropy, made as the meshes appear.
    private static readonly Dictionary<Entity, (AssetHandle Anisotropic, AssetHandle Isotropic)> Variants = [];

    public static void Build(App app)
    {
        Variants.Clear();
        (_mode, _anisotropic, _sphereShown, _turned, _lamp) = (LightMode.Directional, true, false, 0f, Entity.None);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _camera = ecs.Camera(Transform.LookingAt(CameraStart, Vec3.Zero, Vec3.UnitY));
            _light = SpawnDirectional();

            // A sphere with tangents, which a primitive is made with, in Tailwind's gray-300.
            var gray = Color.FromHex("#d1d5db");
            _sphere = ecs.Mesh(
                Render.CreateMesh(MeshShape.Sphere, 0.1f),
                Render.CreateMaterial(new MaterialSettings { BaseColor = (gray.R, gray.G, gray.B, 1f), AnisotropyRotation = 0.5f, AnisotropyStrength = 1f }),
                Transform.Identity);
            ecs.SetVariant(_sphere, VisibilityType, "", "Hidden");

            _text = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        }, "anisotropy.Setup");

        app.SpawnGltf("models/AnisotropyBarnLamp/AnisotropyBarnLamp.gltf", (ctx, root) =>
        {
            _lamp = root;
            ctx.Ecs.Set(root, Transform.At(0f, 0.07f, -0.13f));
        });

        app.Update(CreateMaterialVariants, "anisotropy.CreateMaterialVariants");
        app.Update(ctx =>
        {
            // Round the scene at a radius of three and a height of four, facing the middle.
            var now = ctx.Time.Elapsed;
            var at = new Vec3(MathF.Cos(now) * 3f, 4f, MathF.Sin(now) * 3f);
            if (ctx.Ecs.IsAlive(_light)) ctx.Ecs.Set(_light, Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY));
        }, "anisotropy.AnimateLight");
        app.Update(ctx =>
        {
            // The camera turns about the middle only while the environment map lights the scene.
            if (_mode == LightMode.EnvironmentMap) _turned += ctx.Time.Delta;
            ctx.Ecs.Set(_camera, Transform.LookingAt(Quat.FromRotationY(_turned) * CameraStart, Vec3.Zero, Vec3.UnitY));
        }, "anisotropy.RotateCamera");
        app.Update(HandleInput, "anisotropy.HandleInput");
    }

    private static Entity SpawnDirectional() =>
        Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 3000f, Shadows = false });

    // As Bevy's create_material_variants, a copy of each mesh's material with the anisotropy taken
    // out, for Enter to switch to.
    private static void CreateMaterialVariants(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var root in new[] { _lamp, _sphere })
            if (root != Entity.None) Walk(root);

        void Walk(Entity entity)
        {
            if (!Variants.ContainsKey(entity)
                && Render.MaterialOf(ecs, entity) is var material && material != AssetHandle.None
                && Render.TryReadMaterial(material, out var settings) && settings is not null)
            {
                settings.AnisotropyTexture = AssetHandle.None;
                settings.AnisotropyStrength = 0f;
                settings.AnisotropyRotation = 0f;
                Variants[entity] = (material, Render.CreateMaterial(settings));
            }

            foreach (var child in ecs.ChildrenOf(entity)) Walk(child);
        }
    }

    private static void HandleInput(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var input = ctx.Input;
        var changed = false;

        if (input.KeyPressed(Key.Space))
        {
            changed = true;
            switch (_mode)
            {
                case LightMode.Directional:
                    _mode = LightMode.Point;
                    ecs.Despawn(_light);
                    _light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 200_000f, Shadows = false });
                    ecs.Add(_light, Transform.Identity);
                    break;
                case LightMode.Point:
                    _mode = LightMode.EnvironmentMap;
                    ecs.Despawn(_light);
                    var specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
                    Render.SetSkybox(_camera, specular, 5000f);
                    Render.SetEnvironmentMap(_camera, AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"), specular, 2500f);
                    break;
                default:
                    _mode = LightMode.Directional;
                    Render.SetSkybox(_camera, AssetHandle.None);
                    Render.SetEnvironmentMap(_camera, AssetHandle.None, AssetHandle.None);
                    _light = SpawnDirectional();
                    break;
            }
        }

        if (input.KeyPressed(Key.Enter))
        {
            changed = true;
            _anisotropic = !_anisotropic;
            foreach (var (entity, (anisotropic, isotropic)) in Variants)
                if (ecs.IsAlive(entity)) Render.SetMaterial(ecs, entity, _anisotropic ? anisotropic : isotropic);
        }

        if (input.KeyPressed(Key.Q))
        {
            changed = true;
            _sphereShown = !_sphereShown;
            ecs.SetVariant(_sphere, VisibilityType, "", _sphereShown ? "Inherited" : "Hidden");
            if (_lamp != Entity.None) ecs.SetVariant(_lamp, VisibilityType, "", _sphereShown ? "Hidden" : "Inherited");
        }

        if (changed) Ui.SetText(_text, HelpText());
    }

    private static string HelpText()
    {
        var material = _anisotropic ? "Press Enter to disable anisotropy" : "Press Enter to enable anisotropy";
        var light = _mode switch
        {
            LightMode.Directional => "Press Space to switch to a point light",
            LightMode.Point => "Press Space to switch to an environment map",
            _ => "Press Space to switch to a directional light",
        };
        return $"{material}\n{light}\nPress Q to change to {(_sphereShown ? "Barn Lamp" : "Sphere")}";
    }
}
