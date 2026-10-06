// Bevy's volumetric_fog example, examples/3d/volumetric_fog.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates volumetric fog and lighting (light shafts or god rays), Bevy's VolumetricFog,
// FogVolume and VolumetricLight put on through reflection.
internal static class VolumetricFog
{
    private const float LightSpeed = 0.01f;

    private static Entity _root, _sun, _point, _spot, _text;
    private static bool _volumetricPoint = true, _volumetricSpot = true;
    private static float _pointSpeed = -0.2f;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_root, _sun, _volumetricPoint, _volumetricSpot, _pointSpeed) = (Entity.None, Entity.None, true, true, -0.2f);
            Render.SetClearColor(Color.FromSrgb(0.02f, 0.02f, 0.02f));
            Render.SetAmbientLight((1f, 1f, 1f), 0f);

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(-1.7f, 1.5f, 4.5f), new Vec3(-1.5f, 1.7f, 3.5f), Vec3.UnitY));
            Render.SetPostProcessing(camera, new PostSettings { Bloom = true });
            Render.SetSkybox(camera, AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"), 1000f);
            ecs.Insert<VolumetricFogRef>(camera).AmbientIntensity = 0f;

            _point = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 10_000f, Range = 150f, Color = (1f, 0f, 0f), Shadows = true });
            ecs.Add(_point, Transform.At(-0.4f, 1.9f, 1f));
            ecs.Insert<VolumetricLightRef>(_point);

            _spot = Render.SpawnLight(new LightSettings { Kind = LightKind.Spot, Intensity = 50_000f, Shadows = true, InnerAngle = 0.76f, OuterAngle = 0.94f });
            ecs.Add(_spot, Transform.LookingAt(new Vec3(-1.8f, 3.9f, -2.7f), Vec3.Zero, Vec3.UnitY));
            ecs.Insert<VolumetricLightRef>(_spot);

            var fog = ecs.Spawn();
            ecs.Add(fog, new Transform(Vec3.Zero, Quat.Identity, new Vec3(35f)));
            ecs.Insert<FogVolumeRef>(fog);

            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.SpawnGltf("models/VolumetricFogExample/VolumetricFogExample.glb", (_, root) => _root = root);

        app.Update(ctx =>
        {
            var ecs = ctx.Ecs;
            var input = ctx.Input;

            // The scene's own sun, given shadows and light shafts once the scene has spawned it.
            if (_sun.IsNone && !_root.IsNone && Find(ecs, _root) is { IsNone: false } sun)
            {
                _sun = sun;
                ecs.Wrap<DirectionalLightRef>(sun).ShadowMapsEnabled = true;
                ecs.Insert<VolumetricLightRef>(sun);
            }

            if (!_sun.IsNone)
            {
                var pitch = (input.KeyDown(Key.W) || input.KeyDown(Key.ArrowUp) ? LightSpeed : 0f) - (input.KeyDown(Key.S) || input.KeyDown(Key.ArrowDown) ? LightSpeed : 0f);
                var yaw = (input.KeyDown(Key.A) || input.KeyDown(Key.ArrowLeft) ? LightSpeed : 0f) - (input.KeyDown(Key.D) || input.KeyDown(Key.ArrowRight) ? LightSpeed : 0f);
                if (pitch != 0f || yaw != 0f)
                {
                    // Bevy's XZY order, X then Z then Y.
                    var transform = ecs.GetOrDefault<Transform>(_sun);
                    transform.Rotation = Quat.FromRotationX(pitch) * Quat.FromRotationY(yaw) * transform.Rotation;
                    ecs.Set(_sun, transform);
                }
            }

            // The red light goes back and forth along X.
            var point = ecs.GetOrDefault<Transform>(_point);
            var x = point.Translation.X + _pointSpeed * ctx.Time.Delta;
            if (x > -0.4f || x < -1.93f) (x, _pointSpeed) = (Math.Clamp(x, -1.93f, -0.4f), -_pointSpeed);
            point.Translation = point.Translation with { X = x };
            ecs.Set(_point, point);

            var changed = false;
            if (input.KeyPressed(Key.P)) { _volumetricPoint = !_volumetricPoint; Toggle(ecs, _point, _volumetricPoint); changed = true; }
            if (input.KeyPressed(Key.L)) { _volumetricSpot = !_volumetricSpot; Toggle(ecs, _spot, _volumetricSpot); changed = true; }
            if (changed) Ui.SetText(_text, Text());
        }, "volumetric_fog.Update");
    }

    private static Entity Find(EcsWorld ecs, Entity entity)
    {
        if (ecs.Get<DirectionalLightRef>(entity) is not null) return entity;
        foreach (var child in ecs.ChildrenOf(entity))
            if (Find(ecs, child) is { IsNone: false } found) return found;
        return Entity.None;
    }

    private static void Toggle(EcsWorld ecs, Entity light, bool on)
    {
        if (on) ecs.Insert<VolumetricLightRef>(light);
        else ecs.Wrap<VolumetricLightRef>(light).Remove();
    }

    private static string Text() =>
        "Press WASD or the arrow keys to change the direction of the directional light\n"
        + (_volumetricPoint ? "Press P to turn volumetric point light off\n" : "Press P to turn volumetric point light on\n")
        + (_volumetricSpot ? "Press L to turn volumetric spot light off" : "Press L to turn volumetric spot light on");
}
