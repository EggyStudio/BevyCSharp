using Bevy;

namespace BevyCSharp.Examples.Games;

// Eat the cakes. Eat them all. An example 3D game, in which an alien is moved across a board of
// tiles with the arrow keys after a cake that appears somewhere new every five seconds. A cake eaten is two
// points and one left to go is three lost, and at minus five the game is over, with the cakes eaten
// shown until Space starts again.
internal static class AlienCakeAddict
{
    private enum GameState { Playing, GameOver }

    private const int BoardSizeI = 14, BoardSizeJ = 21;
    private static readonly Vec3 ResetFocus = new(BoardSizeI / 2f, 0f, BoardSizeJ / 2f - 0.5f);

    private static float[,] _heights = new float[BoardSizeJ, BoardSizeI];
    private static (Entity Entity, int I, int J) _player;
    private static (Entity Entity, int I, int J) _bonus;
    private static AssetHandle _cake;
    private static int _score, _cakeEaten;
    private static float _moveCooldown, _bonusTimer;
    private static Vec3 _cameraShouldFocus, _cameraIsFocus;
    private static Entity _camera, _scoreboard;
    private static Random _random = new();

    public static void Build(App app)
    {
        app.AddState(GameState.Playing);
        app.Startup(SetupCameras, "alien_cake_addict.SetupCameras");
        app.AddStateSystem(GameState.Playing, entering: true, new SystemDescriptor(world => Setup(new BehaviorContext(world)), "alien_cake_addict.Setup"));
        app.On(Stage.Update, MovePlayer, "alien_cake_addict.MovePlayer", BehaviorConditions.InState(GameState.Playing));
        app.On(Stage.Update, FocusCamera, "alien_cake_addict.FocusCamera", BehaviorConditions.InState(GameState.Playing));
        app.On(Stage.Update, RotateBonus, "alien_cake_addict.RotateBonus", BehaviorConditions.InState(GameState.Playing));
        app.On(Stage.Update, ctx => Ui.SetText(_scoreboard, $"Sugar Rush: {_score}"), "alien_cake_addict.Scoreboard", BehaviorConditions.InState(GameState.Playing));
        app.On(Stage.Update, SpawnBonus, "alien_cake_addict.SpawnBonus", BehaviorConditions.InState(GameState.Playing));
        app.AddStateSystem(GameState.GameOver, entering: true, new SystemDescriptor(world => DisplayScore(new BehaviorContext(world)), "alien_cake_addict.DisplayScore"));
        app.On(Stage.Update, ctx => { if (ctx.Input.KeyPressed(Key.Space)) ctx.SetState(GameState.Playing); }, "alien_cake_addict.GameOverKeyboard", BehaviorConditions.InState(GameState.GameOver));
    }

    private static void SetupCameras(BehaviorContext ctx)
    {
        (_cameraShouldFocus, _cameraIsFocus, _bonusTimer) = (ResetFocus, ResetFocus, 0f);
        _camera = ctx.Ecs.Camera(Transform.LookingAt(new Vec3(-(BoardSizeI / 2f), 2f * BoardSizeJ / 3f, BoardSizeJ / 2f - 0.5f), _cameraIsFocus, Vec3.UnitY));
    }

    private static void Setup(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;

        // Seeded where the workflow runs, so its picture is the same every time, as Bevy's is.
        _random = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true" ? new Random(19878367) : new Random();
        (_cakeEaten, _score, _moveCooldown) = (0, 0, 0.3f);
        _player = (Entity.None, BoardSizeI / 2, BoardSizeJ / 2);
        _bonus = (Entity.None, 0, 0);

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Intensity = 2_000_000f, Range = 30f, Shadows = true });
        ecs.Add(light, Transform.At(4f, 10f, 4f));
        ecs.DespawnOnExit(light, GameState.Playing);

        // A board of tiles, each a little above or below the next.
        var tile = AssetServer.LoadGltfScene("models/AlienCake/tile.glb");
        _heights = new float[BoardSizeJ, BoardSizeI];
        for (var j = 0; j < BoardSizeJ; j++)
        {
            for (var i = 0; i < BoardSizeI; i++)
            {
                _heights[j, i] = (float)(_random.NextDouble() * 0.2 - 0.1);
                var cell = ecs.SpawnScene(tile);
                ecs.Set(cell, Transform.At(i, _heights[j, i] - 0.2f, j));
                ecs.DespawnOnExit(cell, GameState.Playing);
            }
        }

        var alien = ecs.SpawnScene(AssetServer.LoadGltfScene("models/AlienCake/alien.glb"));
        ecs.Set(alien, new Transform(new Vec3(_player.I, _heights[_player.J, _player.I], _player.J), Quat.FromRotationY(-MathF.PI / 2f), Vec3.One));
        ecs.DespawnOnExit(alien, GameState.Playing);
        _player.Entity = alien;

        _cake = AssetServer.LoadGltfScene("models/AlienCake/cakeBirthday.glb");

        _scoreboard = Ui.SpawnText("Score:", new UiSettings { Absolute = true, Top = Length.Px(5f), Left = Length.Px(5f), Color = Scene.Srgb(0.5f, 0.5f, 1f) }, 33f);
        ecs.DespawnOnExit(_scoreboard, GameState.Playing);
    }

    // A step at most every three tenths of a second, the alien turned the way it went, and a cake
    // under it eaten.
    private static void MovePlayer(BehaviorContext ctx)
    {
        var (ecs, input) = (ctx.Ecs, ctx.Input);
        _moveCooldown -= ctx.Time.Delta;
        if (_moveCooldown <= 0f)
        {
            var (moved, rotation) = (false, 0f);
            if (input.KeyDown(Key.ArrowUp)) { if (_player.I < BoardSizeI - 1) _player.I++; (rotation, moved) = (-MathF.PI / 2f, true); }
            if (input.KeyDown(Key.ArrowDown)) { if (_player.I > 0) _player.I--; (rotation, moved) = (MathF.PI / 2f, true); }
            if (input.KeyDown(Key.ArrowRight)) { if (_player.J < BoardSizeJ - 1) _player.J++; (rotation, moved) = (MathF.PI, true); }
            if (input.KeyDown(Key.ArrowLeft)) { if (_player.J > 0) _player.J--; (rotation, moved) = (0f, true); }

            if (moved)
            {
                _moveCooldown = 0.3f;
                ecs.Set(_player.Entity, new Transform(new Vec3(_player.I, _heights[_player.J, _player.I], _player.J), Quat.FromRotationY(rotation), Vec3.One));
            }
        }

        if (_bonus.Entity != Entity.None && _player.I == _bonus.I && _player.J == _bonus.J)
        {
            (_score, _cakeEaten) = (_score + 2, _cakeEaten + 1);
            ecs.Despawn(_bonus.Entity);
            _bonus.Entity = Entity.None;
        }
    }

    // The camera turns toward the point between the alien and the cake, a part of the way each
    // frame, while that point is more than a fifth of a unit off.
    private static void FocusCamera(BehaviorContext ctx)
    {
        const float Speed = 2f;
        var ecs = ctx.Ecs;
        var player = ecs.GetOrDefault<Transform>(_player.Entity).Translation;
        _cameraShouldFocus = _bonus.Entity != Entity.None ? (player + ecs.GetOrDefault<Transform>(_bonus.Entity).Translation) * 0.5f : player;

        var motion = _cameraShouldFocus - _cameraIsFocus;
        if (motion.Length > 0.2f) _cameraIsFocus += motion * (Speed * ctx.Time.Delta);
        ecs.Set(_camera, Transform.LookingAt(ecs.GetOrDefault<Transform>(_camera).Translation, _cameraIsFocus, Vec3.UnitY));
    }

    // Every five seconds a cake left uneaten costs three points, the game ends at minus five, and a
    // new one appears where the alien is not, with a yellow light over it.
    private static void SpawnBonus(BehaviorContext ctx)
    {
        _bonusTimer += ctx.Time.Delta;
        if (_bonusTimer < 5f) return;
        _bonusTimer -= 5f;

        var ecs = ctx.Ecs;
        if (_bonus.Entity != Entity.None)
        {
            _score -= 3;
            ecs.Despawn(_bonus.Entity);
            _bonus.Entity = Entity.None;
            if (_score <= -5)
            {
                ctx.SetState(GameState.GameOver);
                return;
            }
        }

        do (_bonus.I, _bonus.J) = (_random.Next(BoardSizeI), _random.Next(BoardSizeJ));
        while (_bonus.I == _player.I && _bonus.J == _player.J);

        var cake = ecs.SpawnScene(_cake);
        ecs.Set(cake, Transform.At(_bonus.I, _heights[_bonus.J, _bonus.I] + 0.2f, _bonus.J));
        ecs.DespawnOnExit(cake, GameState.Playing);

        var light = Render.SpawnLight(new LightSettings { Kind = LightKind.Point, Color = (1f, 1f, 0f), Intensity = 500_000f, Range = 10f, Shadows = false });
        ecs.Add(light, Transform.At(0f, 2f, 0f));
        ecs.SetParent(light, cake);
        _bonus.Entity = cake;
    }

    // The cake turns, and swells and shrinks the more as the score grows.
    private static void RotateBonus(BehaviorContext ctx)
    {
        if (_bonus.Entity == Entity.None) return;
        var at = ctx.Ecs.GetOrDefault<Transform>(_bonus.Entity);
        var scale = 1f + MathF.Abs(_score / 10f * MathF.Sin(ctx.Time.Elapsed));
        ctx.Ecs.Set(_bonus.Entity, at with { Rotation = Quat.FromRotationY(ctx.Time.Delta) * at.Rotation, Scale = new Vec3(scale) });
    }

    private static void DisplayScore(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        var screen = Ui.SpawnNode(new UiSettings { Width = Length.Percent(100f), Height = Length.Percent(100f), Align = UiAlign.Center, Justify = UiJustify.Center });
        ecs.SetParent(Ui.SpawnText($"Cake eaten: {_cakeEaten}", new UiSettings { Color = Scene.Srgb(0.5f, 0.5f, 1f) }, 67f), screen);
        ecs.DespawnOnExit(screen, GameState.GameOver);
    }
}
