// Bevy's overflow_debug example, examples/ui/scroll_and_overflow/overflow_debug.rs at v0.19.1, by
// Bevy's contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Interface;

// Shows clipping against nodes that move, grow and turn, a logo and a word in six frames, O cycling
// what the frames clip, S their size, and Space starting and stopping the motion.
internal static class OverflowDebug
{
    private const float Size = 150f;
    private const float LoopLength = 4f;

    // Bevy's AnimationState resource, whether the motion plays, when it was paused and for how
    // long in all, and how far round its loop it is.
    private static bool _playing;
    private static float _pausedAt, _pausedTotal;
    internal static float T;

    public static void Build(App app)
    {
        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            (_playing, _pausedAt, _pausedTotal, T) = (false, 0f, 0f, 0f);
            Render2d.SpawnCamera2d();

            var instructions = Ui.SpawnText("Next Overflow Setting (O)\nNext Container Size (S)\nToggle Animation (space)\n\n", new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });
            Ui.SpawnTextSpan(instructions, Describe(NodeRef.OverflowXVariant.Clip, NodeRef.OverflowYVariant.Clip), new UiTextSettings(), (1f, 1f, 1f, 1f));
            ecs.Add(instructions, new Instructions());

            var middle = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Justify = UiJustify.Center, Align = UiAlign.Center });
            var grid = Ui.SpawnNode(new UiSettings { Display = UiDisplay.Grid, RowGap = Length.Px(80f), ColumnGap = Length.Px(80f) });
            UiGrid.Set(grid, new GridSettings { Columns = [Track.Px(Size).Repeated(3)], Rows = [Track.Px(Size).Repeated(2)] });
            ecs.SetParent(grid, middle);

            var logo = AssetServer.Load(AssetKind.Image, "branding/bevy_logo_dark_big.png");
            var font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf");
            foreach (var motion in new Action<Entity>[] { inner => ecs.Add(inner, new Move()), inner => ecs.Add(inner, new Scale()), inner => ecs.Add(inner, new Rotate()) })
            {
                var image = Ui.SpawnNode(new UiSettings { Height = Length.Px(100f), Absolute = true, Top = Length.Px(-50f), Left = Length.Px(-200f) });
                Ui.SetImage(image, logo);
                motion(ContainerFor(ecs, grid, image));
            }

            foreach (var motion in new Action<Entity>[] { inner => ecs.Add(inner, new Move()), inner => ecs.Add(inner, new Scale()), inner => ecs.Add(inner, new Rotate()) })
                motion(ContainerFor(ecs, grid, Ui.SpawnText("Bevy", new UiSettings(), new UiTextSettings { Font = font, FontSize = 100f })));
        }, "overflow_debug.Setup");

        app.Update(UpdateAnimation, "overflow_debug.UpdateAnimation");
    }

    // Space starts and stops the motion, its time counting only while it plays.
    private static void UpdateAnimation(BehaviorContext ctx)
    {
        var elapsed = ctx.Time.Elapsed;
        if (ctx.Input.KeyPressed(Key.Space))
        {
            _playing = !_playing;
            if (_playing) _pausedTotal += elapsed - _pausedAt;
            else _pausedAt = elapsed;
        }

        if (_playing) T = (elapsed - _pausedTotal) % LoopLength / LoopLength;
    }

    // A clipping frame with a node inside it that moves, grows or turns, holding the content, the
    // inner node returned for its motion.
    private static Entity ContainerFor(EcsWorld ecs, Entity grid, Entity content)
    {
        var container = Ui.SpawnNode(new UiSettings
        {
            Width = Length.Percent(100f),
            Height = Length.Percent(100f),
            Align = UiAlign.Center,
            Justify = UiJustify.Center,
            OverflowX = UiOverflow.Clip,
            OverflowY = UiOverflow.Clip,
            Color = Color.FromSrgb(0.25f, 0.25f, 0.25f),
        });
        ecs.SetParent(container, grid);
        ecs.Add(container, new Container());

        var inner = Ui.SpawnNode(new UiSettings { Align = UiAlign.Center, Justify = UiJustify.Center });
        ecs.Insert<UiTransformRef>(inner);
        ecs.SetParent(inner, container);
        ecs.SetParent(content, inner);
        return inner;
    }

    // The setting as Bevy's debug output prints it.
    internal static string Describe(NodeRef.OverflowXVariant x, NodeRef.OverflowYVariant y) => $"Overflow {{ x: {x}, y: {y} }}";
}

/// <summary>The instructions, whose span says what the frames clip.</summary>
[Behavior]
public partial struct Instructions;

/// <summary>A clipping frame, and which of its three sizes it has.</summary>
[Behavior]
public partial struct Container
{
    /// <summary>Full, short or narrow, from zero to two.</summary>
    public byte Size;

    /// <summary>
    /// O moves what it clips on from both axes to neither, then the vertical alone, then the
    /// horizontal alone, and back, as Bevy's <c>toggle_overflow</c> does, the instructions saying so.
    /// </summary>
    [OnUpdate]
    public void ToggleOverflow(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.O)) return;

        var node = ctx.Ecs.Wrap<NodeRef>(ctx.Entity);
        (node.OverflowX, node.OverflowY) = (node.OverflowX, node.OverflowY) switch
        {
            (NodeRef.OverflowXVariant.Visible, NodeRef.OverflowYVariant.Visible) => (NodeRef.OverflowXVariant.Visible, NodeRef.OverflowYVariant.Clip),
            (NodeRef.OverflowXVariant.Visible, NodeRef.OverflowYVariant.Clip) => (NodeRef.OverflowXVariant.Clip, NodeRef.OverflowYVariant.Visible),
            (NodeRef.OverflowXVariant.Clip, NodeRef.OverflowYVariant.Visible) => (NodeRef.OverflowXVariant.Clip, NodeRef.OverflowYVariant.Clip),
            _ => (NodeRef.OverflowXVariant.Visible, NodeRef.OverflowYVariant.Visible),
        };

        foreach (var instructions in ctx.Ecs.EntitiesWith<Instructions>())
            ctx.Ecs.Wrap<TextSpanRef>(ctx.Ecs.ChildrenOf(instructions)[0]).Value = OverflowDebug.Describe(node.OverflowX, node.OverflowY);
    }

    /// <summary>S moves it on to its next size, full, short and narrow in turn.</summary>
    [OnUpdate]
    public void NextContainerSize(BehaviorContext ctx)
    {
        if (!ctx.Input.KeyPressed(Key.S)) return;

        Size = (byte)((Size + 1) % 3);
        var node = ctx.Ecs.Wrap<NodeRef>(ctx.Entity);
        node.Width = new Val.Percent(Size == 2 ? 30f : 100f);
        node.Height = new Val.Percent(Size == 1 ? 30f : 100f);
    }
}

/// <summary>A node that circles inside its frame.</summary>
[Behavior]
public partial struct Move
{
    /// <summary>Moved round a circle half the frame across, by how far round its loop the motion is.</summary>
    [OnUpdate]
    public void UpdateTransform(BehaviorContext ctx)
    {
        var a = OverflowDebug.T * MathF.Tau;
        var transform = ctx.Ecs.Wrap<UiTransformRef>(ctx.Entity);
        transform.TranslationX = new Val.Percent(MathF.Sin(a - MathF.PI / 2f) * 50f);
        transform.TranslationY = new Val.Percent(-MathF.Cos(a - MathF.PI / 2f) * 50f);
    }
}

/// <summary>A node that grows across and then up inside its frame.</summary>
[Behavior]
public partial struct Scale
{
    /// <summary>Scaled by up to half again across and then up, by how far round its loop the motion is.</summary>
    [OnUpdate]
    public void UpdateTransform(BehaviorContext ctx)
    {
        var a = OverflowDebug.T * MathF.Tau;
        ctx.Ecs.Wrap<UiTransformRef>(ctx.Entity).Scale = new Vec2(1f + 0.5f * MathF.Max(MathF.Cos(a), 0f), 1f + 0.5f * MathF.Max(MathF.Cos(a + MathF.PI), 0f));
    }
}

/// <summary>A node that swings inside its frame.</summary>
[Behavior]
public partial struct Rotate
{
    /// <summary>Turned to the cosine of the loop's angle times forty-five radians, as Bevy's is.</summary>
    [OnUpdate]
    public void UpdateTransform(BehaviorContext ctx)
    {
        var angle = MathF.Cos(OverflowDebug.T * MathF.Tau) * 45f;
        var transform = ctx.Ecs.Wrap<UiTransformRef>(ctx.Entity);
        (transform.RotationCos, transform.RotationSin) = (MathF.Cos(angle), MathF.Sin(angle));
    }
}
