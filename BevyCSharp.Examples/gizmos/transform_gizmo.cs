// Bevy's transform_gizmo example, examples/gizmos/transform_gizmo.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Gizmo;

using Mode = Bevy.Reflected.TransformGizmoSettingsRef.ModeVariant;
using Space = Bevy.Reflected.TransformGizmoSettingsRef.SpaceVariant;

// Bevy's interactive transform gizmo on a table and a cube. A click selects one, and the gizmo's
// arrows, rings and handles move, turn or scale it, along the world's axes or its own. The table's
// legs are its children, so they go where it goes and local axes turn with it. The buttons at the
// bottom left choose what the gizmo does and along which axes, and beside them while it scales a
// slider sets how fast. WASD, E and Q fly the camera, which the mouse turns while its left button
// is held, as Bevy's free camera does.
internal static class TransformGizmoExample
{
    private static Entity _sensitivityRow;

    // Bevy's TransformGizmoPlugin and MeshPickingPlugin, the gizmo and the clicks that select.
    public static void Configure(Config config) =>
        (config.TransformGizmo, config.MeshPicking, config.CameraControllers, config.Feathers) = (true, true, CameraControllers.Free, true);

    public static void Build(App app)
    {
        app.Startup(Setup, "transform_gizmo.Setup");
        app.Update(ToggleScaleSlider, "transform_gizmo.ToggleScaleSlider");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Theme.BasicExampleTheme(Color.White);

        var column = Radio.MainUiNode();
        Feathers.Label(column, "Click an object to select it");
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, string.Empty,
            [(Mode.Translate, "Translate"), (Mode.Rotate, "Rotate"), (Mode.Scale, "Scale")], 0), UiWidgetKind.RadioGroup);
        Ui.SelfUpdate(Radio.FeathersOptionButtons(ecs, column, string.Empty,
            [(Space.World, "World"), (Space.Local, "Local")], 0), UiWidgetKind.RadioGroup);
        ecs.Observe<ValueChange<Entity>>(on => UpdateRadioButton(on.Ecs, on.Event.Value));
        SpawnSensitivitySlider(ecs, column);

        // The ground, which no click selects.
        var ground = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Plane, 10f, 10f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.3f, 0.3f)), Transform.Identity);
        Unpickable(ecs, ground);

        // The table, a top with four legs under it, the top selected to start with.
        var table = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1.5f, 0.15f, 1f), Render.CreateMaterial(Color.FromSrgb(0.8f, 0.3f, 0.3f)), Transform.At(-2f, 1f, 0f));
        ecs.Insert<TransformGizmoFocusRef>(table);
        ecs.Observe<Pointer<Click>>(table, OnClickSelect);

        var leg = Render.CreateMesh(MeshShape.Cuboid, 0.1f, 0.85f, 0.1f);
        var legMaterial = Render.CreateMaterial(Color.FromSrgb(0.6f, 0.2f, 0.2f));
        foreach (var (x, z) in new[] { (-0.6f, 0.4f), (0.6f, 0.4f), (-0.6f, -0.4f), (0.6f, -0.4f) })
        {
            var part = ecs.SpawnMesh(leg, legMaterial, Transform.At(x, -0.5f, z));
            ecs.SetParent(part, table);
            Unpickable(ecs, part);
        }

        var cube = ecs.SpawnMesh(Render.CreateMesh(MeshShape.Cuboid, 1f, 1f, 1f), Render.CreateMaterial(Color.FromSrgb(0.3f, 0.8f, 0.3f)), Transform.At(2f, 0.5f, 0f));
        ecs.Observe<Pointer<Click>>(cube, OnClickSelect);

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Directional, Shadows = true });
        ecs.Add(light, new Transform(Vec3.Zero, Quat.FromRotationX(-0.8f) * Quat.FromRotationY(0.4f), Vec3.One));

        var camera = ecs.SpawnCamera3d(Transform.LookingAt(new Vec3(0f, 4f, 8f), Vec3.Zero, Vec3.UnitY));
        Render.SetFreeCamera(camera, new FreeCameraSettings());
        ecs.Insert<TransformGizmoCameraRef>(camera);
    }

    private static void Unpickable(EcsWorld ecs, Entity entity)
    {
        var pickable = ecs.Insert<PickableRef>(entity);
        (pickable.ShouldBlockLower, pickable.IsHoverable) = (false, false);
    }

    // Bevy's on_click_select, the focus moved to what the primary button clicked.
    private static void OnClickSelect(On<Pointer<Click>> on)
    {
        if (on.Event.Event.Button != PointerButton.Primary) return;

        var ecs = on.Ecs;
        foreach (var focused in ecs.All<TransformGizmoFocusRef>()) focused.Remove();
        ecs.Insert<TransformGizmoFocusRef>(on.Entity);
    }

    // Bevy's update_radio_button, the gizmo's mode or axes from the button checked.
    private static void UpdateRadioButton(EcsWorld ecs, Entity button)
    {
        if (ecs.Resource<TransformGizmoSettingsRef>() is not { } settings) return;

        if (Radio.OptionValue<Mode>(button, out var mode)) settings.Mode = mode;
        else if (Radio.OptionValue<Space>(button, out var space)) settings.Space = space;
    }

    // Bevy's slider_update, the value asked for written to the slider and taken as the scaling's
    // speed.
    private static void SliderUpdate(On<ValueChange<float>> on)
    {
        on.Ecs.Wrap<SliderValueRef>(on.Event.Source).Value = on.Event.Value;
        if (on.Ecs.Resource<TransformGizmoSettingsRef>() is { } settings) settings.ScaleSensitivity = on.Event.Value;
    }

    // Bevy's toggle_scale_slider, the slider shown only while the gizmo scales.
    private static void ToggleScaleSlider(BehaviorContext ctx)
    {
        if (ctx.Ecs.Resource<TransformGizmoSettingsRef>() is not { } settings) return;
        ctx.Ecs.Wrap<NodeRef>(_sensitivityRow).Display = settings.Mode == Mode.Scale ? NodeRef.DisplayVariant.Flex : NodeRef.DisplayVariant.None;
    }

    // The sensitivity slider, Feathers' from 0.1 to 2 in steps of 0.1, beside its label.
    private static void SpawnSensitivitySlider(EcsWorld ecs, Entity column)
    {
        _sensitivityRow = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, ColumnGap = Length.Px(4f) });
        ecs.SetParent(_sensitivityRow, column);
        Feathers.Label(_sensitivityRow, "Sensitivity");

        var slider = Feathers.Slider(_sensitivityRow, 0.1f, 2f);
        ecs.Insert<SliderValueRef>(slider).Value = 1f;
        ecs.Insert<SliderPrecisionRef>(slider).Value = 2;
        ecs.Insert<SliderStepRef>(slider).Value = 0.1f;
        ecs.Observe<ValueChange<float>>(slider, SliderUpdate);
    }
}
