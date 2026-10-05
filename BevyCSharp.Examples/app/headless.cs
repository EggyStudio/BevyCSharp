using Bevy;

namespace BevyCSharp.Examples.Application;

// Runs two apps with no window, one after the other. The first runs a single frame and says hello,
// and the second counts its frames at sixty a second, printing every sixtieth.
//
// Bevy's second app counts for as long as it is left running. Here it stops after three seconds,
// so the example ends, and the count printed is the same each time.
internal static class Headless
{
    private const uint CountedFrames = 181;

    public static void Build(App app) => app.Update(_ => Console.WriteLine("hello world"), "headless.HelloWorldSystem");

    public static void Returned()
    {
        var count = 0u;
        BevyApp.Run(app => app.Update(_ =>
        {
            if (count % 60 == 0) Console.WriteLine(count);
            count++;
        }, "headless.Counter"), new Config { Headless = true, HeadlessFrames = CountedFrames, HeadlessFps = 60 });
    }
}
