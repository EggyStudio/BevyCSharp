// Bevy's run_conditions example, examples/ecs/run_conditions.rs at v0.20.0, by Bevy's contributors
// under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Runs systems only when one or several conditions are met, here a resource that exists, input
// this frame, and a time that has passed and one that has not.
internal static class RunConditions
{
    internal sealed class InputCounter
    {
        public int Value;
    }

    // Never added, so a condition asking for it always fails.
    internal sealed class Unused;

    public static void Build(App app)
    {
        Console.WriteLine();
        Console.WriteLine("For the first 2 seconds you will not be able to increment the counter");
        Console.WriteLine("Once that time has passed you can press space, enter, left mouse, right mouse or touch the screen to increment the counter");
        Console.WriteLine();

        app.Startup(ctx => ctx.World.InsertResource(new InputCounter()), "run_conditions.Setup");

        var exists = BehaviorConditions.HasResource<InputCounter>();
        app.On(Stage.Update, ctx => ctx.Res<InputCounter>().Value++, "run_conditions.IncrementInputCounter",
            world => exists(world) && (BehaviorConditions.HasResource<Unused>()(world) || HasUserInput(world)));

        // Bevy asks whether the counter changed this frame, and here the system remembers what it
        // printed last, which comes to the same for a counter that only goes up.
        var printed = 0;
        app.On(Stage.Update, ctx => Console.WriteLine($"Input counter: {printed = ctx.Res<InputCounter>().Value}"), "run_conditions.PrintInputCounter",
            world => exists(world) && world.Resource<InputCounter>().Value != printed);

        // Both timers are asked every frame, as Bevy asks each of a system's conditions, so the
        // second keeps counting while the first still fails.
        var (overTwo, overTwoAndAHalf) = (TimePassed(2f), TimePassed(2.5f));
        app.On(Stage.Update, _ => Console.WriteLine("It has been more than 2 seconds since the program started and less than 2.5 seconds"), "run_conditions.PrintTimeMessage",
            world => overTwo(world) & !overTwoAndAHalf(world));
    }

    private static bool HasUserInput(World world)
    {
        var input = world.Resource<Input>();
        if (input.KeyPressed(Key.Space) || input.KeyPressed(Key.Enter) || input.MousePressed(MouseButton.Left) || input.MousePressed(MouseButton.Right))
            return true;

        foreach (var touch in input.Touches)
            if (touch.Phase == TouchPhase.Started) return true;
        return false;
    }

    // A condition that counts the time it has been asked over, and passes once it reaches seconds,
    // as Bevy's time_passed keeps a Local.
    private static Func<World, bool> TimePassed(float seconds)
    {
        var timer = 0f;
        return world =>
        {
            timer += world.Resource<Time>().Delta;
            return timer >= seconds;
        };
    }
}
