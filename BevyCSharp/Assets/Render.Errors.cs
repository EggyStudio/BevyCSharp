using Bevy.Interop;

namespace Bevy;

public static unsafe partial class Render
{
    /// <summary>
    /// What an error the renderer reports does to the app, closing it unless set otherwise.
    /// Callable at any time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bevy closes the app at any error the GPU reports, which is right while the cause is
    /// unknown. A game that can do better says so here, carrying on, stopping drawing with its
    /// systems still running, or making the renderer again, which is how a game survives losing its
    /// device to a driver update or a GPU reset. Bevy asks again every frame while drawing is stopped, so
    /// a policy set then takes over from the next frame, and <see cref="ErrorCount"/> and
    /// <see cref="Shaders.LastRenderError"/> say what was reported.
    /// </para>
    /// <para>
    /// A validation error is ignored whatever this says while
    /// <see cref="Shaders.KeepRenderingAfterErrors"/> is set, for a shader being edited.
    /// Process-wide, because the renderer asks from its own thread, so a policy outlives the app it
    /// was set in.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">A value that is not one of the policies.</exception>
    /// <exception cref="BevyNativeException">This build has no renderer.</exception>
    public static RenderErrorPolicy ErrorPolicy
    {
        get => App.HasRenderer ? (RenderErrorPolicy)Native.bcs_render_error_policy() : RenderErrorPolicy.Quit;
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value), value, "Not one of the policies.");

            var status = Native.bcs_render_set_error_policy((int)value);
            if (status == NativeStatus.Unsupported) throw NoRenderer("Setting what a render error does");
            Native.Check(status, "setting what a render error does");
        }
    }

    /// <summary>
    /// How many errors the renderer has reported since the program started, each counted once
    /// however many frames drawing stays stopped over it. Callable at any time.
    /// </summary>
    /// <remarks>
    /// What a game compares with the count it saw last to learn an error arrived, the text of the
    /// last one in <see cref="Shaders.LastRenderError"/>. Zero where nothing draws.
    /// </remarks>
    public static int ErrorCount => App.HasRenderer ? Math.Max(0, Native.bcs_render_error_count()) : 0;

    /// <summary>
    /// Causes an error on the GPU, for a game to see what its <see cref="ErrorPolicy"/> does with
    /// one, as Bevy's render_recovery example causes them. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The error is caused in a frame with no pipeline compiling, since one compiling would take a
    /// validation error as its own, and arrives as the renderer next reports, a frame or so later.
    /// <see cref="RenderErrorCause.Validation"/> hurts nothing else, and
    /// <see cref="RenderErrorCause.DeviceLost"/> loses this app's device alone.
    /// <see cref="RenderErrorCause.OutOfMemory"/> and <see cref="RenderErrorCause.EndlessLoop"/>
    /// reach the machine's own graphics, which may need a restart to recover from either, as
    /// Bevy's example warns of its own.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">A value that is not one of the causes.</exception>
    /// <exception cref="BevyNativeException">The app draws nothing, or this build has no renderer.</exception>
    public static void CauseError(RenderErrorCause cause)
    {
        if (!Enum.IsDefined(cause)) throw new ArgumentOutOfRangeException(nameof(cause), cause, "Not one of the causes.");

        if (!App.HasRenderer) throw NoRenderer("Causing a render error");
        Native.Check(Native.bcs_render_cause_error((int)cause), "causing a render error, which needs an app that draws");
    }
}
