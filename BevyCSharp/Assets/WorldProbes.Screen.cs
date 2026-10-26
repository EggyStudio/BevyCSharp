using System.Numerics;

namespace Bevy;

/// <summary>
/// Global illumination's screen probes, a probe on the surface the camera's depth holds every few
/// pixels, traced, blended and held over the frames by dispatches on the camera the field lies around
/// before its opaque surfaces are lit, as 3DEngine's screen probes are
/// (<see cref="Render.SetGlobalIlluminationScreenProbes"/>).
/// </summary>
/// <remarks>
/// <para>
/// Each probe sends sixteen rays over the hemisphere around its surface's normal through the depth
/// and the field (<c>gi_screen.slang</c>), as far as twice the world's probes' spacing, and takes the
/// world's first cascade's light from beyond them, so the light under a table or in a corner is seen
/// at the size the picture draws it. Its light is blended with the probes around on like surfaces and
/// with the frame before's (<c>gi_screen_filter.slang</c>), and the glow lights' light on the picture
/// is blended from what the probes around each point see of each (<c>gi_screen_glow.slang</c>), so a
/// pixel reads the glow lights at a cost they do not change. The pass blends the probes around each
/// pixel that stand on a surface like its own (<c>gi_light.slang</c>).
/// </para>
/// <para>
/// The images are the camera's, a texel a probe and half the picture's size for the glow lights'
/// light, those the next frame reads kept with their history. Where the program leaves the screen's
/// probes out they are not made, unless a glow light is lit, when probes stand for the glow lights
/// alone, marching to them once a tile and tracing no ray.
/// </para>
/// </remarks>
internal static partial class WorldProbes
{
    /// <summary>The name the screen probes' images and dispatches are kept under on a camera beside the engine's others.</summary>
    private const string ScreenOwner = "gi.screen";

    // Whether the program asked for screen probes, and the app it asked in, so a new app has them.
    private static App? _screenAskedIn;
    private static bool _screenAsked = true;

    // The screen probes' programs, the camera they run on, what its images and dispatches were set for,
    // and the frame they were set in, the next frame reading the history they keep.
    private static ShaderInstance _screenTrace, _screenFilter, _screenGlow;
    private static Entity _screenCamera = Entity.None;
    private static (int Tile, bool GlowOnly)? _screenImages;
    private static (bool GlowOnly, bool Glows)? _screenDispatches;
    private static long _screenSince;

    // The places the glow lights were written in, kept from frame to frame, and the field's cascades
    // built this frame as meshes came or went with the box around those meshes.
    private static readonly GlowPlaces Places = new();
    private static uint _fieldRebuilt;
    private static SceneFieldPlan.Box _fieldChanged;

    /// <summary>Takes whether the running app has the screen's probes traced.</summary>
    internal static void AskScreen(bool on)
    {
        _screenAskedIn = App.Running;
        _screenAsked = on;
    }

    /// <summary>Whether the running app has the screen's probes, which it does unless it left them out.</summary>
    internal static bool ScreenAsked => App.Running is not { } running || !ReferenceEquals(_screenAskedIn, running) || _screenAsked;

    /// <summary>
    /// Whether the pass leaves the screen's probes it traced unread, every pixel reading the world's
    /// probes and marching to the glow lights itself, as 3DEngine's <c>gi.toggle screen off</c> sets
    /// it, which a test compares the probes' reading with.
    /// </summary>
    internal static bool ScreenUnread { get; set; }

    /// <summary>One more than the screen probe whose light is written as a value that is not a number, as a test feeds it, or 0 for none.</summary>
    internal static int FedScreenProbe { get; set; }

    /// <summary>The pixels along each side of the tile a screen probe stands in, at a quality, as 3DEngine's TileAt.</summary>
    internal static int TileAt(GlobalIllumination quality) => quality switch
    {
        GlobalIllumination.Low => 16,
        GlobalIllumination.Medium => 12,
        _ => 8,
    };

    /// <summary>
    /// How far from its tile's middle each screen probe marches to the glow lights from in frame
    /// <paramref name="frame"/>, in pixels, the same for every probe, the middles of the tile's four by
    /// four cells in the order of Bayer's matrix, sixteen frames a round, so a round visits each cell
    /// once and each frame's cell lies far from the last, as 3DEngine's JitterAt.
    /// </summary>
    /// <remarks>
    /// The Halton points of bases 2 and 3 fell on one row of a tile's cells five times a round in
    /// 3DEngine and on another three, and the floor along a lamp's penumbra moved with turns of one to
    /// eight pixels by 0.64 to 1.27% of its light, where by 0.45 to 0.94% with these.
    /// </remarks>
    internal static Vector2 JitterAt(long frame, int tile)
    {
        ReadOnlySpan<byte> cells = [0, 10, 2, 8, 5, 15, 7, 13, 1, 11, 3, 9, 4, 14, 6, 12];
        var cell = cells[(int)(frame % 16)];
        return new Vector2(((cell & 3) + 0.5f) / 4 - 0.5f, ((cell >> 2) + 0.5f) / 4 - 0.5f) * tile;
    }

    /// <summary>
    /// Whether the screen's probes stand for the glow lights alone this frame, which they do where the
    /// program left them out and some glow light is lit.
    /// </summary>
    /// <remarks>
    /// Without them each pixel marches to the four brightest glow lights itself, which in 3DEngine
    /// took the model pass from 0.31 to 1.01 ms at Low on the Cornell box's four small emitters at 800
    /// by 450, and its voxel game's from 1.8 to 12.9 ms with forty glowstones in view, so they are
    /// marched to once a tile and a pixel reads the texels around it, its cost the same however many
    /// lights there are.
    /// </remarks>
    private static bool GlowOnly(int glows) => !ScreenAsked && glows > 0;

    /// <summary>What the screen's probes are, said in one line for <c>gi.state</c>.</summary>
    internal static string ScreenSaid()
    {
        if (!ReferenceEquals(_app, App.Running) || _made is null || _screenImages is not { } images)
            return ScreenAsked ? "Screen probes waiting for the camera" : "Screen probes left out";
        return images.GlowOnly
            ? FormattableString.Invariant($"Screen probes left out, standing for the glow lights alone every {images.Tile} pixels")
            : FormattableString.Invariant($"Screen probes every {images.Tile} pixels, sixteen rays each{(ScreenUnread ? ", left unread" : "")}");
    }

    /// <summary>Whether the screen's probes stand on a camera this frame, and whether for the glow lights alone.</summary>
    internal static (bool On, bool GlowOnly) ScreenState =>
        ReferenceEquals(_app, App.Running) && _screenImages is { } images ? (true, images.GlowOnly) : (false, false);

    /// <summary>
    /// Notes the field's cascades this frame's builds finished as meshes came or went within them, and
    /// the box around those meshes, which the glow lights' blend drops what it held within.
    /// </summary>
    private static void NoteRebuilt(SceneFieldPlan plan)
    {
        _fieldRebuilt = 0;
        foreach (var build in plan.Builds)
            if (build.Changed is { } changed)
            {
                _fieldChanged = _fieldRebuilt == 0 ? changed : _fieldChanged.With(changed);
                _fieldRebuilt |= 1u << build.Cascade;
            }
    }

    /// <summary>
    /// Puts the screen's probes on the camera the field lies around, their images and dispatches where
    /// they change and their values each frame, or takes them off where they stop, and gives the
    /// pass's <c>screen</c> values.
    /// </summary>
    private static Vector4 Screen(Made made, SceneFieldPlan plan, Entity camera, int glows)
    {
        var glowOnly = GlowOnly(glows);
        var on = (ScreenAsked || glowOnly) && !camera.IsNone;
        var tile = TileAt(made.Quality);
        if (!on || camera != _screenCamera)
            ReleaseScreen();
        if (!on)
            return Vector4.Zero;

        if (!_screenTrace.IsValid)
        {
            _screenTrace = Shaders.CreateInstance(SceneField.KernelProgram("gi_screen.slang"));
            _screenFilter = Shaders.CreateInstance(SceneField.KernelProgram("gi_screen_filter.slang"));
            _screenGlow = Shaders.CreateInstance(SceneField.KernelProgram("gi_screen_glow.slang"));
        }

        // The images a texel a probe, the probes' blend and surfaces and the glow lights' light kept
        // with their history, which the next frame's blends read.
        if (_screenImages != (tile, glowOnly))
        {
            var probe = 1f / tile;
            Shaders.SetEngineViewImages(camera, ScreenOwner,
                new ViewImage("gi_screen_light", ShaderImageFormat.Rgba16Float, Scale: probe),
                new ViewImage("gi_screen_surfaces", ShaderImageFormat.Rgba16Float, Scale: probe, History: true),
                new ViewImage("gi_screen_blended", ShaderImageFormat.Rgba16Float, Scale: probe, History: true),
                new ViewImage("gi_glow_seen0", ShaderImageFormat.Rgba32UInt, Scale: probe),
                new ViewImage("gi_glow_seen1", ShaderImageFormat.Rgba32UInt, Scale: probe),
                new ViewImage("gi_glow_seen2", ShaderImageFormat.Rgba32UInt, Scale: probe),
                new ViewImage("gi_glow_seen3", ShaderImageFormat.Rgba32UInt, Scale: probe),
                new ViewImage("gi_glow_faint", ShaderImageFormat.Rgba32Float, Scale: probe),
                new ViewImage("gi_glow_light", ShaderImageFormat.Rgba16Float, Scale: 0.5f, History: true),
                new ViewImage("gi_glow_state", ShaderImageFormat.Rgba16Float, Scale: 0.5f, History: true),
                new ViewImage("gi_glow_far", ShaderImageFormat.Rgba16Float, Scale: 0.5f, History: true),
                new ViewImage("gi_glow_normal", ShaderImageFormat.Rgba8, Scale: 0.5f));
            (_screenImages, _screenDispatches, _screenSince, _screenCamera) = ((tile, glowOnly), null, _frame, camera);
        }

        // Traced, then blended where they trace rays, then the glow lights' light where some are lit,
        // after the prepass that gives the depth they stand on. A frame with no glow lights skips the
        // last, the pass reading none of it then.
        if (_screenDispatches != (glowOnly, glows > 0))
        {
            var probe = 1f / tile;
            var dispatches = new List<ViewDispatch> { ViewDispatch.PerPixel(_screenTrace, FramePoint.AfterPrepass, scale: probe) };
            if (!glowOnly) dispatches.Add(ViewDispatch.PerPixel(_screenFilter, FramePoint.AfterPrepass, scale: probe));
            if (glows > 0) dispatches.Add(ViewDispatch.PerPixel(_screenGlow, FramePoint.AfterPrepass, scale: 0.5f));
            Shaders.SetEngineViewDispatches(camera, ScreenOwner, [.. dispatches]);
            _screenDispatches = (glowOnly, glows > 0);
        }

        // The frame before's images are the camera's from the frame after they were set.
        var history = _frame > _screenSince ? 1f : 0f;
        var cascades = made.Cascades;
        var reach = 2 * ProbeSpacing * plan.CellSize;
        var jitter = JitterAt(_frame, tile);
        var (field, info, albedo, faces, glow) = (SceneField.Image, SceneField.InfoBuffer, SceneField.Albedo, SceneField.Faces, SceneField.Glow);
        ReadOnlySpan<uint> texels = [(uint)made.Texels[0], cascades > 1 ? (uint)made.Texels[1] : 0, SceneField.LightThrough ? 1u : 0u, 0];
        _screenTrace.SetTexture("scene_field", field).SetSampler("scene_field_sampler", SamplerSettings.Clamped).SetBuffer("scene_field_info", info)
            .SetTexture("scene_albedo", albedo).SetTexture("scene_faces", faces).SetTexture("scene_glow", glow)
            .SetBuffer("gi_lights", made.Lights).SetBuffer("glow_lights", made.Glows)
            .SetTexture("probe_cubes", made.Cubes).SetTexture("probe_reach", made.Reach)
            .SetTexture("merged_first", made.Merged[0]).SetTexture("merged_second", made.Merged[Math.Min(1, cascades - 1)])
            .Set("screen", new Vector4(tile, reach, glowOnly ? 1 : 0, FedScreenProbe))
            .Set("probes", new Vector4(Probes, ProbeSpacing, cascades, 0))
            .SetNumbers("texels", 4, texels)
            .Set("jitter", new Vector4(jitter, 0, 0));
        _screenFilter.Set("screen", new Vector4(tile, history, 0, 0));
        ReadOnlySpan<uint> places = [Places.Kept.Low, Places.Kept.High, _fieldRebuilt, 0];
        _screenGlow.SetBuffer("scene_field_info", info).SetBuffer("gi_lights", made.Lights).SetBuffer("glow_lights", made.Glows)
            .Set("screen", new Vector4(tile, history, 0, 0))
            .Set("jitter", new Vector4(jitter, 0, 0))
            .SetNumbers("places", 4, places)
            .Set("changed_min", new Vector4(_fieldChanged.Min, 0)).Set("changed_max", new Vector4(_fieldChanged.Max, 0));

        var read = !ScreenUnread;
        return new Vector4(tile, read && !glowOnly ? 1 : 0, read && glows > 0 ? 1 : 0, 0);
    }

    /// <summary>Takes the screen's probes off the camera they were on.</summary>
    private static void ReleaseScreen()
    {
        if (!_screenCamera.IsNone)
        {
            Shaders.SetEngineViewDispatches(_screenCamera, ScreenOwner);
            Shaders.SetEngineViewImages(_screenCamera, ScreenOwner);
        }
        (_screenCamera, _screenImages, _screenDispatches) = (Entity.None, null, null);
    }
}
