namespace Bevy;

/// <summary>Canonical stage orderings.</summary>
public static class StageOrder
{
    private static readonly Stage[] All =
    [
        Stage.PreStartup,
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
    /// <see cref="Stage.FixedUpdate"/> is not among them, because it runs zero or more times a
    /// frame rather than once, which is the whole reason it exists.
    /// </remarks>
    public static ReadOnlySpan<Stage> FrameStages() => Frame;

    /// <summary>True for the two stages reserved for the engine itself.</summary>
    public static bool IsInternal(Stage stage) =>
        stage is Stage.FrameSync or Stage.CommandFlush;
}
