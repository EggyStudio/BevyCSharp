using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Steps through the systems of chosen stages one at a time, as Bevy's <c>Stepping</c> resource
/// does, for watching a frame's work happen system by system. Only valid inside a system.
/// </summary>
/// <remarks>
/// <para>
/// A stage is added with <see cref="Add"/> and stepping turned on with <see cref="Enable"/>, after
/// which the systems of the added stages wait, frame after frame, while every other stage runs as
/// it did. <see cref="StepFrame"/> runs the next system of them in the next frame and
/// <see cref="ContinueFrame"/> the rest of them, so a stepping frame of every stepped system can
/// take many of the app's frames, and the app keeps drawing and reading input across them. Bevy's
/// breakout example steps this with keys, and its system_stepping from a script.
/// </para>
/// <para>
/// Each call takes effect at the start of the next frame, as Bevy's does, so a call from a system
/// changes nothing in the frame it was made in. A system is named by the name it was added under,
/// <see cref="SystemDescriptor.Name"/>, and a behavior's by its type and method, as
/// <c>"Paddle.MovePaddle"</c>. Every system of the stage with that name is changed, from the frame
/// after the stage first runs, because Bevy has a stage's systems to change only once it has built
/// it, and a name no system of the stage has is reported in the log.
/// </para>
/// <para>
/// A system that steps others is left out of the stages it steps, or set to
/// <see cref="AlwaysRun"/>, since it would otherwise wait with the rest and never be asked to go
/// on. Bevy's own systems and the engine's are stepped like a game's in a stage that is added, so
/// <see cref="Stage.First"/>, which updates <see cref="Time"/> and the input, freezes both while
/// it waits. <see cref="Stage.Render"/> and <see cref="Stage.Last"/> are one schedule of Bevy's,
/// stepped together. The stages that run once and the engine's internal two have nothing to step.
/// </para>
/// </remarks>
public static unsafe class Stepping
{
    /// <summary>Steps the systems of <paramref name="stage"/>, from the next frame, while stepping is on.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The stage runs once or is the engine's own.</exception>
    public static void Add(Stage stage) =>
        Native.Check(Native.bcs_stepping_add(Steppable(stage)), $"stepping {stage}");

    /// <summary>Runs <paramref name="stage"/> whole again, and forgets what was set for its systems.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The stage runs once or is the engine's own.</exception>
    public static void Remove(Stage stage) =>
        Native.Check(Native.bcs_stepping_remove(Steppable(stage)), $"ending the stepping of {stage}");

    /// <summary>Whether stepping is on.</summary>
    public static bool IsEnabled => Native.Check(Native.bcs_stepping_enabled(), "asking whether stepping is on") == 1;

    /// <summary>Turns stepping on, so the systems of the added stages wait for a step.</summary>
    public static void Enable() => Act(1, "turning stepping on");

    /// <summary>Turns stepping off, so every system runs every frame again.</summary>
    public static void Disable() => Act(0, "turning stepping off");

    /// <summary>Runs the next stepped system in the next frame.</summary>
    /// <remarks>Does nothing while stepping is off, as Bevy's <c>step_frame</c> does, which logs a warning.</remarks>
    public static void StepFrame() => Act(2, "stepping a system");

    /// <summary>
    /// Runs the stepped systems from the next one to the end of the stepping frame, or to the next
    /// breakpoint, in the next frame.
    /// </summary>
    public static void ContinueFrame() => Act(3, "continuing the stepping frame");

    /// <summary>Runs the systems of <paramref name="stage"/> called <paramref name="system"/> every frame while stepping is on.</summary>
    public static void AlwaysRun(Stage stage, string system) => Behave(stage, system, 1, "to always run");

    /// <summary>Runs the systems of <paramref name="stage"/> called <paramref name="system"/> in no frame while stepping is on.</summary>
    public static void NeverRun(Stage stage, string system) => Behave(stage, system, 2, "to never run");

    /// <summary>
    /// Stops a continued frame before the systems of <paramref name="stage"/> called
    /// <paramref name="system"/>, which the next step or continue then runs.
    /// </summary>
    public static void SetBreakpoint(Stage stage, string system) => Behave(stage, system, 3, "to stop before");

    /// <summary>
    /// Steps the systems of <paramref name="stage"/> called <paramref name="system"/> as the rest are
    /// stepped, clearing a breakpoint or <see cref="AlwaysRun"/> or <see cref="NeverRun"/>, as Bevy's
    /// <c>clear_breakpoint</c> clears whichever was set.
    /// </summary>
    public static void ClearBreakpoint(Stage stage, string system) => Behave(stage, system, 0, "to step as the rest");

    /// <summary>
    /// The stepped systems, in the order a stepping frame runs them, or <see langword="null"/> until
    /// a frame has ended since a stage was added.
    /// </summary>
    /// <remarks>
    /// Read at the end of each frame that changed a stepped stage's systems, since a stage's
    /// systems can be read only while it is not running, and every system runs in one. A game can
    /// read it before stepping is first on, to leave systems other than its own to run. Bevy's
    /// systems are among them, by the paths of their functions, as <c>bevy_time::time_system</c>.
    /// </remarks>
    public static SteppedSystem[]? Systems
    {
        get
        {
            var length = Native.bcs_stepping_systems(null, 0);
            if (length == NativeStatus.NotPresent) return null;
            Native.Check(length, "reading the stepped systems");

            var text = Native.ReadText(Native.bcs_stepping_systems, "reading the stepped systems");
            return text.Split('\n').Select(line =>
            {
                var tab = line.IndexOf('\t');
                return new SteppedSystem((Stage)int.Parse(line.AsSpan(0, tab)), line[(tab + 1)..]);
            }).ToArray();
        }
    }

    /// <summary>
    /// The place in <see cref="Systems"/> of the system stepping runs next, or <see langword="null"/>
    /// while stepping is off or the stepping frame has run its last system.
    /// </summary>
    public static int? Cursor
    {
        get
        {
            var cursor = Native.bcs_stepping_cursor();
            if (cursor == NativeStatus.NotPresent) return null;
            return Native.Check(cursor, "reading where stepping is");
        }
    }

    /// <summary>
    /// Bevy's own account of the stepping, its action, stages, order and cursor, as Bevy's helper
    /// prints it, or <see langword="null"/> where nothing has asked for stepping.
    /// </summary>
    public static string? Describe()
    {
        var length = Native.bcs_stepping_describe(null, 0);
        if (length == NativeStatus.NotPresent) return null;
        Native.Check(length, "describing the stepping");
        return Native.ReadText(Native.bcs_stepping_describe, "describing the stepping");
    }

    private static void Act(int action, string operation) => Native.Check(Native.bcs_stepping_act(action), operation);

    private static void Behave(Stage stage, string system, int behavior, string what)
    {
        ArgumentException.ThrowIfNullOrEmpty(system);
        Native.Check(
            Native.bcs_stepping_set_behavior(Steppable(stage), system, behavior),
            $"setting {system} in {stage} {what}");
    }

    private static int Steppable(Stage stage) => stage switch
    {
        Stage.First or Stage.PreUpdate or Stage.Update or Stage.FixedUpdate or Stage.PostUpdate
            or Stage.Render or Stage.Last => (int)stage,
        _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Only a stage that runs every frame or every fixed step can be stepped."),
    };
}
