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
    private static readonly (float X, float Y) PaddleSize = (120f, 20f);
    private const float GapBetweenPaddleAndFloor = 60f, PaddleSpeed = 500f, PaddlePadding = 10f;

    private static readonly Vec3 BallStartingPosition = new(0f, -50f, 1f);
    private const float BallDiameter = 30f, BallSpeed = 400f;

    private const float WallThickness = 10f, LeftWall = -450f, RightWall = 450f, BottomWall = -300f, TopWall = 300f;

    private static readonly (float X, float Y) BrickSize = (100f, 30f);
    private const float GapBetweenPaddleAndBricks = 270f, GapBetweenBricks = 5f, GapBetweenBricksAndCeiling = 20f, GapBetweenBricksAndSides = 20f;

    private const float ScoreboardFontSize = 33f;

    private static readonly (float R, float G, float B, float A) BackgroundColor = Color.FromSrgb(0.9f, 0.9f, 0.9f);
    private static readonly (float R, float G, float B, float A) PaddleColor = Color.FromSrgb(0.3f, 0.3f, 0.7f);
    private static readonly (float R, float G, float B, float A) BallColor = Color.FromSrgb(1f, 0.5f, 0.5f);
    private static readonly (float R, float G, float B, float A) BrickColor = Color.FromSrgb(0.5f, 0.5f, 1f);
    private static readonly (float R, float G, float B, float A) WallColor = Color.FromSrgb(0.8f, 0.8f, 0.8f);
    private static readonly (float R, float G, float B, float A) TextColor = Color.FromSrgb(0.5f, 0.5f, 1f);
    private static readonly (float R, float G, float B, float A) ScoreColor = Color.FromSrgb(1f, 0.5f, 0.5f);

    internal struct Paddle;

    internal struct Ball;

    internal struct Velocity
    {
        public float X, Y;
    }

    internal struct Brick;

    internal struct Collider;

    private enum Collision { Left, Right, Top, Bottom }

    private static int _score;
    private static Entity _scoreSpan;
    private static AssetHandle _collisionSound;

    public static void Build(App app)
    {
        app.Startup(Setup, "breakout.Setup");

        // Bevy chains these three, so the ball has moved before the collisions are looked for.
        app.Update(ApplyVelocity, "breakout.ApplyVelocity");
        app.Update(MovePaddle, "breakout.MovePaddle");
        app.Update(CheckForCollisions, "breakout.CheckForCollisions");
        app.Update(ctx => ctx.Ecs.Wrap<TextSpanRef>(_scoreSpan).Value = _score.ToString(), "breakout.UpdateScoreboard");
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        _score = 0;
        Render.SetClearColor(BackgroundColor);
        Render2d.SpawnCamera2d();
        _collisionSound = AssetServer.Load(AssetKind.Audio, "sounds/breakout_collision.ogg");

        // Bevy's Sprite::from_color is a white pixel tinted, here stretched by the transform's
        // scale, which the collisions read as the size.
        var white = Render.CreateImage([255, 255, 255, 255], 1, 1);
        Entity Block(float x, float y, (float X, float Y) size, (float R, float G, float B, float A) color)
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
        var direction = start * (BallSpeed / start.Length);
        ecs.Add(ball, new Velocity { X = direction.X, Y = direction.Y });

        var style = new UiTextSettings { FontSize = ScoreboardFontSize };
        var scoreboard = Ui.SpawnText(string.Empty, new UiSettings { Absolute = true, Top = Length.Px(5f), Left = Length.Px(5f) }, style);
        Ui.SpawnTextSpan(scoreboard, "Score: ", style, TextColor);
        _scoreSpan = Ui.SpawnTextSpan(scoreboard, string.Empty, style, ScoreColor);

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

    // Held arrows move the paddle, kept off the walls by a little padding.
    private static void MovePaddle(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var direction = (ctx.Input.KeyDown(Key.ArrowLeft) ? -1f : 0f) + (ctx.Input.KeyDown(Key.ArrowRight) ? 1f : 0f);
        var leftBound = LeftWall + WallThickness / 2f + PaddleSize.X / 2f + PaddlePadding;
        var rightBound = RightWall - WallThickness / 2f - PaddleSize.X / 2f - PaddlePadding;
        foreach (var paddle in ecs.EntitiesWith<Paddle>())
        {
            var transform = ecs.GetOrDefault<Transform>(paddle);
            transform.Translation.X = Math.Clamp(transform.Translation.X + direction * PaddleSpeed * ctx.Time.Delta, leftBound, rightBound);
            ecs.Set(paddle, transform);
        }
    }

    private static void ApplyVelocity(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var entity in ecs.EntitiesWith<Velocity>())
        {
            var (transform, velocity) = (ecs.GetOrDefault<Transform>(entity), ecs.GetOrDefault<Velocity>(entity));
            transform.Translation.X += velocity.X * ctx.Time.Delta;
            transform.Translation.Y += velocity.Y * ctx.Time.Delta;
            ecs.Set(entity, transform);
        }
    }

    // A ball touching a collider sounds, breaks it if it is a brick, and turns back from the side
    // it struck unless it is already moving away from that side.
    private static void CheckForCollisions(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var ball in ecs.EntitiesWith<Ball>())
        {
            var center = ecs.GetOrDefault<Transform>(ball).Translation;
            var velocity = ecs.GetOrDefault<Velocity>(ball);
            foreach (var collider in ecs.EntitiesWith<Collider>())
            {
                var box = ecs.GetOrDefault<Transform>(collider);
                if (BallCollision(center.X, center.Y, BallDiameter / 2f, box.Translation.X, box.Translation.Y, box.Scale.X / 2f, box.Scale.Y / 2f) is not { } collision) continue;

                Audio.Play(_collisionSound, new AudioSettings { Mode = PlaybackMode.Despawn });
                if (ecs.Has<Brick>(collider))
                {
                    ecs.Despawn(collider);
                    _score++;
                }

                switch (collision)
                {
                    case Collision.Left when velocity.X > 0f:
                    case Collision.Right when velocity.X < 0f:
                        velocity.X = -velocity.X;
                        break;
                    case Collision.Top when velocity.Y < 0f:
                    case Collision.Bottom when velocity.Y > 0f:
                        velocity.Y = -velocity.Y;
                        break;
                }
            }

            ecs.Set(ball, velocity);
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
