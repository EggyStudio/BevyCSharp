// Bevy's pccm example, examples/3d/pccm.rs at v0.20.0, by Bevy's contributors under MIT or
// Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// Demonstrates parallax-corrected cubemap reflections. A mirror-like slab sits inside a box whose
// walls a reflection probe was captured in, and with the correction on, the walls the slab
// reflects line up with the walls around it.
internal static class Pccm
{
    private const float EnvironmentMapIntensity = 100f;

    private enum PccmEnableStatus { Enabled, Disabled }

    private static Entity _probe;

    public static void Configure(Config config) => (config.CameraControllers, config.Feathers) = (CameraControllers.Free, true);

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            Theme.BasicExampleTheme(Color.White);

            var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 0f, 4f), new Vec3(0f, -2.5f, 0f), Vec3.UnitY));
            Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
            Render.SetFreeCamera(camera, new FreeCameraSettings());

            // Bevy's cuboid of half size 5 by 1 by 2, a perfect mirror.
            var slab = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (1f, 1f, 1f, 1f),
                Metallic = 1f,
                Reflectance = 1f,
                Roughness = 0f,
            });
            ecs.Add(ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 10f, 2f, 4f), slab, Transform.At(0f, -4f, -2.5f)), new InnerCube());

            // The probe's box is its transform, the room's size, and the maps were captured in it.
            _probe = ecs.Spawn();
            ecs.Add(_probe, new Transform(Vec3.Zero, Quat.Identity, new Vec3(10.01f, 10.01f, 10.01f)));
            Render.SetReflectionProbe(
                _probe,
                AssetServer.Load(AssetKind.Image, "pccm_example/env_diffuse.ktx2"),
                AssetServer.Load(AssetKind.Image, "pccm_example/env_specular.ktx2"),
                EnvironmentMapIntensity);

            var row = Radio.FeathersOptionButtons(ecs, Radio.MainUiNode(), "Parallax Correction",
                [(PccmEnableStatus.Enabled, "On"), (PccmEnableStatus.Disabled, "Off")], 0);
            Ui.SelfUpdate(row, UiWidgetKind.RadioGroup);
            ecs.Observe<ValueChange<Entity>>(on => HandlePccmEnableChange(on.Ecs, on.Event.Value));
        }, "pccm.Setup");

        app.SpawnGltf("pccm_example/outer_cube.glb");
    }

    // The probe's ParallaxCorrection set to the variant chosen.
    private static void HandlePccmEnableChange(EcsWorld ecs, Entity button)
    {
        if (!Radio.OptionValue<PccmEnableStatus>(button, out var status)) return;
        ParallaxCorrection wanted = status == PccmEnableStatus.Enabled ? new ParallaxCorrection.Auto() : new ParallaxCorrection.None();
        (ecs.Get<ParallaxCorrectionRef>(_probe) ?? ecs.Insert<ParallaxCorrectionRef>(_probe)).Value = wanted;
    }
}

/// <summary>The mirrored slab inside the room, which the probe's reflections are seen in.</summary>
[Behavior]
public partial struct InnerCube;
