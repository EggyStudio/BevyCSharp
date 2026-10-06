using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Tools;

// Shows a gamepad's buttons, sticks and triggers. A button lights while it is held, each stick's
// knob moves as it does over its dead and live zones with its two axes written above it, the lower
// triggers show how far down they are, and the pads connected are listed in the corner.
internal static class GamepadViewer
{
    private const float ButtonRadius = 25f, ButtonClusterRadius = 50f, StickBoundsSize = 100f;
    private const float ButtonsX = 150f, ButtonsY = 80f, SticksX = 150f, SticksY = -135f;
    private static readonly Vec2 StartSize = new(30f, 15f), TriggerSize = new(70f, 20f);
    private static readonly Color LiveColor = Color.FromSrgb(0.4f, 0.4f, 0.4f);
    private static readonly Color DeadColor = Color.FromSrgb(0.13f, 0.13f, 0.13f);

    // Bevy keeps the meshes in a resource of their own, made once and shared by every button.
    private static AssetHandle _circle, _triangle, _startPause, _trigger;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            ReactTo.Normal = Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.FromSrgb(0.3f, 0.3f, 0.3f) });
            ReactTo.Active = Render2d.CreateMaterial(new ColorMaterialSettings { Color = Color.FromSrgb(0.5f, 0f, 0.5f) });
            _circle = Render.CreateMesh(MeshShape.Circle, ButtonRadius);
            _triangle = Render.CreateMesh(MeshShape.RegularPolygon, ButtonRadius, 3f);
            _startPause = Render.CreateMesh(MeshShape.Rectangle, StartSize.X, StartSize.Y);
            _trigger = Render.CreateMesh(MeshShape.Rectangle, TriggerSize.X, TriggerSize.Y);

            Render2d.SpawnCamera2d();
            Setup(ecs);
            SetupSticks(ecs);
            SetupTriggers(ecs);
            SetupConnected(ecs);
        }, "gamepad_viewer.Setup");
    }

    // The face buttons, the select and start buttons, the D-pad and the upper triggers.
    private static void Setup(EcsWorld ecs)
    {
        var buttons = Group(ecs, ButtonsX, ButtonsY);
        Button(ecs, GamepadButton.North, _circle, 0f, ButtonClusterRadius, parent: buttons);
        Button(ecs, GamepadButton.South, _circle, 0f, -ButtonClusterRadius, parent: buttons);
        Button(ecs, GamepadButton.West, _circle, -ButtonClusterRadius, 0f, parent: buttons);
        Button(ecs, GamepadButton.East, _circle, ButtonClusterRadius, 0f, parent: buttons);

        Button(ecs, GamepadButton.Select, _startPause, -30f, ButtonsY);
        Button(ecs, GamepadButton.Start, _startPause, 30f, ButtonsY);

        var dpad = Group(ecs, -ButtonsX, ButtonsY);
        Button(ecs, GamepadButton.DPadUp, _triangle, 0f, ButtonClusterRadius, parent: dpad);
        Button(ecs, GamepadButton.DPadDown, _triangle, 0f, -ButtonClusterRadius, MathF.PI, dpad);
        Button(ecs, GamepadButton.DPadLeft, _triangle, -ButtonClusterRadius, 0f, MathF.PI / 2f, dpad);
        Button(ecs, GamepadButton.DPadRight, _triangle, ButtonClusterRadius, 0f, -MathF.PI / 2f, dpad);

        Button(ecs, GamepadButton.LeftTrigger, _trigger, -ButtonsX, ButtonsY + 115f);
        Button(ecs, GamepadButton.RightTrigger, _trigger, ButtonsX, ButtonsY + 115f);
    }

    // Each stick over a square of its whole travel, the live zone lighter and the dead zone in the
    // middle as dark as the bounds, with its two axes written above it.
    private static void SetupSticks(EcsWorld ecs)
    {
        // Bevy reads the zones from a default GamepadSettings, as each pad is given one when it
        // connects, and here the default is put on a spare entity and read back.
        var spare = ecs.Spawn();
        var settings = ecs.Insert<GamepadSettingsRef>(spare);
        var deadUpper = StickBoundsSize * settings.DefaultAxisSettingsDeadzoneUpperbound;
        var deadLower = StickBoundsSize * settings.DefaultAxisSettingsDeadzoneLowerbound;
        var liveUpper = StickBoundsSize * settings.DefaultAxisSettingsLivezoneUpperbound;
        var liveLower = StickBoundsSize * settings.DefaultAxisSettingsLivezoneLowerbound;
        ecs.Despawn(spare);

        var deadSize = MathF.Abs(deadLower) + MathF.Abs(deadUpper);
        var deadMid = (deadLower + deadUpper) / 2f;
        var liveSize = MathF.Abs(liveLower) + MathF.Abs(liveUpper);
        var liveMid = (liveLower + liveUpper) / 2f;

        void SpawnStick(float x, float y, GamepadAxis xAxis, GamepadAxis yAxis, GamepadButton button)
        {
            var stick = Group(ecs, x, y);
            Square(ecs, DeadColor, StickBoundsSize * 2f, Vec3.Zero, stick);
            Square(ecs, LiveColor, liveSize, new Vec3(liveMid, liveMid, 2f), stick);
            Square(ecs, DeadColor, deadSize, new Vec3(deadMid, deadMid, 3f), stick);

            var text = ecs.Spawn();
            ecs.Add(text, Transform.At(0f, StickBoundsSize + 2f, 4f));
            ecs.Insert<Text2dRef>(text);
            ecs.Insert<AnchorRef>(text).Value = new Vec2(0f, -0.5f);
            ecs.Add(text, new TextWithAxes
            {
                XAxis = xAxis,
                YAxis = yAxis,
                XSpan = Span(ecs, text, "0.000"),
                YSpan = Span(ecs, text, "0.000", before: ", "),
            });
            ecs.SetParent(text, stick);

            var knob = Button(ecs, button, _circle, 0f, 0f, parent: stick);
            ecs.Set(knob, new Transform(new Vec3(0f, 0f, 5f), Quat.Identity, new Vec3(0.15f, 0.15f, 1f)));
            ecs.Add(knob, new MoveWithAxes { XAxis = xAxis, YAxis = yAxis, Scale = StickBoundsSize });
        }

        SpawnStick(-SticksX, SticksY, GamepadAxis.LeftX, GamepadAxis.LeftY, GamepadButton.LeftThumb);
        SpawnStick(SticksX, SticksY, GamepadAxis.RightX, GamepadAxis.RightY, GamepadButton.RightThumb);
    }

    // The lower triggers, each with how far it is down written on it.
    private static void SetupTriggers(EcsWorld ecs)
    {
        void SpawnTrigger(float x, float y, GamepadButton button, GamepadAxis axis)
        {
            var trigger = Button(ecs, button, _trigger, x, y);

            // Bevy's example writes this as interface text, which its update, reading text in the
            // world, never finds, so the value is text in the world here to be shown at all.
            var value = ecs.Spawn();
            ecs.Add(value, Transform.At(0f, 0f, 1f));
            ecs.Insert<Text2dRef>(value).Value = "0.000";
            ecs.Insert<TextFontRef>(value).FontSize = new FontSize.Px(13f);
            ecs.Add(value, new TextWithButtonValue { Axis = axis });
            ecs.SetParent(value, trigger);
        }

        SpawnTrigger(-ButtonsX, ButtonsY + 145f, GamepadButton.LeftTrigger2, GamepadAxis.LeftTrigger);
        SpawnTrigger(ButtonsX, ButtonsY + 145f, GamepadButton.RightTrigger2, GamepadAxis.RightTrigger);
    }

    // Interface text, unlike the rest of the text here, which is in the world.
    private static void SetupConnected(EcsWorld ecs)
    {
        var text = Ui.SpawnText("Connected Gamepads:\n", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
        var span = ecs.Spawn();
        ecs.Insert<TextSpanRef>(span).Value = "None";
        ecs.SetParent(span, text);
        ecs.Add(text, new ConnectedGamepadsText { Span = span });
    }

    // A button's mesh in the color of one let go, which lights while its button is held.
    private static Entity Button(EcsWorld ecs, GamepadButton button, AssetHandle mesh, float x, float y, float angle = 0f, Entity? parent = null)
    {
        var entity = ecs.Spawn();
        ecs.Add(entity, new Transform(new Vec3(x, y, 0f), Quat.FromRotationZ(angle), Vec3.One));
        Render2d.SetMesh(ecs, entity, mesh);
        Render2d.SetMaterial(ecs, entity, ReactTo.Normal);
        ecs.Add(entity, new ReactTo { Button = button });
        if (parent is { } group) ecs.SetParent(entity, group);
        return entity;
    }

    // An empty place for a cluster of parts to hang from.
    private static Entity Group(EcsWorld ecs, float x, float y)
    {
        var group = ecs.Spawn();
        ecs.Add(group, Transform.At(x, y, 0f));
        ecs.Insert<VisibilityRef>(group);
        return group;
    }

    // A square sprite of one color.
    private static void Square(EcsWorld ecs, Color color, float size, Vec3 at, Entity parent)
    {
        var square = ecs.Spawn();
        ecs.Add(square, new Transform(at));
        var sprite = ecs.Insert<SpriteRef>(square);
        sprite.Color = color;
        sprite.CustomSize = new Vec2(size, size);
        ecs.SetParent(square, parent);
    }

    // A span of text at 13 pixels under a run of text, after a span of its own where one is given.
    private static Entity Span(EcsWorld ecs, Entity parent, string text, string? before = null)
    {
        if (before is not null) Span(ecs, parent, before);

        var span = ecs.Spawn();
        ecs.Insert<TextSpanRef>(span).Value = text;
        ecs.Insert<TextFontRef>(span).FontSize = new FontSize.Px(13f);
        ecs.SetParent(span, parent);
        return span;
    }
}

/// <summary>A part that lights while one of the pad's buttons is held.</summary>
[Behavior]
public partial struct ReactTo
{
    /// <summary>The color of a button let go, which Bevy keeps in a resource.</summary>
    public static AssetHandle Normal;

    /// <summary>The color of a button held.</summary>
    public static AssetHandle Active;

    /// <summary>The button it shows.</summary>
    public GamepadButton Button;

    /// <summary>Lit the frame its button goes down on any pad, and dimmed the frame it comes up.</summary>
    [OnUpdate]
    public void UpdateButtons(BehaviorContext ctx)
    {
        foreach (var pad in ctx.Input.Gamepads)
        {
            if (pad.Pressed(Button)) Render2d.SetMaterial(ctx.Ecs, ctx.Entity, Active);
            if (pad.Released(Button)) Render2d.SetMaterial(ctx.Ecs, ctx.Entity, Normal);
        }
    }
}

/// <summary>A stick's knob, moved across its bounds by the stick's two axes.</summary>
[Behavior]
public partial struct MoveWithAxes
{
    /// <summary>The axis that moves it across.</summary>
    public GamepadAxis XAxis;

    /// <summary>The axis that moves it up and down.</summary>
    public GamepadAxis YAxis;

    /// <summary>How far a stick pushed to its end moves it.</summary>
    public float Scale;

    /// <summary>Put where each pad's stick stands.</summary>
    [OnUpdate]
    public void UpdateAxes(BehaviorContext ctx, ref Transform transform)
    {
        foreach (var pad in ctx.Input.Gamepads)
        {
            transform.Translation.X = pad.Axis(XAxis) * Scale;
            transform.Translation.Y = pad.Axis(YAxis) * Scale;
        }
    }
}

/// <summary>The text above a stick, its two axes written to three places.</summary>
[Behavior]
public partial struct TextWithAxes
{
    /// <summary>The axis written first.</summary>
    public GamepadAxis XAxis;

    /// <summary>The axis written second.</summary>
    public GamepadAxis YAxis;

    /// <summary>The span the first is written to.</summary>
    public Entity XSpan;

    /// <summary>The span the second is written to.</summary>
    public Entity YSpan;

    /// <summary>
    /// Rewritten where a pad's axis has moved, as Bevy rewrites it only for an axis that has sent
    /// that it changed, so text that stays the same is not laid out again.
    /// </summary>
    [OnUpdate]
    public void UpdateAxes(BehaviorContext ctx)
    {
        foreach (var pad in ctx.Input.Gamepads)
        {
            Write(ctx.Ecs, XSpan, pad.Axis(XAxis));
            Write(ctx.Ecs, YSpan, pad.Axis(YAxis));
        }
    }

    private static void Write(EcsWorld ecs, Entity span, float value)
    {
        var text = ecs.Wrap<TextSpanRef>(span);
        var written = value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
        if (text.Value != written) text.Value = written;
    }
}

/// <summary>The text on a lower trigger, how far it is down.</summary>
[Behavior]
public partial struct TextWithButtonValue
{
    /// <summary>
    /// The trigger's axis, which is the same as the analog value of the button Bevy's example
    /// reads, LeftTrigger2 or RightTrigger2.
    /// </summary>
    public GamepadAxis Axis;

    /// <summary>Rewritten where a pad's trigger has moved.</summary>
    [OnUpdate]
    public void UpdateButtonValues(BehaviorContext ctx)
    {
        foreach (var pad in ctx.Input.Gamepads)
        {
            var text = ctx.Ecs.Wrap<Text2dRef>(ctx.Entity);
            var written = pad.Axis(Axis).ToString("F3", System.Globalization.CultureInfo.InvariantCulture);
            if (text.Value != written) text.Value = written;
        }
    }
}

/// <summary>The list of the pads connected, rewritten when one connects or disconnects.</summary>
[Behavior]
public partial struct ConnectedGamepadsText
{
    /// <summary>The span the list is written to.</summary>
    public Entity Span;

    /// <summary>Each pad by its entity and name, one a line, or None where there is none.</summary>
    [OnUpdate]
    public void UpdateConnected(BehaviorContext ctx)
    {
        var changed = false;
        foreach (var _ in ctx.Read<GamepadConnected>()) changed = true;
        foreach (var _ in ctx.Read<GamepadDisconnected>()) changed = true;
        if (!changed) return;

        var formatted = string.Join("\n", ctx.Input.Gamepads.Select(pad => $"{pad.Entity.Index}v{pad.Entity.Generation} - {pad.Name}"));
        ctx.Ecs.Wrap<TextSpanRef>(Span).Value = formatted.Length > 0 ? formatted : "None";
    }
}
