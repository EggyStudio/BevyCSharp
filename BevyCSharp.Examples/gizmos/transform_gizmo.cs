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
    private static RadioButtons<Mode>? _modes;
    private static RadioButtons<Space>? _spaces;
    private static Entity _sensitivityRow, _slider, _fill, _value;

    // Bevy's TransformGizmoPlugin and MeshPickingPlugin, the gizmo and the clicks that select.
    public static void Configure(Config config) => (config.TransformGizmo, config.MeshPicking) = (true, true);

    public static void Build(App app)
    {
        app.Startup(Setup, "transform_gizmo.Setup");
        app.Update(UpdateRadioButtons, "transform_gizmo.UpdateRadioButtons");
        app.Update(SliderUpdate, "transform_gizmo.SliderUpdate");
        app.Update(ToggleScaleSlider, "transform_gizmo.ToggleScaleSlider");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        var column = RadioButtons<Mode>.Column();
        ecs.SetParent(Ui.SpawnText("Click an object to select it", new UiSettings(), 18f), column);
        _modes = new RadioButtons<Mode>(ecs, column, string.Empty, [(Mode.Translate, "Translate"), (Mode.Rotate, "Rotate"), (Mode.Scale, "Scale")], Mode.Translate);
        _spaces = new RadioButtons<Space>(ecs, column, string.Empty, [(Space.World, "World"), (Space.Local, "Local")], Space.World);
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
        ecs.Add(camera, new FreeCamera());
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

    // Bevy's update_radio_button, the gizmo's mode and axes from the buttons.
    private static void UpdateRadioButtons(BehaviorContext ctx)
    {
        if (ctx.Ecs.Resource<TransformGizmoSettingsRef>() is not { } settings) return;

        if (_modes!.Pressed(out var mode) && mode != settings.Mode)
        {
            settings.Mode = mode;
            _modes.Select(ctx.Ecs, mode);
        }
        if (_spaces!.Pressed(out var space) && space != settings.Space)
        {
            settings.Space = space;
            _spaces.Select(ctx.Ecs, space);
        }
    }

    // Bevy's slider_update, the scaling's speed from the slider, shown as its bar and number.
    private static void SliderUpdate(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (ecs.Resource<TransformGizmoSettingsRef>() is not { } settings) return;

        var value = ecs.Wrap<SliderValueRef>(_slider).Value;
        if (value != settings.ScaleSensitivity) settings.ScaleSensitivity = value;

        ecs.Wrap<NodeRef>(_fill).Width = new Val.Percent((value - 0.1f) / (2f - 0.1f) * 100f);
        Ui.SetText(_value, value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
    }

    // Bevy's toggle_scale_slider, the slider shown only while the gizmo scales.
    private static void ToggleScaleSlider(BehaviorContext ctx)
    {
        if (ctx.Ecs.Resource<TransformGizmoSettingsRef>() is not { } settings) return;
        ctx.Ecs.Wrap<NodeRef>(_sensitivityRow).Display = settings.Mode == Mode.Scale ? NodeRef.DisplayVariant.Flex : NodeRef.DisplayVariant.None;
    }

    // The sensitivity slider, from 0.1 to 2 in steps of 0.1, a bar filled to its value with the
    // value written over it, as Feathers draws its slider.
    private static void SpawnSensitivitySlider(EcsWorld ecs, Entity column)
    {
        _sensitivityRow = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, ColumnGap = Length.Px(4f), Display = UiDisplay.None });
        ecs.SetParent(_sensitivityRow, column);
        ecs.SetParent(Ui.SpawnText("Sensitivity", new UiSettings(), 18f), _sensitivityRow);

        _slider = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(160f),
            Height = Length.Px(24f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Color = (0.15f, 0.15f, 0.15f, 1f),
            Corners = Length.Px(4f),
            OverflowX = UiOverflow.Clip,
            OverflowY = UiOverflow.Clip,
        });
        ecs.SetParent(_slider, _sensitivityRow);
        ecs.Insert<SliderRef>(_slider).TrackClick = SliderRef.TrackClickVariant.Drag;
        ecs.Insert<SliderValueRef>(_slider).Value = 1f;
        var range = ecs.Insert<SliderRangeRef>(_slider);
        (range.Start, range.End) = (0.1f, 2f);
        ecs.Insert<SliderStepRef>(_slider).Value = 0.1f;
        ecs.Insert<SliderPrecisionRef>(_slider).Value = 2;
        Ui.SelfUpdate(_slider, UiWidgetKind.Slider);

        _fill = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Zero, Top = Length.Zero, Height = Length.Percent(100f), Color = (0.2f, 0.4f, 0.8f, 1f) });
        ecs.SetParent(_fill, _slider);
        _value = Ui.SpawnText("1.00", new UiSettings(), 16f);
        ecs.SetParent(_value, _slider);
    }
}
