namespace BevyCSharp.Examples.StressTests;

// Bevy's warning_string.txt, a file its stress tests share and most of them say as they start. The
// window each asks for and the log of its frame times are said by each test, in its configuration.
internal static class StressTest
{
    public static void Warn() => Console.Error.WriteLine(
        "This is a stress test used to push Bevy to its limit and debug performance issues. It is not representative of an actual game. It must be built in Release or it will be very slow.");
}
