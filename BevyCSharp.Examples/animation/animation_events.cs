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
    // An event placed on the clip, the text it sets and its color.
    internal readonly record struct SetMessage(string Value, Color Color) : IAnimationEvent;

    public static void Build(App app) => app.Startup(Setup, "animation_events.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        var camera = Render2d.SpawnCamera2d();
        ecs.Wrap<CameraRef>(camera).ClearColor = new ClearColorConfig.Custom(new Color(0f, 0f, 0f, 1f));
        ecs.Insert<BloomRef>(camera).Intensity = 0.4f;

        var message = ecs.Spawn();
        ecs.Add(message, Transform.Identity);
        ecs.Add(message, new MessageText());
        ecs.Insert<Text2dRef>(message).Value = string.Empty;
        ecs.Insert<TextFontRef>(message).FontSize = new FontSize.Px(119f);
        ecs.Insert<TextColorRef>(message).Value = new Color(0f, 0f, 0f, 0f);

        // Bevy's on_set_message, observing every such event, which sets the text and color of the
        // one message there is from the event.
        ecs.Observe<SetMessage>(on =>
        {
            if (on.Ecs.EntitiesWith<MessageText>() is not [var text]) return;
            on.Ecs.Wrap<Text2dRef>(text).Value = on.Event.Value;
            on.Ecs.Wrap<TextColorRef>(text).Value = on.Event.Color;
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
}

/// <summary>The text the clip's events set, Bevy's example's <c>MessageText</c>.</summary>
[Behavior]
public partial struct MessageText
{
    /// <summary>
    /// Fades by the frame's time, so it is gone shortly before the next event.
    /// </summary>
    /// <remarks>
    /// Bevy's animate_text_opacity fades every text color there is, and the message's is the only
    /// one, so it runs here on the message.
    /// </remarks>
    [OnUpdate]
    public void AnimateTextOpacity(BehaviorContext ctx)
    {
        var color = ctx.Ecs.Wrap<TextColorRef>(ctx.Entity);
        var value = color.Value;
        color.Value = new Color(value.R, value.G, value.B, value.A - ctx.Time.Delta);
    }
}
