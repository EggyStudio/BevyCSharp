// Bevy's animated_ui example, examples/animation/animated_ui.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Animations;

// Shows how to use animation clips to animate interface properties. The word "Bevy" in the middle
// of the window grows and shrinks, turns a whole turn and goes from red to green to blue and back,
// each a curve of one clip aimed at the text by its name.
internal static class AnimatedUi
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();

        // The clip, three curves aimed at the text.
        var target = AnimationTarget.FromNames("Text");
        var clip = Animation.CreateClip();
        Animation.AddCurve(clip, target, AnimationCurve.UiScale([0f, 0.5f, 1f, 1.5f, 2f, 2.5f, 3f],
            [new Vec2(0.3f), new Vec2(1f), new Vec2(0.3f), new Vec2(1f), new Vec2(0.3f), new Vec2(1f), new Vec2(0.3f)]));
        Animation.AddCurve(clip, target, AnimationCurve.TextColor([0f, 1f, 2f, 3f],
            [Color.FromSrgb(1f, 0f, 0f), Color.FromSrgb(0f, 1f, 0f), Color.FromSrgb(0f, 0f, 1f), Color.FromSrgb(1f, 0f, 0f)]));
        Animation.AddCurve(clip, target, AnimationCurve.UiRotation([0f, 1f, 2f, 3f], [0f, MathF.Tau / 3f, MathF.Tau / 1.5f, MathF.Tau]));
        var (graph, node) = Animation.GraphFromClip(clip);

        // The node filling the window plays it over and over for the text inside it.
        var player = Ui.SpawnNode(new UiSettings
        {
            Absolute = true,
            Top = Length.Px(0f),
            Left = Length.Px(0f),
            Right = Length.Px(0f),
            Bottom = Length.Px(0f),
            Justify = UiJustify.Center,
            Align = UiAlign.Center,
        });
        Animation.PlayGraph(player, graph, node, repeat: true);

        var text = Ui.SpawnText("Bevy", new UiSettings { Color = Color.FromSrgb(1f, 0f, 0f) },
            new UiTextSettings { Font = AssetServer.Load(AssetKind.Font, "fonts/FiraSans-Bold.ttf"), FontSize = 80f, Justify = TextJustify.Center });
        ecs.SetName(text, "Text");
        ecs.SetParent(text, player);
        Animation.Animate(text, target, player);
    }, "animated_ui.Setup");
}
