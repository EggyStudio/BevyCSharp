namespace Bevy;

/// <summary>Where in a camera's frame a compute shader on it runs.</summary>
public enum FramePoint
{
    /// <summary>Once depth, normals and motion are drawn, before anything is lit.</summary>
    AfterPrepass = 0,

    /// <summary>Once opaque geometry is drawn, before transparent geometry.</summary>
    AfterOpaque = 1,

    /// <summary>On the linear picture, before the passes that run before tonemapping.</summary>
    BeforeTonemapping = 2,

    /// <summary>On the picture as the screen will show it, before the passes that run after it.</summary>
    AfterTonemapping = 3,

    /// <summary>
    /// Inside the prepass, once Bevy's geometry has drawn depth, normals, motion and the deferred
    /// buffers, before anything reads them. The first point of the frame, numbered last because it
    /// came last.
    /// </summary>
    /// <remarks>
    /// <para>
    /// What a draw writes here is part of the prepass, so everything after it sees the draw as it
    /// sees Bevy's own geometry. Ambient occlusion and the passes at <see cref="AfterPrepass"/>
    /// read its depth, and on a camera drawing deferred, Bevy's deferred lighting lights what it
    /// wrote into the G-buffer (<c>gbuffer</c> and <c>lighting_pass</c> among
    /// <see cref="ViewDraw.Targets"/>) as it lights a standard material. The camera's depth is
    /// copied into the prepass's once the draws here are done, where any of them wrote it.
    /// </para>
    /// <para>
    /// A draw here goes into targets, never the picture, which the main pass clears afterward.
    /// The G-buffer can be drawn into nowhere else, since the deferred lighting has taken which
    /// pixels to light from it by the next point.
    /// </para>
    /// </remarks>
    InPrepass = 4,
}
