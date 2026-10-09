// Bevy's sprite_animation example, examples/2d/sprite_animation.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.TwoD;

// Animates two sprites once each time an arrow key is pressed, the left one at ten frames a second
// and the right one at twenty.
internal static class SpriteAnimation
{
    public static void Build(App app) => app.Startup(ctx =>
    {
        var ecs = ctx.Ecs;
        Render2d.SpawnCamera2d();
        Ui.SpawnText("Left Arrow: Animate Left Sprite\nRight Arrow: Animate Right Sprite",
            new UiSettings { Absolute = true, Top = Length.Px(12f), Left = Length.Px(12f) });

        var texture = AssetServer.LoadImage("textures/rpg/chars/gabe/gabe-idle-run.png", new TextureSettings());
        var layout = Render2d.CreateAtlas(24, 24, 7, 1);

        Entity Spawn(float x, AnimationConfig config)
        {
            var sprite = ecs.Spawn();
            ecs.Add(sprite, new Transform(new Vec3(x, 0f, 0f), Quat.Identity, new Vec3(6f)));
            Render2d.SetSprite(ecs, sprite, texture, new SpriteSettings { Atlas = layout, Frame = (uint)config.FirstSpriteIndex });
            ecs.Add(sprite, config);
            return sprite;
        }

        ecs.Add(Spawn(-70f, AnimationConfig.New(1, 6, 10)), new LeftSprite());
        ecs.Add(Spawn(70f, AnimationConfig.New(1, 6, 20)), new RightSprite());
    }, "sprite_animation.Setup");
}

/// <summary>How a sprite's animation runs, its frames, its speed and the timer for the frame it is on.</summary>
[Behavior]
public partial struct AnimationConfig
{
    /// <summary>The first frame on the sheet.</summary>
    public int FirstSpriteIndex;

    /// <summary>The last frame on the sheet.</summary>
    public int LastSpriteIndex;

    /// <summary>How many frames a second.</summary>
    public int Fps;

    /// <summary>The time the frame shown has left, which runs out once and is set going again for the next.</summary>
    public GameTimer FrameTimer;

    /// <summary>An animation over the frames given, its first frame's timer already running.</summary>
    public static AnimationConfig New(int first, int last, int fps) =>
        new() { FirstSpriteIndex = first, LastSpriteIndex = last, Fps = fps, FrameTimer = TimerFromFps(fps) };

    /// <summary>A frame's time at the speed given, once.</summary>
    public static GameTimer TimerFromFps(int fps) => GameTimer.FromSeconds(1f / fps, TimerMode.Once);

    /// <summary>
    /// On to the next frame as the frame's time runs out, its timer set going again, or at the last
    /// frame back to the first, where it stops until a key starts it.
    /// </summary>
    [OnUpdate]
    public void ExecuteAnimations(BehaviorContext ctx)
    {
        if (!FrameTimer.Tick(ctx.Time.Delta).JustFinished || ctx.Ecs.Wrap<SpriteRef>(ctx.Entity).TextureAtlas is not { } atlas) return;

        if ((int)atlas.Index == LastSpriteIndex)
        {
            Render2d.SetSpriteFrames([ctx.Entity], [(uint)FirstSpriteIndex]);
        }
        else
        {
            Render2d.SetSpriteFrames([ctx.Entity], [(uint)atlas.Index + 1]);
            FrameTimer = TimerFromFps(Fps);
        }
    }
}

/// <summary>The sprite on the left, which the left arrow starts.</summary>
[Behavior]
public partial struct LeftSprite
{
    /// <summary>Its animation started again by the left arrow.</summary>
    [OnUpdate]
    public void TriggerAnimation(BehaviorContext ctx, ref AnimationConfig animation)
    {
        if (ctx.Input.KeyPressed(Key.ArrowLeft)) animation.FrameTimer = AnimationConfig.TimerFromFps(animation.Fps);
    }
}

/// <summary>The sprite on the right, which the right arrow starts.</summary>
[Behavior]
public partial struct RightSprite
{
    /// <summary>Its animation started again by the right arrow.</summary>
    [OnUpdate]
    public void TriggerAnimation(BehaviorContext ctx, ref AnimationConfig animation)
    {
        if (ctx.Input.KeyPressed(Key.ArrowRight)) animation.FrameTimer = AnimationConfig.TimerFromFps(animation.Fps);
    }
}
