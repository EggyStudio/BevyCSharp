// Bevy's pbr example, examples/3d/pbr.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

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
    // The camera's environment maps, which Bevy reads off the camera's EnvironmentMapLight.
    internal static AssetHandle Diffuse, Specular;

    public static void Build(App app) => app.Startup(Setup, "pbr.Setup");

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
                ecs.SpawnMesh(sphere, material, Transform.At(x, y + 0.5f, 0f));
            }
        }

        ecs.SpawnMesh(sphere, Render.CreateMaterial(new MaterialSettings { BaseColor = (gold.R, gold.G, gold.B, 1f), Unlit = true }), Transform.At(-5f, -2.5f, 0f));

        var sun = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Intensity = 1500f, Shadows = false });
        ecs.Add(sun, Transform.LookingAt(new Vec3(50f, 50f, 50f), Vec3.Zero, Vec3.UnitY));

        var large = new UiTextSettings { FontSize = 30f };
        Ui.SpawnText("Perceptual Roughness", new UiSettings { Absolute = true, Top = Length.Px(20f), Left = Length.Px(100f) }, large);
        var metallic = Ui.SpawnText("Metallic", new UiSettings { Absolute = true, Top = Length.Px(130f), Right = Length.Px(0f) }, large);
        var turned = ecs.Insert<UiTransformRef>(metallic);
        (turned.RotationCos, turned.RotationSin) = (0f, 1f);
        var label = Ui.SpawnText("Loading Environment Map...", new UiSettings { Absolute = true, Bottom = Length.Px(20f), Right = Length.Px(20f) }, large);
        ecs.Add(label, new EnvironmentMapLabel());

        var camera = ecs.SpawnCamera3d(
            Transform.LookingAt(new Vec3(0f, 0f, 8f), Vec3.Zero, Vec3.UnitY),
            new CameraSettings { Projection = CameraProjection.Orthographic, Height = 7.2f });

        Diffuse = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_diffuse_rgb9e5_zstd.ktx2");
        Specular = AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2");
        Render.SetEnvironmentMap(camera, Diffuse, Specular, 900f);
    }
}

/// <summary>The label saying the environment map is loading.</summary>
[Behavior]
public partial struct EnvironmentMapLabel
{
    /// <summary>Despawned by a command once both maps have loaded.</summary>
    [OnUpdate]
    public void EnvironmentMapLoadFinish(BehaviorContext ctx)
    {
        if (AssetServer.StateOf(Pbr.Diffuse) == AssetLoadState.Loaded && AssetServer.StateOf(Pbr.Specular) == AssetLoadState.Loaded)
            ctx.Cmd.Despawn(ctx.Entity);
    }
}
