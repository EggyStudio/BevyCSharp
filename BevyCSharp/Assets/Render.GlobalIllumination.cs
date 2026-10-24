namespace Bevy;

public static partial class Render
{
    /// <summary>
    /// Lights the camera's picture with the light each surface reflects onto the others, traced
    /// through the scene's distance field each frame as cascades of probes around the camera, at a
    /// quality, or none with <see cref="GlobalIllumination.Off"/>, which it is by default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is 3DEngine's radiance cascades over the field <see cref="SetSceneField"/> builds, which
    /// is turned on at four cascades of a quarter of a unit where the program asked for none. A
    /// cascade of the world's probes stands every eight cells of the field's cascade of the same
    /// number, eight a side, and each probe traces an octahedron of directions across an interval,
    /// the first cascade's from the probe to four times the probes' spacing and each after's from
    /// its spacing to four times that, the last's on to the field's edge, so the far light, which
    /// changes slowly from point to point, is gathered by few probes and the near light by many.
    /// The last cascade of probes stands in the field's far cascade where it has one. A ray that
    /// meets a surface brings back its color, which the field paints, times the sun's light where
    /// the field lets it through, each point and spot light's, those that cast shadows where the
    /// field lets them through too, and the indirect light that reached the surface the frame
    /// before, so each frame adds a bounce. A ray of the last cascade that meets nothing brings
    /// back the light from all around, the camera's ambient light or the world's, and the light of
    /// the camera's environment map along its way, which a dispatch on the camera copies for the
    /// probes each frame. The cascades are merged from the last down, and each probe gathers what
    /// reached it into the six faces of a cube.
    /// </para>
    /// <para>
    /// A surface's emissive light is painted into the field beside its color, as Bevy shows it, the
    /// camera's exposure taken by the share of it the emission's alpha sets, and a ray that meets
    /// it brings it back. A small emitter, one whose box's longest side is no more than twice the
    /// first cascade's probe spacing, as a lamp's panel or a glowing block, is carried as a light
    /// of its own, its light reaching a surface from its box in closed form and shadowed by a march
    /// through the field, at each pixel and at each probe's ray's hit, since the probes' rays meet
    /// a small emitter seldom. Touching emitters square to the axes with the same light are one
    /// light, the 64 nearest the camera are lit, and each is taken as its box with every face
    /// emitting, so a round emitter is carried brighter than it is. An emitter thinner than a cell
    /// and too long to be carried lends its light to the cells beside it.
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
    /// Bevy's lighting takes no indirect light from outside, so the probes' light is added to the
    /// picture of the camera the field lies around by a pass of the library's own after its opaque
    /// surfaces are lit, which takes back the diffuse share of the light from all around Bevy's
    /// lighting gave each surface and adds in its place the surface's diffuse color times the light
    /// the probes around it hold, times its material's occlusion. Past the probes' last cascade,
    /// faded in across its outer band, the light from all around is a sky over a ground, the
    /// ambient light from above and below the horizon the light the ground sends back, its color
    /// taken as the surface's own, with each directional light's share of the ground's light
    /// through its shadow map, so the field's edge shows as no line. The pass reads the camera's
    /// G-buffer and draws its picture in floats while it is on, as the field's contact shadows do.
    /// The ambient light is taken as what Bevy's lighting returns from a white surface under it, so
    /// an open floor reads as it does without global illumination. The diffuse light an environment
    /// map gave each surface is taken back as the ambient light's is, so a closed room under a
    /// bright sky is dark inside where Bevy's lighting lights it through the walls. A glossy
    /// surface's reflection of the light from all around is given again as 3DEngine gives it,
    /// traced through the picture's depth and the field below a roughness of a half, using the
    /// frame before's picture, and Bevy's own reflection above it times the share of the sky the
    /// probes see along the mirror way, so a room sealed from the sky reflects none of it. Bevy's
    /// screen-space ambient occlusion is left as Bevy gives it.
    /// </para>
    /// <para>
    /// Beside the world's probes stands a probe for every few pixels of the picture, every 16 at
    /// <see cref="GlobalIllumination.Low"/>, 12 at <see cref="GlobalIllumination.Medium"/> and 8 at
    /// <see cref="GlobalIllumination.High"/>, on the surface the camera's depth shows there. Each
    /// traces the near light through the depth first and the field past it, as far as twice the first
    /// cascade's probes' spacing, and takes the world's probes' light past that, so the light under a
    /// table, in a corner or where a wall meets the floor is seen at the size the picture draws it and
    /// not at the probes' spacing. Their light is blended with the probes around on like surfaces and
    /// with the frame before's where the surface is the same one, held within the spread of this
    /// frame's light, so it holds still as the camera moves and follows a light that changed at once,
    /// and a pixel blends the probes around it that stand on a surface like its own. The glow lights'
    /// light on the picture is read from what the probes around each point see of each, blended over
    /// the frames, so a pixel reads it at a cost the lights do not change.
    /// <see cref="SetGlobalIlluminationScreenProbes"/> leaves them out.
    /// </para>
    /// <para>
    /// The probes and the pass are compute and draw shaders the library carries, which compile as the
    /// field's do, and the setting lasts while the app runs.
    /// </para>
    /// </remarks>
    /// <param name="quality">How many cascades and directions, or none.</param>
    /// <exception cref="InvalidOperationException">No app is running.</exception>
    /// <example>
    /// <code>
    /// Render.SetGlobalIllumination(GlobalIllumination.High);
    /// </code>
    /// </example>
    public static void SetGlobalIllumination(GlobalIllumination quality)
    {
        if (App.Running is null)
            throw new InvalidOperationException("Global illumination belongs to an app, and none is running.");

        WorldProbes.Ask(quality);
    }

    /// <summary>
    /// Leaves global illumination's screen probes out, or puts them back, which they are by default,
    /// so every surface reads the world's probes' light alone where they are out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A game leaves them out where its art shows their tiles, as a world of blocks does, or where it
    /// has no time for them. They are then neither made nor traced, and every surface reads the
    /// cascades' light alone, smoother and coarser, with nothing of the light under a table that is
    /// smaller than the world's probes' spacing.
    /// </para>
    /// <para>
    /// Where a small emitter is carried as a glow light, probes stand for the glow lights alone while
    /// they are out, marching to them once a tile and tracing nothing else, so a pixel reads their
    /// light at a cost the lights do not change, where each pixel marching to the four brightest itself
    /// took 3DEngine's model pass from 0.31 to 1.01 ms on a Cornell box's four small emitters at 800 by
    /// 450. A frame with none of them pays nothing for them. The setting lasts while the app runs, and
    /// <c>gi.state</c> says what the probes are.
    /// </para>
    /// </remarks>
    /// <param name="on">Whether the screen's probes are traced.</param>
    /// <exception cref="InvalidOperationException">No app is running.</exception>
    /// <example>
    /// <code>
    /// Render.SetGlobalIllumination(GlobalIllumination.Low);
    /// Render.SetGlobalIlluminationScreenProbes(false);
    /// </code>
    /// </example>
    public static void SetGlobalIlluminationScreenProbes(bool on)
    {
        if (App.Running is null)
            throw new InvalidOperationException("Global illumination belongs to an app, and none is running.");

        WorldProbes.AskScreen(on);
    }
}
