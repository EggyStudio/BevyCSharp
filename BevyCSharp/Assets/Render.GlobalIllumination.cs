namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Traces the light each surface reflects onto the others through the scene's distance field each
    /// frame, as cascades of probes around the camera, at a quality, or none with
    /// <see cref="GlobalIllumination.Off"/>, which it is by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is 3DEngine's radiance cascades over the field <see cref="SetSceneField"/> builds, which
    /// is turned on at four cascades of a quarter of a unit where the program asked for none. A
    /// cascade of the world's probes stands every eight cells of the field's cascade of the same
    /// number, eight a side, and each probe traces an octahedron of directions across an interval,
    /// the first cascade's from the probe to four times the probes' spacing and each after's from its
    /// spacing to four times that, the last's on to the field's edge, so the far light, which changes
    /// slowly from point to point, is gathered by few probes and the near light by many. The last
    /// cascade of probes stands in the field's far cascade where it has one. A ray that meets a
    /// surface brings back its color, which the field paints, times the sun's light where the field
    /// lets it through, each point and spot light's, those that cast shadows where the field lets them
    /// through too, and the indirect light that reached the surface the frame before, so each frame
    /// adds a bounce. A ray of the last cascade that meets nothing brings back the light from all
    /// around, the camera's ambient light or the world's. The cascades are merged from the last down,
    /// and each probe gathers what reached it into the six faces of a cube.
    /// </para>
    /// <para>
    /// <see cref="GlobalIllumination.Low"/> traces two cascades of 64 directions a probe,
    /// <see cref="GlobalIllumination.Medium"/> adds a third of 256, and
    /// <see cref="GlobalIllumination.High"/> traces 64 then 256 in four, no more cascades than the
    /// field has. A probe nearer a surface than a tenth of its spacing is moved off it, and one inside
    /// a mesh is passed over. The light the probes hold is in the units of the camera's picture, its
    /// exposure applied, and <c>gi.state</c> says what they hold and cost. Nothing is baked, so every
    /// light and mesh may move, a light switched off taking its indirect light with it the frame
    /// after.
    /// </para>
    /// <para>
    /// The probes are traced with compute shaders the library carries, which compile as the field's
    /// do, and the setting lasts while the app runs.
    /// </para>
    /// </remarks>
    /// <param name="quality">How many cascades and directions, or none.</param>
    /// <exception cref="InvalidOperationException">No app is running.</exception>
    /// <example>
    /// <code>
    /// Render.SetGlobalIllumination(GlobalIllumination.High);
    /// </code>
    /// </example>
    internal static void SetGlobalIllumination(GlobalIllumination quality)
    {
        if (App.Running is null)
            throw new InvalidOperationException("Global illumination belongs to an app, and none is running.");

        WorldProbes.Ask(quality);
    }
}
