// Bevy's clearcoat example, examples/3d/clearcoat.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates the clearcoat PBR feature: car paint, a coated glass bubble, a golf ball and
// scratched gold, each turning under a light that circles them. Space swaps the point light for a
// directional one and back, which here is spawning the other kind where the first was.
internal static class Clearcoat
{
    private const float SphereScale = 0.9f;

    private static Entity _light, _text;
    private static bool _directional;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            _directional = false;
            var sphere = Render.CreateMesh(MeshShape.Sphere, 1f);

            Ball(ecs, sphere, new MaterialSettings
            {
                BaseColor = (0f, 0f, 1f, 1f),
                Metallic = 0.9f,
                Roughness = 0.5f,
                Clearcoat = 1f,
                ClearcoatRoughness = 0.1f,
                NormalMap = AssetServer.LoadImage("textures/BlueNoise-Normal.png", TextureSettings.Data),
            }, new Vec3(-1f, 1f, 0f));

            Ball(ecs, sphere, new MaterialSettings
            {
                BaseColor = Color.FromSrgb(0.9f, 0.9f, 0.9f, 0.3f),
                Metallic = 0.5f,
                Roughness = 0.1f,
                Clearcoat = 1f,
                ClearcoatRoughness = 0.1f,
                AlphaMode = AlphaMode.Blend,
            }, new Vec3(-1f, -1f, 0f));

            Ball(ecs, sphere, new MaterialSettings
            {
                BaseColor = Color.FromSrgb8(255, 215, 0),
                Metallic = 0.9f,
                Roughness = 0.1f,
                Clearcoat = 1f,
                ClearcoatRoughness = 0.3f,
                ClearcoatNormalTexture = AssetServer.LoadImage("textures/ScratchedGold-Normal.png", TextureSettings.Data),
            }, new Vec3(1f, -1f, 0f));

            _light = Light(ecs, Transform.Identity);

            var camera = ecs.SpawnCamera3d(Transform.At(0f, 0f, 10f), new CameraSettings { FieldOfView = 27f });
            Render.SetPostProcessing(camera, new PostSettings { Hdr = true, Tonemapper = Tonemapper.AcesFitted });
            var specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
            Render.SetSkybox(camera, specular, 5000f);
            Render.SetEnvironmentMap(camera, AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2"), specular, 2000f);

            _text = Ui.SpawnText(Text(), new UiSettings { Absolute = true, Bottom = Length.Px(12f), Left = Length.Px(12f) });
        });

        app.SpawnGltf("models/GolfBall/GolfBall.glb", (ctx, root) =>
        {
            ctx.Ecs.Set(root, new Transform(new Vec3(1f, 1f, 0f), Quat.Identity, new Vec3(SphereScale)));
            ctx.Ecs.Add(root, new ExampleSphere());
        });

        app.Update(ctx =>
        {
            // The light on a path round the balls, always facing them.
            var now = ctx.Time.Elapsed;
            var at = new Vec3(MathF.Sin(now * 1.4f) * 3f, MathF.Cos(now) * 4f, MathF.Cos(now * 0.6f) * 3f);
            ctx.Ecs.Set(_light, Transform.LookingAt(at, Vec3.Zero, Vec3.UnitY));

            if (!ctx.Input.KeyPressed(Key.Space)) return;

            _directional = !_directional;
            var where = ctx.Ecs.GetOrDefault<Transform>(_light);
            ctx.Ecs.Despawn(_light);
            _light = Light(ctx.Ecs, where);
            Ui.SetText(_text, Text());
        }, "clearcoat.Update");
    }

    private static void Ball(EcsWorld ecs, AssetHandle sphere, MaterialSettings material, Vec3 at)
    {
        var ball = ecs.SpawnMesh(sphere, Render.CreateMaterial(material), new Transform(at, Quat.Identity, new Vec3(SphereScale)));
        ecs.Add(ball, new ExampleSphere());
    }

    private static Entity Light(EcsWorld ecs, Transform at)
    {
        var light = Render.SpawnLight(_directional
            ? new LightSettings { Kind = LightKind.Directional, Intensity = 1000f, Shadows = false }
            : new LightSettings { Kind = LightKind.Point, Intensity = 100_000f, Shadows = false });
        ecs.Add(light, at);
        return light;
    }

    private static string Text() =>
        _directional ? "Press Space to switch to a point light" : "Press Space to switch to a directional light";
}

/// <summary>One of the balls, which turn about Y together.</summary>
[Behavior]
public partial struct ExampleSphere
{
    [OnUpdate]
    public void Turn(BehaviorContext ctx, ref Transform transform) =>
        transform.Rotation = Quat.FromRotationY(0.8f * ctx.Time.Elapsed);
}
