// Bevy's feathers_gallery example, examples/ui/widgets/feathers_gallery.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Every Feathers widget in three columns. The first holds buttons, menus, selects, a row of joined
// buttons, a cursor override and a dialog, checkboxes, radio buttons, toggles, a slider, and a color
// set through a hex field, a plane and sliders in sRGB, HSL and Okhsl, which stay in step with one
// another. The second holds an editor's pane with its tool buttons, a subpane, a group of number
// inputs and a list. The third holds two color inputs.
internal static class FeathersGallery
{
    private enum Months { Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec }

    private static readonly string[] MonthNames =
        ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

    // Bevy's DemoWidgetStates, each color in the space its own widgets set it in.
    private static Vec4 _rgb;
    private static Vec4 _hsl;
    private static Vec4 _okhsl;
    private static float _scalar;
    private static Vec3 _vec3;
    private static bool _changed;

    private static Entity _disabledButton;
    private static Entity _dialogToggle;
    private static Entity _hexInput;
    private static readonly List<(Entity Slider, ColorChannel Channel)> Sliders = [];
    private static readonly List<(Entity Swatch, ColorSpace Space)> Swatches = [];
    private static readonly List<(Entity Plane, ColorPlaneAxes Axes)> Planes = [];
    private static readonly List<Entity> ScalarFields = [];
    private static readonly List<(Entity Field, int Axis)> Vec3Fields = [];

    public static void Configure(Config config) => config.Feathers = true;

    public static void Build(App app)
    {
        app.Startup(Setup, "feathers_gallery.Setup");
        app.Update(UpdateColors, "feathers_gallery.UpdateColors");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Sliders.Clear();
        Swatches.Clear();
        Planes.Clear();
        ScalarFields.Clear();
        Vec3Fields.Clear();

        // Emerald at 800 with alpha 0.7, and amber at 800 in HSL and in Okhsl, Tailwind's.
        _rgb = new Vec4(0.023529412f, 0.37254903f, 0.27450982f, 0.7f);
        var amber = Color.FromSrgb(0.57254905f, 0.2509804f, 0.05490196f);
        (_hsl, _okhsl) = (amber.In(ColorSpace.Hsl), amber.In(ColorSpace.Okhsl));
        (_scalar, _vec3, _changed) = (7f, new Vec3(10.1f, 7.124f, 100f), true);

        Render2d.SpawnCamera2d();

        var root = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Start,
            Justify = UiJustify.Start,
            ColumnGap = Length.Px(8f),
        });
        ecs.Insert<TabGroupRef>(root);
        Feathers.ThemeColor(root, FeathersToken.WindowBg);

        Column1(ecs, Column(ecs, root, 30f, 200f));
        Column2(ecs, Column(ecs, root, 30f, 200f));
        Column3(ecs, Column(ecs, root, 15f, 100f));
    }

    private static Entity Column(EcsWorld ecs, Entity root, float percent, float minWidth)
    {
        var column = Node(ecs, root, new UiSettings
        {
            Direction = UiDirection.Column,
            Align = UiAlign.Stretch,
            Justify = UiJustify.Start,
            Padding = Sides.All(Length.Px(8f)),
            RowGap = Length.Px(8f),
            Width = Length.Percent(percent),
            MinWidth = Length.Px(minWidth),
        });
        return column;
    }

    private static Entity Node(EcsWorld ecs, Entity parent, UiSettings settings)
    {
        var node = Ui.SpawnNode(settings);
        ecs.SetParent(node, parent);
        return node;
    }

    private static Entity Row(EcsWorld ecs, Entity parent, float gap, UiJustify justify = UiJustify.Start) =>
        Node(ecs, parent, new UiSettings { Align = UiAlign.Center, Justify = justify, ColumnGap = Length.Px(gap) });

    private static void Grow(EcsWorld ecs, Entity entity) => ecs.Wrap<NodeRef>(entity).FlexGrow = 1f;

    private static void Label(EcsWorld ecs, Entity entity, string label) => ecs.Insert<AccessibleLabelRef>(entity).Value = label;

    private static Entity Logged(EcsWorld ecs, Entity entity, string message)
    {
        ecs.Observe<Activate>(entity, _ => Log.Info(message));
        return entity;
    }

    private static void Column1(EcsWorld ecs, Entity column)
    {
        var buttons = Row(ecs, column, 8f);

        var normal = Logged(ecs, Feathers.Button(buttons, "Normal"), "Normal button clicked!");
        Grow(ecs, normal);
        Label(ecs, normal, "Normal");
        ecs.Insert<AutoFocusRef>(normal);

        _disabledButton = Logged(ecs, Feathers.Button(buttons, "Disabled"), "Disabled button clicked!");
        Grow(ecs, _disabledButton);
        Label(ecs, _disabledButton, "Disabled");
        ecs.Insert<InteractionDisabledRef>(_disabledButton);

        var primary = Logged(ecs, Feathers.Button(buttons, "Primary", ButtonVariant.Primary), "Primary button clicked!");
        Label(ecs, primary, "Primary");
        Grow(ecs, primary);

        var menu = Feathers.Menu(buttons);
        var menuButton = Feathers.MenuButton(menu, "Menu");
        Label(ecs, menuButton, "Menu Example");
        Grow(ecs, menuButton);
        var popup = Feathers.MenuPopup(menu);
        Logged(ecs, Feathers.MenuItem(popup, "MenuItem 1"), "Menu item 1 clicked!");
        Logged(ecs, Feathers.MenuItem(popup, "MenuItem 2"), "Menu item 2 clicked!");
        Feathers.MenuDivider(popup);
        Logged(ecs, Feathers.MenuItem(popup, "MenuItem 3"), "Menu item 3 clicked!");

        // Bevy builds this menu's popup as it opens, and here it is spawned with the menu, placed
        // at its button's right edge, below or else above.
        var toolMenu = Feathers.Menu(buttons);
        var toolButton = Feathers.MenuToolButton(toolMenu, "Θ");
        Label(ecs, toolButton, "Menu Example");
        Grow(ecs, toolButton);
        var toolPopup = Feathers.MenuPopup(toolMenu);
        ecs.SetReflected(toolPopup, "bevy_ui_widgets::popover::Popover", "",
            """{"positions":[{"side":"Bottom","align":"End","gap":2.0},{"side":"Top","align":"End","gap":2.0}],"window_margin":10.0}""");
        Logged(ecs, Feathers.MenuItem(toolPopup, "MenuItem 4"), "Menu item 4 clicked!");
        Logged(ecs, Feathers.MenuItem(toolPopup, "MenuItem 5"), "Menu item 5 clicked!");
        Logged(ecs, Feathers.MenuItem(toolPopup, "MenuItem 6"), "Menu item 6 clicked!");

        var numbers = Feathers.Select(column, ["One", "Two", "Three"], selected: 0);
        Grow(ecs, numbers);
        ecs.Observe<ValueChange<Entity>>(numbers, on =>
        {
            if (on.Ecs.Get<OptionIndexRef>(on.Event.Value) is { } option) Log.Info($"Select changed to index {option.Value}");
            else Log.Info("Select changed, not sure");
        });

        var months = Feathers.Select(column, MonthNames, selected: 0, maxVisible: 6);
        Grow(ecs, months);
        ecs.Observe<ValueChange<Entity>>(months, on =>
        {
            if (on.Ecs.Get<OptionIndexRef>(on.Event.Value) is { } option) Log.Info($"Select changed to {(Months)option.Value}");
        });

        var joined = Row(ecs, column, 1f);
        foreach (var (name, variant, corners) in new[]
        {
            ("Left", ButtonVariant.Normal, RoundedCorners.Left),
            ("Center", ButtonVariant.Normal, RoundedCorners.None),
            ("Right", ButtonVariant.Primary, RoundedCorners.Right),
        })
        {
            var button = Logged(ecs, Feathers.Button(joined, name, variant, corners), $"{name} button clicked!");
            Grow(ecs, button);
            Label(ecs, button, name);
        }

        var cursor = Row(ecs, column, 8f);
        var overrideButton = Feathers.Button(cursor, "Toggle override");
        Grow(ecs, overrideButton);
        ecs.Observe<Activate>(overrideButton, on => ToggleOverrideCursor(on.Ecs));
        var quit = Feathers.Button(cursor, "Quit…");
        Grow(ecs, quit);
        ecs.Observe<Activate>(quit, on => SpawnQuitDialog(on.Ecs, quit));

        var dialog = Row(ecs, column, 8f);
        Feathers.Label(dialog, "Dialog:");
        _dialogToggle = Feathers.ToggleSwitch(dialog);
        ecs.Observe<ValueChange<bool>>(_dialogToggle, on => ToggleDemoDialog(on.Ecs, on.Event.Value));

        var checkbox = Feathers.Checkbox(column, "Checkbox");
        ecs.Insert<CheckedRef>(checkbox);
        Label(ecs, checkbox, "Checkbox Example");
        ecs.Observe<ValueChange<bool>>(checkbox, on =>
        {
            Log.Info("Checkbox clicked!");
            if (on.Event.Value) on.Ecs.Insert<InteractionDisabledRef>(_disabledButton);
            else on.Ecs.Wrap<InteractionDisabledRef>(_disabledButton).Remove();
            SetChecked(on.Ecs, on.Event.Source, on.Event.Value);
        });

        var fast = Feathers.Checkbox(column, "Fast Click Checkbox");
        ecs.Insert<ActivateOnPressRef>(fast);
        Label(ecs, fast, "Fast Click Checkbox Example");
        ecs.Observe<ValueChange<bool>>(fast, on =>
        {
            Log.Info("Checkbox clicked!");
            SetChecked(on.Ecs, on.Event.Source, on.Event.Value);
        });

        var disabled = Feathers.Checkbox(column, "Disabled");
        ecs.Insert<InteractionDisabledRef>(disabled);
        Label(ecs, disabled, "Disabled Checkbox Example");
        ecs.Observe<ValueChange<bool>>(disabled, _ => Log.Warn("Disabled checkbox clicked!"));

        var checkedDisabled = Feathers.Checkbox(column, "Checked+Disabled");
        ecs.Insert<InteractionDisabledRef>(checkedDisabled);
        ecs.Insert<CheckedRef>(checkedDisabled);
        Label(ecs, checkedDisabled, "Disabled and Checked Checkbox Example");
        ecs.Observe<ValueChange<bool>>(checkedDisabled, _ => Log.Warn("Disabled checkbox clicked!"));

        var radios = Row(ecs, column, 8f);
        var group = Node(ecs, radios, new UiSettings { Direction = UiDirection.Column, RowGap = Length.Px(4f) });
        ecs.Insert<RadioGroupRef>(group);
        Ui.SelfUpdate(group, UiWidgetKind.RadioGroup);
        ecs.Insert<CheckedRef>(Feathers.Radio(group, "One"));
        Feathers.Radio(group, "Two");
        ecs.Insert<ActivateOnPressRef>(Feathers.Radio(group, "Fast Click"));
        ecs.Insert<InteractionDisabledRef>(Feathers.Radio(group, "Disabled"));

        var toggles = Row(ecs, column, 8f);
        Ui.SelfUpdate(Feathers.ToggleSwitch(toggles), UiWidgetKind.Checkbox);
        var fastToggle = Feathers.ToggleSwitch(toggles);
        ecs.Insert<ActivateOnPressRef>(fastToggle);
        Ui.SelfUpdate(fastToggle, UiWidgetKind.Checkbox);
        var disabledToggle = Feathers.ToggleSwitch(toggles);
        ecs.Insert<InteractionDisabledRef>(disabledToggle);
        Ui.SelfUpdate(disabledToggle, UiWidgetKind.Checkbox);
        var checkedToggle = Feathers.ToggleSwitch(toggles);
        ecs.Insert<InteractionDisabledRef>(checkedToggle);
        ecs.Insert<CheckedRef>(checkedToggle);
        Ui.SelfUpdate(checkedToggle, UiWidgetKind.Checkbox);
        Ui.SelfUpdate(Feathers.DisclosureToggle(toggles), UiWidgetKind.Checkbox);

        var slider = Feathers.Slider(column, max: 100f);
        ecs.Insert<SliderValueRef>(slider).Value = 20f;
        ecs.Insert<SliderStepRef>(slider).Value = 10f;
        ecs.Insert<SliderPrecisionRef>(slider).Value = 2;
        Ui.SelfUpdate(slider, UiWidgetKind.Slider);

        var srgba = Row(ecs, column, 4f, UiJustify.SpaceBetween);
        Feathers.Label(srgba, "Srgba");
        Feathers.FlexSpacer(srgba);
        var container = Feathers.TextInputContainer(srgba);
        var frame = ecs.Wrap<NodeRef>(container);
        (frame.FlexGrow, frame.PaddingLeft) = (0f, new Val.Px(4f));
        _hexInput = Feathers.TextInput(container, visibleWidth: 10f, maxCharacters: 9);
        ecs.Insert<InheritableFontRef>(_hexInput).Font = AssetServer.Load(AssetKind.Font, "embedded://bevy_feathers/assets/fonts/FiraMono-Medium.ttf");
        ecs.Observe<TextEditChange>(_hexInput, on => HandleHexColorChange(on.Ecs));
        Swatches.Add((Feathers.ColorSwatch(srgba, opaquePercentage: 30f), ColorSpace.Srgb));

        var redBlue = Feathers.ColorPlane(column, ColorPlaneAxes.RedBlue);
        Planes.Add((redBlue, ColorPlaneAxes.RedBlue));
        ecs.Observe<ValueChange<Vec2>>(redBlue, on => Change(ref _rgb, x: on.Event.Value.X, z: on.Event.Value.Y));

        ColorSlider(ecs, column, ColorChannel.Red, "Red Channel", value => Change(ref _rgb, x: value));
        ColorSlider(ecs, column, ColorChannel.Green, "Green Channel", value => Change(ref _rgb, y: value));
        ColorSlider(ecs, column, ColorChannel.Blue, "Blue Channel", value => Change(ref _rgb, z: value));
        ColorSlider(ecs, column, ColorChannel.Alpha, "Alpha Channel", value => Change(ref _rgb, w: value));

        var hsl = Row(ecs, column, 0f, UiJustify.SpaceBetween);
        Feathers.Label(hsl, "Hsl");
        Swatches.Add((Feathers.ColorSwatch(hsl), ColorSpace.Hsl));
        ColorSlider(ecs, column, ColorChannel.HslHue, "Hue Channel", value => Change(ref _hsl, x: value));
        ColorSlider(ecs, column, ColorChannel.HslSaturation, "Saturation Channel", value => Change(ref _hsl, y: value));
        ColorSlider(ecs, column, ColorChannel.HslLightness, "Lightness Channel", value => Change(ref _hsl, z: value));

        var okhsl = Row(ecs, column, 0f, UiJustify.SpaceBetween);
        Feathers.Label(okhsl, "Okhsl");
        Swatches.Add((Feathers.ColorSwatch(okhsl), ColorSpace.Okhsl));
        var hueLightness = Feathers.ColorPlane(column, ColorPlaneAxes.OkhslHueLightness);
        Planes.Add((hueLightness, ColorPlaneAxes.OkhslHueLightness));
        ecs.Observe<ValueChange<Vec2>>(hueLightness, on => Change(ref _okhsl, x: on.Event.Value.X * 360f, z: 1f - on.Event.Value.Y));
        ColorSlider(ecs, column, ColorChannel.OkhslHue, "Okhsl Hue Channel", value => Change(ref _okhsl, x: value));
        ColorSlider(ecs, column, ColorChannel.OkhslSaturation, "Okhsl Saturation Channel", value => Change(ref _okhsl, y: value));
        ColorSlider(ecs, column, ColorChannel.OkhslLightness, "Okhsl Lightness Channel", value => Change(ref _okhsl, z: value));
    }

    private static void ColorSlider(EcsWorld ecs, Entity column, ColorChannel channel, string label, Action<float> changed)
    {
        var slider = Feathers.ColorSlider(column, channel, 0.5f);
        Label(ecs, slider, label);
        ecs.Observe<ValueChange<float>>(slider, on => changed(on.Event.Value));
        Sliders.Add((slider, channel));
    }

    // One of a color's four components written, the rest kept, and the widgets told.
    private static void Change(ref Vec4 color, float? x = null, float? y = null, float? z = null, float? w = null)
    {
        color = new Vec4(x ?? color.X, y ?? color.Y, z ?? color.Z, w ?? color.W);
        _changed = true;
    }

    private static void Column2(EcsWorld ecs, Entity column)
    {
        var pane = Feathers.Pane(column);
        var header = Feathers.PaneHeader(pane);
        Feathers.ToolButton(header, "Θ", ButtonVariant.Primary);
        Feathers.PaneHeaderDivider(header);
        Feathers.ToolButton(header, "¼", ButtonVariant.Plain);
        Feathers.ToolButton(header, "½", ButtonVariant.Plain);
        Feathers.ToolButton(header, "¾", ButtonVariant.Plain);
        Feathers.PaneHeaderDivider(header);
        Feathers.Icon(Feathers.ToolButton(header, "", ButtonVariant.Plain), FeathersIcon.ChevronDown);
        Feathers.FlexSpacer(header);
        Feathers.Icon(Feathers.ToolButton(header, "", ButtonVariant.Plain), FeathersIcon.X);

        var body = Feathers.PaneBody(pane);
        Feathers.LabelDim(body, "A standard editor pane");

        var subpane = Feathers.Subpane(body);
        var subpaneHeader = Feathers.SubpaneHeader(subpane);
        Feathers.Caption(subpaneHeader, "Left");
        Feathers.Caption(subpaneHeader, "Center");
        Feathers.Caption(subpaneHeader, "Right");
        var subpaneBody = Feathers.SubpaneBody(subpane);
        Feathers.LabelDim(subpaneBody, "A standard sub-pane");

        var group = Feathers.Group(subpaneBody);
        Feathers.Caption(Feathers.GroupHeader(group), "Group");
        var groupBody = Feathers.GroupBody(group);
        Feathers.Label(groupBody, "A standard group");

        Feathers.LabelSmall(groupBody, "Scalar property");
        ScalarField(ecs, groupBody, 2, hardLimit: true);
        Feathers.LabelSmall(groupBody, "Scalar property (copy)");
        ScalarField(ecs, groupBody, 4, hardLimit: false);

        Feathers.LabelSmall(groupBody, "Vec3 property");
        var axes = Row(ecs, groupBody, 6f, UiJustify.SpaceBetween);
        string[] letters = ["X", "Y", "Z"];
        string[] sigils = [FeathersToken.TextInputXAxis, FeathersToken.TextInputYAxis, FeathersToken.TextInputZAxis];
        for (var axis = 0; axis < 3; axis++)
        {
            var field = Feathers.NumberInput(axes, letters[axis], sigils[axis]);
            ecs.Insert<NumberInputPrecisionRef>(field).Value = 2;
            Grow(ecs, field);
            if (axis == 0) XAxisBorder(ecs, field);
            var which = axis;
            ecs.Observe<ValueChange<float>>(field, on =>
            {
                _vec3 = which switch
                {
                    0 => new Vec3(on.Event.Value, _vec3.Y, _vec3.Z),
                    1 => new Vec3(_vec3.X, on.Event.Value, _vec3.Z),
                    _ => new Vec3(_vec3.X, _vec3.Y, on.Event.Value),
                };
                _changed = true;
            });
            Vec3Fields.Add((field, axis));
        }

        Feathers.LabelSmall(groupBody, "Color property");
        var channels = Row(ecs, groupBody, 6f, UiJustify.SpaceBetween);
        string[] colorLetters = ["R", "G", "B", "A"];
        string[] colorSigils = [FeathersToken.TextInputXAxis, FeathersToken.TextInputYAxis, FeathersToken.TextInputZAxis, FeathersToken.TextInputWAxis];
        for (var channel = 0; channel < 4; channel++)
        {
            var field = Feathers.NumberInput(channels, colorLetters[channel], colorSigils[channel]);
            ecs.Insert<InteractionDisabledRef>(field);
            ecs.Insert<NumberInputPrecisionRef>(field).Value = 2;
            Feathers.SetNumberLimit(field, 0f, 1f, hard: true);
            Grow(ecs, field);
            if (channel == 0) XAxisBorder(ecs, field);
        }

        var list = Feathers.Subpane(column);
        Feathers.Caption(Feathers.SubpaneHeader(list), "List");
        var view = Feathers.ListView(Feathers.SubpaneBody(list),
        [
            "First World", "Second Nature", "Third Degree", "Fourth Wall", "Fifth Column", "Sixth Sense",
            "Seventh Heaven", "Eighth Wonder", "Ninth Inning", "Tenth Amendment", "Eleventh Hour", "Twelfth Night",
        ], selected: 1);
        ecs.Wrap<NodeRef>(view).MaxHeight = new Val.Px(130f);
        ecs.Insert<InteractionDisabledRef>(ecs.ChildrenOf(Feathers.ContentsOf(view))[3]);
        Ui.SelfUpdate(view, UiWidgetKind.ListBox);
    }

    private static void ScalarField(EcsWorld ecs, Entity parent, int precision, bool hardLimit)
    {
        var field = Feathers.NumberInput(parent);
        ecs.Insert<NumberInputPrecisionRef>(field).Value = precision;
        if (hardLimit) Feathers.SetNumberLimit(field, 0f, 100f, hard: true);
        var node = ecs.Wrap<NodeRef>(field);
        (node.FlexGrow, node.MaxWidth) = (1f, new Val.Px(100f));
        ecs.Observe<ValueChange<float>>(field, on => (_scalar, _changed) = (on.Event.Value, true));
        ScalarFields.Add(field);
    }

    // Feathers' palette's red of the X axis, around the field as well as along its edge.
    private static void XAxisBorder(EcsWorld ecs, Entity field)
    {
        var red = Color.From(ColorSpace.Oklch, new Vec4(0.5232f, 0.1404f, 13.84f, 1f));
        var border = ecs.Insert<BorderColorRef>(field);
        (border.Top, border.Right, border.Bottom, border.Left) = (red, red, red, red);
    }

    private static void Column3(EcsWorld ecs, Entity column)
    {
        // Tailwind's amber at 600 and blue at 800.
        ColorInput(ecs, column, "Armor Tint", Color.FromSrgb(0.8509804f, 0.46666667f, 0.023529412f));
        ColorInput(ecs, column, "Skin Color", Color.FromSrgb(0.11764706f, 0.2509804f, 0.6862745f));
    }

    private static void ColorInput(EcsWorld ecs, Entity column, string label, Color color)
    {
        var row = Row(ecs, column, 0f, UiJustify.SpaceBetween);
        Feathers.Label(row, label);
        var input = Feathers.ColorInput(row);
        ecs.Wrap<ColorInputValueRef>(input).Value = color;
        Ui.SelfUpdate(input, UiWidgetKind.ColorInput);
    }

    private static void UpdateColors(BehaviorContext ctx)
    {
        if (!_changed) return;
        _changed = false;
        var ecs = ctx.Ecs;
        var rgb = Color.From(ColorSpace.Srgb, _rgb);
        var hsl = Color.From(ColorSpace.Hsl, _hsl);
        var okhsl = Color.From(ColorSpace.Okhsl, _okhsl);

        foreach (var (slider, channel) in Sliders)
        {
            var (color, value) = channel switch
            {
                ColorChannel.Red => (rgb, _rgb.X),
                ColorChannel.Green => (rgb, _rgb.Y),
                ColorChannel.Blue => (rgb, _rgb.Z),
                ColorChannel.Alpha => (rgb, _rgb.W),
                ColorChannel.HslHue => (hsl, _hsl.X),
                ColorChannel.HslSaturation => (hsl, _hsl.Y),
                ColorChannel.HslLightness => (hsl, _hsl.Z),
                ColorChannel.OkhslHue => (okhsl, _okhsl.X),
                ColorChannel.OkhslSaturation => (okhsl, _okhsl.Y),
                _ => (okhsl, _okhsl.Z),
            };
            ecs.Wrap<SliderBaseColorRef>(slider).Value = color;
            ecs.Insert<SliderValueRef>(slider).Value = value;
        }

        foreach (var (swatch, space) in Swatches)
        {
            ecs.Insert<ColorSwatchValueRef>(swatch).Value = space switch
            {
                ColorSpace.Hsl => hsl,
                ColorSpace.Okhsl => okhsl,
                _ => rgb,
            };
        }

        foreach (var (plane, axes) in Planes)
        {
            ecs.Wrap<ColorPlaneValueRef>(plane).Value = axes switch
            {
                ColorPlaneAxes.OkhslHueLightness => new Vec3(_okhsl.X / 360f, _okhsl.Z, _okhsl.Y),
                _ => new Vec3(_rgb.X, _rgb.Z, _rgb.Y),
            };
        }

        // Written while the field does not have the focus, which would fight the typing.
        if (ecs.Resource<InputFocusRef>()?.CurrentFocus != _hexInput)
        {
            Ui.EditText(_hexInput, TextEdit.SelectAll);
            Ui.EditText(_hexInput, TextEdit.Insert(rgb.ToHex()));
        }

        foreach (var field in ScalarFields) Feathers.SetNumber(field, _scalar);
        foreach (var (field, axis) in Vec3Fields) Feathers.SetNumber(field, axis switch { 0 => _vec3.X, 1 => _vec3.Y, _ => _vec3.Z });
    }

    private static void HandleHexColorChange(EcsWorld ecs)
    {
        Color color;
        try { color = Color.FromHex(Ui.EditableTextOf(_hexInput) ?? ""); }
        catch (FormatException) { return; }

        var srgb = color.In(ColorSpace.Srgb);
        if (srgb != _rgb) (_rgb, _changed) = (srgb, true);
    }

    private static void SetChecked(EcsWorld ecs, Entity widget, bool value)
    {
        if (value) ecs.Insert<CheckedRef>(widget);
        else ecs.Wrap<CheckedRef>(widget).Remove();
    }

    // Bevy's OverrideCursor, the cursor shown whatever the pointer is over, the system's wait
    // cursor or none.
    private static void ToggleOverrideCursor(EcsWorld ecs)
    {
        var cursor = ecs.Resource<OverrideCursorRef>()!.Value;
        cursor.Value = cursor.Value is null ? new EntityCursor.System(SystemCursorIcon.Wait) : null;
        Log.Info("Override cursor button clicked!");
    }

    private static void SpawnQuitDialog(EcsWorld ecs, Entity button)
    {
        var dialog = Feathers.Dialog(button, width: 320f);
        var contents = Feathers.ContentsOf(dialog);

        var header = Feathers.DialogHeader(contents);
        Feathers.Caption(header, "Quit Feathers Gallery");
        Feathers.DialogClose(header);
        Feathers.Caption(Feathers.DialogBody(contents), "Are you really sure you want to quit? I mean, really, really sure?");

        var footer = Feathers.DialogFooter(contents);
        var cancel = Feathers.Button(footer, "Cancel");
        Label(ecs, cancel, "Cancel");
        ecs.Observe<Activate>(cancel, on => on.Ecs.Despawn(dialog));
        var exit = Feathers.Button(footer, "Exit Application", ButtonVariant.Primary);
        Label(ecs, exit, "Exit Application");
        ecs.Observe<Activate>(exit, _ => App.RequestExit());

        ecs.Observe<RequestClose>(dialog, on => on.Ecs.Despawn(dialog));
    }

    private static void ToggleDemoDialog(EcsWorld ecs, bool open)
    {
        if (open)
        {
            ecs.Insert<CheckedRef>(_dialogToggle);
            // At the root rather than under the toggle, so a click on the dialog does not reach it.
            var dialog = Feathers.FloatingDialog(Entity.None, "Hello", width: 280f);
            Feathers.Caption(Feathers.ContentsOf(dialog), "Close this dialog to unset the toggle.");
            // The dialog despawns itself as it closes, and this clears the toggle.
            ecs.Observe<RequestClose>(dialog, on => on.Ecs.Wrap<CheckedRef>(_dialogToggle).Remove());
        }
        else
        {
            ecs.Wrap<CheckedRef>(_dialogToggle).Remove();
            foreach (var dialog in ecs.All<FeathersFloatingDialogRef>()) ecs.Despawn(dialog.Entity);
        }
    }
}
