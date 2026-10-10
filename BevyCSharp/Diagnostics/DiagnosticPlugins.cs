namespace Bevy;

/// <summary>Bevy's plugins that measure into its diagnostics store and log it, a flag each.</summary>
/// <remarks>
/// Each adds systems that run every frame, so an app has the ones it asks for in
/// <see cref="Config.DiagnosticPlugins"/> and none by default, as Bevy's own programs add them.
/// </remarks>
[Flags]
public enum DiagnosticPlugins
{
    /// <summary>None of them.</summary>
    None = 0,

    /// <summary>Bevy's log of every diagnostic once a second (<c>LogDiagnosticsPlugin</c>).</summary>
    /// <remarks>Which it prints is <see cref="Diagnostics.SetLogFilter"/>'s to say.</remarks>
    Log = 1,

    /// <summary>
    /// Frame time, frames a second and the frame count (<c>FrameTimeDiagnosticsPlugin</c>), as
    /// <see cref="Diagnostics.FrameTime"/>, <see cref="Diagnostics.Fps"/> and
    /// <see cref="Diagnostics.FrameCount"/>.
    /// </summary>
    FrameTime = 2,

    /// <summary>
    /// How many entities the world has (<c>EntityCountDiagnosticsPlugin</c>), as
    /// <see cref="Diagnostics.EntityCount"/>.
    /// </summary>
    EntityCount = 4,

    /// <summary>
    /// How long each render pass takes on the CPU and the GPU (<c>RenderDiagnosticsPlugin</c>), under
    /// paths that begin <c>render/</c>.
    /// </summary>
    /// <remarks>
    /// The GPU's where the adapter has timestamp queries. <see cref="Config.GpuTimings"/> adds the
    /// same plugin and reads its numbers into <see cref="Render.Timings"/> as well.
    /// </remarks>
    Render = 8,
}
