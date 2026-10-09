// Bevy's animation_events example, examples/animation/animation_events.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Animations;

// Demonstrates events placed on an animation clip. A clip of two seconds holding no curves at all
// sets a message to "HELLO" at its start and "BYE" a second in, each in its own color, and the
// message fades as it waits for the next.
internal static class AnimationEvents
{
    private static Entity _message;

    // An event placed on the clip, the text it sets and its color.
    internal readonly record struct SetMessage(string Value, Color Color) : IAnimationEvent;

    public static void Build(App app)
    {
        app.Startup(Setup, "animation_events.Setup");
        app.Update(AnimateTextOpacity, "animation_events.AnimateTextOpacity");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        var camera = Render2d.SpawnCamera2d();
        ecs.Wrap<CameraRef>(camera).ClearColor = new ClearColorConfig.Custom(new Color(0f, 0f, 0f, 1f));
        ecs.Insert<BloomRef>(camera).Intensity = 0.4f;

        _message = ecs.Spawn();
        ecs.Add(_message, Transform.Identity);
        ecs.Insert<Text2dRef>(_message).Value = string.Empty;
        ecs.Insert<TextFontRef>(_message).FontSize = new FontSize.Px(119f);
        ecs.Insert<TextColorRef>(_message).Value = new Color(0f, 0f, 0f, 0f);

        // Bevy's on_set_message, observing every such event, which sets the message's text and
        // color from the event.
        ecs.Observe<SetMessage>(on =>
        {
            on.Ecs.Wrap<Text2dRef>(_message).Value = on.Event.Value;
            on.Ecs.Wrap<TextColorRef>(_message).Value = on.Event.Color;
        });

        // The clip, two seconds long, with an event at its start and one a second in.
        var clip = Animation.CreateClip();
        Animation.SetClipDuration(clip, 2f);
        Animation.AddEvent(clip, 0f, new SetMessage("HELLO", Color.FromSrgb8(240, 248, 255)));
        Animation.AddEvent(clip, 1f, new SetMessage("BYE", Color.FromSrgb8(220, 20, 60)));

        // A player of nothing but the clip, over and over.
        var (graph, node) = Animation.GraphFromClip(clip);
        Animation.PlayGraph(ecs.Spawn(), graph, node, repeat: true);
    }

    // The message fading by the frame's time, so it is gone shortly before the next event.
    private static void AnimateTextOpacity(BehaviorContext ctx)
    {
        var color = ctx.Ecs.Wrap<TextColorRef>(_message);
        var value = color.Value;
        color.Value = new Color(value.R, value.G, value.B, value.A - ctx.Time.Delta);
    }
}
