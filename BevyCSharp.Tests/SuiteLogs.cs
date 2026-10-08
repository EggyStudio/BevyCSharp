using System.Runtime.CompilerServices;
using Bevy;

namespace Bevy.Tests;

/// <summary>
/// Keeps the run's log and the crash hooks from the suite's apps, and holds the suite to a memory
/// cap.
/// </summary>
/// <remarks>
/// <para>
/// A suite is many apps in one process, and the first would start the run's log for the whole of
/// it, teeing every test's output into the console's ring that the console's own tests read, and
/// writing a log beside the test assembly. Set as the assembly loads, before any test makes an
/// app, and <c>CrashLogTests</c> start the log themselves in a folder of their own.
/// </para>
/// <para>
/// The cap is the default the tools give (<see cref="MemoryGuard.DefaultCap"/>) where the
/// environment names none, so a test that runs away ends the run and says so, where a machine
/// running out of memory ended whichever process the system chose, the session driving the run
/// among them.
/// </para>
/// </remarks>
internal static class SuiteLogs
{
#pragma warning disable CA2255 // Run as the test assembly loads, before any test makes an app.
    [ModuleInitializer]
    internal static void LeaveTheLogToTheTests()
    {
        CrashLog.StartedByApps = false;
        MemoryGuard.DefaultTheEnvironment();
    }
#pragma warning restore CA2255
}
