using Bevy;

namespace BevyCSharp.Examples.Application;

// Shows the program getting back to its own code after the app has stopped, Bevy's by its window
// being closed and this one, headless, after its frames.
internal static class ReturnAfterRun
{
    public static void Build(App app)
    {
        Console.WriteLine("Running Bevy App");
        app.Update(_ => Console.WriteLine("Logging from Bevy App"), "return_after_run.System");
    }

    public static void Returned() => Console.WriteLine("Bevy App has exited. We are back in our main function.");
}
