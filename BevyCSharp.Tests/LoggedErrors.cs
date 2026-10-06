using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit.Sdk;

[assembly: Bevy.Tests.FailOnLoggedErrors]

namespace Bevy.Tests;

/// <summary>
/// Fails a test during which the engine logged an error the test did not say it expects, as N 3.7
/// of NORM.md has it, taken from 3DEngine's hook of the same name.
/// </summary>
/// <remarks>
/// <para>
/// The errors are those <c>EngineLog</c> names, a system's exception, a callback's, and Bevy's own
/// at its error level among them. Tests run side by side and the error stream is the process's, so
/// an error is laid to a test by the app that logged it. An app is the test's when it was made in
/// the test's flow, which follows the test over its awaits to whichever thread goes on with it and
/// into the tasks and threads it starts, and an error the engine logs from the app's loop or from
/// Bevy's threads says which app it is, or is the running app's. An error logged with no app in the
/// test's own flow is the test's as well. One app runs at a time, so an error a test of another
/// collection logs with no app of its own while an app runs is laid to that app, which no test does
/// today.
/// </para>
/// <para>
/// A test of a failure names each error it expects with <see cref="ExpectsErrorAttribute"/>, and
/// fails where one does not come. With <c>BCS_LOGGED_ERRORS</c> naming a file, each test that logged
/// an error it did not expect is written there with the first of them, and none fails for it, which
/// is how the tests that log one were found.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class FailOnLoggedErrorsAttribute : BeforeAfterTestAttribute
{
    private sealed class Heard
    {
        // An exception kept as its type's name and its message, since a type a script defines would
        // keep the script's load context from unloading for as long as the test runs.
        public readonly ConcurrentQueue<(string Kind, string Text, string? Exception, string? ExceptionMessage)> Errors = new();
        public volatile bool Closed;
    }

    // The ears of the test whose flow this is. xUnit calls Before from a method that is not async,
    // so what Before sets here is the test's for the rest of its flow.
    private static readonly AsyncLocal<Heard?> Ears = new();
    private static readonly ConditionalWeakTable<App, Heard> Apps = new();
    private static readonly string? Survey = Environment.GetEnvironmentVariable("BCS_LOGGED_ERRORS");
    private static readonly object SurveyGate = new();

    private Heard? _heard;

    static FailOnLoggedErrorsAttribute()
    {
        App.Created += app =>
        {
            if (Ears.Value is { } heard) Apps.AddOrUpdate(app, heard);
        };
        EngineLog.ErrorLogged += (app, kind, text, exception) =>
        {
            var heard = app is not null && Apps.TryGetValue(app, out var made) ? made : Ears.Value;
            if (heard is { Closed: false }) heard.Errors.Enqueue((kind, text, exception?.GetType().Name, exception?.Message));
        };
    }

    /// <inheritdoc />
    public override void Before(MethodInfo methodUnderTest) => Ears.Value = _heard = new Heard();

    /// <inheritdoc />
    public override void After(MethodInfo methodUnderTest)
    {
        var heard = _heard;
        _heard = null;
        if (ReferenceEquals(Ears.Value, heard)) Ears.Value = null;
        if (heard is null) return;
        heard.Closed = true;

        var expected = methodUnderTest.GetCustomAttributes<ExpectsErrorAttribute>().ToList();
        var errors = heard.Errors.ToList();
        var unexpected = errors.Where(error => !expected.Any(e => e.Matches(error.Kind, error.Text, error.ExceptionMessage))).ToList();
        var missing = expected.Where(e => !errors.Any(error => e.Matches(error.Kind, error.Text, error.ExceptionMessage))).ToList();

        if (Survey is not null)
        {
            if (unexpected.Count > 0)
                lock (SurveyGate) File.AppendAllText(Survey, $"{methodUnderTest.DeclaringType?.FullName}.{methodUnderTest.Name}\t{Line(unexpected[0])}\n");
            return;
        }

        if (missing.Count > 0)
            throw new XunitException($"N 3.7: the test expects an error of {missing[0].Kind} saying '{missing[0].Part}', and none was logged.");
        if (unexpected.Count > 0)
            throw new XunitException($"N 3.7: the engine logged {unexpected.Count} error{(unexpected.Count == 1 ? "" : "s")} during this test, the first: {Line(unexpected[0])}");
    }

    // The first line of an error, the most a summary has room for, with the kind before it.
    private static string Line((string Kind, string Text, string? Exception, string? ExceptionMessage) error) =>
        $"[{error.Kind}] {error.Text.Split('\n')[0].TrimEnd('\r')}";
}

/// <summary>
/// An error a test of a failure expects the engine to log, by its kind, or the start of it, and a
/// part of its text or its exception's message (N 3.7).
/// </summary>
/// <param name="kind">What it comes from, as <c>EngineLog</c> names it, such as <c>system</c> or <c>bevy</c>.</param>
/// <param name="part">Some of what it says.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class ExpectsErrorAttribute(string kind, string part) : Attribute
{
    /// <summary>What it comes from, or the start of it.</summary>
    public string Kind { get; } = kind;

    /// <summary>Some of what it says.</summary>
    public string Part { get; } = part;

    internal bool Matches(string kind, string text, string? exceptionMessage) =>
        kind.StartsWith(Kind, StringComparison.Ordinal)
        && (text.Contains(Part, StringComparison.Ordinal) || (exceptionMessage?.Contains(Part, StringComparison.Ordinal) ?? false));
}
