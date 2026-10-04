using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates how to combine baked and dynamic lighting, a chair in a room lit four ways, from all
// of it baked to all of it worked out each frame, with a sphere that moves where a click lands on
// the floor in the modes that light it as it goes.
internal static class MixedLighting
{
    private enum LightingMode { Baked, MixedDirect, MixedIndirect, RealTime }


    private const float LightmapExposure = 600f;
    private const float SphereOffset = 0.2f;
    private static readonly Vec3 InitialSpherePosition = new(0f, 0.5233223f, 0f);

    // Where each mesh's light sits in the shared lightmap, given from the bottom left as the tool
    // that baked it wrote them, and turned to Bevy's top left by UvRect.
    private static readonly Dictionary<string, (float X, float Y, float W, float H)> Lightmaps = new()
    {
        ["Plane"] = (0.026f, 0.026f, 0.710f, 0.710f),
        ["SheenChair_fabric"] = (0.7864f, 0.02377f, 0.1910f, 0.1912f),
        ["SheenChair_label"] = (0.275f, -0.016f, 0.858f, 0.486f),
        ["SheenChair_metal"] = (0.998f, 0.506f, -0.029f, -0.067f),
        ["SheenChair_wood"] = (0.787f, 0.257f, 0.179f, 0.177f),
    };
    private static readonly (float X, float Y, float W, float H) SphereRect = (0.788f, 0.484f, 0.062f, 0.062f);

    private static LightingMode _mode;
    private static RadioButtons<LightingMode>? _buttons;
    private static Entity _camera, _text, _scene = Entity.None;
    private static bool _ready;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            (_mode, _ready) = (LightingMode.MixedIndirect, false);

            // Bevy's clear color, the ambient light's too, bright enough to read on lightmapped meshes.
            var clear = Scene.Srgb8(43, 44, 47);
            Render.SetAmbientLight((clear.R, clear.G, clear.B), 10000f);
            _camera = ctx.Ecs.Camera(Transform.LookingAt(new Vec3(-0.7f, 0.7f, 1f), new Vec3(0f, 0.3f, 0f), Vec3.UnitY));

            _buttons = new RadioButtons<LightingMode>(ctx.Ecs, RadioButtons<LightingMode>.Column(), "Lighting",
                [(LightingMode.Baked, "Baked"), (LightingMode.MixedDirect, "Mixed (Direct)"), (LightingMode.MixedIndirect, "Mixed (Indirect)"), (LightingMode.RealTime, "Real-Time")],
                _mode);
            _text = Ui.SpawnText(HelpText(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.SpawnGltf("models/MixedLightingExample/MixedLightingExample.gltf", (_, root) => _scene = root);

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            if (_scene == Entity.None) return;

            // The scene's meshes come a frame or more after its root, and the first mode is taken
            // once they are all there, as Bevy takes it when the scene says it is ready.
            if (!_ready)
            {
                if (Meshes(ecs).Count() < Lightmaps.Count + 1) return;
                _ready = true;
                ChangeMode(ecs);
            }

            if (_buttons!.Pressed(out var mode) && mode != _mode)
            {
                _mode = mode;
                ChangeMode(ecs);
            }

            MoveSphere(ctx);
        }, "mixed_lighting.Update");
    }

    private static void ChangeMode(EcsWorld ecs)
    {
        _buttons!.Select(ecs, _mode);
        Ui.SetText(_text, HelpText());

        var image = _mode switch
        {
            LightingMode.Baked => "lightmaps/MixedLightingExample-Baked.zstd.ktx2",
            LightingMode.MixedDirect => "lightmaps/MixedLightingExample-MixedDirect.zstd.ktx2",
            LightingMode.MixedIndirect => "lightmaps/MixedLightingExample-MixedIndirect.zstd.ktx2",
            _ => null,
        };
        var lightmap = image is null ? AssetHandle.None : AssetServer.Load(AssetKind.Image, image);

        foreach (var (entity, name) in Meshes(ecs))
        {
            var material = Render.MaterialOf(ecs, entity);
            if (Render.TryReadMaterial(material, out var settings) && settings.LightmapExposure != LightmapExposure)
            {
                settings.LightmapExposure = LightmapExposure;
                Render.WriteMaterial(material, settings);
            }

            // The sphere keeps its baked light only while everything is baked, since anywhere else
            // it moves and the light baked where it started would go with it.
            var rect = name == "Sphere" ? SphereRect : Lightmaps[name];
            if (lightmap.IsValid && (name != "Sphere" || _mode == LightingMode.Baked))
            {
                var map = ecs.Insert<LightmapRef>(entity);
                map.Image = lightmap;
                (map.UvRectMin, map.UvRectMax) = UvRect(rect);
            }
            else
            {
                ecs.Wrap<LightmapRef>(entity).Remove();
            }
        }

        // The sun lights the scenery itself only where its direct light was not baked in.
        var realTime = _mode is LightingMode.MixedIndirect or LightingMode.RealTime;
        foreach (var entity in Descendants(ecs, _scene))
        {
            if (ecs.Get<DirectionalLightRef>(entity) is not { } sun) continue;
            (sun.AffectsLightmappedMeshDiffuse, sun.ShadowMapsEnabled) = (realTime, realTime);
        }

        if (_mode == LightingMode.Baked && Sphere(ecs) is var sphere && sphere != Entity.None)
        {
            var parent = ecs.ParentOf(sphere);
            var transform = ecs.GetOrDefault<Transform>(parent);
            transform.Translation = InitialSpherePosition;
            ecs.Set(parent, transform);
        }
    }

    // The sphere follows the pointer while the button is held, set down on whatever the pointer is
    // over, past the sphere itself, as Bevy's leaves the sphere out of picking.
    private static void MoveSphere(BehaviorContext ctx)
    {
        if (_mode == LightingMode.Baked || !App.HasEditor || !ctx.Input.MouseDown(MouseButton.Left)) return;

        var ecs = ctx.Ecs;
        var sphere = Sphere(ecs);
        if (sphere == Entity.None) return;

        var (x, y) = ctx.Input.MousePosition;
        if (!Render.TryRay(_camera, x, y, out var origin, out var direction)) return;

        for (var tries = 0; tries < 4; tries++)
        {
            if (!Picking.TryCast(origin, direction, out var hit, out var point, out _)) return;
            if (hit == sphere)
            {
                origin = point + direction * 1e-3f;
                continue;
            }

            var parent = ecs.ParentOf(sphere);
            var transform = ecs.GetOrDefault<Transform>(parent);
            transform.Translation = point + new Vec3(0f, SphereOffset, 0f);
            ecs.Set(parent, transform);
            return;
        }
    }

    // A rectangle given from the bottom left, as OpenGL counts, turned to Bevy's top left.
    private static (Vec2 Min, Vec2 Max) UvRect((float X, float Y, float W, float H) gl)
    {
        var min = new Vec2(gl.X, 1f - gl.Y - gl.H);
        return (min, new Vec2(min.X + gl.W, min.Y + gl.H));
    }

    private static Entity Sphere(EcsWorld ecs) => Meshes(ecs).FirstOrDefault(mesh => mesh.Name == "Sphere").Entity;

    // The meshes the lightmaps are for, by the names the file gives them.
    private static IEnumerable<(Entity Entity, string Name)> Meshes(EcsWorld ecs)
    {
        foreach (var entity in Descendants(ecs, _scene))
        {
            if (ecs.Get<GltfMeshNameRef>(entity) is not { } meshName) continue;
            var name = meshName.Value;
            if (name == "Sphere" || Lightmaps.ContainsKey(name)) yield return (entity, name);
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

    private static string HelpText() => _mode switch
    {
        LightingMode.Baked => "Scenery: Static, baked direct light, baked indirect light\nSphere: Static, baked direct light, baked indirect light",
        LightingMode.MixedDirect => "Scenery: Static, baked direct light, baked indirect light\nSphere: Dynamic, real-time direct light, no indirect light\nClick in the scene to move the sphere",
        LightingMode.MixedIndirect => "Scenery: Static, real-time direct light, baked indirect light\nSphere: Dynamic, real-time direct light, no indirect light\nClick in the scene to move the sphere",
        _ => "Scenery: Dynamic, real-time direct light, no indirect light\nSphere: Dynamic, real-time direct light, no indirect light\nClick in the scene to move the sphere",
    };
}
