namespace Bevy;

/// <summary>
/// Where in the frame a system runs. Each value maps onto one of Bevy's schedules.
/// </summary>
/// <remarks>
/// Bevy has no dedicated render schedule in its main loop, so <see cref="Render"/> and
/// <see cref="Last"/> both live in Bevy's <c>Last</c> schedule with an explicit ordering
/// between them. <see cref="Cleanup"/> has no Bevy equivalent at all; those systems are held
/// back and run once the main loop returns.
/// </remarks>
public enum Stage
{
    /// <summary>Once, before the first frame.</summary>
    Startup = 0,

    /// <summary>Top of every frame, after the engine has refreshed time and input.</summary>
    First = 1,

    /// <summary>Before <see cref="Update"/>.</summary>
    PreUpdate = 2,

    /// <summary>The main gameplay stage. Most behavior methods belong here.</summary>
    Update = 3,

    /// <summary>After <see cref="Update"/>, before queued commands are applied.</summary>
    PostUpdate = 4,

    /// <summary>Drawing and overlay work; ordered before <see cref="Last"/>.</summary>
    Render = 5,

    /// <summary>The very end of every frame.</summary>
    Last = 6,

    /// <summary>Once, after the main loop exits.</summary>
    Cleanup = 7,

    /// <summary>Engine-internal: refreshes <see cref="Time"/> and <see cref="Input"/>.</summary>
    FrameSync = 8,

    /// <summary>Engine-internal: applies the queued <see cref="EcsCommands"/>.</summary>
    CommandFlush = 9,

    /// <summary>
    /// Bevy's fixed timestep: zero or more times a frame, each covering the same slice of time.
    /// </summary>
    /// <remarks>
    /// Runs between <see cref="PreUpdate"/> and <see cref="Update"/>, as many times as the time
    /// since the last frame allows: twice after a slow frame, not at all after a fast one. That
    /// is what makes it frame-rate independent, and it is why the number to integrate with is
    /// <see cref="Time.FixedDelta"/> rather than <see cref="Time.Delta"/>.
    /// </remarks>
    FixedUpdate = 10,
}

/// <summary>Canonical stage orderings.</summary>
public static class StageOrder
{
    private static readonly Stage[] All =
    [
        Stage.Startup,
        Stage.First,
        Stage.PreUpdate,
        Stage.FixedUpdate,
        Stage.Update,
        Stage.PostUpdate,
        Stage.Render,
        Stage.Last,
        Stage.Cleanup,
    ];

    private static readonly Stage[] Frame =
    [
        Stage.First,
        Stage.PreUpdate,
        Stage.Update,
        Stage.PostUpdate,
        Stage.Render,
        Stage.Last,
    ];

    /// <summary>Every user-facing stage, in execution order.</summary>
    public static ReadOnlySpan<Stage> AllInOrder() => All;

    /// <summary>The stages that run exactly once every frame, in execution order.</summary>
    /// <remarks>
    /// <see cref="Stage.FixedUpdate"/> is not among them: it runs zero or more times a frame
    /// rather than once, which is the whole reason it exists.
    /// </remarks>
    public static ReadOnlySpan<Stage> FrameStages() => Frame;

    /// <summary>True for the two stages reserved for the engine itself.</summary>
    public static bool IsInternal(Stage stage) =>
        stage is Stage.FrameSync or Stage.CommandFlush;
}
