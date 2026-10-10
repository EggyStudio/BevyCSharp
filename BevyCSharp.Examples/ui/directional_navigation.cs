// Bevy's directional_navigation example, examples/ui/navigation/directional_navigation.rs at
// v0.20.0, by Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Demonstrates automatic directional navigation. Buttons scattered about the window, each carrying
// Bevy's AutoDirectionalNavigation, are moved between with the arrows or a pad's directions, the
// nearest button each way found from where they are on the screen, with no edges drawn by hand.
// Enter or the pad's south button presses the one with the focus.
internal static class DirectionalNavigationExample
{
    // Where each button stands, irregular, and still navigated by.
    private static readonly (float Left, float Top)[] ButtonPositions =
    [
        (350f, 100f), (520f, 120f), (700f, 90f),
        (380f, 220f), (600f, 240f),
        (450f, 340f), (620f, 360f),
        (360f, 480f), (540f, 460f), (720f, 490f),
    ];

    public static void Build(App app)
    {
        app.Startup(SetupScatteredUi, "directional_navigation.SetupScatteredUi");
        NavigationInput.AddSystems(app, "directional_navigation", moved: null);
    }

    // Bevy's setup_scattered_ui, the buttons scattered so the search has more to do than a grid.
    private static void SetupScatteredUi(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        NavigationInput.Setup(ecs, minAlignment: 0.1f, maxDistance: 500f);
        Render2d.SpawnCamera2d();

        Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f) });
        NavigationInput.SpawnPanels(ecs, """
            Directional Navigation Demo

            Use arrow keys or D-pad to navigate.
            Press Enter or A button to interact.

            Buttons are scattered irregularly,
            but navigation is automatic!
            """);

        var first = Entity.None;
        for (var i = 0; i < ButtonPositions.Length; i++)
        {
            var (left, top) = ButtonPositions[i];
            var button = NavigationInput.SpawnButton(ecs, $"Button {i + 1}", left, top, 140f, 80f, page: 0);

            // The fifth turned on its side and made larger, which the search reads as it is drawn.
            if (i == 4)
            {
                var turned = ecs.Insert<UiTransformRef>(button);
                (turned.Scale, turned.RotationCos, turned.RotationSin) = (new Vec2(1.2f, 1.2f), 0f, 1f);
            }

            if (first.IsNone) first = button;
        }

        // Bevy's init_focus, the first button focused as it is spawned.
        ecs.Insert<AutoFocusRef>(first);
    }
}

// What Bevy's two navigation examples share, their input, the focus drawn and the buttons pressed,
// as theirs repeat it line for line, each page with its colors from Tailwind's palette, as Bevy's.
internal static class NavigationInput
{
    internal static readonly Color[] Normal = [Color.FromHex("#60a5fa"), Color.FromHex("#f87171"), Color.FromHex("#4ade80")];
    internal static readonly Color[] Pressed = [Color.FromHex("#3b82f6"), Color.FromHex("#ef4444"), Color.FromHex("#22c55e")];
    internal static readonly Color[] Focused = [Color.FromHex("#eff6ff"), Color.FromHex("#fef2f2"), Color.FromHex("#f0fdf4")];

    // Bevy's DirectionalNavigationAction, Up, Down, Left, Right and Select, each with its key and
    // its pad's button and the names the key display gives them.
    private const int Up = 0, Down = 1, Left = 2, Right = 3, Select = 4;
    private static readonly (Key Key, GamepadButton Pad, string KeyName, string PadName)[] Actions =
    [
        (Key.ArrowUp, GamepadButton.DPadUp, "Up Arrow", "D-Pad Up"),
        (Key.ArrowDown, GamepadButton.DPadDown, "Down Arrow", "D-Pad Down"),
        (Key.ArrowLeft, GamepadButton.DPadLeft, "Left Arrow", "D-Pad Left"),
        (Key.ArrowRight, GamepadButton.DPadRight, "Right Arrow", "D-Pad Right"),
        (Key.Enter, GamepadButton.South, "Enter", "A Button"),
    ];

    internal static void AddSystems(App app, string example, Action<EcsWorld, Entity?, Entity>? moved)
    {
        app.On(Stage.PreUpdate, ctx => Navigate(ctx, moved), $"{example}.Navigate");
        app.Update(HighlightFocusedElement, $"{example}.HighlightFocusedElement");
        app.Update(InteractWithFocusedButton, $"{example}.InteractWithFocusedButton");
        app.Update(UpdateFocusDisplay, $"{example}.UpdateFocusDisplay");
        app.Update(UpdateKeyDisplay, $"{example}.UpdateKeyDisplay");
    }

    // The focus drawn, as Bevy's InputFocusVisible asks, the search's settings, and Bevy's
    // universal_button_click_behavior, a click showing the button pressed for a moment and the
    // focus drawn again, which a click with the pointer hides.
    internal static void Setup(EcsWorld ecs, float minAlignment, float maxDistance)
    {
        (ecs.Resource<InputFocusVisibleRef>() ?? ecs.InsertResource<InputFocusVisibleRef>()).Value = true;
        var config = ecs.Resource<AutoNavigationConfigRef>() ?? ecs.InsertResource<AutoNavigationConfigRef>();
        (config.MinAlignmentFactor, config.MaxSearchDistance, config.PreferAligned) = (minAlignment, maxDistance, true);

        ecs.Observe<Pointer<Click>>(on =>
        {
            if (!on.Ecs.TryGet<NavigationButton>(on.Entity, out var button)) return;
            on.Ecs.Wrap<BackgroundColorRef>(on.Entity).Value = Pressed[button.Page];
            on.Ecs.Set(on.Entity, button with { Reset = GameTimer.FromSeconds(0.3f, TimerMode.Once) });
            on.Propagate(false);
            if (on.Ecs.Resource<InputFocusVisibleRef>() is { } shown) shown.Value = true;
        });
    }

    // The instructions, and the two texts saying which button has the focus and which key came last.
    internal static void SpawnPanels(EcsWorld ecs, string instructions)
    {
        // A text with a color behind it, which a text's own settings color the letters of.
        Entity Panel(string text, Length? top, Length? bottom, Color color)
        {
            var panel = Ui.SpawnText(text, new UiSettings
            {
                Absolute = true,
                Left = Length.Px(20f),
                Top = top ?? Length.Auto,
                Bottom = bottom ?? Length.Auto,
                Width = Length.Px(280f),
                Padding = Length.Px(12f),
                Corners = Length.Px(8f),
            });
            ecs.Insert<BackgroundColorRef>(panel).Value = color;
            return panel;
        }

        Panel(instructions, Length.Px(20f), null, Color.FromSrgb(0.1f, 0.1f, 0.1f, 0.8f));
        ecs.Add(Panel("Focused: None", null, Length.Px(80f), Color.FromSrgb(0.1f, 0.5f, 0.1f, 0.8f)), new FocusDisplay());
        ecs.Add(Panel("Last Key: None", null, Length.Px(20f), Color.FromSrgb(0.5f, 0.1f, 0.5f, 0.8f)), new KeyDisplay());
    }

    // A button the search reaches, Bevy's AutoDirectionalNavigation on it, named for the focus
    // display, and a widget's button with a tab index, so a click on it moves the focus to it.
    internal static Entity SpawnButton(EcsWorld ecs, string name, float left, float top, float width, float height, int page)
    {
        var button = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Left = Length.Px(left),
            Top = Length.Px(top),
            Width = Length.Px(width),
            Height = Length.Px(height),
            Border = Length.Px(4f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
            Corners = Length.Px(12f),
            Color = Normal[page],
            Interactive = true,
        });
        ecs.Insert<AutoDirectionalNavigationRef>(button);
        ecs.Insert<ButtonRef>(button);
        ecs.Insert<TabIndexRef>(button);
        ecs.Add(button, new NavigationButton { Page = page });
        ecs.SetName(button, name);
        ecs.SetParent(Ui.SpawnText(name, new UiSettings(), new UiTextSettings { Justify = TextJustify.Center }), button);
        return button;
    }

    private static bool Acted(BehaviorContext ctx, int action) =>
        ctx.Input.KeyPressed(Actions[action].Key) || ctx.Input.Gamepads.Any(pad => pad.Pressed(Actions[action].Pad));

    // Bevy's navigate, the net of the arrows held this frame as a direction, the diagonal where two
    // are, and the focus moved that way, with what it moved from and to for a game that follows it.
    private static void Navigate(BehaviorContext ctx, Action<EcsWorld, Entity?, Entity>? moved)
    {
        var x = (Acted(ctx, Right) ? 1f : 0f) - (Acted(ctx, Left) ? 1f : 0f);
        var y = (Acted(ctx, Up) ? 1f : 0f) - (Acted(ctx, Down) ? 1f : 0f);
        if (CompassOctants.Of(new Vec2(x, y)) is not { } direction) return;

        var previous = ctx.Ecs.Resource<InputFocusRef>()?.CurrentFocus;
        if (Navigation.Move(direction) is { } next) moved?.Invoke(ctx.Ecs, previous, next);
    }

    // Bevy's highlight_focused_element, the button with the focus bordered in its page's light color.
    private static void HighlightFocusedElement(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var focused = ecs.Resource<InputFocusRef>()?.CurrentFocus;
        var visible = ecs.Resource<InputFocusVisibleRef>()?.Value ?? false;
        foreach (var entity in ecs.EntitiesWith<NavigationButton>())
        {
            var border = ecs.Wrap<BorderColorRef>(entity);
            border.Top = border.Right = border.Bottom = border.Left =
                focused == entity && visible ? Focused[ecs.GetOrDefault<NavigationButton>(entity).Page] : Color.Transparent;
        }
    }

    // Bevy's interact_with_focused_button, Enter or the pad's south button clicking the button with
    // the focus as the mouse would.
    private static void InteractWithFocusedButton(BehaviorContext ctx)
    {
        if (!Acted(ctx, Select) || ctx.Ecs.Resource<InputFocusRef>()?.CurrentFocus is not { } focused) return;
        var click = new Click(PointerButton.Primary, new PointerHit(Entity.None, 0f, null, null), TimeSpan.FromSeconds(0.1), 1);
        ctx.Ecs.Trigger(new Pointer<Click>(focused, new PointerId(PointerKind.Mouse, 0), Vec2.Zero, click));
    }

    // Bevy's update_focus_display.
    private static void UpdateFocusDisplay(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var focused = ecs.Resource<InputFocusRef>()?.CurrentFocus;
        var text = focused is not { } entity ? "Focused: None"
            : ecs.Has<NavigationButton>(entity) ? $"Focused: {ecs.NameOf(entity)}"
            : "Focused: Unknown";
        foreach (var display in ecs.EntitiesWith<FocusDisplay>()) Ui.SetText(display, text);
    }

    // Bevy's update_key_display, the first action pressed this frame by key, and else by a pad.
    private static void UpdateKeyDisplay(BehaviorContext ctx)
    {
        var said = Enumerable.Range(0, Actions.Length).Where(action => ctx.Input.KeyPressed(Actions[action].Key)).Select(action => Actions[action].KeyName)
            .Concat(Enumerable.Range(0, Actions.Length).Where(action => ctx.Input.Gamepads.Any(pad => pad.Pressed(Actions[action].Pad))).Select(action => Actions[action].PadName))
            .FirstOrDefault();
        if (said is null) return;
        foreach (var display in ctx.Ecs.EntitiesWith<KeyDisplay>()) Ui.SetText(display, $"Last Key: {said}");
    }
}

/// <summary>A button's place among the pages of a navigation example, for its colors.</summary>
[Behavior]
public partial struct NavigationButton
{
    /// <summary>Which page it is on.</summary>
    public int Page;

    /// <summary>How long it shows as pressed.</summary>
    public GameTimer Reset;

    /// <summary>Bevy's reset_button_after_interaction, the page's own color back once the press has shown.</summary>
    [OnUpdate]
    public void ResetButtonAfterInteraction(BehaviorContext ctx)
    {
        if (Reset.Tick(ctx.Time.Delta).JustFinished)
            ctx.Ecs.Wrap<BackgroundColorRef>(ctx.Entity).Value = NavigationInput.Normal[Page];
    }
}

/// <summary>The text saying which button has the focus.</summary>
[Behavior]
public partial struct FocusDisplay;

/// <summary>The text saying which key was pressed last.</summary>
[Behavior]
public partial struct KeyDisplay;
