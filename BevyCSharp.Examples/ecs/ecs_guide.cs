using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Bevy's guide to its ECS, a game of rounds in which each player may score a point a round, played
// until somebody reaches the winning score or the rounds run out. It is made of the parts of an
// ECS, components, resources, systems run once, each frame and last in a frame, systems that touch
// the whole world, and an order among them.
internal static class EcsGuide
{
    // A player, by its place in Names, since a component here holds no string.
    internal struct Player
    {
        public int Name;
    }

    internal struct Score
    {
        public int Value;
    }

    // Bevy's PlayerStreak enum, Hot(n), None or Cold(n), as a sign and a count.
    internal struct PlayerStreak
    {
        public int Sign;
        public int Rounds;

        public override readonly string ToString() => Sign switch
        {
            > 0 => $"{Rounds} round hot streak",
            < 0 => $"{Rounds} round cold streak",
            _ => "0 round streak",
        };
    }

    internal sealed class GameState
    {
        public int CurrentRound;
        public int TotalPlayers;
        public string? WinningPlayer;
    }

    internal sealed class GameRules
    {
        public int WinningScore;
        public int MaxRounds;
        public int MaxPlayers;
    }

    private static readonly List<string> Names = [];

    // Seeded, so a capture plays the same game each time, where Bevy's draws from the system.
    private static Random _random = new(19878367);

    public static void Build(App app)
    {
        Names.Clear();
        _random = new Random(19878367);

        app.Startup(ctx =>
        {
            ctx.World.InsertResource(new GameState());
            ctx.World.InsertResource(new GameRules { MaxRounds = 10, WinningScore = 4, MaxPlayers = 4 });
            AddPlayer(ctx.Ecs, "Alice");
            AddPlayer(ctx.Ecs, "Bob");
            ctx.Res<GameState>().TotalPlayers = 2;
        }, "ecs_guide.Startup");

        // In no order against the rest, as Bevy's is in no set.
        app.Update(_ => Console.WriteLine("This game is fun!"), "ecs_guide.PrintMessage");

        // Bevy's three sets, before the round, the round and after it, each after the one before,
        // stated here as each system after the ones it follows. The two that add players run in
        // either order, as they do in Bevy's set.
        app.Chain(Stage.Update, System(NewRound, "ecs_guide.NewRound"), System(NewPlayer, "ecs_guide.NewPlayer"));
        app.AddSystem(Stage.Update, System(ExclusivePlayer, "ecs_guide.ExclusivePlayer"));
        app.AddSystem(Stage.Update, System(ScoreRound, "ecs_guide.Score").After("ecs_guide.NewPlayer").After("ecs_guide.ExclusivePlayer"));
        app.Chain(Stage.Update, System(ScoreCheck, "ecs_guide.ScoreCheck").After("ecs_guide.Score"), System(GameOver, "ecs_guide.GameOver"));

        var counter = 0;
        app.On(Stage.Last, _ =>
        {
            counter++;
            Console.WriteLine($"In set 'Last' for the {counter}th time");
            Console.WriteLine();
        }, "ecs_guide.PrintAtEndRound");
    }

    private static SystemDescriptor System(Action<BehaviorContext> run, string name) => new(world => run(new BehaviorContext(world)), name);

    private static void NewRound(BehaviorContext ctx)
    {
        var state = ctx.Res<GameState>();
        state.CurrentRound++;
        Console.WriteLine($"Begin round {state.CurrentRound} of {ctx.Res<GameRules>().MaxRounds}");
    }

    private static void NewPlayer(BehaviorContext ctx)
    {
        var state = ctx.Res<GameState>();
        if (_random.Next(2) == 0 || state.TotalPlayers >= ctx.Res<GameRules>().MaxPlayers) return;

        state.TotalPlayers++;
        AddPlayer(ctx.Ecs, $"Player {state.TotalPlayers}");
        Console.WriteLine($"Player {state.TotalPlayers} joined the game!");
    }

    // Bevy's exclusive system, which takes the whole world, does what NewPlayer does through it.
    private static void ExclusivePlayer(BehaviorContext ctx)
    {
        var state = ctx.World.Resource<GameState>();
        if (_random.Next(2) == 0 || state.TotalPlayers >= ctx.World.Resource<GameRules>().MaxPlayers) return;

        Console.WriteLine($"Player {state.TotalPlayers + 1} has joined the game!");
        AddPlayer(ctx.Ecs, $"Player {state.TotalPlayers + 1}");
        state.TotalPlayers++;
    }

    private static void ScoreRound(BehaviorContext ctx)
    {
        var ecs = ctx.Ecs;
        foreach (var entity in ecs.EntitiesWith<Player>())
        {
            var name = Names[ecs.GetOrDefault<Player>(entity).Name];
            var score = ecs.GetOrDefault<Score>(entity);
            var streak = ecs.GetOrDefault<PlayerStreak>(entity);

            if (_random.Next(2) == 1)
            {
                score.Value++;
                streak = streak.Sign > 0 ? streak with { Rounds = streak.Rounds + 1 } : new PlayerStreak { Sign = 1, Rounds = 1 };
                Console.WriteLine($"{name} scored a point! Their score is: {score.Value} ({streak})");
            }
            else
            {
                streak = streak.Sign < 0 ? streak with { Rounds = streak.Rounds + 1 } : new PlayerStreak { Sign = -1, Rounds = 1 };
                Console.WriteLine($"{name} did not score a point! Their score is: {score.Value} ({streak})");
            }

            ecs.Set(entity, score);
            ecs.Set(entity, streak);
        }
    }

    private static void ScoreCheck(BehaviorContext ctx)
    {
        foreach (var entity in ctx.Ecs.EntitiesWith<Player>())
            if (ctx.Ecs.GetOrDefault<Score>(entity).Value == ctx.Res<GameRules>().WinningScore)
                ctx.Res<GameState>().WinningPlayer = Names[ctx.Ecs.GetOrDefault<Player>(entity).Name];
    }

    private static void GameOver(BehaviorContext ctx)
    {
        var state = ctx.Res<GameState>();
        if (state.WinningPlayer is { } player)
        {
            Console.WriteLine($"{player} won the game!");
            ctx.Exit();
        }
        else if (state.CurrentRound == ctx.Res<GameRules>().MaxRounds)
        {
            Console.WriteLine("Ran out of rounds. Nobody wins!");
            ctx.Exit();
        }
    }

    private static void AddPlayer(EcsWorld ecs, string name)
    {
        Names.Add(name);
        var player = ecs.Spawn();
        ecs.Add(player, new Player { Name = Names.Count - 1 });
        ecs.Add(player, new Score());
        ecs.Add(player, new PlayerStreak());
    }
}
