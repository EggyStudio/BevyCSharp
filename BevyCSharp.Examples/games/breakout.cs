// Bevy's breakout example, examples/showcase/breakout.rs at v0.19.1, by Bevy's contributors under
// MIT or Apache-2.0, written again in C#.

using Bevy;
using Bevy.Reflected;

namespace BevyCSharp.Examples.Games;

// A simplified Breakout game. The arrow keys move the paddle, the ball bounces off the walls, the
// paddle and the bricks, and every brick it breaks is a point. Bevy's helper for stepping through
// the systems one at a time is left out, since stepping is not offered here.
internal static class Breakout
{
    internal static readonly (float X, float Y) PaddleSize = (120f, 20f);
    private const float GapBetweenPaddleAndFloor = 60f;
    internal const float PaddleSpeed = 500f, PaddlePadding = 10f;

    private static readonly Vec3 BallStartingPosition = new(0f, -50f, 1f);
    internal const float BallDiameter = 30f;
    private const float BallSpeed = 400f;

    internal const float WallThickness = 10f, LeftWall = -450f, RightWall = 450f, BottomWall = -300f, TopWall = 300f;

    private static readonly (float X, float Y) BrickSize = (100f, 30f);
    private const float GapBetweenPaddleAndBricks = 270f, GapBetweenBricks = 5f, GapBetweenBricksAndCeiling = 20f, GapBetweenBricksAndSides = 20f;

    private const float ScoreboardFontSize = 33f;

    private static readonly Color BackgroundColor = Color.FromSrgb(0.9f, 0.9f, 0.9f);
    private static readonly Color PaddleColor = Color.FromSrgb(0.3f, 0.3f, 0.7f);
    private static readonly Color BallColor = Color.FromSrgb(1f, 0.5f, 0.5f);
    private static readonly Color BrickColor = Color.FromSrgb(0.5f, 0.5f, 1f);
    private static readonly Color WallColor = Color.FromSrgb(0.8f, 0.8f, 0.8f);
    private static readonly Color TextColor = Color.FromSrgb(0.5f, 0.5f, 1f);
    private static readonly Color ScoreColor = Color.FromSrgb(1f, 0.5f, 0.5f);

    // Bevy's Score and CollisionSound resources.
    internal static int Score;
    internal static AssetHandle CollisionSound;

    public static void Build(App app) => app.Startup(Setup, "breakout.Setup");

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        Score = 0;
        Render.SetClearColor(BackgroundColor);
        Render2d.SpawnCamera2d();
        CollisionSound = AssetServer.Load(AssetKind.Audio, "sounds/breakout_collision.ogg");

        // Bevy's Sprite::from_color is a white pixel tinted, here stretched by the transform's
        // scale, which the collisions read as the size.
        var white = Render.CreateImage([255, 255, 255, 255], 1, 1);
        Entity Block(float x, float y, (float X, float Y) size, Color color)
        {
            var entity = ecs.Spawn();
            ecs.Add(entity, new Transform(new Vec3(x, y, 0f), Quat.Identity, new Vec3(size.X, size.Y, 1f)));
            Render2d.SetSprite(ecs, entity, white, new SpriteSettings { Color = color, Size = (1f, 1f) });
            ecs.Add(entity, new Collider());
            return entity;
        }

        var paddleY = BottomWall + GapBetweenPaddleAndFloor;
        ecs.Add(Block(0f, paddleY, PaddleSize, PaddleColor), new Paddle());

        var ball = ecs.Spawn();
        ecs.Add(ball, new Transform(BallStartingPosition, Quat.Identity, new Vec3(BallDiameter, BallDiameter, 1f)));
        Render2d.SetMesh(ecs, ball, Render.CreateMesh(MeshShape.Circle, 0.5f));
        Render2d.SetMaterial(ecs, ball, Render2d.CreateMaterial(new ColorMaterialSettings { Color = BallColor }));
        ecs.Add(ball, new Ball());
        var start = new Vec2(0.5f, -0.5f);
        ecs.Add(ball, new Velocity { Value = start * (BallSpeed / start.Length) });

        // "Score: " in the text's own color, and the score as its span, which the scoreboard writes.
        var style = new UiTextSettings { FontSize = ScoreboardFontSize };
        var scoreboard = Ui.SpawnText("Score: ", new UiSettings { Absolute = true, Top = Length.Px(5f), Left = Length.Px(5f) }, style);
        ecs.Wrap<TextColorRef>(scoreboard).Value = TextColor;
        Ui.SpawnTextSpan(scoreboard, string.Empty, style, ScoreColor);
        ecs.Add(scoreboard, new ScoreboardUi());

        // The four walls, each as long as its side of the arena and a wall's thickness more, so
        // the corners close.
        var (arenaWidth, arenaHeight) = (RightWall - LeftWall, TopWall - BottomWall);
        Block(LeftWall, 0f, (WallThickness, arenaHeight + WallThickness), WallColor);
        Block(RightWall, 0f, (WallThickness, arenaHeight + WallThickness), WallColor);
        Block(0f, BottomWall, (arenaWidth + WallThickness, WallThickness), WallColor);
        Block(0f, TopWall, (arenaWidth + WallThickness, WallThickness), WallColor);

        // As many rows and columns of bricks as fit between the sides, the ceiling and a gap above
        // the paddle, centered across the arena.
        var totalWidthOfBricks = arenaWidth - 2f * GapBetweenBricksAndSides;
        var bottomEdgeOfBricks = paddleY + GapBetweenPaddleAndBricks;
        var totalHeightOfBricks = TopWall - bottomEdgeOfBricks - GapBetweenBricksAndCeiling;
        var columns = (int)MathF.Floor(totalWidthOfBricks / (BrickSize.X + GapBetweenBricks));
        var rows = (int)MathF.Floor(totalHeightOfBricks / (BrickSize.Y + GapBetweenBricks));
        var centerOfBricks = (LeftWall + RightWall) / 2f;
        var leftEdgeOfBricks = centerOfBricks - columns / 2f * BrickSize.X - (columns - 1) / 2f * GapBetweenBricks;
        var (offsetX, offsetY) = (leftEdgeOfBricks + BrickSize.X / 2f, bottomEdgeOfBricks + BrickSize.Y / 2f);
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var brick = Block(offsetX + column * (BrickSize.X + GapBetweenBricks), offsetY + row * (BrickSize.Y + GapBetweenBricks), BrickSize, BrickColor);
                ecs.Add(brick, new Brick());
            }
        }
    }
}

/// <summary>A side of a box a ball struck.</summary>
internal enum Collision { Left, Right, Top, Bottom }

/// <summary>How fast a thing moves, in units a second.</summary>
[Behavior]
public partial struct Velocity
{
    /// <summary>The speed along X and Y.</summary>
    public Vec2 Value;

    /// <summary>Moved by its speed over the frame, first of the three Bevy chains.</summary>
    [OnUpdate]
    public void ApplyVelocity(BehaviorContext ctx, ref Transform transform)
    {
        transform.Translation.X += Value.X * ctx.Time.Delta;
        transform.Translation.Y += Value.Y * ctx.Time.Delta;
    }
}

/// <summary>The paddle, which the arrow keys move.</summary>
[Behavior]
public partial struct Paddle
{
    /// <summary>Moved by the held arrows, kept off the walls by a little padding, once the ball has moved.</summary>
    [OnUpdate]
    [After("Velocity.ApplyVelocity")]
    public void MovePaddle(BehaviorContext ctx, ref Transform transform)
    {
        var direction = (ctx.Input.KeyDown(Key.ArrowLeft) ? -1f : 0f) + (ctx.Input.KeyDown(Key.ArrowRight) ? 1f : 0f);
        var leftBound = Breakout.LeftWall + Breakout.WallThickness / 2f + Breakout.PaddleSize.X / 2f + Breakout.PaddlePadding;
        var rightBound = Breakout.RightWall - Breakout.WallThickness / 2f - Breakout.PaddleSize.X / 2f - Breakout.PaddlePadding;
        transform.Translation.X = Math.Clamp(transform.Translation.X + direction * Breakout.PaddleSpeed * ctx.Time.Delta, leftBound, rightBound);
    }
}

/// <summary>The ball.</summary>
[Behavior]
public partial struct Ball
{
    /// <summary>
    /// Each collider it touches sounds, is broken if it is a brick, and turns the ball back from the
    /// side it struck unless the ball is already moving away from that side, once the paddle has
    /// moved, last of the three Bevy chains.
    /// </summary>
    /// <remarks>
    /// A brick is despawned by a command, as Bevy's is, since the colliders are being walked. Bevy
    /// triggers an event that an observer plays the sound for, and here the sound is played where
    /// the event is triggered.
    /// </remarks>
    [OnUpdate]
    [After("Paddle.MovePaddle")]
    public void CheckForCollisions(BehaviorContext ctx, in Transform transform, ref Velocity velocity)
    {
        var center = transform.Translation;
        foreach (var collider in ctx.Ecs.Query<Collider>(markChanged: false))
        {
            var box = ctx.Ecs.GetOrDefault<Transform>(collider.Entity);
            if (BallCollision(center.X, center.Y, Breakout.BallDiameter / 2f, box.Translation.X, box.Translation.Y, box.Scale.X / 2f, box.Scale.Y / 2f) is not { } collision) continue;

            Audio.Play(Breakout.CollisionSound, new AudioSettings { Mode = PlaybackMode.Despawn });
            if (ctx.Ecs.Has<Brick>(collider.Entity))
            {
                ctx.Cmd.Despawn(collider.Entity);
                Breakout.Score++;
            }

            switch (collision)
            {
                case Collision.Left when velocity.Value.X > 0f:
                case Collision.Right when velocity.Value.X < 0f:
                    velocity.Value.X = -velocity.Value.X;
                    break;
                case Collision.Top when velocity.Value.Y < 0f:
                case Collision.Bottom when velocity.Value.Y > 0f:
                    velocity.Value.Y = -velocity.Value.Y;
                    break;
            }
        }
    }

    // Which side of the box a circle touches, judged by where the box's closest point to the
    // circle's center lies, or nothing when they do not touch.
    private static Collision? BallCollision(float x, float y, float radius, float boxX, float boxY, float halfWidth, float halfHeight)
    {
        var closestX = Math.Clamp(x, boxX - halfWidth, boxX + halfWidth);
        var closestY = Math.Clamp(y, boxY - halfHeight, boxY + halfHeight);
        var (offsetX, offsetY) = (x - closestX, y - closestY);
        if (offsetX * offsetX + offsetY * offsetY > radius * radius) return null;

        if (MathF.Abs(offsetX) > MathF.Abs(offsetY)) return offsetX < 0f ? Collision.Left : Collision.Right;
        return offsetY > 0f ? Collision.Top : Collision.Bottom;
    }
}

/// <summary>A thing the ball bounces off, the walls, the paddle and the bricks.</summary>
[Behavior]
public partial struct Collider;

/// <summary>A brick, which the ball breaks for a point.</summary>
[Behavior]
public partial struct Brick;

/// <summary>The scoreboard's text, whose span shows the score.</summary>
[Behavior]
public partial struct ScoreboardUi
{
    /// <summary>The score written into the text's span, as Bevy writes its text's second section.</summary>
    [OnUpdate]
    public void UpdateScoreboard(BehaviorContext ctx) =>
        ctx.Ecs.Wrap<TextSpanRef>(ctx.Ecs.ChildrenOf(ctx.Entity)[0]).Value = Breakout.Score.ToString();
}
