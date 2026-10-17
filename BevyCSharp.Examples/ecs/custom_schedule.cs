// Bevy's custom_schedule example, examples/ecs/custom_schedule.rs at v0.20.0, by Bevy's
// contributors under MIT or Apache-2.0, written again in C#.

using Bevy;

namespace BevyCSharp.Examples.Ecs;

// Prints the name of each schedule as it runs, Bevy's startup schedules with one of the example's
// own placed after PreStartup, and in every frame Bevy's First, Update and Last with one of its own
// placed after Update, which runs its systems one after another on one thread.
internal static class CustomSchedule
{
    private static readonly ScheduleLabel SingleThreadedUpdate = new("SingleThreadedUpdate");
    private static readonly ScheduleLabel CustomStartup = new("CustomStartup");

    public static void Build(App app)
    {
        app.AddSchedule(SingleThreadedUpdate, Stage.Update, singleThreaded: true);
        app.AddSchedule(CustomStartup, Stage.PreStartup);

        app.AddSystem(SingleThreadedUpdate, new SystemDescriptor(_ => Console.WriteLine("Single Threaded Update"), "custom_schedule.SingleThreadedUpdate"));
        app.AddSystem(CustomStartup, new SystemDescriptor(_ => Console.WriteLine("Custom Startup"), "custom_schedule.CustomStartup"));
        app.On(Stage.PreStartup, _ => Console.WriteLine("Pre Startup"), "custom_schedule.PreStartup");
        app.On(Stage.Startup, _ => Console.WriteLine("Startup"), "custom_schedule.Startup");
        app.On(Stage.First, _ => Console.WriteLine("First"), "custom_schedule.First");
        app.On(Stage.Update, _ => Console.WriteLine("Update"), "custom_schedule.Update");
        app.On(Stage.Last, _ => Console.WriteLine("Last"), "custom_schedule.Last");
    }
}
