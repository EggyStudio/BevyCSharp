// Bevy's pccm example, examples/3d/pccm.rs at v0.19.1, by Bevy's contributors under MIT or
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

    private static Entity _probe;
    private static bool _enabled;
    private static RadioButtons<bool>? _buttons;

    public static void Build(App app)
    {
        _enabled = true;

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;

            var camera = ecs.Camera(Transform.LookingAt(new Vec3(0f, 0f, 4f), new Vec3(0f, -2.5f, 0f), Vec3.UnitY));
            Render.SetPostProcessing(camera, new PostSettings { Hdr = true });
            ecs.Add(camera, new FreeCamera());

            // Bevy's cuboid of half size 5 by 1 by 2, a perfect mirror.
            var slab = Render.CreateMaterial(new MaterialSettings
            {
                BaseColor = (1f, 1f, 1f, 1f),
                Metallic = 1f,
                Reflectance = 1f,
                Roughness = 0f,
            });
            ecs.Mesh(Render.CreateMesh(MeshShape.Cuboid, 10f, 2f, 4f), slab, Transform.At(0f, -4f, -2.5f));

            // The probe's box is its transform, the room's size, and the maps were captured in it.
            _probe = ecs.Spawn();
            ecs.Add(_probe, new Transform(Vec3.Zero, Quat.Identity, new Vec3(10.01f, 10.01f, 10.01f)));
            Render.SetReflectionProbe(
                _probe,
                AssetServer.Load(AssetKind.Image, "pccm_example/env_diffuse.ktx2"),
                AssetServer.Load(AssetKind.Image, "pccm_example/env_specular.ktx2"),
                EnvironmentMapIntensity);

            _buttons = new RadioButtons<bool>(ecs, RadioButtons<bool>.Column(), "Parallax Correction",
                [(true, "On"), (false, "Off")], _enabled);
        }, "pccm.Setup");

        app.SpawnGltf("pccm_example/outer_cube.glb");

        app.Update(ctx =>
        {
            if (_buttons is not null && _buttons.Pressed(out var enabled) && enabled != _enabled)
            {
                _enabled = enabled;
                _buttons.Select(ctx.Ecs, _enabled);
            }

            // Bevy gives a probe ParallaxCorrection::Auto itself once its maps have loaded and it
            // is a probe, so the choice is that component's variant, set once it is there.
            ParallaxCorrection wanted = _enabled ? new ParallaxCorrection.Auto() : new ParallaxCorrection.None();
            if (ctx.Ecs.Get<ParallaxCorrectionRef>(_probe) is { } correction && correction.Value != wanted)
                correction.Value = wanted;
        }, "pccm.HandlePccmEnableChange");
    }
}
