using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;
using Bevy.Reflected;

namespace Bevy;

/// <summary>
/// The measure of global illumination, a path-traced reference of the camera's picture
/// (<c>gi_reference.slang</c>) and the camera's own light before tonemapping compared with it over
/// named regions of the view, as <c>gi.reference</c> and <c>gi.compare</c> run them, 3DEngine's
/// BounceReference over a ray scene of the meshes the camera draws.
/// </summary>
/// <remarks>
/// <para>
/// A reference is written as a PNG of its light as the picture shows it untonemapped, with its linear
/// light beside it in a PFM file, what each pixel shows in a regions file, the slot's number times
/// eight and the axis its face turns toward most, plus one, and the regions' names in a text file,
/// each the entity's name, or its slot where it has none, and the axis.
/// </para>
/// <para>
/// The ray scene holds every shown entity that draws a 3D mesh of triangles, with its standard
/// material's base color, emissive light, roughness, metallic and reflectance as the engine's
/// material buffer has them, so a texture counts as its base color and a mesh's vertices' colors are
/// left out. The lights are those global illumination's probes are lit by, the sun, the light from
/// all around and the sixteen point and spot lights nearest the camera. It needs a GPU that traces
/// rays (<see cref="Shaders.SupportsRayQueries"/>). A ray scene cannot be freed, so the app keeps one,
/// with its geometry pool, from one reference to the next, and makes a larger one where a scene has
/// more meshes than it holds.
/// </para>
/// <para>
/// Both the reference and the copy of the picture run on the camera over a few frames, the copy at
/// first on an image of one texel to read the picture's size, since an offscreen camera has no window
/// to ask, and each moves on a frame at a time through <see cref="Step"/>, which a console command
/// asks once a frame and a test asks from its steps.
/// </para>
/// </remarks>
internal static unsafe class GiReference
{
    /// <summary>The most paths a frame traces across the whole picture, so a frame takes tens of milliseconds.</summary>
    private const int PathsAFrame = 1 << 22;

    /// <summary>The most paths a pixel takes in one frame, however small the picture.</summary>
    private const int MostAFrame = 64;

    /// <summary>The most faces a path meets where no count of bounces is given, Russian roulette ending most before.</summary>
    private const int MostFaces = 32;

    private static readonly string[] Axes = ["+x", "-x", "+y", "-y", "+z", "-z"];

    /// <summary>The name the reference's dispatches on a camera are kept under beside the engine's others.</summary>
    private const string Owner = "gi.reference";

    /// <summary>A reference as traced, its light in rows from the top, three floats a pixel, what each pixel shows, and the names of those regions.</summary>
    internal sealed record Traced(int Width, int Height, float[] Light, uint[] Regions, int Samples, IReadOnlyDictionary<uint, string> Names);

    private enum Stage { Sizing, Tracing, Reading, Copying }

    private sealed class Job
    {
        public required bool Reference;
        public required Entity Camera;
        public string? Path;
        public int Samples, Faces, Width, Height, Added, Frames;
        public Stage Stage;
        public AssetHandle Size = AssetHandle.None, Picture = AssetHandle.None, Sums = AssetHandle.None, Regions = AssetHandle.None;
        public BufferRead? SizeRead, SumsRead, RegionsRead, PictureRead;
        public byte[]? SumsBytes, RegionsBytes;
        public string[] Slots = [];
        public int Skipped;
        public long Started;
    }

    // The app's ray scene of the drawn meshes with its pool, instance and material buffers, the
    // lights it is lit by and its programs, and each mesh's place in the pool by its handle.
    private sealed class Rays
    {
        public GeometryPool Pool;
        public RayScene Scene;
        public int Capacity;
        public AssetHandle Instances = AssetHandle.None, Materials = AssetHandle.None, Lights = AssetHandle.None;
        public ShaderInstance Trace, Copy;
        public readonly Dictionary<int, int> PoolMeshes = [];
        public readonly byte[] LightBytes = new byte[WorldProbes.LightsBytes];
    }

    private static App? _app;
    private static Rays? _rays;
    private static Job? _job;

    /// <summary>The reference traced last, in the running app.</summary>
    internal static Traced? Last { get; private set; }

    /// <summary>The camera's light read last, in rows from the top, three floats a pixel, with its size.</summary>
    internal static (int Width, int Height, float[] Light)? LastPicture { get; private set; }

    /// <summary>Whether a reference is being traced or the picture read.</summary>
    internal static bool Busy => Own() && _job is not null;

    /// <summary>
    /// Starts tracing a reference of the camera's picture with <paramref name="samples"/> paths a
    /// pixel, each meeting <paramref name="bounces"/> faces past the first at most, or until Russian
    /// roulette ends it where that is negative, written to <paramref name="path"/> and its files beside
    /// it where a path is given. Answers why it cannot, or nothing.
    /// </summary>
    internal static string? BeginReference(EcsWorld ecs, string? path, int samples, int bounces = -1)
    {
        if (Refused(ecs, rays: true) is { } why) return why;
        var rays = _rays!;
        var (slots, skipped) = Fill(ecs, rays);
        if (slots.Length == 0) return "the camera draws no meshes for the rays to meet";

        _job = new Job
        {
            Reference = true,
            Camera = CameraOf(ecs),
            Path = path,
            Samples = Math.Max(1, samples),
            Faces = bounces < 0 ? MostFaces : bounces + 1,
            Slots = slots,
            Skipped = skipped,
            Started = Stopwatch.GetTimestamp(),
        };
        AskSize(_job, rays);
        return null;
    }

    /// <summary>
    /// Starts reading the camera's light before tonemapping, compared with the reference at
    /// <paramref name="path"/> where one is given. Answers why it cannot, or nothing.
    /// </summary>
    internal static string? BeginPicture(EcsWorld ecs, string? path)
    {
        if (Refused(ecs, rays: false) is { } why) return why;
        _job = new Job { Reference = false, Camera = CameraOf(ecs), Path = path, Started = Stopwatch.GetTimestamp() };
        AskSize(_job, _rays!);
        return null;
    }

    /// <summary>
    /// Moves the reference or the reading on by a frame, answering what was written or found once it
    /// is done, and nothing before.
    /// </summary>
    internal static string? Step(EcsWorld ecs)
    {
        if (!Own() || _job is not { } job || _rays is not { } rays) return "nothing is being traced or read";
        if (!ecs.IsAlive(job.Camera))
        {
            Finish(job);
            return "the camera went away before it was done";
        }

        job.Frames++;
        switch (job.Stage)
        {
            case Stage.Sizing:
                return Sized(job, rays);
            case Stage.Tracing:
                Trace(job, rays);
                return null;
            case Stage.Reading:
                return Read(job);
            default:
                return Copied(job);
        }
    }

    // Why nothing can start, or nothing, with the app's programs made, the tracer and its ray scene
    // where `rays` is set for the reference, which a GPU that traces no rays cannot trace, though it
    // reads its picture.
    private static string? Refused(EcsWorld ecs, bool rays)
    {
        if (!App.HasRenderer) return "there is no renderer to trace with";
        if (Busy) return "a reference is being traced or the picture read, and Step moves it on";
        if (rays && !Shaders.SupportsRayQueries) return "the GPU traces no rays, which the reference is traced through";
        if (CameraOf(ecs).IsNone) return "there is no 3D camera to trace the picture of";

        _rays ??= new Rays { Copy = Shaders.CreateInstance(SceneField.KernelProgram("gi_frame.slang")) };
        if (rays && !_rays.Trace.IsValid)
        {
            _rays.Pool = Shaders.CreateGeometryPool();
            _rays.Lights = Shaders.CreateBuffer(WorldProbes.LightsBytes);
            _rays.Trace = Shaders.CreateInstance(SceneField.KernelProgram("gi_reference.slang", ShaderTarget.SpirV));
        }
        return null;
    }

    // The camera the field lies around, or the first 3D camera where there is none.
    private static Entity CameraOf(EcsWorld ecs)
    {
        var camera = SceneField.ViewCamera;
        if (!camera.IsNone && ecs.IsAlive(camera)) return camera;
        var cameras = ecs.All<Camera3dRef>();
        return cameras.Length > 0 ? cameras[0].Entity : Entity.None;
    }

    // Whether the state belongs to the running app, forgetting what belonged to an app before it,
    // whose buffers went with it.
    private static bool Own()
    {
        if (ReferenceEquals(_app, App.Running)) return true;
        (_app, _rays, _job, Last, LastPicture) = (App.Running, null, null, null, null);
        return App.Running is not null;
    }

    /// <summary>
    /// Puts every shown entity drawing a mesh of triangles in a slot of the ray scene and of the
    /// instance and material buffers, making them larger where they hold too few, and answers each
    /// slot's name and how many entities were left out, their meshes not triangles or not loaded.
    /// </summary>
    private static (string[] Slots, int Skipped) Fill(EcsWorld ecs, Rays rays)
    {
        var placed = new List<(Entity Entity, int Mesh)>();
        var skipped = 0;
        foreach (var drawn in ecs.All<Mesh3dRef>())
        {
            var entity = drawn.Entity;
            if (ecs.Get<InheritedVisibilityRef>(entity) is { } seen && !seen.Value) continue;
            var mesh = Render.MeshOf(ecs, entity);
            if (!mesh.IsValid) continue;
            if (!rays.PoolMeshes.TryGetValue(mesh.Key, out var index))
            {
                try
                {
                    index = Shaders.AddToGeometryPool(rays.Pool, mesh);
                }
                catch (BevyNativeException)
                {
                    skipped++;
                    continue;
                }
                rays.PoolMeshes[mesh.Key] = index;
            }
            placed.Add((entity, index));
        }

        if (placed.Count > rays.Capacity)
        {
            rays.Capacity = Math.Max(placed.Count, rays.Capacity * 2);
            rays.Scene = Shaders.CreateRayScene(rays.Pool, rays.Capacity);
            if (rays.Instances.IsValid) AssetServer.Release(rays.Instances);
            if (rays.Materials.IsValid) AssetServer.Release(rays.Materials);
            rays.Instances = Shaders.CreateInstanceBuffer(rays.Capacity);
            rays.Materials = Shaders.CreateMaterialBuffer(rays.Capacity);
        }

        var names = new string[placed.Count];
        for (var slot = 0; slot < rays.Capacity; slot++)
        {
            var (entity, mesh) = slot < placed.Count ? placed[slot] : (Entity.None, 0);
            Shaders.SetRaySceneInstance(rays.Scene, slot, entity, mesh);
            Shaders.SetInstance(rays.Instances, slot, entity);
            Shaders.SetInstance(rays.Materials, slot, entity);
            if (slot < placed.Count) names[slot] = ecs.NameOf(entity) is { Length: > 0 } name ? name : $"mesh {slot}";
        }

        // A mesh changed since it came into the pool is traced as it stands.
        Shaders.RebuildRayScene(rays.Scene);
        return (names, skipped);
    }

    // Puts the copy on the camera over an image of one texel, which writes the picture's size.
    private static void AskSize(Job job, Rays rays)
    {
        job.Size = Shaders.CreateBuffer(sizeof(uint) * 2);
        job.Picture = Shaders.CreateImage(1, 1, ShaderImageFormat.Rgba32Float);
        rays.Copy.SetTexture("frame", job.Picture).SetBuffer("size", job.Size);
        Shaders.SetEngineViewDispatches(job.Camera, Owner, ViewDispatch.PerPixel(rays.Copy, FramePoint.BeforeTonemapping));
        job.Stage = Stage.Sizing;
    }

    // Reads the picture's size once the copy has run, and makes the images the reference or the
    // copy are written into at that size.
    private static string? Sized(Job job, Rays rays)
    {
        if (job.SizeRead is null)
        {
            // The copy runs once its program has compiled, and its size is read a frame after.
            if (job.Frames >= 3 && rays.Copy.Program.State == ShaderProgramState.Ready) job.SizeRead = Shaders.BeginBufferRead(job.Size);
            return (ulong)job.Frames > ConsoleHost.LaterFrames ? Failed(job, "the picture's size was never read, its copy not compiling") : null;
        }

        if (!Shaders.TryReadBuffer<uint>(job.SizeRead.Value, out var size) || size is null) return null;
        if (size[0] == 0 || size[1] == 0)
        {
            job.SizeRead = null;
            return null;
        }

        (job.Width, job.Height) = ((int)size[0], (int)size[1]);
        AssetServer.Release(job.Picture);
        if (job.Reference)
        {
            job.Sums = Shaders.CreateImage((uint)job.Width, (uint)job.Height, ShaderImageFormat.Rgba32Float);
            job.Regions = Shaders.CreateImage((uint)job.Width, (uint)job.Height, ShaderImageFormat.R32UInt);
            job.Picture = AssetHandle.None;
            rays.Trace.SetRayScene("scene", rays.Scene).SetBuffer("vertices", rays.Pool.Vertices).SetBuffer("indices", rays.Pool.Indices)
                .SetBuffer("meshes", rays.Pool.Meshes).SetBuffer("instances", rays.Instances).SetBuffer("materials", rays.Materials)
                .SetBuffer("lights", rays.Lights).SetTexture("sums", job.Sums).SetTexture("regions", job.Regions);
            Shaders.SetEngineViewDispatches(job.Camera, Owner, ViewDispatch.PerPixel(rays.Trace, FramePoint.BeforeTonemapping));
            job.Stage = Stage.Tracing;
        }
        else
        {
            job.Picture = Shaders.CreateImage((uint)job.Width, (uint)job.Height, ShaderImageFormat.Rgba32Float);
            rays.Copy.SetTexture("frame", job.Picture);
            job.Stage = Stage.Copying;
        }
        job.Frames = 0;
        return null;
    }

    // Asks for the frame's share of the paths, with the lights as they stand, once the tracer has
    // compiled, and once every share has been asked for takes the tracer off the camera.
    private static void Trace(Job job, Rays rays)
    {
        if (job.Added >= job.Samples)
        {
            Shaders.SetEngineViewDispatches(job.Camera, Owner);
            (job.Stage, job.Frames) = (Stage.Reading, 0);
            return;
        }
        if (rays.Trace.Program.State != ShaderProgramState.Ready) return;

        fixed (byte* lights = rays.LightBytes)
            Native.Check(Native.bcs_gi_lights((NativeGiLights*)lights), "reading the lights the reference is lit by");
        Shaders.WriteBuffer<byte>(rays.Lights, rays.LightBytes);

        var share = Math.Clamp(PathsAFrame / Math.Max(1, job.Width * job.Height), 1, MostAFrame);
        share = Math.Min(share, job.Samples - job.Added);
        ReadOnlySpan<uint> counts = [(uint)share, (uint)job.Added, (uint)job.Faces, 0];
        rays.Trace.SetNumbers("counts", 4, counts);
        job.Added += share;
    }

    // Reads the sums and the regions back a frame after the last share ran, and writes the files.
    private static string? Read(Job job)
    {
        if (job.Frames == 2)
        {
            job.SumsRead = Shaders.BeginImageRead(job.Sums);
            job.RegionsRead = Shaders.BeginImageRead(job.Regions);
        }
        if (job.SumsRead is not { } sums || job.RegionsRead is not { } regions) return null;
        if (job.SumsBytes is null && Shaders.TryReadBuffer(sums, out var summed)) job.SumsBytes = summed;
        if (job.RegionsBytes is null && Shaders.TryReadBuffer(regions, out var met)) job.RegionsBytes = met;
        if (job.SumsBytes is null || job.RegionsBytes is null) return null;

        var pixels = job.Width * job.Height;
        var texels = MemoryMarshal.Cast<byte, Vector4>(job.SumsBytes);
        var light = new float[pixels * 3];
        var samples = int.MaxValue;
        for (var i = 0; i < pixels; i++)
        {
            var sum = texels[i];
            var count = Math.Max(sum.W, 1f);
            (light[i * 3], light[i * 3 + 1], light[i * 3 + 2]) = (sum.X / count, sum.Y / count, sum.Z / count);
            samples = Math.Min(samples, (int)sum.W);
        }
        var shown = MemoryMarshal.Cast<byte, uint>(job.RegionsBytes)[..pixels].ToArray();
        var names = new Dictionary<uint, string>();
        foreach (var region in shown.Where(r => r != 0).Distinct())
        {
            var slot = (int)((region - 1) / 8);
            names[region] = $"{(slot < job.Slots.Length ? job.Slots[slot] : $"mesh {slot}")} {Axes[(region - 1) % 8]}";
        }

        var traced = new Traced(job.Width, job.Height, light, shown, samples, names);
        Last = traced;
        var seconds = Stopwatch.GetElapsedTime(job.Started).TotalSeconds;
        var skipped = job.Skipped == 0 ? "" : $", {job.Skipped} meshes left out as not triangles or not loaded";
        var said = $"traced {job.Width} by {job.Height} at {samples} paths a pixel in {seconds:0.0} s, {job.Slots.Length} meshes{skipped}, {names.Count} regions";
        if (job.Path is { } path)
        {
            var full = System.IO.Path.GetFullPath(path);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, Png(light, job.Width, job.Height));
            WritePfm(full + ".pfm", light, job.Width, job.Height);
            File.WriteAllBytes(full + ".regions", MemoryMarshal.AsBytes(shown.AsSpan()).ToArray());
            File.WriteAllLines(full + ".regions.txt", names.OrderBy(n => n.Key).Select(n => $"{n.Key}\t{n.Value}"));
            said += $", into {full} with its .pfm, .regions and .regions.txt";
        }
        Finish(job);
        return said;
    }

    // Takes the copy off the camera once it has run a few frames, reads the picture back and, where a
    // reference was named, compares the two.
    private static string? Copied(Job job)
    {
        if (job.Frames == 3) Shaders.SetEngineViewDispatches(job.Camera, Owner);
        if (job.Frames == 5) job.PictureRead = Shaders.BeginImageRead(job.Picture);
        if (job.PictureRead is not { } read || !Shaders.TryReadBuffer(read, out var bytes) || bytes is null) return null;

        var pixels = job.Width * job.Height;
        var texels = MemoryMarshal.Cast<byte, Vector4>(bytes);
        var light = new float[pixels * 3];
        for (var i = 0; i < pixels; i++)
            (light[i * 3], light[i * 3 + 1], light[i * 3 + 2]) = (texels[i].X, texels[i].Y, texels[i].Z);
        LastPicture = (job.Width, job.Height, light);
        Finish(job);
        return job.Path is { } path ? Compare(light, job.Width, job.Height, path) : $"read the picture's light, {job.Width} by {job.Height}";
    }

    private static string Failed(Job job, string why)
    {
        Finish(job);
        return why;
    }

    // Takes whatever the job put on its camera off it and lets its images go.
    private static void Finish(Job job)
    {
        if (job.Stage != Stage.Reading) Shaders.SetEngineViewDispatches(job.Camera, Owner);
        foreach (var handle in new[] { job.Size, job.Picture, job.Sums, job.Regions })
            if (handle.IsValid) AssetServer.Release(handle);
        _job = null;
    }

    /// <summary>
    /// The camera's light against the reference at <paramref name="path"/>, how far the view lies
    /// from it as its pixels' differences summed over the reference's light, and each region's
    /// share of the picture, its mean light and the reference's, the largest first, with a picture of
    /// the difference written beside the reference, red where the picture is brighter and blue where
    /// darker.
    /// </summary>
    internal static string Compare(float[] light, int width, int height, string path)
    {
        var full = System.IO.Path.GetFullPath(path);
        if (!File.Exists(full + ".pfm")) return $"no reference at {full}, which gi.reference writes";
        var (rw, rh, reference) = ReadPfm(full + ".pfm");
        if (rw != width || rh != height) return $"the reference is {rw} by {rh} and the picture {width} by {height}, which a reference is traced at";
        var regions = File.Exists(full + ".regions") ? MemoryMarshal.Cast<byte, uint>(File.ReadAllBytes(full + ".regions")).ToArray() : new uint[width * height];
        var names = File.Exists(full + ".regions.txt")
            ? File.ReadAllLines(full + ".regions.txt").Select(line => line.Split('\t')).Where(p => p.Length == 2)
                .ToDictionary(p => uint.Parse(p[0], CultureInfo.InvariantCulture), p => p[1])
            : [];

        var each = new Dictionary<uint, (int Pixels, Vector3 Picture, Vector3 Reference)>();
        for (var i = 0; i < width * height; i++)
        {
            var region = i < regions.Length ? regions[i] : 0u;
            var (pixels, seen, traced) = each.GetValueOrDefault(region);
            each[region] = (pixels + 1, seen + At(light, i), traced + At(reference, i));
        }

        var text = new StringBuilder();
        text.Append(CultureInfo.InvariantCulture, $"the picture lies {Distance(light, reference):0.000} of its light from the reference, {width} by {height}");
        foreach (var (region, (pixels, seen, traced)) in each.OrderByDescending(e => e.Value.Pixels).Take(24))
        {
            var name = region == 0 ? "nothing" : names.GetValueOrDefault(region, $"region {region}");
            var (mean, wanted) = (seen / pixels, traced / pixels);
            var over = Luminance(wanted) > 1e-6f ? (Luminance(mean) / Luminance(wanted) - 1f) * 100f : 0f;
            text.Append(CultureInfo.InvariantCulture,
                $"\n{name}: {100f * pixels / (width * height):0.0}% of the view, {Rgb(mean)} against {Rgb(wanted)}, {over:+0;-0;0}%");
        }

        File.WriteAllBytes(full + ".difference.png", Difference(light, reference, width, height));
        text.Append(CultureInfo.InvariantCulture, $"\nthe difference is drawn in {full}.difference.png");
        return text.ToString();

        static string Rgb(Vector3 v) => string.Create(CultureInfo.InvariantCulture, $"{v.X:0.000} {v.Y:0.000} {v.Z:0.000}");
    }

    /// <summary>How far one light lies from another, the pixels' differences in each channel summed over the second's light.</summary>
    internal static double Distance(float[] light, float[] reference)
    {
        double differs = 0, lit = 0;
        for (var i = 0; i < Math.Min(light.Length, reference.Length); i++)
        {
            differs += Math.Abs(light[i] - reference[i]);
            lit += reference[i];
        }
        return lit > 0 ? differs / lit : 0;
    }

    /// <summary>A light of three floats a pixel averaged over squares of <paramref name="down"/> pixels a side.</summary>
    internal static float[] Averaged(float[] light, int width, int height, int down)
    {
        var (w, h) = (width / down, height / down);
        var averaged = new float[w * h * 3];
        for (var y = 0; y < h * down; y++)
        for (var x = 0; x < w * down; x++)
        for (var c = 0; c < 3; c++)
            averaged[((y / down) * w + x / down) * 3 + c] += light[(y * width + x) * 3 + c] / (down * down);
        return averaged;
    }

    /// <summary>Writes a light of three floats a pixel, rows from the top, as a PFM file, which keeps its rows from the bottom.</summary>
    internal static void WritePfm(string path, float[] light, int width, int height)
    {
        using var file = File.Create(path);
        file.Write(Encoding.ASCII.GetBytes(string.Create(CultureInfo.InvariantCulture, $"PF\n{width} {height}\n-1.0\n")));
        for (var y = height - 1; y >= 0; y--)
            file.Write(MemoryMarshal.AsBytes(light.AsSpan(y * width * 3, width * 3)));
    }

    /// <summary>Reads a PFM file <see cref="WritePfm"/> wrote, its light in rows from the top.</summary>
    internal static (int Width, int Height, float[] Light) ReadPfm(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var (at, lines) = (0, new string[3]);
        for (var line = 0; line < 3; line++)
        {
            var end = Array.IndexOf(bytes, (byte)'\n', at);
            lines[line] = Encoding.ASCII.GetString(bytes, at, end - at);
            at = end + 1;
        }
        var size = lines[1].Split(' ');
        var (width, height) = (int.Parse(size[0], CultureInfo.InvariantCulture), int.Parse(size[1], CultureInfo.InvariantCulture));
        var rows = MemoryMarshal.Cast<byte, float>(bytes.AsSpan(at));
        var light = new float[width * height * 3];
        for (var y = 0; y < height; y++)
            rows.Slice((height - 1 - y) * width * 3, width * 3).CopyTo(light.AsSpan(y * width * 3));
        return (width, height, light);
    }

    private static Vector3 At(float[] light, int pixel) => new(light[pixel * 3], light[pixel * 3 + 1], light[pixel * 3 + 2]);

    private static float Luminance(Vector3 c) => 0.2126f * c.X + 0.7152f * c.Y + 0.0722f * c.Z;

    // The light as eight-bit sRGB, as the picture shows it with no tonemapping.
    private static byte[] Png(float[] light, int width, int height)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            for (var c = 0; c < 3; c++)
            {
                var v = Math.Clamp(light[i * 3 + c], 0f, 1f);
                var encoded = v <= 0.0031308f ? v * 12.92f : 1.055f * MathF.Pow(v, 1f / 2.4f) - 0.055f;
                pixels[i * 4 + c] = (byte)MathF.Round(encoded * 255f);
            }
            pixels[i * 4 + 3] = 255;
        }
        return PngWriter.Still((uint)width, (uint)height, pixels);
    }

    // Each pixel's difference in luminance over the reference's mean, red where the picture is
    // brighter and blue where it is darker, a difference as large as the mean at full strength.
    private static byte[] Difference(float[] light, float[] reference, int width, int height)
    {
        var mean = 0f;
        for (var i = 0; i < width * height; i++) mean += Luminance(At(reference, i)) / (width * height);
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            var d = (Luminance(At(light, i)) - Luminance(At(reference, i))) / Math.Max(mean, 1e-6f);
            var strength = (byte)MathF.Round(Math.Clamp(MathF.Abs(d), 0f, 1f) * 255f);
            (pixels[i * 4], pixels[i * 4 + 2], pixels[i * 4 + 3]) = (d > 0 ? strength : (byte)0, d < 0 ? strength : (byte)0, 255);
        }
        return PngWriter.Still((uint)width, (uint)height, pixels);
    }
}
