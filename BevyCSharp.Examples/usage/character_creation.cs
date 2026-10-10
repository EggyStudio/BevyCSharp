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
    private static readonly Color Gray = Color.FromSrgb(0.5019608f, 0.5019608f, 0.5019608f);
    private static readonly Color Green = Color.FromSrgb(0f, 0.5019608f, 0f);
    private static readonly Color Teal = Color.FromSrgb(0f, 0.5019608f, 0.5019608f);
    private static readonly Color Yellow = Color.FromSrgb(1f, 1f, 0f);

    // The model, Bevy's Character resource, the character being made and which of its parts
    // changed since it was drawn.
    private static string _name = "Bevy";
    private static int _age = 5;
    private static HatType _hat;
    private static bool _tintYellow;
    private static bool _nameChanged, _ageChanged, _hatChanged, _tintChanged;

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
        SpawnCharacter(ecs);
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
        var nameInput = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Px(200f),
            Border = Sides.All(Length.Px(5f)),
            Corners = Corners.All(Length.Px(10f)),
            Padding = new Sides(Length.Px(5f), Length.Px(2f), Length.Px(5f), Length.Px(2f)),
            Color = Color.Black,
        });
        Ui.SetEditableText(nameInput, new UiEditableTextSettings { Text = _name });
        // A tab index lets the field take the focus to be typed into.
        ecs.Insert<TabIndexRef>(nameInput).Value = 0;
        ecs.Add(nameInput, new NameInput());
        Cursor(ecs, nameInput, over: CursorShape.Text);
        ecs.SetParent(nameInput, row);
    }

    private static void OnChangedEditableText(BehaviorContext ctx)
    {
        foreach (var input in ctx.Ecs.EntitiesWith<NameInput>())
        {
            if (Ui.EditableTextOf(input) is not { } name || name == _name) continue;
            _name = name;
            _nameChanged = true;
        }
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
        ecs.Add(slider, new AgeSlider());
        Cursor(ecs, slider, over: CursorShape.Pointer, dragging: true);
        ecs.Observe<ValueChange<float>>(slider, OnValueChangeAgeSlider);
        ecs.SetParent(slider, row);

        var track = Ui.SpawnNode(new UiSettings { Height = Length.Px(5f), Corners = Corners.All(Length.Px(3f)), Color = Color.Black });
        ecs.SetParent(track, slider);

        var glide = Ui.SpawnNode(new UiSettings { Absolute = true, Left = Length.Px(0f), Right = Length.Px(20f), Top = Length.Px(0f), Bottom = Length.Px(0f) });
        ecs.SetParent(glide, slider);

        var thumb = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Width = Length.Px(20f),
            Height = Length.Px(10f),
            Left = Length.Percent(ThumbPosition(_age) * 100f),
            Color = Color.White,
        });
        ecs.Insert<SliderThumbRef>(thumb);
        ecs.Add(thumb, new AgeSliderThumb());
        Cursor(ecs, thumb, over: CursorShape.Grab, dragging: true);
        ecs.SetParent(thumb, glide);

        var readout = Ui.SpawnNode(new UiSettings { Width = Length.Px(30f) });
        var ageText = Ui.SpawnText($"{_age}", new UiSettings());
        ecs.Add(ageText, new AgeSliderText());
        ecs.SetParent(ageText, readout);
        ecs.SetParent(readout, row);
    }

    // Bevy's SliderRange::thumb_position, how far along the range a value is.
    private static float ThumbPosition(float value) => (value - 1f) / (100f - 1f);

    private static void OnValueChangeAgeSlider(On<ValueChange<float>> on)
    {
        var ecs = on.Ecs;
        if (!ecs.Has<AgeSlider>(on.Event.Source)) return;

        // The slider's precision makes the value a whole number.
        _age = (int)on.Event.Value;
        _ageChanged = true;

        ecs.Wrap<SliderValueRef>(on.Event.Source).Value = _age;
        foreach (var thumb in ecs.EntitiesWith<AgeSliderThumb>()) ecs.Wrap<NodeRef>(thumb).Left = new Val.Percent(ThumbPosition(_age) * 100f);
        foreach (var text in ecs.EntitiesWith<AgeSliderText>()) Ui.SetText(text, $"{_age}");
    }

    // The hat chosen from a radio group, the chosen button checked and its name in green.
    private static void HatRow(EcsWorld ecs, Entity pane)
    {
        var row = Row(ecs, pane, "Hat: ");
        ecs.Insert<RadioGroupRef>(row);
        ecs.Add(row, new HatTypeRadioGroup());
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
            ecs.Add(button, new HatTypeButton { Hat = hat });
            if (hat == _hat) ecs.Insert<CheckedRef>(button);
            Cursor(ecs, button, over: CursorShape.Pointer);

            var label = Ui.SpawnText($"{hat}", new UiSettings { Color = hat == _hat ? Green : Color.White });
            ecs.SetParent(label, button);
            ecs.SetParent(button, row);
        }
    }

    // The radio buttons' one child each is the text naming the hat, colored as it is chosen.
    private static void OnValueChangeHatType(On<ValueChange<Entity>> on)
    {
        var ecs = on.Ecs;
        if (!ecs.Has<HatTypeRadioGroup>(on.Event.Source)) return;
        if (!ecs.TryGet<HatTypeButton>(on.Event.Value, out var chosen) || ecs.Get<CheckedRef>(on.Event.Value) is not null) return;

        _hat = chosen.Hat;
        _hatChanged = true;

        // The buttons gathered before any is changed, since checking one moves it among Bevy's
        // tables, which a query going on cannot follow.
        foreach (var button in ecs.EntitiesWith<HatTypeButton>())
        {
            if (!ecs.TryGet<HatTypeButton>(button, out var choice) || ecs.ChildrenOf(button) is not [var label, ..]) continue;
            if (choice.Hat == _hat)
            {
                ecs.Insert<CheckedRef>(button);
                ecs.Wrap<TextColorRef>(label).Value = Green;
            }
            else if (ecs.Get<CheckedRef>(button) is { } was)
            {
                was.Remove();
                ecs.Wrap<TextColorRef>(label).Value = Color.White;
            }
        }
    }

    // A yellow tint turned on and off with a checkbox, marked with an X while it is checked.
    private static void TintRow(EcsWorld ecs, Entity pane)
    {
        var row = Row(ecs, pane, "Tint Yellow: ");
        var checkbox = Ui.SpawnNode(new UiSettings { Padding = Sides.Horizontal(Length.Px(5f)), Color = Color.White });
        ecs.Insert<CheckboxRef>(checkbox);
        ecs.Add(checkbox, new TintYellowCheckbox());
        if (_tintYellow) ecs.Insert<CheckedRef>(checkbox);
        Cursor(ecs, checkbox, over: CursorShape.Pointer);
        ecs.Observe<ValueChange<bool>>(checkbox, OnValueChangeTintYellow);

        ecs.SetParent(Ui.SpawnText(_tintYellow ? "X" : " ", new UiSettings { Color = Green }), checkbox);
        ecs.SetParent(checkbox, row);
    }

    // The checkbox's one child is the X marking it.
    private static void OnValueChangeTintYellow(On<ValueChange<bool>> on)
    {
        var ecs = on.Ecs;
        if (!ecs.Has<TintYellowCheckbox>(on.Event.Source) || ecs.ChildrenOf(on.Event.Source) is not [var mark, ..]) return;

        _tintYellow = on.Event.Value;
        _tintChanged = true;

        if (_tintYellow) ecs.Insert<CheckedRef>(on.Event.Source);
        else ecs.Get<CheckedRef>(on.Event.Source)?.Remove();
        Ui.SetText(mark, _tintYellow ? "X" : " ");
    }

    // The character on the right of the screen: Bevy's icon, its hat, and a line saying who it is.
    private static void SpawnCharacter(EcsWorld ecs)
    {
        var view = ecs.Spawn();
        ecs.Add(view, Transform.At(320f, 0f, 0f));
        ecs.Insert<VisibilityRef>(view);
        ecs.Add(view, new CharacterView());

        var sprite = ecs.Spawn();
        ecs.Add(sprite, Transform.Identity);
        ecs.Add(sprite, new CharacterSprite());
        ecs.SetParent(sprite, view);
        DrawSprite(ecs, sprite);

        DrawHat(ecs, view);

        var nameAndAge = ecs.Spawn();
        ecs.Add(nameAndAge, Transform.At(0f, -200f, 0f));
        ecs.Insert<Text2dRef>(nameAndAge).Value = NameAndAge();
        ecs.Add(nameAndAge, new CharacterNameAndAge());
        ecs.SetParent(nameAndAge, view);
    }

    private static void DrawSprite(EcsWorld ecs, Entity sprite) =>
        Render2d.SetSprite(ecs, sprite, AssetServer.Load(AssetKind.Image, "branding/icon.png"), new SpriteSettings { Color = _tintYellow ? Yellow : Color.White });

    private static void DrawHat(EcsWorld ecs, Entity view)
    {
        var hat = ecs.Spawn();
        ecs.Add(hat, new CharacterHat());
        ecs.SetParent(hat, view);

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
    }

    private static string NameAndAge() => $"Hi! My name is {_name}.\nI am {_age} {(_age == 1 ? "year" : "years")} old.";

    // The view drawn again where the model changed, each part found by its mark as Bevy finds it
    // among the view's children.
    private static void RefreshCharacter(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        if (ecs.EntitiesWith<CharacterView>() is not [var view]) return;

        if (_nameChanged || _ageChanged)
            foreach (var text in ecs.EntitiesWith<CharacterNameAndAge>()) ecs.Wrap<Text2dRef>(text).Value = NameAndAge();
        if (_tintChanged)
            foreach (var sprite in ecs.EntitiesWith<CharacterSprite>()) DrawSprite(ecs, sprite);
        if (_hatChanged)
        {
            foreach (var hat in ecs.EntitiesWith<CharacterHat>()) ecs.Despawn(hat);
            DrawHat(ecs, view);
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

/// <summary>A hat the character can wear.</summary>
public enum HatType { None, TopHat, DunceCap }

/// <summary>The field the character's name is typed into, Bevy's example's <c>NameInput</c>.</summary>
[Behavior]
public partial struct NameInput;

/// <summary>The slider setting the character's age, Bevy's example's <c>AgeSlider</c>.</summary>
[Behavior]
public partial struct AgeSlider;

/// <summary>The age slider's thumb, Bevy's example's <c>AgeSliderThumb</c>.</summary>
[Behavior]
public partial struct AgeSliderThumb;

/// <summary>The readout of the age beside the slider, Bevy's example's <c>AgeSliderText</c>.</summary>
[Behavior]
public partial struct AgeSliderText;

/// <summary>The radio group choosing the hat, Bevy's example's <c>HatTypeRadioGroup</c>.</summary>
[Behavior]
public partial struct HatTypeRadioGroup;

/// <summary>A radio button choosing a hat, which Bevy's example marks with the <c>HatType</c> itself.</summary>
[Behavior]
public partial struct HatTypeButton
{
    /// <summary>The hat it chooses.</summary>
    public HatType Hat;
}

/// <summary>The checkbox tinting the character yellow, Bevy's example's <c>TintYellowCheckbox</c>.</summary>
[Behavior]
public partial struct TintYellowCheckbox;

/// <summary>The character drawn on the right, its parts its children, Bevy's example's <c>CharacterView</c>.</summary>
[Behavior]
public partial struct CharacterView;

/// <summary>The character's picture, Bevy's example's <c>CharacterSprite</c>.</summary>
[Behavior]
public partial struct CharacterSprite;

/// <summary>The character's hat, Bevy's example's <c>CharacterHat</c>.</summary>
[Behavior]
public partial struct CharacterHat;

/// <summary>The line saying who the character is, Bevy's example's <c>CharacterNameAndAge</c>.</summary>
[Behavior]
public partial struct CharacterNameAndAge;
