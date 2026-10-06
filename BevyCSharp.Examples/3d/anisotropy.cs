// Bevy's anisotropy example, examples/3d/anisotropy.rs at v0.19.1, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

using Visibility = Bevy.Reflected.VisibilityRef.ValueVariant;

// Demonstrates anisotropy, the highlight stretched along a surface as on brushed metal, on a
// barn lamp loaded from glTF and on a sphere, under a directional light, a point light or an
// environment map, with each material's anisotropy turned on and off.
internal static class Anisotropy
{
    private static readonly Vec3 CameraStart = new(-0.4f, 0f, 0f);

    private enum LightMode
    {
        Directional,
        Point,
        EnvironmentMap,
    }

    private static Entity _camera, _light, _text;
    private static LightMode _mode;
    private static bool _anisotropic;
    private static AnisotropySceneKind _visibleScene;
    private static float _turned;

    public static void Build(App app)
    {
        (_mode, _anisotropic, _visibleScene, _turned) = (LightMode.Directional, true, AnisotropySceneKind.BarnLamp, 0f);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _camera = ecs.SpawnCamera3d(Transform.LookingAt(CameraStart, Vec3.Zero, Vec3.UnitY));
            _light = SpawnDirectional();

            // A sphere with tangents, which a primitive is made with, in Tailwind's gray-300.
            var gray = Color.FromHex("#d1d5db");
            var sphere = ecs.SpawnMesh(
                Render.CreateMesh(MeshShape.Sphere, 0.1f),
                Render.CreateMaterial(new MaterialSettings { BaseColor = (gray.R, gray.G, gray.B, 1f), AnisotropyRotation = 0.5f, AnisotropyStrength = 1f }),
                Transform.Identity);
            ecs.Wrap<VisibilityRef>(sphere).Value = Visibility.Hidden;
            ecs.Add(sphere, new AnisotropyScene { Kind = AnisotropySceneKind.Sphere });

            _text = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        }, "anisotropy.Setup");

        app.SpawnGltf("models/AnisotropyBarnLamp/AnisotropyBarnLamp.gltf", (ctx, root) =>
        {
            ctx.Ecs.Set(root, Transform.At(0f, 0.07f, -0.13f));
            ctx.Ecs.Add(root, new AnisotropyScene { Kind = AnisotropySceneKind.BarnLamp });
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
        foreach (var root in ecs.EntitiesWith<AnisotropyScene>())
        {
            foreach (var entity in ecs.Descendants(root).Prepend(root).ToArray())
            {
                if (ecs.Has<MaterialVariants>(entity)) continue;
                var material = Render.MaterialOf(ecs, entity);
                if (material == AssetHandle.None || !Render.TryReadMaterial(material, out var settings) || settings is null) continue;

                settings.AnisotropyTexture = AssetHandle.None;
                settings.AnisotropyStrength = 0f;
                settings.AnisotropyRotation = 0f;
                ecs.Add(entity, new MaterialVariants { Anisotropic = material, Isotropic = Render.CreateMaterial(settings) });
            }
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
            foreach (var entity in ecs.EntitiesWith<MaterialVariants>())
            {
                var variants = ecs.GetOrDefault<MaterialVariants>(entity);
                Render.SetMaterial(ecs, entity, _anisotropic ? variants.Anisotropic : variants.Isotropic);
            }
        }

        if (input.KeyPressed(Key.Q))
        {
            changed = true;
            _visibleScene = _visibleScene == AnisotropySceneKind.BarnLamp ? AnisotropySceneKind.Sphere : AnisotropySceneKind.BarnLamp;
            foreach (var scene in ecs.EntitiesWith<AnisotropyScene>())
                ecs.Wrap<VisibilityRef>(scene).Value = ecs.GetOrDefault<AnisotropyScene>(scene).Kind == _visibleScene ? Visibility.Inherited : Visibility.Hidden;
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
        return $"{material}\n{light}\nPress Q to change to {(_visibleScene == AnisotropySceneKind.Sphere ? "Barn Lamp" : "Sphere")}";
    }
}

/// <summary>Which of the two scenes a thing is.</summary>
public enum AnisotropySceneKind { BarnLamp, Sphere }

/// <summary>
/// A scene Q shows in turn, the barn lamp or the sphere, Bevy's <c>Scene</c> enum under another
/// name since the examples' own <c>Scene</c> helper is in every example's scope.
/// </summary>
[Behavior]
public partial struct AnisotropyScene
{
    /// <summary>Which scene.</summary>
    public AnisotropySceneKind Kind;
}

/// <summary>A mesh's material as the file has it, with anisotropy, and the same with it taken out, for Enter to switch between.</summary>
[Behavior]
public partial struct MaterialVariants
{
    /// <summary>The material as the file has it.</summary>
    public AssetHandle Anisotropic;

    /// <summary>The same with no anisotropy.</summary>
    public AssetHandle Isotropic;
}
