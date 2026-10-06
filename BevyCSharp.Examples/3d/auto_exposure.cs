// Bevy's auto_exposure example, examples/3d/auto_exposure.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.ThreeD;

// This example showcases auto exposure, which automatically (but not instantly) adjusts the
// brightness of the scene in a way that mimics the function of the human eye.
internal static class AutoExposure
{
    private static Entity _camera, _display, _mask;
    private static AssetHandle _meteringMask;
    private static bool _curve, _masked;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_curve, _masked) = (false, true);
            _meteringMask = AssetServer.Load(AssetKind.Image, "textures/basic_metering_mask.png");

            _camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(1f, 0f, 0f), Vec3.Zero, Vec3.UnitY));
            Render.SetSkybox(_camera, AssetServer.Load(AssetKind.Image, "environment_maps/pisa_specular_rgb9e5_zstd.ktx2"), 100_000f);
            Apply();

            // Twelve walls in three rings, each a plane four wide and one high facing the middle,
            // one gap left to see the sky through.
            var plane = Render.CreateMesh(MeshShape.Plane, 4f, 1f);
            var standUp = Quat.FromRotationX(-MathF.PI / 2f);
            for (var level = -1; level <= 1; level++)
            {
                foreach (var side in new[] { -Vec3.UnitX, Vec3.UnitX, -Vec3.UnitZ, Vec3.UnitZ })
                {
                    if (level == 0 && side == Vec3.UnitZ) continue;

                    var height = Vec3.UnitY * level;
                    var facing = Transform.LookingAt(side * 2f + height, height, Vec3.UnitY);
                    facing.Rotation *= standUp;
                    var color = Color.FromSrgb(0.5f + side.X * 0.5f, 0.75f - level * 0.25f, 0.5f + side.Z * 0.5f);
                    ecs.SpawnMesh(plane, Render.CreateMaterial(new MaterialSettings { BaseColor = color }), facing);
                }
            }

            Render.SetAmbientLight((1f, 1f, 1f), 0f);
            ecs.SpawnPointLight(Vec3.Zero, intensity: 2000f);

            // The mask over the whole picture, shown while V is held.
            _mask = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Display = UiDisplay.None });
            Ui.SetImage(_mask, _meteringMask);

            Ui.SpawnText(
                "Left / Right - Rotate Camera\nC - Toggle Compensation Curve\nM - Toggle Metering Mask\nV - Visualize Metering Mask",
                new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
            _display = Ui.SpawnText(Display(), new UiSettings { Absolute = true, Top = Length.Px(12f), Right = Length.Px(12f) });
        });

        app.Update(ctx =>
        {
            var input = ctx.Input;
            var turn = input.KeyDown(Key.ArrowLeft) ? ctx.Time.Delta : input.KeyDown(Key.ArrowRight) ? -ctx.Time.Delta : 0f;
            if (turn != 0f)
            {
                var camera = ctx.Ecs.GetOrDefault<Transform>(_camera);
                var spin = Quat.FromRotationY(turn);
                (camera.Translation, camera.Rotation) = (spin * camera.Translation, spin * camera.Rotation);
                ctx.Ecs.Set(_camera, camera);
            }

            var changed = false;
            if (input.KeyPressed(Key.C)) { _curve = !_curve; changed = true; }
            if (input.KeyPressed(Key.M)) { _masked = !_masked; changed = true; }
            if (changed)
            {
                Apply();
                Ui.SetText(_display, Display());
            }

            if (input.KeyPressed(Key.V) || input.KeyReleased(Key.V))
                ctx.Ecs.Wrap<NodeRef>(_mask).Display = input.KeyDown(Key.V) ? NodeRef.DisplayVariant.Flex : NodeRef.DisplayVariant.None;
        }, "auto_exposure.ExampleControlSystem");
    }

    // Auto exposure with the mask and the curve each where they are turned on. The curve leaves the
    // exposure two stops darker in the dark, and two brighter in the bright.
    private static void Apply() => Render.SetEffects(_camera, new EffectSettings
    {
        AutoExposure = true,
        MeteringMask = _masked ? _meteringMask : AssetHandle.None,
        ExposureCompensation = _curve ? [(-4f, -2f), (0f, 0f), (2f, 0f), (4f, 2f)] : null,
    });

    private static string Display() =>
        $"Compensation Curve: {(_curve ? "Enabled" : "Disabled")}\nMetering Mask: {(_masked ? "Enabled" : "Disabled")}";
}
