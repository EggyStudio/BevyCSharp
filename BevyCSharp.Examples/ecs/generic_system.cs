// Bevy's generic_system example, examples/ecs/generic_system.rs at v0.19.1, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how one system written over a type is used with different types, here to despawn what a
// state leaves behind. Space moves from the menu into the game.
internal static class GenericSystem
{
    internal enum AppState { MainMenu, InGame }

    internal static readonly string[] Lines = ["I will print until you press space.", "I will always print"];

    public static void Build(App app)
    {
        app.AddState(AppState.MainMenu);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var menu = ecs.Spawn();
            ecs.Add(menu, new PrinterTick { Timer = GameTimer.FromSeconds(1f, TimerMode.Repeating) });
            ecs.Add(menu, new TextToPrint { Line = 0 });
            ecs.Add(menu, new MenuClose());

            var level = ecs.Spawn();
            ecs.Add(level, new PrinterTick { Timer = GameTimer.FromSeconds(1f, TimerMode.Repeating) });
            ecs.Add(level, new TextToPrint { Line = 1 });
            ecs.Add(level, new LevelUnload());
        }, "generic_system.Setup");

        app.On(Stage.Update, ctx =>
        {
            if (ctx.Input.KeyDown(Key.Space)) ctx.SetState(AppState.InGame);
        }, "generic_system.TransitionToInGame", BehaviorConditions.InState(AppState.MainMenu));

        app.AddStateSystem(AppState.MainMenu, entering: false, new SystemDescriptor(world => Cleanup<MenuClose>(new BehaviorContext(world)), "generic_system.Cleanup<MenuClose>"));
        app.AddStateSystem(AppState.InGame, entering: false, new SystemDescriptor(world => Cleanup<LevelUnload>(new BehaviorContext(world)), "generic_system.Cleanup<LevelUnload>"));
    }

    // Despawns every entity carrying T, whichever T it is given.
    private static void Cleanup<T>(BehaviorContext ctx) where T : unmanaged
    {
        foreach (var entity in ctx.Ecs.EntitiesWith<T>()) ctx.Ecs.Despawn(entity);
    }
}

/// <summary>A line to print, by its place among the example's lines, since a component here holds no string.</summary>
[Behavior]
public partial struct TextToPrint
{
    /// <summary>The line's place.</summary>
    public int Line;
}

/// <summary>A timer that says when its entity's line is printed again.</summary>
[Behavior]
public partial struct PrinterTick
{
    /// <summary>A second, over and over.</summary>
    public GameTimer Timer;

    /// <summary>The entity's line printed each time the timer runs out.</summary>
    [OnUpdate]
    public void PrintText(BehaviorContext ctx, in TextToPrint text)
    {
        if (Timer.Tick(ctx.Time.Delta).JustFinished) Console.WriteLine(GenericSystem.Lines[text.Line]);
    }
}

/// <summary>What goes when the menu closes.</summary>
[Behavior]
public partial struct MenuClose;

/// <summary>What goes when the level ends.</summary>
[Behavior]
public partial struct LevelUnload;
