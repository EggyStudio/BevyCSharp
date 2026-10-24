using System.Numerics;
using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// Global illumination's world probes, cascades of light probes over the scene's distance field,
/// traced, merged and gathered on the GPU each frame through the library's compute shaders
/// (<see cref="Render.SetGlobalIllumination"/>), as 3DEngine's global illumination renderer does.
/// </summary>
/// <remarks>
/// <para>
/// A cascade's probes lie every <see cref="ProbeSpacing"/> cells of the field's cascade of the same
/// number, or of its far cascade for the last where it has one, eight a side, so each cascade's
/// probes are twice as far apart as the one before's and cover eight times the room. Each frame,
/// after the field, the lights the probes are lit by are read from the bridge and written with where
/// each cascade lay the frame before, each cascade's rays are traced (<c>gi_trace.slang</c>), the
/// cascades are merged from the last down (<c>gi_merge.slang</c>), and each probe's light is
/// gathered into the six faces of a cube with its reach and its share of the sky
/// (<c>gi_ambient.slang</c>).
/// </para>
/// <para>
/// The images are made for a quality and the field's cascades and made again where either changes,
/// and nothing is traced before the three programs have compiled.
/// </para>
/// </remarks>
internal static unsafe class WorldProbes
{
    /// <summary>How many cells of their field cascade the probes lie apart.</summary>
    internal const int ProbeSpacing = 8;

    /// <summary>The most cascades of probes, the four <see cref="GlobalIllumination.High"/> traces and the far cascade's, as <c>bcs_gi</c>'s <c>MostProbeCascades</c>.</summary>
    internal const int MostCascades = 5;

    /// <summary>The probes along each side of a cascade.</summary>
    internal const int Probes = SceneFieldPlan.Resolution / ProbeSpacing;

    /// <summary>The bytes of the lights' buffer, <c>bcs_gi</c>'s <c>GiLights</c>, the bridge's lights and where each cascade lay the frame before.</summary>
    internal const int LightsBytes = NativeGiLights.Bytes + MostCascades * 16;

    // What the program asked for, and the app it asked in, so a new app starts with none.
    private static App? _askedIn;
    private static GlobalIllumination _asked;

    // The running app's probes and programs, made again for a new app or new settings, the frames
    // traced, and where each cascade lay the frame before.
    private static App? _app;
    private static Made? _made;
    private static ShaderProgram _traceProgram, _mergeProgram, _ambientProgram;
    // The copy of the camera's environment map the probes' rays read where they meet nothing, and
    // the camera it runs on.
    private static ShaderInstance _sky;
    private static Entity _skyCamera = Entity.None;

    /// <summary>The texels along each side of the octahedron the camera's environment map is copied onto.</summary>
    private const uint SkySize = 64;

    /// <summary>The name the copy's dispatch on a camera is kept under beside the engine's others.</summary>
    private const string SkyOwner = "gi.sky";

    /// <summary>The name the frame before's picture a glossy surface's reflection reads is kept under on a camera.</summary>
    private const string PictureOwner = "gi.reflection";

    /// <summary>
    /// The steps a glossy surface's reflection is traced through the picture's depth in, and how far
    /// in world units, at a quality, as 3DEngine's ReflectionStepsAt.
    /// </summary>
    internal static (int Steps, float Reach) ReflectionStepsAt(GlobalIllumination quality) => quality switch
    {
        GlobalIllumination.Low => (12, 8),
        GlobalIllumination.Medium => (16, 12),
        _ => (24, 16),
    };
    // The pass that adds the probes' light to the picture of the camera the field lies around.
    private static ShaderInstance _light;
    private static long _frame;
    private static Vector3?[] _origins = [];

    /// <summary>The probes' images and buffers, for the quality and cascades they were made for.</summary>
    private sealed class Made(GlobalIllumination quality, int[] texels)
    {
        public readonly GlobalIllumination Quality = quality;
        public readonly int[] Texels = texels;
        public AssetHandle[] Radiance = [], Merged = [], Distances = [], MergedSky = [];
        public AssetHandle Cubes, Reach, Skies, Held, Partial, Lights, Glows, SkyMap, NoSky = AssetHandle.None;
        public ShaderInstance[] Trace = [], Merge = [], Ambient = [];
        public (float Start, float End)[] Intervals = [];
        public readonly byte[] LightBytes = new byte[LightsBytes];
        public readonly byte[] GlowBytes = new byte[GlowLights.Most * GlowLights.Bytes];

        public int Cascades => Texels.Length;
    }

    /// <summary>Takes the program's quality for the running app.</summary>
    internal static void Ask(GlobalIllumination quality)
    {
        _askedIn = App.Running;
        _asked = quality;
    }

    /// <summary>The quality asked for in the running app, none where it asked for none.</summary>
    internal static GlobalIllumination Asked =>
        App.Running is { } running && ReferenceEquals(_askedIn, running) ? _asked : GlobalIllumination.Off;

    /// <summary>Each cascade's octahedron's texels along each side at a quality, before the field's cascades cut them short.</summary>
    internal static int[] TexelsAt(GlobalIllumination quality) => quality switch
    {
        GlobalIllumination.Low => [8, 8],
        GlobalIllumination.Medium => [8, 8, 16],
        GlobalIllumination.High => [8, 16, 16, 16],
        _ => [],
    };

    /// <summary>
    /// Each of <paramref name="cascades"/> cascades' octahedron's texels at a quality, the far
    /// cascade's, the last where <paramref name="far"/>, as many as the cascade before it.
    /// </summary>
    internal static int[] TexelsFor(GlobalIllumination quality, int cascades, bool far)
    {
        var texels = TexelsAt(quality);
        return far ? [.. texels[..(cascades - 1)], texels[Math.Max(0, cascades - 2)]] : texels[..cascades];
    }

    /// <summary>How many cascades of probes a quality traces over a field's plan, no more than its counted cascades, and one more in its far cascade.</summary>
    internal static int CascadesFor(GlobalIllumination quality, SceneFieldPlan plan) =>
        quality == GlobalIllumination.Off ? 0 : Math.Min(TexelsAt(quality).Length, plan.Counted) + (plan.HasFar ? 1 : 0);

    /// <summary>
    /// Each probe's six faces, the light arriving at a surface facing along each axis, every
    /// cascade, or none where no probes are traced, laid out as <c>bcs_gi</c> reads them.
    /// </summary>
    internal static AssetHandle Cubes => ReferenceEquals(_app, App.Running) && _made is { } made ? made.Cubes : AssetHandle.None;

    /// <summary>
    /// The pass that adds the probes' light to the picture of the camera the field lies around, bound
    /// to this frame's probes, or none where none are traced or it has not compiled
    /// (<c>gi_light.slang</c>).
    /// </summary>
    internal static ShaderInstance LightPass =>
        ReferenceEquals(_app, App.Running) && _made is not null && _light.IsValid && _light.Program.State == ShaderProgramState.Ready ? _light : default;

    /// <summary>How many frames the running app's probes have been traced in, for a test to wait on.</summary>
    internal static long TracedFrames => ReferenceEquals(_app, App.Running) && _made is not null ? _frame : 0;

    /// <summary>Each cascade's octahedron's texels along each side, or none where no probes are traced.</summary>
    internal static IReadOnlyList<int> Texels => ReferenceEquals(_app, App.Running) && _made is { } made ? made.Texels : [];

    /// <summary>Each cascade's rays' interval, where they start and where they end, or none where no probes are traced.</summary>
    internal static IReadOnlyList<(float Start, float End)> Intervals => ReferenceEquals(_app, App.Running) && _made is { } made ? made.Intervals : [];

    /// <summary>
    /// What the probes are, said in one line, for the debug overlay and <c>gi.state</c>, or that they
    /// are off.
    /// </summary>
    internal static string Said()
    {
        if (!ReferenceEquals(_app, App.Running) || _made is not { } made) return Asked == GlobalIllumination.Off ? "Global illumination off" : $"Global illumination {Asked}, waiting for the field and its shaders";
        return FormattableString.Invariant(
            $"Global illumination {made.Quality}, {made.Cascades} cascades of {Probes} probes a side, {string.Join(", ", made.Texels.Select(n => n * n * RaysAt(n)))} rays a probe, GPU memory {Bytes() / 1048576.0:0.0} MB");
    }

    /// <summary>The rays each texel of an octahedron of <paramref name="texels"/> a side sends, as <c>bcs_gi</c>'s <c>RaysAt</c> has them.</summary>
    internal static int RaysAt(int texels) => texels <= 8 ? 4 : 1;

    /// <summary>The bytes of GPU memory the probes hold.</summary>
    internal static long Bytes()
    {
        if (!ReferenceEquals(_app, App.Running) || _made is not { } made) return 0;
        const long p = Probes;
        long bytes = 0;
        foreach (var n in made.Texels) bytes += p * n * p * n * p * (8 + 8 + 2 + 2);
        var cascades = (long)made.Cascades;
        bytes += 4 * p * p * p * cascades * 16 * 2;
        bytes += 6 * p * p * p * cascades * (8 + 2);
        bytes += 8 * p * 8 * p * p * cascades * 2;
        return bytes + LightsBytes;
    }

    /// <summary>Traces, merges and gathers the frame's probes, once a frame after the field, or lets them go where light does not bounce.</summary>
    internal static void Tick(World world)
    {
        // The probes' images and programs are an app's, and the next app makes its own.
        if (!ReferenceEquals(App.Running, _app))
        {
            _app = App.Running;
            (_made, _traceProgram, _mergeProgram, _ambientProgram, _light, _frame, _origins) = (null, default, default, default, default, 0, []);
            (_sky, _skyCamera) = (default, Entity.None);
        }

        var quality = Asked;
        if (quality == GlobalIllumination.Off || SceneField.Plan is not { } plan || SceneField.Image == AssetHandle.None)
        {
            Release();
            return;
        }

        if (!_traceProgram.IsValid)
        {
            _traceProgram = SceneField.KernelProgram("gi_trace.slang");
            _mergeProgram = SceneField.KernelProgram("gi_merge.slang");
            _ambientProgram = SceneField.KernelProgram("gi_ambient.slang");
            _light = SceneField.Draw("gi_light.slang");
            _sky = Shaders.CreateInstance(SceneField.KernelProgram("gi_sky.slang"));
        }

        if (_traceProgram.State != ShaderProgramState.Ready || _mergeProgram.State != ShaderProgramState.Ready
            || _ambientProgram.State != ShaderProgramState.Ready)
            return;

        var texels = TexelsFor(quality, CascadesFor(quality, plan), plan.HasFar);
        if (_made is null || _made.Quality != quality || !_made.Texels.AsSpan().SequenceEqual(texels))
        {
            Release();
            _made = Make(quality, texels);
        }

        // The camera's environment map copied for the rays by a dispatch on the camera the field lies
        // around, which the rays read the frame after.
        var camera = SceneField.ViewCamera;
        _sky.SetTexture("sky_map", _made.SkyMap);
        if (camera != _skyCamera)
        {
            if (!_skyCamera.IsNone)
            {
                Shaders.SetEngineViewDispatches(_skyCamera, SkyOwner);
                Shaders.SetEngineViewImages(_skyCamera, PictureOwner);
            }
            if (!camera.IsNone)
            {
                Shaders.SetEngineViewDispatches(camera, SkyOwner, ViewDispatch.Fixed(_sky, FramePoint.AfterPrepass, SkySize / 8, SkySize / 8));
                // The lit picture copied before tonemapping each frame, the frame before's read as
                // gi_picture_previous by the reflections the pass traces.
                Shaders.SetEngineViewImages(camera, PictureOwner,
                    new ViewImage("gi_picture", ShaderImageFormat.Rgba16Float, History: true, CopyAt: FramePoint.BeforeTonemapping));
            }
            _skyCamera = camera;
        }

        _frame++;
        Trace(plan, _made);
    }

    /// <summary>Makes the probes' images and buffers for a quality and its cascades' texels, every face dark and every probe seeing every way.</summary>
    private static Made Make(GlobalIllumination quality, int[] texels)
    {
        const uint p = Probes;
        var made = new Made(quality, texels);
        var cascades = (uint)texels.Length;
        (made.Radiance, made.Merged, made.Distances, made.MergedSky) = (new AssetHandle[cascades], new AssetHandle[cascades], new AssetHandle[cascades], new AssetHandle[cascades]);
        (made.Trace, made.Merge, made.Ambient) = (new ShaderInstance[cascades], new ShaderInstance[cascades], new ShaderInstance[cascades]);
        for (var c = 0; c < cascades; c++)
        {
            var n = (uint)texels[c];
            made.Radiance[c] = Shaders.CreateImage(p * n, p * n, ShaderImageFormat.Rgba16Float, p);
            made.Merged[c] = Shaders.CreateImage(p * n, p * n, ShaderImageFormat.Rgba16Float, p);
            made.Distances[c] = Shaders.CreateImage(p * n, p * n, ShaderImageFormat.R16Float, p);
            made.MergedSky[c] = Shaders.CreateImage(p * n, p * n, ShaderImageFormat.R16Float, p);
            (made.Trace[c], made.Merge[c], made.Ambient[c]) =
                (Shaders.CreateInstance(_traceProgram), Shaders.CreateInstance(_mergeProgram), Shaders.CreateInstance(_ambientProgram));
        }

        // No probe's own light known yet, and every probe taking the whole of the indirect light.
        var held = new Vector4[4 * p * p * p * cascades];
        Array.Fill(held, new Vector4(1, 1, 1, -1));
        made.Held = Shaders.CreateBuffer<Vector4>(held);
        made.Partial = Shaders.CreateBuffer((int)(4 * p * p * p * cascades * 16));
        made.Cubes = Shaders.CreateImage(6 * p, p, ShaderImageFormat.Rgba16Float, p * cascades);

        // Every probe sees every way, and every face the whole sky, until its rays have first been traced.
        var far = new Half[8 * p * 8 * p * p * cascades];
        Array.Fill(far, (Half)10000f);
        made.Reach = Shaders.CreateImage<Half>(8 * p, 8 * p, ShaderImageFormat.R16Float, far, p * cascades);
        var open = new Half[6 * p * p * p * cascades];
        Array.Fill(open, Half.One);
        made.Skies = Shaders.CreateImage<Half>(6 * p, p, ShaderImageFormat.R16Float, open, p * cascades);
        made.Lights = Shaders.CreateBuffer(LightsBytes);
        made.Glows = Shaders.CreateBuffer(GlowLights.Most * GlowLights.Bytes);
        made.SkyMap = Shaders.CreateImage(SkySize, SkySize, ShaderImageFormat.Rgba16Float);

        // The last cascade's merge reads no cascade above, and an image of nothing stands in for the
        // share of the sky above it, its own being the one it writes.
        made.NoSky = Shaders.CreateImage(1, 1, ShaderImageFormat.R16Float, 2);
        return made;
    }

    /// <summary>Lets the probes' images and buffers go, on a frame they are off or made again.</summary>
    private static void Release()
    {
        if (!_skyCamera.IsNone)
        {
            Shaders.SetEngineViewDispatches(_skyCamera, SkyOwner);
            Shaders.SetEngineViewImages(_skyCamera, PictureOwner);
            _skyCamera = Entity.None;
        }
        if (_made is not { } made) return;
        foreach (var handle in made.Radiance.Concat(made.Merged).Concat(made.Distances).Concat(made.MergedSky)
                     .Concat([made.Cubes, made.Reach, made.Skies, made.Held, made.Partial, made.Lights, made.Glows, made.SkyMap, made.NoSky]))
            AssetServer.Release(handle);
        _made = null;
        _origins = [];
    }

    /// <summary>
    /// Writes the frame's lights and asks for each cascade's trace, then the merges from the last
    /// cascade down, then each cascade's gather.
    /// </summary>
    private static void Trace(SceneFieldPlan plan, Made made)
    {
        const uint p = Probes;
        var cascades = made.Cascades;
        var last = cascades - 1;

        // Each cascade's interval, from its probes' spacing to four times that, the first's from the
        // probe, so a ray reaches as far again as the next cascade's begin. The last cascade's ends at
        // the field's edge, the diagonal of its coarsest cascade, since past it no cascade takes over,
        // and the far cascade's begins half way along the one before's, as each cascade's does.
        var fieldEdge = SceneFieldPlan.Resolution * plan.CellOf(plan.Cascades - 1) * MathF.Sqrt(3);
        made.Intervals = new (float, float)[cascades];
        for (var c = 0; c < cascades; c++)
        {
            var spacing = ProbeSpacing * plan.CellSize * (1 << c);
            var start = c == 0 ? 0 : plan.HasFar && c == last ? 2 * ProbeSpacing * plan.CellSize * (1 << (c - 1)) : spacing;
            made.Intervals[c] = (start, c == last ? fieldEdge : 4 * spacing);
        }

        // The lights, from the bridge, with the frame's number, the indirect light of the frame before
        // added whole, and where each cascade lay the frame before, which the rays read the faces of
        // the light that bounced at, since they were gathered there. A cascade lying where it lay the
        // frame before has the same probes, so a probe's own light of the frame before is its own.
        var bytes = made.LightBytes.AsSpan();
        fixed (byte* lights = bytes)
            Native.Check(Native.bcs_gi_lights((NativeGiLights*)lights), "reading the lights global illumination is lit by");
        var floats = MemoryMarshal.Cast<byte, float>(bytes);
        (floats[13], floats[14]) = (_frame, 1);

        // The small emitters carried as lights of their own, the nearest the eye, counted in the
        // lights' fourth count.
        floats[15] = GlowLights.Write(made.GlowBytes, SceneField.DrawnThisFrame, plan.Eye, plan.CellSize);
        Shaders.WriteBuffer<byte>(made.Glows, made.GlowBytes);
        var corners = MemoryMarshal.Cast<byte, Vector4>(bytes[NativeGiLights.Bytes..]);
        var origins = new Vector3?[cascades];
        var lying = new bool[cascades];
        for (var c = 0; c < cascades; c++)
        {
            origins[c] = plan.BuiltOrigin(plan.CascadeOfProbes(c, cascades));
            var then = c < _origins.Length ? _origins[c] : null;
            lying[c] = origins[c] is not null && then == origins[c];
            corners[c] = (then ?? origins[c]) is { } corner ? new Vector4(corner, 0) : Vector4.Zero;
        }

        _origins = origins;
        Shaders.WriteBuffer<byte>(made.Lights, bytes);

        var (field, info, albedo, faces, glow) = (SceneField.Image, SceneField.InfoBuffer, SceneField.Albedo, SceneField.Faces, SceneField.Glow);
        var parity = (uint)(_frame & 1);
        for (var c = 0; c < cascades; c++)
        {
            var n = (uint)made.Texels[c];
            ReadOnlySpan<uint> cascade = [(uint)c, p, n, c == last ? 1u : 0u];
            ReadOnlySpan<uint> follow = [lying[c] ? 1u : 0u, 0, parity, 0];
            made.Trace[c].SetTexture("field", field).SetSampler("field_sampler", SamplerSettings.Clamped).SetBuffer("info", info)
                .SetTexture("albedo", albedo).SetTexture("faces", faces).SetTexture("glow", glow).SetBuffer("lights", made.Lights)
                .SetBuffer("glow_lights", made.Glows).SetTexture("sky_map", made.SkyMap)
                .SetTexture("radiance", made.Radiance[c]).SetTexture("bounced", made.Cubes).SetTexture("distances", made.Distances[c])
                .SetBuffer("held", made.Held).SetBuffer("partial", made.Partial)
                .SetNumbers("cascade", 4, cascade).Set("interval", new Vector4(made.Intervals[c].Start, made.Intervals[c].End, ProbeSpacing, cascades))
                .SetNumbers("follow", 4, follow);
            Shaders.Dispatch(made.Trace[c], p * p * p * n * n * (uint)RaysAt((int)n) / 64);
        }

        for (var c = last; c >= 0; c--)
        {
            var n = (uint)made.Texels[c];
            ReadOnlySpan<uint> cascade = [(uint)c, p, n, c == last ? 0u : (uint)made.Texels[c + 1]];
            ReadOnlySpan<uint> counts = [(uint)cascades, 0, 0, 0];
            made.Merge[c].SetTexture("radiance", made.Radiance[c]).SetTexture("above", c == last ? made.Radiance[c] : made.Merged[c + 1])
                .SetTexture("merged", made.Merged[c]).SetBuffer("info", info).SetTexture("field", field).SetSampler("field_sampler", SamplerSettings.Clamped)
                .SetBuffer("held", made.Held).SetTexture("merged_sky", made.MergedSky[c]).SetTexture("above_sky", c == last ? made.NoSky : made.MergedSky[c + 1])
                .SetTexture("reach_above", made.Reach)
                .SetNumbers("cascade", 4, cascade).Set("spacing", new Vector4(ProbeSpacing, 0, 0, c == last ? 0 : made.Intervals[c + 1].End))
                .SetNumbers("counts", 4, counts);
            Shaders.Dispatch(made.Merge[c], (p * p * p * n * n + 63) / 64);
        }

        for (var c = 0; c < cascades; c++)
        {
            var n = (uint)made.Texels[c];
            ReadOnlySpan<uint> cascade = [(uint)c, p, n, parity + (lying[c] ? 2u : 0u)];
            made.Ambient[c].SetTexture("merged", made.Merged[c]).SetTexture("cubes", made.Cubes).SetTexture("distances", made.Distances[c])
                .SetTexture("reach", made.Reach).SetBuffer("partial", made.Partial).SetBuffer("held", made.Held)
                .SetTexture("merged_sky", made.MergedSky[c]).SetTexture("skies", made.Skies)
                .SetNumbers("cascade", 4, cascade).Set("interval", new Vector4(made.Intervals[c].End, 0, 0, 0));
            Shaders.Dispatch(made.Ambient[c], (p * p * p * 6 + 63) / 64);
        }

        // The pass that adds the light, which reads the faces the gathers wrote this frame.
        _light.SetTexture("scene_field", field).SetSampler("scene_field_sampler", SamplerSettings.Clamped).SetBuffer("scene_field_info", info)
            .SetTexture("probe_cubes", made.Cubes).SetTexture("probe_reach", made.Reach).SetBuffer("gi_lights", made.Lights)
            .SetBuffer("glow_lights", made.Glows).SetTexture("probe_skies", made.Skies)
            .SetTexture("scene_albedo", albedo).SetTexture("scene_faces", faces).SetTexture("scene_glow", glow)
            .Set("probes", new Vector4(p, ProbeSpacing, cascades, 0))
            .Set("reflection", new Vector4(ReflectionStepsAt(made.Quality).Steps, ReflectionStepsAt(made.Quality).Reach, 1, 0));
    }
}
