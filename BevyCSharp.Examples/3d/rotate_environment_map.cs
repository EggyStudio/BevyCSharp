using Bevy;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates how to rotate the skybox and the environment map simultaneously.
internal static class RotateEnvironmentMap
{
    private static Entity _camera;
    private static AssetHandle _diffuse, _specular;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            // A gold ball under a scratched clearcoat, whose normal map is data and not color.
            var gold = Scene.Srgb8(255, 215, 0);
            ecs.Mesh(
                Render.CreateMesh(MeshShape.Sphere, 1f),
                Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = gold,
                    Metallic = 0.9f,
                    Roughness = 0.1f,
                    Clearcoat = 1f,
                    ClearcoatRoughness = 0.3f,
                    ClearcoatNormalTexture = AssetServer.LoadImage("textures/ScratchedGold-Normal.png", TextureSettings.Data),
                }),
                new Transform(Vec3.Zero, Quat.Identity, new Vec3(1.25f)));

            Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 100_000f, Shadows = false });

            _camera = ecs.Camera(Transform.At(0f, 0f, 10f), new CameraSettings { FieldOfView = 27f });
            Render.SetPostProcessing(_camera, new PostSettings { Hdr = true, Tonemapper = Tonemapper.AcesFitted });

            _diffuse = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2");
            _specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
        });

        // The sky and the light it gives turned together, a fifth of a radian a second.
        app.Update(ctx =>
        {
            var rotation = Quat.FromRotationY(0.2f * ctx.Time.Elapsed);
            Render.SetSkybox(_camera, _specular, 5000f, rotation);
            Render.SetEnvironmentMap(_camera, _diffuse, _specular, 2000f, rotation);
        }, "rotate_environment_map.Rotate");
    }
}
