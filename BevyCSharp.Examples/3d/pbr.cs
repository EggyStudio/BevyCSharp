using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// This example shows how to configure Physically Based Rendering (PBR) parameters.
//
// Bevy's camera sizes its orthographic view by the window, a hundredth of a unit a pixel, which
// at 1280 by 720 is 7.2 units high, and that is the height given here at any size. Its "Metallic"
// label is turned a quarter turn by Bevy's UiTransform, put on through reflection.
internal static class Pbr
{
    private static Entity _label;
    private static AssetHandle _diffuse;
    private static AssetHandle _specular;

    public static void Build(App app)
    {
        app.Startup(Setup);
        app.Update(EnvironmentMapLoadFinish, "pbr.EnvironmentMapLoadFinish");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var sphere = Render.CreateMesh(MeshShape.Sphere, 0.45f);
        var gold = Color.FromHex("#ffd891");

        // Metallic rising up the grid and roughness along it.
        for (var y = -2; y <= 2; y++)
        {
            for (var x = -5; x <= 5; x++)
            {
                var material = Render.CreateMaterial(new MaterialSettings
                {
                    BaseColor = (gold.R, gold.G, gold.B, 1f),
                    Metallic = (y + 2) / 4f,
                    Roughness = (x + 5) / 10f,
                });
                ecs.Mesh(sphere, material, Transform.At(x, y + 0.5f, 0f));
            }
        }

        ecs.Mesh(sphere, Render.CreateMaterial(new MaterialSettings { BaseColor = (gold.R, gold.G, gold.B, 1f), Unlit = true }), Transform.At(-5f, -2.5f, 0f));

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 1500f, Shadows = false });
        ecs.Add(sun, Transform.LookingAt(new Vec3(50f, 50f, 50f), Vec3.Zero, Vec3.UnitY));

        var large = new UiTextSettings { FontSize = 30f };
        Ui.SpawnText("Perceptual Roughness", new UiSettings { Absolute = true, Top = Length.Px(20f), Left = Length.Px(100f) }, large);
        var metallic = Ui.SpawnText("Metallic", new UiSettings { Absolute = true, Top = Length.Px(130f), Right = Length.Px(0f) }, large);
        var turned = ecs.Insert<UiTransformRef>(metallic);
        (turned.RotationCos, turned.RotationSin) = (0f, 1f);
        _label = Ui.SpawnText("Loading Environment Map...", new UiSettings { Absolute = true, Bottom = Length.Px(20f), Right = Length.Px(20f) }, large);

        var camera = ecs.Camera(
            Transform.LookingAt(new Vec3(0f, 0f, 8f), Vec3.Zero, Vec3.UnitY),
            new CameraSettings { Projection = CameraProjection.Orthographic, Height = 7.2f });

        _diffuse = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2");
        _specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
        Render.SetEnvironmentMap(camera, _diffuse, _specular, 900f);
    }

    // The label goes once both maps have loaded.
    private static void EnvironmentMapLoadFinish(BehaviorContext ctx)
    {
        if (_label.IsNone || AssetServer.StateOf(_diffuse) != AssetLoadState.Loaded || AssetServer.StateOf(_specular) != AssetLoadState.Loaded) return;

        ctx.Ecs.Despawn(_label);
        _label = Entity.None;
    }
}
