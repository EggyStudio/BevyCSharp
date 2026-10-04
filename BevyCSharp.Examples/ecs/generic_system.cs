using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Shows how one system written over a type is used with different types, here to despawn what a
// state leaves behind. Space moves from the menu into the game.
internal static class GenericSystem
{
    internal enum AppState { MainMenu, InGame }

    internal struct PrinterTick
    {
        public float Elapsed;
    }

    // The line it prints, by its place in Lines, since a component here holds no string.
    internal struct TextToPrint
    {
        public int Line;
    }

    internal struct MenuClose;

    internal struct LevelUnload;

    private static readonly string[] Lines = ["I will print until you press space.", "I will always print"];

    public static void Build(App app)
    {
        app.AddState(AppState.MainMenu);

        app.Startup(ctx =>
        {
            var ecs = ctx.Ecs;
            var menu = ecs.Spawn();
            ecs.Add(menu, new PrinterTick());
            ecs.Add(menu, new TextToPrint { Line = 0 });
            ecs.Add(menu, new MenuClose());

            var level = ecs.Spawn();
            ecs.Add(level, new PrinterTick());
            ecs.Add(level, new TextToPrint { Line = 1 });
            ecs.Add(level, new LevelUnload());
        }, "generic_system.Setup");

        // Each line once a second, as a repeating timer of one second finishes.
        app.Update(ctx =>
        {
            foreach (var entity in ctx.Ecs.EntitiesWith<PrinterTick>())
            {
                var tick = ctx.Ecs.GetOrDefault<PrinterTick>(entity);
                tick.Elapsed += ctx.Time.Delta;
                if (tick.Elapsed >= 1f)
                {
                    tick.Elapsed -= 1f;
                    Console.WriteLine(Lines[ctx.Ecs.GetOrDefault<TextToPrint>(entity).Line]);
                }

                ctx.Ecs.Set(entity, tick);
            }
        }, "generic_system.PrintText");

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
