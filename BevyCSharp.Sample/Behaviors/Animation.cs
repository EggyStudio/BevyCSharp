using Bevy;

namespace BevyCSharp.Sample.Behaviors;

/// <summary>
/// Steps a sprite through the frames of a sheet.
/// </summary>
/// <remarks>
/// <para>
/// This is the whole of sprite animation, and it lives here rather than in the library on purpose.
/// Nothing about it needs a bridge: <see cref="SpriteSettings.Frame"/> names a frame by number, so
/// counting the seconds and calling <see cref="Render2d.SetSprite(EcsWorld, Entity, AssetHandle,
/// SpriteSettings)"/> is all there is to it. A game wanting frame events, a queue of clips, or
/// animation that follows the state machine will write its own, and one shipped in the library
/// would be the wrong shape for every one of them while still costing a query a frame.
/// </para>
/// <para>
/// Copy it. It is here to be read and taken.
/// </para>
/// </remarks>
[Behavior]
public partial struct SpriteAnimation
{
    /// <summary>The sheet the frames are cut from.</summary>
    public AssetHandle Sheet;

    /// <summary>The layout naming where each frame sits on it.</summary>
    public AssetHandle Frames;

    /// <summary>The first frame of the run, counted from zero.</summary>
    public uint First;

    /// <summary>The last frame of the run, which is played and then followed by the first.</summary>
    public uint Last;

    /// <summary>Which frame is on screen.</summary>
    public uint Current;

    /// <summary>How long each frame is held, in seconds.</summary>
    public float Hold;

    /// <summary>How much of the current frame's time has passed.</summary>
    public float Elapsed;

    /// <summary>How wide and tall the sprite is drawn, in world units.</summary>
    public float Size;

    /// <summary>Whether it starts again at <see cref="First"/> after <see cref="Last"/>.</summary>
    public bool Loops;

    /// <summary>
    /// Advances this entity's frame, and writes it to the sprite when it changes.
    /// </summary>
    /// <remarks>
    /// Written only on a change, because <see cref="Render2d.SetSprite(EcsWorld, Entity,
    /// AssetHandle, SpriteSettings)"/> replaces the whole component, and doing that every frame
    /// would mark it changed every frame for anything watching.
    /// </remarks>
    [OnUpdate]
    public void Tick(BehaviorContext ctx)
    {
        if (Hold <= 0f || Last < First) return;

        Elapsed += ctx.Time.Delta;
        if (Elapsed < Hold) return;

        // A subtraction rather than a reset, so a frame that took longer than its hold carries the
        // overflow into the next one instead of dropping it, which is what keeps a long animation
        // from drifting against the clock.
        Elapsed -= Hold;

        if (Current >= Last)
        {
            if (!Loops)
            {
                Hold = 0f;
                return;
            }

            Current = First;
        }
        else
        {
            Current++;
        }

        Render2d.SetSprite(ctx.Ecs, ctx.Entity, Sheet, new SpriteSettings
        {
            Atlas = Frames,
            Frame = Current,
            Size = (Size, Size),
        });
    }
}

/// <summary>
/// Puts an animated sprite on screen, over the 3D scene.
/// </summary>
/// <remarks>
/// The sheet is drawn here rather than loaded, so the sample carries no picture file for it and
/// <see cref="Render.CreateImage"/> is shown doing what it is for. A real game loads a PNG an
/// artist made and the rest of this is unchanged.
/// </remarks>
[Behavior]
public partial struct AnimatedBadge
{
    /// <summary>How many frames the drawn sheet holds.</summary>
    private const uint Count = 8;

    /// <summary>How large one frame is, in pixels of the sheet.</summary>
    private const uint Side = 16;

    /// <summary>Builds the sheet, the layout and the sprite that plays it.</summary>
    [OnStartup]
    public static void Spawn(BehaviorContext ctx)
    {
        if (!App.HasRenderer || ctx.Res<Config>().Headless) return;

        // Above the scene camera, so it draws over what is already there rather than clearing it.
        Render2d.SpawnCamera2d(order: 1);

        var sheet = Render.CreateImage(Strip(), Side * Count, Side);
        var frames = Render2d.CreateAtlas(Side, Side, columns: Count, rows: 1);

        var badge = ctx.Ecs.Spawn();
        ctx.Ecs.SetName(badge, "Animated badge");

        // A 2D camera counts in pixels from the middle of the window, so this corner is the top
        // left with room for the sprite's own size.
        ctx.Ecs.Add(badge, Transform.At(-540f, 300f, 0f));

        ctx.Ecs.Add(badge, new SpriteAnimation
        {
            Sheet = sheet,
            Frames = frames,
            Last = Count - 1,
            Hold = 0.08f,
            Size = 96f,
            Loops = true,
        });

        Render2d.SetSprite(ctx.Ecs, badge, sheet, new SpriteSettings
        {
            Atlas = frames,
            Size = (96f, 96f),
        });

        Console.WriteLine($"[Animation] a {Count}-frame sprite is playing at the top left");
    }

    /// <summary>
    /// Draws a strip of frames, each a dot one step further around a circle.
    /// </summary>
    /// <remarks>
    /// A throbber, which is the smallest animation that is obviously animating. The frames sit
    /// side by side in one picture, which is the layout every sprite sheet uses and the one
    /// <see cref="Render2d.CreateAtlas"/> cuts.
    /// </remarks>
    private static byte[] Strip()
    {
        var pixels = new byte[Side * Count * Side * 4];

        for (var frame = 0u; frame < Count; frame++)
        {
            var angle = frame / (float)Count * MathF.Tau;
            var dotX = (Side / 2f) + (MathF.Cos(angle) * Side * 0.3f);
            var dotY = (Side / 2f) + (MathF.Sin(angle) * Side * 0.3f);

            for (var y = 0u; y < Side; y++)
            {
                for (var x = 0u; x < Side; x++)
                {
                    var across = (frame * Side) + x;
                    var at = (int)(((y * Side * Count) + across) * 4);

                    var reach = MathF.Sqrt(
                        ((x + 0.5f - dotX) * (x + 0.5f - dotX))
                        + ((y + 0.5f - dotY) * (y + 0.5f - dotY)));

                    // Soft at the edge, so the dot reads as round at this size rather than as a
                    // cross of four pixels.
                    var lit = Math.Clamp(1f - (reach / 3f), 0f, 1f);

                    pixels[at] = 255;
                    pixels[at + 1] = (byte)(220 * lit);
                    pixels[at + 2] = (byte)(80 * lit);
                    pixels[at + 3] = (byte)(255 * lit);
                }
            }
        }

        return pixels;
    }
}
