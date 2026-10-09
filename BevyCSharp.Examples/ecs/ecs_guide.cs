// Bevy's ecs_guide example, examples/ecs/ecs_guide.rs at v0.20.0, by Bevy's contributors under MIT
// or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Bevy's guide to its ECS, a game of rounds in which each player may score a point a round, played
// until somebody reaches the winning score or the rounds run out. It is made of the parts of an
// ECS, components, resources, systems run once, each frame and last in a frame, systems that touch
// the whole world, and an order among them.
internal static class EcsGuide
{
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

    // The players' names, which a player points into, since a component here holds no string.
    internal static readonly List<string> Names = [];

    // Seeded, so a capture plays the same game each time, where Bevy's draws from the system.
    internal static Random Random = new(19878367);

    public static void Build(App app)
    {
        Names.Clear();
        Random = new Random(19878367);

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

        // Bevy's three sets, before the round, the round and after it, each after the one before.
        // The players' own systems are the round and the check after it, and the example's are
        // ordered about them by their names. The two that add players run in either order, as
        // they do in Bevy's set.
        app.Chain(Stage.Update, System(NewRound, "ecs_guide.NewRound"), System(NewPlayer, "ecs_guide.NewPlayer").Before("Player.ScoreSystem"));
        app.AddSystem(Stage.Update, System(ExclusivePlayer, "ecs_guide.ExclusivePlayer").Before("Player.ScoreSystem"));
        app.AddSystem(Stage.Update, System(GameOver, "ecs_guide.GameOver").After("Player.ScoreCheckSystem"));

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
        if (Random.Next(2) == 0 || state.TotalPlayers >= ctx.Res<GameRules>().MaxPlayers) return;

        state.TotalPlayers++;
        AddPlayer(ctx.Ecs, $"Player {state.TotalPlayers}");
        Console.WriteLine($"Player {state.TotalPlayers} joined the game!");
    }

    // Bevy's exclusive system, which takes the whole world, does what NewPlayer does through it.
    private static void ExclusivePlayer(BehaviorContext ctx)
    {
        var state = ctx.World.Resource<GameState>();
        if (Random.Next(2) == 0 || state.TotalPlayers >= ctx.World.Resource<GameRules>().MaxPlayers) return;

        Console.WriteLine($"Player {state.TotalPlayers + 1} has joined the game!");
        AddPlayer(ctx.Ecs, $"Player {state.TotalPlayers + 1}");
        state.TotalPlayers++;
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

/// <summary>A player, by its place among the names.</summary>
[Behavior]
public partial struct Player
{
    /// <summary>The player's place among the names.</summary>
    public int Name;

    /// <summary>
    /// The round played, each player scoring a point or not at random, with their score and their
    /// streak kept beside them, as Bevy's <c>score_system</c> plays it.
    /// </summary>
    [OnUpdate]
    public void ScoreSystem(BehaviorContext ctx, ref Score score, ref PlayerStreak streak)
    {
        var name = EcsGuide.Names[Name];
        if (EcsGuide.Random.Next(2) == 1)
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
    }

    /// <summary>A player who has reached the winning score made the winner, after the round.</summary>
    [OnUpdate]
    [After("Player.ScoreSystem")]
    public void ScoreCheckSystem(BehaviorContext ctx, in Score score)
    {
        if (score.Value == ctx.Res<EcsGuide.GameRules>().WinningScore) ctx.Res<EcsGuide.GameState>().WinningPlayer = EcsGuide.Names[Name];
    }
}

/// <summary>A player's score.</summary>
[Behavior]
public partial struct Score
{
    /// <summary>The points scored.</summary>
    public int Value;
}

/// <summary>Bevy's <c>PlayerStreak</c> enum, <c>Hot(n)</c>, <c>None</c> or <c>Cold(n)</c>, as a sign and a count.</summary>
[Behavior]
public partial struct PlayerStreak
{
    /// <summary>Above zero for a hot streak, below it for a cold one, and zero for none.</summary>
    public int Sign;

    /// <summary>How many rounds it has run.</summary>
    public int Rounds;

    /// <summary>The streak as Bevy prints it.</summary>
    public override readonly string ToString() => Sign switch
    {
        > 0 => $"{Rounds} round hot streak",
        < 0 => $"{Rounds} round cold streak",
        _ => "0 round streak",
    };
}
