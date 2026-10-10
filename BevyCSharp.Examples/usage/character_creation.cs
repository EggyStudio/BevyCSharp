// Bevy's character_creation example, examples/usage/character_creation.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Usage;

// Illustrates managing data input from select widgets, a text field, a slider, a radio group and
// a checkbox, in a model, view and controller design. The character is the model, the widgets on
// the left and the character drawn on the right are the view, and the observers of the widgets'
// changes are the controller, each writing the model and the widget, the character being drawn
// again from the model where it changed.
//
// Bevy spawns each part of the character from a scene and despawns it to draw it again. Here the
// text and the sprite are changed in place and only the hat is made again.
internal static class CharacterCreation
{
    private enum HatType { None, TopHat, DunceCap }

    private static readonly Color Gray = Color.FromSrgb(0.5019608f, 0.5019608f, 0.5019608f);
    private static readonly Color Green = Color.FromSrgb(0f, 0.5019608f, 0f);
    private static readonly Color Teal = Color.FromSrgb(0f, 0.5019608f, 0.5019608f);
    private static readonly Color Yellow = Color.FromSrgb(1f, 1f, 0f);

    // The model, the character being made, and which of its parts changed since it was drawn.
    private static string _name = "Bevy";
    private static int _age = 5;
    private static HatType _hat;
    private static bool _tintYellow;
    private static bool _nameChanged, _ageChanged, _hatChanged, _tintChanged;

    private static readonly Dictionary<Entity, (HatType Hat, Entity Label)> HatButtons = [];
    private static Entity _nameInput, _ageText, _ageThumb, _checkboxMark;
    private static Entity _view, _sprite, _hatEntity, _nameAndAge;
    private static AssetHandle _icon;

    public static void Build(App app)
    {
        app.Startup(Setup, "character_creation.Setup");
        app.Update(OnChangedEditableText, "character_creation.OnChangedEditableText");
        app.Update(RefreshCharacter, "character_creation.RefreshCharacter");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        (_name, _age, _hat, _tintYellow) = ("Bevy", 5, HatType.None, false);
        (_nameChanged, _ageChanged, _hatChanged, _tintChanged) = (false, false, false, false);
        HatButtons.Clear();
        Render2d.SpawnCamera2d();

        // The interface takes the left half of the screen, the pane in the middle of it.
        var left = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Column, Justify = UiJustify.Center, Align = UiAlign.Center, Height = Length.Percent(100f), Width = Length.Percent(50f) });
        var pane = Ui.SpawnNode(new UiSettings
        {
            Direction = UiDirection.Column,
            Justify = UiJustify.SpaceEvenly,
            Align = UiAlign.Center,
            Margin = Sides.All(Length.Percent(5f)),
            Width = Length.Percent(90f),
            Padding = Sides.Vertical(Length.Px(10f)),
            Corners = Corners.All(Length.Px(5f)),
            RowGap = Length.Px(10f),
            Color = Gray,
        });
        ecs.SetParent(pane, left);
        ecs.SetParent(Ui.SpawnText("Character Creator", new UiSettings()), pane);

        NameRow(ecs, pane);
        AgeRow(ecs, pane);
        HatRow(ecs, pane);
        TintRow(ecs, pane);
        CharacterView(ecs);
    }

    private static Entity Row(EcsWorld ecs, Entity pane, string label, float gap = 0f)
    {
        var row = Ui.SpawnNode(new UiSettings { Direction = UiDirection.Row, Align = UiAlign.Center, Justify = UiJustify.SpaceBetween, ColumnGap = Length.Px(gap) });
        ecs.SetParent(Ui.SpawnText(label, new UiSettings()), row);
        ecs.SetParent(row, pane);
        return row;
    }

    // The name typed into a field, which reports no change, so it is read every frame.
    private static void NameRow(EcsWorld ecs, Entity pane)
    {
        var row = Row(ecs, pane, "Name: ");
        _nameInput = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(200f),
            Border = Sides.All(Length.Px(5f)),
            Corners = Corners.All(Length.Px(10f)),
            Padding = new Sides(Length.Px(5f), Length.Px(2f), Length.Px(5f), Length.Px(2f)),
            Color = Color.Black,
        });
        Ui.SetEditableText(_nameInput, new UiEditableTextSettings { Text = _name });
        // A tab index lets the field take the focus to be typed into.
        ecs.Insert<TabIndexRef>(_nameInput).Value = 0;
        Cursor(ecs, _nameInput, over: CursorShape.Text);
        ecs.SetParent(_nameInput, row);
    }

    private static void OnChangedEditableText(BehaviorContext ctx)
    {
        if (Ui.EditableTextOf(_nameInput) is not { } name || name == _name) return;
        _name = name;
        _nameChanged = true;
    }

    // The age on a slider from one to a hundred in whole years, its thumb on a track shorter by the
    // thumb's width so it stops at the ends.
    private static void AgeRow(EcsWorld ecs, Entity pane)
    {
        var row = Row(ecs, pane, "Age:", gap: 10f);

        var slider = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(220f),
            Border = Sides.All(Length.Px(5f)),
            Padding = new Sides(Length.Px(5f), Length.Px(2f), Length.Px(5f), Length.Px(2f)),
            Color = Color.Black,
        });
        var widget = ecs.Insert<SliderRef>(slider);
        (widget.TrackClick, widget.Orientation) = (SliderRef.TrackClickVariant.Snap, SliderRef.OrientationVariant.Horizontal);
        ecs.Insert<SliderValueRef>(slider).Value = _age;
        ecs.Insert<SliderPrecisionRef>(slider).Value = 0;
        var range = ecs.Insert<SliderRangeRef>(slider);
        (range.Start, range.End) = (1f, 100f);
        Cursor(ecs, slider, over: CursorShape.Pointer, dragging: true);
        ecs.Observe<ValueChange<float>>(slider, OnValueChangeAgeSlider);
        ecs.SetParent(slider, row);

        var track = Ui.SpawnNode(new UiSettings { Height = Length.Px(5f), Corners = Corners.All(Length.Px(3f)), Color = Color.Black });
        ecs.SetParent(track, slider);

        var glide = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Right = Length.Px(20f), Top = Length.Px(0f), Bottom = Length.Px(0f) });
        ecs.SetParent(glide, slider);

        _ageThumb = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Width = Length.Px(20f),
            Height = Length.Px(10f),
            Left = Length.Percent(ThumbPosition(_age) * 100f),
            Color = Color.White,
        });
        ecs.Insert<SliderThumbRef>(_ageThumb);
        Cursor(ecs, _ageThumb, over: CursorShape.Grab, dragging: true);
        ecs.SetParent(_ageThumb, glide);

        var readout = Ui.SpawnNode(new UiSettings { Width = Length.Px(30f) });
        _ageText = Ui.SpawnText($"{_age}", new UiSettings());
        ecs.SetParent(_ageText, readout);
        ecs.SetParent(readout, row);
    }

    // Bevy's SliderRange::thumb_position, how far along the range a value is.
    private static float ThumbPosition(float value) => (value - 1f) / (100f - 1f);

    private static void OnValueChangeAgeSlider(On<ValueChange<float>> on)
    {
        // The slider's precision makes the value a whole number.
        _age = (int)on.Event.Value;
        _ageChanged = true;

        on.Ecs.Wrap<SliderValueRef>(on.Event.Source).Value = _age;
        on.Ecs.Wrap<NodeRef>(_ageThumb).Left = new Val.Percent(ThumbPosition(_age) * 100f);
        Ui.SetText(_ageText, $"{_age}");
    }

    // The hat chosen from a radio group, the chosen button checked and its name in green.
    private static void HatRow(EcsWorld ecs, Entity pane)
    {
        var row = Row(ecs, pane, "Hat: ");
        ecs.Insert<RadioGroupRef>(row);
        ecs.Observe<ValueChange<Entity>>(row, OnValueChangeHatType);

        foreach (var hat in new[] { HatType.None, HatType.TopHat, HatType.DunceCap })
        {
            var button = Ui.SpawnNode(new UiSettings
            {
                Border = Sides.All(Length.Px(5f)),
                Corners = Corners.All(Length.Px(10f)),
                Padding = new Sides(Length.Px(5f), Length.Px(2f), Length.Px(5f), Length.Px(2f)),
                Color = Color.Black,
            });
            ecs.Insert<RadioButtonRef>(button);
            if (hat == _hat) ecs.Insert<CheckedRef>(button);
            Cursor(ecs, button, over: CursorShape.Pointer);

            var label = Ui.SpawnText($"{hat}", new UiSettings { Color = hat == _hat ? Green : Color.White });
            ecs.SetParent(label, button);
            ecs.SetParent(button, row);
            HatButtons[button] = (hat, label);
        }
    }

    private static void OnValueChangeHatType(On<ValueChange<Entity>> on)
    {
        if (!HatButtons.TryGetValue(on.Event.Value, out var chosen) || on.Ecs.Get<CheckedRef>(on.Event.Value) is not null) return;

        _hat = chosen.Hat;
        _hatChanged = true;

        foreach (var (button, (hat, label)) in HatButtons)
        {
            if (hat == _hat)
            {
                on.Ecs.Insert<CheckedRef>(button);
                on.Ecs.Wrap<TextColorRef>(label).Value = Green;
            }
            else if (on.Ecs.Get<CheckedRef>(button) is { } was)
            {
                was.Remove();
                on.Ecs.Wrap<TextColorRef>(label).Value = Color.White;
            }
        }
    }

    // A yellow tint turned on and off with a checkbox, marked with an X while it is checked.
    private static void TintRow(EcsWorld ecs, Entity pane)
    {
        var row = Row(ecs, pane, "Tint Yellow: ");
        var checkbox = Ui.SpawnNode(new UiSettings { Padding = Sides.Horizontal(Length.Px(5f)), Color = Color.White });
        ecs.Insert<CheckboxRef>(checkbox);
        if (_tintYellow) ecs.Insert<CheckedRef>(checkbox);
        Cursor(ecs, checkbox, over: CursorShape.Pointer);
        ecs.Observe<ValueChange<bool>>(checkbox, OnValueChangeTintYellow);

        _checkboxMark = Ui.SpawnText(_tintYellow ? "X" : " ", new UiSettings { Color = Green });
        ecs.SetParent(_checkboxMark, checkbox);
        ecs.SetParent(checkbox, row);
    }

    private static void OnValueChangeTintYellow(On<ValueChange<bool>> on)
    {
        _tintYellow = on.Event.Value;
        _tintChanged = true;

        if (_tintYellow) on.Ecs.Insert<CheckedRef>(on.Event.Source);
        else on.Ecs.Get<CheckedRef>(on.Event.Source)?.Remove();
        Ui.SetText(_checkboxMark, _tintYellow ? "X" : " ");
    }

    // The character on the right of the screen: Bevy's icon, its hat, and a line saying who it is.
    private static void CharacterView(EcsWorld ecs)
    {
        _icon = AssetServer.Load(AssetKind.Image, "branding/icon.png");

        _view = ecs.Spawn();
        ecs.Add(_view, Transform.At(320f, 0f, 0f));
        ecs.Insert<VisibilityRef>(_view);

        _sprite = ecs.Spawn();
        ecs.Add(_sprite, Transform.Identity);
        ecs.SetParent(_sprite, _view);
        DrawSprite(ecs);

        _hatEntity = DrawHat(ecs);

        _nameAndAge = ecs.Spawn();
        ecs.Add(_nameAndAge, Transform.At(0f, -200f, 0f));
        ecs.Insert<Text2dRef>(_nameAndAge).Value = NameAndAge();
        ecs.SetParent(_nameAndAge, _view);
    }

    private static void DrawSprite(EcsWorld ecs) =>
        Render2d.SetSprite(ecs, _sprite, _icon, new SpriteSettings { Color = _tintYellow ? Yellow : Color.White });

    private static Entity DrawHat(EcsWorld ecs)
    {
        var hat = ecs.Spawn();
        ecs.SetParent(hat, _view);

        switch (_hat)
        {
            case HatType.TopHat:
                // About a quarter turn of a half circle, Bevy's 0.78 radians.
                ecs.Add(hat, Transform.Identity with { Rotation = Quat.FromRotationZ(0.78f) });
                ecs.Insert<VisibilityRef>(hat);
                foreach (var (width, height, x, y) in new[] { (40f, 10f, 55f, 60f), (20f, 50f, 55f, 85f) })
                {
                    var part = ecs.Spawn();
                    ecs.Add(part, Transform.At(x, y, 1f));
                    Render2d.SetMesh(ecs, part, Render.CreateMesh(MeshShape.Rectangle, width, height));
                    Render2d.SetMaterial(ecs, part, Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.Black }));
                    ecs.SetParent(part, hat);
                }
                break;

            case HatType.DunceCap:
                ecs.Add(hat, Transform.At(0f, 80f, 1f) with { Rotation = Quat.FromRotationZ(0.78f) });
                var cone = Render.CreateMesh(new MeshData
                {
                    Positions = [new(0f, 100f, 0f), new(-20f, 0f, 0f), new(20f, 0f, 0f)],
                    Normals = [Vec3.UnitZ, Vec3.UnitZ, Vec3.UnitZ],
                    Uvs = [0.5f, 0f, 0f, 1f, 1f, 1f],
                    Indices = [0, 1, 2],
                });
                Render2d.SetMesh(ecs, hat, cone);
                Render2d.SetMaterial(ecs, hat, Render2d.CreateMaterial(new ColorMaterialSettings { Color = Teal }));
                break;

            default:
                ecs.Add(hat, Transform.Identity);
                break;
        }

        return hat;
    }

    private static string NameAndAge() => $"Hi! My name is {_name}.\nI am {_age} {(_age == 1 ? "year" : "years")} old.";

    // The view drawn again where the model changed.
    private static void RefreshCharacter(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        if (_nameChanged || _ageChanged) ecs.Wrap<Text2dRef>(_nameAndAge).Value = NameAndAge();
        if (_tintChanged) DrawSprite(ecs);
        if (_hatChanged)
        {
            ecs.Despawn(_hatEntity);
            _hatEntity = DrawHat(ecs);
        }

        (_nameChanged, _ageChanged, _hatChanged, _tintChanged) = (false, false, false, false);
    }

    // The shape of the pointer over a widget, and while a drag on it goes on, as Bevy's styling
    // observers set it. Over and out stop at the widget, and a drag goes on to the slider, which
    // reads it.
    private static void Cursor(EcsWorld ecs, Entity widget, CursorShape over, bool dragging = false)
    {
        ecs.Observe<Pointer<Over>>(widget, on => { SetCursor(over); on.Propagate(false); });
        ecs.Observe<Pointer<Out>>(widget, on => { SetCursor(CursorShape.Default); on.Propagate(false); });
        if (!dragging) return;
        ecs.Observe<Pointer<DragStart>>(widget, _ => SetCursor(CursorShape.Grabbing));
        ecs.Observe<Pointer<DragEnd>>(widget, _ => SetCursor(CursorShape.Grab));
    }

    // Only a window has a pointer to shape, which an offscreen run has none of.
    private static void SetCursor(CursorShape shape)
    {
        if (Window.Entity() != Entity.None) Window.SetCursorShape(shape);
    }
}
