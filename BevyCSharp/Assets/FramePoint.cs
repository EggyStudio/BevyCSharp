namespace Bevy;

/// <summary>Where in a camera's frame a compute shader on it runs.</summary>
public enum FramePoint
{
    /// <summary>Once depth, normals and motion are drawn, before anything is lit.</summary>
    AfterPrepass = 0,

    /// <summary>Once opaque geometry is drawn, before transparent geometry.</summary>
    AfterOpaque = 1,

    /// <summary>
    /// On the linear picture once everything is drawn, before Bevy's antialiasing, bloom and
    /// effects and before the passes that run before tonemapping.
    /// </summary>
    /// <remarks>
    /// Where Bevy draws its own volumetric fog. Each of Bevy's passes over the picture takes it into
    /// a fresh texture, so what is drawn here goes through them as Bevy's geometry does, and temporal
    /// antialiasing smooths its edges with the rest. A pass given this point runs after the
    /// antialiasing, as <see cref="AfterTemporalAntialiasing"/> does.
    /// </remarks>
    BeforeTonemapping = 2,

    /// <summary>
    /// On the picture as the screen will show it, after Bevy's antialiasing and before the passes
    /// that run after tonemapping.
    /// </summary>
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

    /// <summary>
    /// On the linear picture after Bevy's temporal antialiasing, before its motion blur, depth of
    /// field, bloom and effects and before the passes that run before tonemapping.
    /// </summary>
    /// <remarks>
    /// For what moves with no motion of its own in the prepass, rain, sparks and other particles a
    /// pixel or two wide. Temporal antialiasing blends each pixel with the history it reprojects by
    /// the prepass's motion, and a speck moving with none of its own is in the history nowhere, so
    /// drawn before it, it shows at a tenth of its strength or so. Drawn here it shows as drawn, and
    /// goes through bloom and depth of field with the rest. Where the camera has no temporal
    /// antialiasing it is the same as <see cref="BeforeTonemapping"/>, after the draws there.
    /// </remarks>
    AfterTemporalAntialiasing = 5,
}
