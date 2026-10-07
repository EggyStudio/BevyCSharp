using System.Runtime.CompilerServices;
using Bevy;

namespace Bevy.Tests;

/// <summary>Keeps the run's log and the crash hooks from the suite's apps.</summary>
/// <remarks>
/// A suite is many apps in one process, and the first would start the run's log for the whole of
/// it, teeing every test's output into the console's ring that the console's own tests read, and
/// writing a log beside the test assembly. Set as the assembly loads, before any test makes an
/// app, and <c>CrashLogTests</c> start the log themselves in a folder of their own.
/// </remarks>
internal static class SuiteLogs
{
#pragma warning disable CA2255 // Run as the test assembly loads, before any test makes an app.
    [ModuleInitializer]
    internal static void LeaveTheLogToTheTests() => CrashLog.StartedByApps = false;
#pragma warning restore CA2255
}
