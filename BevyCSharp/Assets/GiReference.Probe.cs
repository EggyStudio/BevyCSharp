using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// One world probe's rays read against the reference along its texels' ways, as <c>gi.probe</c>
/// runs it.
/// </summary>
/// <remarks>
/// <para>
/// A probe's texel holds the light its rays brought along the texel's ways, four fixed rays a texel
/// of eight a side and one of sixteen, merged with the cascade above where they met nothing, and is
/// read as the probe's faces take it, times the share the rays kept across surfaces letting light
/// through before their interval began, with the light those surfaces scattered along them. The
/// reference traces from the probe's place, moved off a surface as the trace moves it, along each
/// texel's middle way and along ways drawn across the texel, every face met lit as the probes light
/// one, and gives the first face's distance along the middle way beside the probe's ray's. Where
/// the middle way and the probe's rays bring nothing and the drawn ways bring light, the texel's
/// light comes from slivers the fixed rays never meet.
/// </para>
/// <para>
/// It runs on the camera over a few frames as the reference does, through <see cref="StepProbe"/>,
/// and takes the light the probes are lit by, so a probe read with the moon or a lamp off reads
/// what the picture's reference would.
/// </para>
/// </remarks>
internal static unsafe partial class GiReference
{
    private enum ProbeStage { Placing, Tracing, Reading }

    private sealed class ProbeJob
    {
        public required Entity Camera;
        public required int Cascade, Samples, Faces, Texels, X, Y, Z;
        public required Vector3 Spot;
        public required float Apart;
        public required (AssetHandle Merged, AssetHandle Kept, AssetHandle Distances, int Texels, AssetHandle Held) Images;
        public string? Path;
        public Vector3 Origin;
        public bool Inside;
        public ProbeStage Stage;
        public int Added, Frames, Steps;
        public long Started;
        public AssetHandle Sums = AssetHandle.None, Regions = AssetHandle.None;
        public BufferRead? HeldRead, SumsRead, RegionsRead, MergedRead, KeptRead, DistancesRead;
        public byte[]? SumsBytes, RegionsBytes, MergedBytes, KeptBytes, DistancesBytes;
    }

    private static ProbeJob? _probe;

    /// <summary>
    /// The probe read last, its ways' mean light as it holds them, along their middles and drawn
    /// across them, each way weighed by the share of the sphere its texel spans, and how many of
    /// its rays stopped within a tenth of the middle way's first face.
    /// </summary>
    internal static (double Probe, double Middle, double Drawn, int Within)? LastProbe { get; private set; }

    /// <summary>
    /// Starts reading the probe of cascade <paramref name="cascade"/> nearest <paramref name="at"/>
    /// against <paramref name="samples"/> paths along each of its texels' ways, each meeting
    /// <paramref name="bounces"/> faces past the first at most, or until Russian roulette ends it
    /// where that is negative, its table written to <paramref name="path"/> where one is given.
    /// Answers why it cannot, or nothing.
    /// </summary>
    internal static string? BeginProbe(EcsWorld ecs, Vector3 at, int cascade, int samples, int bounces = -1, string? path = null)
    {
        if (Refused(ecs, rays: true) is { } why) return why;
        if (WorldProbes.ProbeImages(cascade) is not { } images) return $"global illumination traces no cascade {cascade} of probes";
        if (SceneField.Plan?.BuiltOrigin(cascade) is not { } corner) return $"the field's cascade {cascade} is not built";

        var rays = _rays!;
        var (slots, _) = Fill(ecs, rays);
        if (slots.Length == 0) return "the camera draws no meshes for the rays to meet";

        const int p = WorldProbes.Probes;
        var apart = WorldProbes.ProbeSpacing * SceneField.Plan.CellOf(cascade);
        int Index(float value, float low) => Math.Clamp((int)MathF.Round((value - low) / apart - 0.5f), 0, p - 1);
        var (x, y, z) = (Index(at.X, corner.X), Index(at.Y, corner.Y), Index(at.Z, corner.Z));
        _probe = new ProbeJob
        {
            Camera = CameraOf(ecs),
            Cascade = cascade,
            Samples = Math.Max(1, samples),
            Faces = bounces < 0 ? MostFaces : bounces + 1,
            Texels = images.Texels,
            X = x,
            Y = y,
            Z = z,
            Spot = corner + (new Vector3(x, y, z) + new Vector3(0.5f)) * apart,
            Apart = apart,
            Images = images,
            Path = path,
            Started = Stopwatch.GetTimestamp(),
        };

        // Where the trace moved the probe off a surface, its rays leave from there.
        _probe.HeldRead = Shaders.BeginBufferRead(images.Held);
        return null;
    }

    /// <summary>
    /// Moves the probe's reading on by a frame, answering what was read once it is done, and
    /// nothing before.
    /// </summary>
    internal static string? StepProbe(EcsWorld ecs)
    {
        if (!Own() || _probe is not { } job || _rays is not { } rays) return "no probe is being read";
        if (++job.Steps > job.Samples / MostAFrame + (int)ConsoleHost.LaterFrames - GiveUpSooner) return FailedProbe(job, "the probe's reading was never read back");
        job.Frames++;
        switch (job.Stage)
        {
            case ProbeStage.Placing:
                if (job.HeldRead is not { } held || !Shaders.TryReadBuffer<Vector4>(held, out var places) || places is null) return null;
                const int p = WorldProbes.Probes;
                var place = places[((job.Cascade * p + job.Z) * p + job.Y) * 4 * p + 3 * p + job.X];
                job.Origin = job.Spot + new Vector3(place.X, place.Y, place.Z) * job.Apart;
                job.Inside = place.W < 0.5f;
                var (wide, high) = ((uint)(2 * job.Texels), (uint)job.Texels);
                job.Sums = Shaders.CreateImage(wide, high, ShaderImageFormat.Rgba32Float);
                job.Regions = Shaders.CreateImage(wide, high, ShaderImageFormat.R32UInt);
                rays.Trace.SetRayScene("scene", rays.Scene).SetBuffer("vertices", rays.Pool.Vertices).SetBuffer("indices", rays.Pool.Indices)
                    .SetBuffer("meshes", rays.Pool.Meshes).SetBuffer("instances", rays.Instances).SetBuffer("materials", rays.Materials)
                    .SetBuffer("lights", rays.Lights).SetBuffer("ways", rays.Ways).SetTexture("sums", job.Sums).SetTexture("regions", job.Regions)
                    .Set("probe", new Vector4(job.Origin, 0f));
                Shaders.SetEngineViewDispatches(job.Camera, Owner, ViewDispatch.PerPixel(rays.Trace, FramePoint.BeforeTonemapping));
                (job.Stage, job.Frames) = (ProbeStage.Tracing, 0);
                return null;
            case ProbeStage.Tracing:
                if (job.Added >= job.Samples)
                {
                    Shaders.SetEngineViewDispatches(job.Camera, Owner);
                    (job.Stage, job.Frames) = (ProbeStage.Reading, 0);
                    return null;
                }
                if (rays.Trace.Program.State != ShaderProgramState.Ready) return null;
                fixed (byte* lights = rays.LightBytes)
                    Native.Check(NativeGlobalIllumination.bcs_gi_lights((NativeGiLights*)lights), "reading the lights the probe's reading is lit by");
                Shaders.WriteBuffer<byte>(rays.Lights, rays.LightBytes);
                var share = Math.Min(MostAFrame, job.Samples - job.Added);
                ReadOnlySpan<uint> counts = [(uint)share, (uint)job.Added, (uint)job.Faces, (uint)job.Texels];
                BindMasks(rays);
                rays.Trace.SetNumbers("counts", 4, counts);
                job.Added += share;
                return null;
            default:
                return ReadProbe(job);
        }
    }

    // Reads the reference's sums and first faces back with the probe's merged light and distances a
    // frame after the last share ran, and writes what they say.
    private static string? ReadProbe(ProbeJob job)
    {
        if (job.Frames == 2)
        {
            job.SumsRead = Shaders.BeginImageRead(job.Sums);
            job.RegionsRead = Shaders.BeginImageRead(job.Regions);
            job.MergedRead = Shaders.BeginImageRead(job.Images.Merged);
            job.KeptRead = Shaders.BeginImageRead(job.Images.Kept);
            job.DistancesRead = Shaders.BeginImageRead(job.Images.Distances);
        }
        if (job.SumsRead is not { } sums || job.RegionsRead is not { } regions || job.MergedRead is not { } merged || job.KeptRead is not { } keptRead
            || job.DistancesRead is not { } distances) return null;
        if (job.SumsBytes is null && Shaders.TryReadBuffer(sums, out var a)) job.SumsBytes = a;
        if (job.RegionsBytes is null && Shaders.TryReadBuffer(regions, out var b)) job.RegionsBytes = b;
        if (job.MergedBytes is null && Shaders.TryReadBuffer(merged, out var c)) job.MergedBytes = c;
        if (job.KeptBytes is null && Shaders.TryReadBuffer(keptRead, out var k)) job.KeptBytes = k;
        if (job.DistancesBytes is null && Shaders.TryReadBuffer(distances, out var d)) job.DistancesBytes = d;
        if (job.SumsBytes is null || job.RegionsBytes is null || job.MergedBytes is null || job.KeptBytes is null || job.DistancesBytes is null) return null;

        var n = job.Texels;
        const int p = WorldProbes.Probes;
        var traced = MemoryMarshal.Cast<byte, Vector4>(job.SumsBytes);
        var first = MemoryMarshal.Cast<byte, uint>(job.RegionsBytes);
        var held = MemoryMarshal.Cast<byte, Half>(job.MergedBytes);
        var kept = MemoryMarshal.Cast<byte, Half>(job.KeptBytes);
        var reached = MemoryMarshal.Cast<byte, Half>(job.DistancesBytes);
        var table = new StringBuilder();
        table.AppendLine("texel\tway\tprobe\tmiddle\tdrawn\tprobe's distance\tmiddle's distance");
        double all = 0, probeSum = 0, middleSum = 0, drawnSum = 0;
        var (near, far, within, slivers) = (0, 0, 0, 0);
        for (var v = 0; v < n; v++)
            for (var u = 0; u < n; u++)
            {
                var point = OctahedronPoint((u + 0.5f) / n, (v + 0.5f) / n);
                var share = 1.0 / Math.Pow(point.Length(), 3);
                var way = Vector3.Normalize(point);
                var at = ((job.Z * (p * n) + job.Y * n + v) * (p * n) + job.X * n + u);
                // As the probe's faces take it, the merged light times the share kept before the
                // interval began, with the light gathered there.
                var probeLight = new Vector3((float)held[at * 4], (float)held[at * 4 + 1], (float)held[at * 4 + 2]) * (float)kept[at * 4 + 3]
                    + new Vector3((float)kept[at * 4], (float)kept[at * 4 + 1], (float)kept[at * 4 + 2]);
                var middle = Mean(traced[v * 2 * n + u]);
                var drawn = Mean(traced[v * 2 * n + n + u]);
                var probeDistance = Math.Abs((float)reached[at]);
                var middleDistance = first[v * 2 * n + u] is var bits and > 0 ? BitConverter.UInt32BitsToSingle(bits) : float.PositiveInfinity;
                if (float.IsFinite(middleDistance))
                {
                    var off = (probeDistance - middleDistance) / middleDistance;
                    if (Math.Abs(off) <= 0.1) within++;
                    else if (off > 0) far++;
                    else near++;
                }
                if (Brightness(drawn) > 1e-6 && Brightness(middle) < 0.1 * Brightness(drawn) && Brightness(probeLight) < 0.1 * Brightness(drawn)) slivers++;
                (all, probeSum, middleSum, drawnSum) = (all + share, probeSum + share * Brightness(probeLight), middleSum + share * Brightness(middle), drawnSum + share * Brightness(drawn));
                table.AppendLine(string.Create(CultureInfo.InvariantCulture,
                    $"{u},{v}\t{way.X:0.00} {way.Y:0.00} {way.Z:0.00}\t{probeLight.X:0.000000} {probeLight.Y:0.000000} {probeLight.Z:0.000000}\t{middle.X:0.000000} {middle.Y:0.000000} {middle.Z:0.000000}\t{drawn.X:0.000000} {drawn.Y:0.000000} {drawn.Z:0.000000}\t{probeDistance:0.00}\t{(float.IsFinite(middleDistance) ? middleDistance.ToString("0.00", CultureInfo.InvariantCulture) : "none")}"));
            }

        LastProbe = (probeSum / all, middleSum / all, drawnSum / all, within);
        var seconds = Stopwatch.GetElapsedTime(job.Started).TotalSeconds;
        var said = string.Create(CultureInfo.InvariantCulture,
            $"cascade {job.Cascade}'s probe {job.X} {job.Y} {job.Z} at {job.Origin.X:0.##} {job.Origin.Y:0.##} {job.Origin.Z:0.##}{(Vector3.Distance(job.Origin, job.Spot) > 1e-3f ? FormattableString.Invariant($" moved from {job.Spot.X:0.##} {job.Spot.Y:0.##} {job.Spot.Z:0.##}") : "")}{(job.Inside ? " inside a mesh" : "")}, its {n * n} ways' mean light {probeSum / all:0.000000} against {middleSum / all:0.000000} along their middles and {drawnSum / all:0.000000} drawn across them at {job.Samples} paths a way, "
            + $"{within} of its rays within a tenth of the middle's first face, {far} further and {near} nearer, {slivers} ways whose light the middle and the probe miss, in {seconds:0.0} s");
        if (job.Path is { } path)
        {
            var full = System.IO.Path.GetFullPath(path);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, said + "\n" + table);
            said += $", its ways into {full}";
        }
        FinishProbe(job);
        return said;

        static Vector3 Mean(Vector4 sum) => new Vector3(sum.X, sum.Y, sum.Z) / Math.Max(sum.W, 1f);
        static double Brightness(Vector3 light) => (light.X + light.Y + light.Z) / 3.0;
    }

    // The point of the octahedron a point of the square stands for, as bcs_gi's octahedronPoint.
    private static Vector3 OctahedronPoint(float u, float v)
    {
        var (x, y) = (u * 2f - 1f, v * 2f - 1f);
        var point = new Vector3(x, y, 1f - MathF.Abs(x) - MathF.Abs(y));
        if (point.Z < 0f) (point.X, point.Y) = ((1f - MathF.Abs(y)) * (x >= 0f ? 1f : -1f), (1f - MathF.Abs(x)) * (y >= 0f ? 1f : -1f));
        return point;
    }

    private static string FailedProbe(ProbeJob job, string why)
    {
        FinishProbe(job);
        return why;
    }

    // Takes the tracer off the camera and lets the probe's images go.
    private static void FinishProbe(ProbeJob job)
    {
        if (job.Stage != ProbeStage.Reading) Shaders.SetEngineViewDispatches(job.Camera, Owner);
        foreach (var handle in new[] { job.Sums, job.Regions })
            if (handle.IsValid) AssetServer.Release(handle);
        // The camera's reference, which the tracer is shared with, starts again on the camera's
        // own.
        _rays?.Trace.Set("probe", Vector4.Zero);
        _probe = null;
    }
}
