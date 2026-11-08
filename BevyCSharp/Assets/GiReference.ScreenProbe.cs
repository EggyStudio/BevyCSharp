using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// One screen probe's rays read against the reference along each, as <c>gi.screenprobe</c> runs it.
/// </summary>
/// <remarks>
/// <para>
/// A screen probe stands on the surface under the middle of its tile and sends sixteen rays across
/// the hemisphere around the surface's normal, each from one of sixteen places in its texel of the
/// hemisphere's square, as far as twice the world's first cascade's spacing. Each ray is met on the
/// picture, through the field within its reach, by the world's light from beyond it, or through the
/// field past its reach where the world's probes hold nothing along its way, and the probe sums
/// their light, weighed by how squarely each falls and the share of the hemisphere its texel spans.
/// The screen kernel writes one probe's rays as it traces them (<c>ray_log</c>), and the reference
/// traces from where they leave along each ray's way and along ways drawn across its texel, every
/// face met lit as the probes light one, so the probe's sum is read against the reference's along
/// the same sixteen ways and across the hemisphere.
/// </para>
/// <para>
/// It runs on the camera over a few frames as the reference does, through
/// <see cref="StepScreenProbe"/>, and takes the light the probes are lit by.
/// </para>
/// </remarks>
internal static unsafe partial class GiReference
{
    private enum ScreenStage { Logging, Tracing, Reading }

    private sealed class ScreenJob
    {
        public required Entity Camera;
        public required int X, Y, Samples, Faces;
        public string? Path;
        public ScreenStage Stage;
        public int Added, Frames, Steps;
        public long Started;
        public Vector4[] Log = [];
        public AssetHandle Sums = AssetHandle.None, Regions = AssetHandle.None;
        public BufferRead? LogRead, SumsRead, RegionsRead;
        public byte[]? SumsBytes, RegionsBytes;
    }

    // A screen probe's sixteen ways, then the hemisphere's two ways across its surface and its
    // normal.
    private const int ScreenWays = 16 + 3;

    // The frames from asking for a probe's rays to reading them, those in flight when it was asked
    // having been built without it.
    private const int ScreenLogFrames = 4;

    private static ScreenJob? _screenProbe;

    /// <summary>
    /// The screen probe read last, the light it sums, irradiance over pi, against the reference's
    /// along its sixteen ways and drawn across the hemisphere, the ways weighed as the probe weighs
    /// them, and how many of its rays were met on the picture, through the field, beyond their
    /// reach and through the field past it.
    /// </summary>
    internal static (double Probe, double Along, double Drawn, int OnPicture, int InField, int Beyond, int PastReach)? LastScreenProbe { get; private set; }

    /// <summary>
    /// Starts reading the screen probe under pixel <paramref name="x"/>, <paramref name="y"/> of
    /// the camera's picture against <paramref name="samples"/> paths along each of its rays' ways,
    /// each meeting <paramref name="bounces"/> faces past the first at most, or until Russian
    /// roulette ends it where that is negative, its table written to <paramref name="path"/> where
    /// one is given.
    /// Answers why it cannot, or nothing.
    /// </summary>
    internal static string? BeginScreenProbe(EcsWorld ecs, int x, int y, int samples, int bounces = -1, string? path = null)
    {
        if (Refused(ecs, rays: true) is { } why) return why;
        if (WorldProbes.ScreenState is not (true, false) || WorldProbes.ScreenRayLog is not { IsValid: true } log)
            return "the camera's screen probes are not traced";
        if (x < 0 || y < 0 || x > 0xFFFF || y > 0xFFFF) return $"{x}, {y} is not a pixel of the picture";

        var rays = _rays!;
        var (slots, _) = Fill(ecs, rays);
        if (slots.Length == 0) return "the camera draws no meshes for the rays to meet";

        // Emptied first, so a pixel under which no probe stands on a surface reads as none.
        Shaders.WriteBuffer<Vector4>(log, new Vector4[WorldProbes.ScreenRayLogValues]);
        WorldProbes.LoggedScreenPixel = (uint)(x | y << 16) + 1;
        _screenProbe = new ScreenJob
        {
            Camera = CameraOf(ecs),
            X = x,
            Y = y,
            Samples = Math.Max(1, samples),
            Faces = bounces < 0 ? MostFaces : bounces + 1,
            Path = path,
            Started = Stopwatch.GetTimestamp(),
        };
        return null;
    }

    /// <summary>
    /// Moves the screen probe's reading on by a frame, answering what was read once it is done, and
    /// nothing before.
    /// </summary>
    internal static string? StepScreenProbe(EcsWorld ecs)
    {
        if (!Own() || _screenProbe is not { } job || _rays is not { } rays) return "no screen probe is being read";
        if (++job.Steps > job.Samples / MostAFrame + (int)ConsoleHost.LaterFrames - GiveUpSooner) return FailedScreenProbe(job, "the screen probe's reading was never read back");
        job.Frames++;
        switch (job.Stage)
        {
            case ScreenStage.Logging:
                if (job.Frames == ScreenLogFrames) job.LogRead = Shaders.BeginBufferRead(WorldProbes.ScreenRayLog);
                if (job.LogRead is not { } read || !Shaders.TryReadBuffer<Vector4>(read, out var log) || log is null) return null;
                WorldProbes.LoggedScreenPixel = 0;
                if (log[1] == Vector4.Zero) return FailedScreenProbe(job, $"no screen probe stands on a surface under {job.X}, {job.Y}");
                job.Log = log;

                // The probe's ways, and its hemisphere's ways as the screen kernel works them out,
                // for the reference's ways drawn across each ray's texel.
                var n = new Vector3(log[1].X, log[1].Y, log[1].Z);
                var side = MathF.Abs(n.Y) < 0.99f ? Vector3.Normalize(Vector3.Cross(n, Vector3.UnitY)) : Vector3.UnitX;
                var along = Vector3.Cross(n, side);
                var ways = new Vector4[ScreenWays];
                for (var i = 0; i < 16; i++)
                    ways[i] = log[4 + i * 4 + 1] with { W = 0f };
                (ways[16], ways[17], ways[18]) = (new Vector4(side, 0f), new Vector4(along, 0f), new Vector4(n, 0f));
                Shaders.WriteBuffer<Vector4>(rays.Ways, ways);

                job.Sums = Shaders.CreateImage(32, 1, ShaderImageFormat.Rgba32Float);
                job.Regions = Shaders.CreateImage(32, 1, ShaderImageFormat.R32UInt);
                rays.Trace.SetRayScene("scene", rays.Scene).SetBuffer("vertices", rays.Pool.Vertices).SetBuffer("indices", rays.Pool.Indices)
                    .SetBuffer("meshes", rays.Pool.Meshes).SetBuffer("instances", rays.Instances).SetBuffer("materials", rays.Materials)
                    .SetBuffer("lights", rays.Lights).SetBuffer("ways", rays.Ways).SetTexture("sums", job.Sums).SetTexture("regions", job.Regions)
                    .Set("probe", new Vector4(log[2].X, log[2].Y, log[2].Z, 1f));
                Shaders.SetEngineViewDispatches(job.Camera, Owner, ViewDispatch.PerPixel(rays.Trace, FramePoint.BeforeTonemapping));
                (job.Stage, job.Frames) = (ScreenStage.Tracing, 0);
                return null;
            case ScreenStage.Tracing:
                if (job.Added >= job.Samples)
                {
                    Shaders.SetEngineViewDispatches(job.Camera, Owner);
                    (job.Stage, job.Frames) = (ScreenStage.Reading, 0);
                    return null;
                }
                if (rays.Trace.Program.State != ShaderProgramState.Ready) return null;
                fixed (byte* lights = rays.LightBytes)
                    Native.Check(Native.bcs_gi_lights((NativeGiLights*)lights), "reading the lights the screen probe's reading is lit by");
                Shaders.WriteBuffer<byte>(rays.Lights, rays.LightBytes);
                var share = Math.Min(MostAFrame, job.Samples - job.Added);
                ReadOnlySpan<uint> counts = [(uint)share, (uint)job.Added, (uint)job.Faces, 16];
                BindMasks(rays);
                rays.Trace.SetNumbers("counts", 4, counts);
                job.Added += share;
                return null;
            default:
                return ReadScreenProbe(job);
        }
    }

    // Reads the reference's sums and first faces back a frame after the last share ran, and writes
    // what they say beside the probe's rays.
    private static string? ReadScreenProbe(ScreenJob job)
    {
        if (job.Frames == 2)
        {
            job.SumsRead = Shaders.BeginImageRead(job.Sums);
            job.RegionsRead = Shaders.BeginImageRead(job.Regions);
        }
        if (job.SumsRead is not { } sums || job.RegionsRead is not { } regions) return null;
        if (job.SumsBytes is null && Shaders.TryReadBuffer(sums, out var a)) job.SumsBytes = a;
        if (job.RegionsBytes is null && Shaders.TryReadBuffer(regions, out var b)) job.RegionsBytes = b;
        if (job.SumsBytes is null || job.RegionsBytes is null) return null;

        var log = job.Log;
        var traced = MemoryMarshal.Cast<byte, Vector4>(job.SumsBytes);
        var first = MemoryMarshal.Cast<byte, uint>(job.RegionsBytes);
        var table = new StringBuilder();
        table.AppendLine("ray\tway\tweight\tmet\tprobe\talong\tdrawn\tits own\tprobe's distance\talong's first face");
        double weights = 0, probeSum = 0, alongSum = 0, drawnSum = 0;
        var met = new int[5];
        for (var i = 0; i < 16; i++)
        {
            var (light, way, end, own) = (log[4 + i * 4], log[4 + i * 4 + 1], log[4 + i * 4 + 2], log[4 + i * 4 + 3]);
            var probeLight = new Vector3(light.X, light.Y, light.Z);
            var alongLight = Mean(traced[i]);
            var drawnLight = Mean(traced[16 + i]);
            var how = (int)MathF.Round(light.W);
            met[Math.Clamp(how, 0, 4)]++;
            var weight = (double)way.W;
            (weights, probeSum, alongSum, drawnSum) = (weights + weight, probeSum + weight * Brightness(probeLight), alongSum + weight * Brightness(alongLight), drawnSum + weight * Brightness(drawnLight));
            var faced = first[i] is var bits and > 0 ? BitConverter.UInt32BitsToSingle(bits) : float.PositiveInfinity;
            table.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{i}\t{way.X:0.00} {way.Y:0.00} {way.Z:0.00}\t{weight:0.000}\t{MetName(how)}\t{probeLight.X:0.000000} {probeLight.Y:0.000000} {probeLight.Z:0.000000}\t{alongLight.X:0.000000} {alongLight.Y:0.000000} {alongLight.Z:0.000000}\t{drawnLight.X:0.000000} {drawnLight.Y:0.000000} {drawnLight.Z:0.000000}\t{own.X:0.000000} {own.Y:0.000000} {own.Z:0.000000}\t{end.W:0.00}\t{(float.IsFinite(faced) ? faced.ToString("0.00", CultureInfo.InvariantCulture) : "none")}"));
        }

        // The sums as the probe takes them, the weights scaled to sum to pi, over pi again, and so
        // the mean light of its ways.
        var probe = probeSum / weights;
        var alongMean = alongSum / weights;
        var drawnMean = drawnSum / weights;
        LastScreenProbe = (probe, alongMean, drawnMean, met[1], met[2], met[3], met[4]);
        var seconds = Stopwatch.GetElapsedTime(job.Started).TotalSeconds;
        var said = string.Create(CultureInfo.InvariantCulture,
            $"the screen probe under {job.X}, {job.Y} at {log[0].X:0.##} {log[0].Y:0.##} {log[0].Z:0.##} facing {log[1].X:0.00} {log[1].Y:0.00} {log[1].Z:0.00}, its rays {log[2].W:0.##} long, sums {probe:0.000000} against {alongMean:0.000000} along its ways and {drawnMean:0.000000} drawn across them at {job.Samples} paths a way, "
            + $"{met[1]} of its rays met on the picture, {met[2]} through the field, {met[3]} beyond their reach and {met[4]} through the field past it, in {seconds:0.0} s");
        if (job.Path is { } path)
        {
            var full = System.IO.Path.GetFullPath(path);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, said + "\n" + table);
            said += $", its rays into {full}";
        }
        FinishScreenProbe(job);
        return said;

        static Vector3 Mean(Vector4 sum) => new Vector3(sum.X, sum.Y, sum.Z) / Math.Max(sum.W, 1f);
        static double Brightness(Vector3 light) => (light.X + light.Y + light.Z) / 3.0;
        static string MetName(int how) => how switch
        {
            1 => "picture",
            2 => "field",
            3 => "beyond",
            4 => "past reach",
            _ => "none",
        };
    }

    private static string FailedScreenProbe(ScreenJob job, string why)
    {
        FinishScreenProbe(job);
        return why;
    }

    // Takes the tracer off the camera, stops the log and lets the reading's images go.
    private static void FinishScreenProbe(ScreenJob job)
    {
        if (job.Stage == ScreenStage.Tracing) Shaders.SetEngineViewDispatches(job.Camera, Owner);
        WorldProbes.LoggedScreenPixel = 0;
        foreach (var handle in new[] { job.Sums, job.Regions })
            if (handle.IsValid) AssetServer.Release(handle);
        // The camera's reference, which the tracer is shared with, starts again on the camera's
        // own.
        _rays?.Trace.Set("probe", Vector4.Zero);
        _screenProbe = null;
    }
}
