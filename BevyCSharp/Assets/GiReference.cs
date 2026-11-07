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
/// The ray scene holds every shown entity that draws a 3D mesh of triangles, a mesh of a visibility
/// range at the level the camera's distance gives, as the picture draws it, with its standard
/// material's base color, emissive light, roughness, metallic and reflectance as the engine's
/// material buffer has them, so a texture counts as its base color and a mesh's vertices' colors
/// are left out. What it says when it is done counts the cut-out textures past the material
/// buffer's masks (<see cref="Shaders.MaterialBufferUnmasked"/>), traced whole by their base color,
/// and the trees not dressed yet when it began, which neither it nor the picture holds. It honors
/// the materials' own models rather than a physical surface's, a face's diffuse transmission let
/// through from behind and a leaf's or a grass card's diffuse share lit by its normal bent toward
/// its way out of the crown as the leaf material lights it, and a mesh Bevy draws no shadow of is
/// met by the camera's own rays alone, as Bevy's shadow maps and the scene's distance field leave
/// it out. Every path, a camera's or a probe's, is lit at every face as Bevy's lighting lights it
/// and scatters there by its material, its diffuse share or its GGX lobe drawn by their weights, so
/// the sky a gloss reflects and the light a glossy wall carries between bounces come in, as global
/// illumination's probes carry a face's gloss and the light pass reflects it. The lights are those
/// global illumination's probes are lit by, the sun, the light from all around and the sixteen
/// point and spot lights nearest the camera, the sun dimmed through the camera's air as Bevy's
/// lighting dims it for the picture. It needs a GPU that traces rays (<see
/// cref="Shaders.SupportsRayQueries"/>). A ray scene cannot be freed, so the app keeps one, with
/// its geometry pool, from one reference to the next, and makes a larger one where a scene has more
/// meshes than it holds.
/// </para>
/// <para>
/// Both the reference and the copy of the picture run on the camera over a few frames, the copy at
/// first on an image of one texel to read the picture's size, since an offscreen camera has no window
/// to ask, and each moves on a frame at a time through <see cref="Step"/>, which a console command
/// asks once a frame and a test asks from its steps.
/// </para>
/// </remarks>
internal static unsafe partial class GiReference
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

    /// <summary>
    /// A reference as traced, its light in rows from the top, three floats a pixel, what each pixel
    /// shows, and the names of those regions, and where it was traced leaf by leaf the share of each
    /// pixel's paths that met a leaf first.
    /// </summary>
    internal sealed record Traced(int Width, int Height, float[] Light, uint[] Regions, int Samples, IReadOnlyDictionary<uint, string> Names, float[]? Leaves = null);

    /// <summary>
    /// The share of a pixel's paths that met a leaf first past which <see cref="Compare"/> counts it
    /// against a reference traced leaf by leaf, the pixel all leaf but for its edges, which the
    /// picture draws a leaf at where the reference's paths spread over it meet the sky beside one.
    /// </summary>
    internal const float WholeLeaf = 0.95f;

    private enum Stage { Sizing, Tracing, Reading, Copying }

    /// <summary>How many frames before the console gives a command up a job gives itself up.</summary>
    private const int GiveUpSooner = 10;

    private sealed class Job
    {
        public required bool Reference;
        public required Entity Camera;
        public string? Path;
        public int Samples, Faces, Width, Height, Added, Frames, Steps, SizesAsked, SizesNone;
        public Stage Stage;
        public AssetHandle Size = AssetHandle.None, Picture = AssetHandle.None, Sums = AssetHandle.None, Regions = AssetHandle.None;
        public BufferRead? SizeRead, SumsRead, RegionsRead, PictureRead;
        public byte[]? SumsBytes, RegionsBytes;
        public string[] Slots = [];
        public int Seed, Skipped, Undressed;
        public bool Leaves;
        public long Started;
    }

    // The app's ray scene of the drawn meshes with its pool, instance and material buffers, the
    // lights it is lit by and its programs, and each mesh's place in the pool by its handle.
    private sealed class Rays
    {
        public GeometryPool Pool;
        public RayScene Scene;
        public int Capacity;
        public AssetHandle Instances = AssetHandle.None, Materials = AssetHandle.None, Lights = AssetHandle.None, Ways = AssetHandle.None, White = AssetHandle.None;
        public ShaderInstance Trace, Copy;
        public int Masks, Limit;
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

    /// <summary>Whether a reference is being traced, the picture read or a probe's or a screen probe's ways traced.</summary>
    internal static bool Busy => Own() && (_job is not null || _probe is not null || _screenProbe is not null);

    /// <summary>
    /// Starts tracing a reference of the camera's picture with <paramref name="samples"/> paths a
    /// pixel, each meeting <paramref name="bounces"/> faces past the first at most, or until Russian
    /// roulette ends it where that is negative, written to <paramref name="path"/> and its files beside
    /// it where a path is given, its paths' draws started from <paramref name="seed"/>, so a test can
    /// read how far a reference moves from one draw of its paths to another. Where
    /// <paramref name="leaves"/> says so, a pixel's light is that of its paths that meet a leaf's or a
    /// grass card's face first, the share of them kept beside it (<see cref="Traced.Leaves"/>), so a
    /// crown's leaves are read without the sky between them, which a far crown's pixels hold more of
    /// in the reference than in the picture, whose leaves keep their share of a card in the distance.
    /// Answers why it cannot, or nothing.
    /// </summary>
    internal static string? BeginReference(EcsWorld ecs, string? path, int samples, int bounces = -1, int seed = 0, bool leaves = false)
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
            Seed = seed,
            Leaves = leaves,
            Slots = slots,
            Skipped = skipped,
            Undressed = Trees.Undressed(ecs),
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

        // The console gives a command up after its frames and steps it no more, so a job waiting on
        // a copy that never ran held the measure busy for every command after. The job gives itself
        // up a few frames sooner, a reference after its paths' frames as well, and says where it was.
        if (++job.Steps > (job.Reference ? job.Samples : 0) + (int)ConsoleHost.LaterFrames - GiveUpSooner)
            return Failed(job, job.Stage switch
            {
                Stage.Sizing => $"the picture's size was never read, its copy not compiling or the camera drawing nothing, asked for {job.SizesAsked} times and answered as none {job.SizesNone}",
                Stage.Copying => "the picture was never read back",
                _ => "the reference was never read back",
            });

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
            // A screen probe's ways, which the tracer reads only where it traces from one.
            _rays.Ways = Shaders.CreateBuffer<Vector4>(new Vector4[ScreenWays]);
            // The mask a slot past the material buffer's masks reads, letting nothing through.
            _rays.White = Render.CreateImage([255, 255, 255, 255], 1, 1, srgb: false, mipmaps: false);
            (_rays.Limit, _rays.Masks) = (Shaders.SampledTexturesPerStage, MasksFor(Shaders.SampledTexturesPerStage));
            _rays.Trace = Shaders.CreateInstance(SceneField.KernelProgram("gi_reference.slang", ShaderTarget.SpirV, defines: new Dictionary<string, ShaderDefine> { ["MASKS"] = (uint)_rays.Masks }));
        }
        return null;
    }

    // Binds the material buffer's masks, the cut-out materials' textures, as the frame before's
    // slots gave them, a white texel for each it has none of.
    private static void BindMasks(Rays rays)
    {
        for (var i = 0; i < rays.Masks; i++)
        {
            var mask = Shaders.MaterialBufferMask(rays.Materials, i);
            rays.Trace.SetTexture($"mask{i}", mask.IsValid ? mask : rays.White);
        }
        rays.Trace.SetSampler("mask_sampler", SamplerSettings.Linear);
    }

    // The most masks the tracer binds, as many as a material buffer keeps, and the sampled
    // textures a stage it leaves for its others, the environment map and the air's tables.
    private const int Masks = 32, Others = 8;

    /// <summary>
    /// How many masks the tracer binds on a device whose shaders may sample
    /// <paramref name="limit"/> textures a stage, 32, or 16 or 8 where those and its other
    /// textures fit and 32 do not.
    /// </summary>
    /// <remarks>
    /// wgpu's lowest default is 16 textures a stage, and the adapters the workflow runs on report
    /// 128, so the masks are bound in steps a smaller device can make the tracer with, and the
    /// reference says when it holds fewer.
    /// </remarks>
    internal static int MasksFor(int limit) => limit >= Masks + Others ? Masks : limit >= 16 + Others ? 16 : 8;

    // The camera the field lies around, or the first 3D camera where there is none.
    private static Entity CameraOf(EcsWorld ecs)
    {
        var camera = SceneField.ViewCamera;
        if (!camera.IsNone && ecs.IsAlive(camera)) return camera;

        // Where the field lies around no camera, the camera the bridge takes for the view, the
        // active 3D one of the highest order, as the field takes it. The world's first 3D camera
        // was taken before, which in the feature test's yard was one drawing nothing, so a read
        // with global illumination off asked for the picture's size until it gave up.
        NativeSceneCounts counts = default;
        if (Native.bcs_render_scene_counts(&counts) != NativeStatus.Ok || counts.Camera == 0) return Entity.None;
        var view = new Entity(counts.Camera);
        return ecs.IsAlive(view) && ecs.Get<Camera3dRef>(view) is not null ? view : Entity.None;
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
    /// instance and material buffers, a mesh of a visibility range where the camera's distance
    /// gives it (<see cref="InRange"/>), making them larger where they hold too few, and answers
    /// each slot's name and how many entities were left out, their meshes not triangles or not
    /// loaded.
    /// </summary>
    private static (string[] Slots, int Skipped) Fill(EcsWorld ecs, Rays rays)
    {
        var placed = new List<(Entity Entity, int Mesh)>();
        var skipped = 0;
        var camera = CameraOf(ecs);
        Vec3? eye = !camera.IsNone && ecs.TryGet<GlobalTransform>(camera, out var view) ? view.Translation : null;
        foreach (var drawn in ecs.All<Mesh3dRef>())
        {
            var entity = drawn.Entity;
            if (ecs.Get<InheritedVisibilityRef>(entity) is { } seen && !seen.Value) continue;
            if (eye is { } at && !InRange(ecs, entity, at)) continue;
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

    /// <summary>
    /// Whether the eye at <paramref name="eye"/> draws a mesh of a visibility range at its
    /// distance, from the middle of the margin it fades in over up to the middle of the one it
    /// fades out over, or any mesh of none.
    /// </summary>
    /// <remarks>
    /// Bevy measures the distance to the entity's place, or to the middle of its box where the
    /// range asks, and over a margin dithers the level fading out and the one fading in each over a
    /// share of the pixels, so the nearer half of each margin is the nearer level's and the farther
    /// half the farther one's, and one level of a tree is traced at any distance. The reference
    /// held every shown mesh, so a tree was traced at every level at once, its leaves, its reduced
    /// leaves and its far cards over each other, and in the feature test's meadow the cards, their
    /// textures past the material buffer's masks, stood as planes over the crowns and cast a dark
    /// shadow over the grass, the camera inside an oak's crown meeting a card and nothing else.
    /// </remarks>
    private static bool InRange(EcsWorld ecs, Entity entity, Vec3 eye)
    {
        if (ecs.Get<VisibilityRangeRef>(entity) is not { } range) return true;
        if (!ecs.TryGet<GlobalTransform>(entity, out var global)) return true;
        var place = global.Translation;
        if (range.UseAabb && ecs.Get<AabbRef>(entity) is { } box)
            place += global.XAxis * box.Center.X + global.YAxis * box.Center.Y + global.ZAxis * box.Center.Z;
        var distance = (place - eye).Length;
        var (start, end) = (range.StartMargin, range.EndMargin);
        return distance >= (start.Start + start.End) / 2f && distance < (end.Start + end.End) / 2f;
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
            if (job.Frames >= 3 && rays.Copy.Program.State == ShaderProgramState.Ready)
            {
                job.SizeRead = Shaders.BeginBufferRead(job.Size);
                job.SizesAsked++;
            }
            return null;
        }

        if (!Shaders.TryReadBuffer<uint>(job.SizeRead.Value, out var size) || size is null) return null;
        if (size[0] == 0 || size[1] == 0)
        {
            job.SizesNone++;
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
                .SetBuffer("lights", rays.Lights).SetBuffer("ways", rays.Ways).SetTexture("sums", job.Sums).SetTexture("regions", job.Regions);
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
        ReadOnlySpan<uint> seed = [(uint)job.Seed, job.Leaves ? 1u : 0u, 0, 0];
        BindMasks(rays);
        rays.Trace.SetNumbers("counts", 4, counts);
        rays.Trace.SetNumbers("seed", 4, seed);
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
        var leaves = job.Leaves ? new float[pixels] : null;
        var samples = job.Leaves ? job.Samples : int.MaxValue;
        for (var i = 0; i < pixels; i++)
        {
            var sum = texels[i];
            var count = Math.Max(sum.W, 1f);
            (light[i * 3], light[i * 3 + 1], light[i * 3 + 2]) = (sum.X / count, sum.Y / count, sum.Z / count);
            if (leaves is not null) leaves[i] = sum.W / job.Samples;
            else samples = Math.Min(samples, (int)sum.W);
        }
        var shown = MemoryMarshal.Cast<byte, uint>(job.RegionsBytes)[..pixels].ToArray();
        var names = new Dictionary<uint, string>();
        foreach (var region in shown.Where(r => r != 0).Distinct())
        {
            var slot = (int)((region - 1) / 8);
            names[region] = $"{(slot < job.Slots.Length ? job.Slots[slot] : $"mesh {slot}")} {Axes[(region - 1) % 8]}";
        }

        var traced = new Traced(job.Width, job.Height, light, shown, samples, names, leaves);
        Last = traced;
        var seconds = Stopwatch.GetElapsedTime(job.Started).TotalSeconds;
        var skipped = job.Skipped == 0 ? "" : $", {job.Skipped} meshes left out as not triangles or not loaded";
        var past = 0;
        var fewer = "";
        if (_rays is { } held)
        {
            past = Shaders.MaterialBufferUnmasked(held.Materials);
            for (var i = held.Masks; i < Masks; i++)
                if (Shaders.MaterialBufferMask(held.Materials, i).IsValid) past++;
            if (held.Masks < Masks) fewer = $", {held.Masks} masks bound of {Masks}, the device sampling {held.Limit} textures a stage";
        }
        var unmasked = past == 0 ? "" : $", {past} cut-out textures past the masks traced by their base color alone";
        var undressed = job.Undressed == 0 ? "" : $", {job.Undressed} trees not dressed yet";
        var said = $"traced {job.Width} by {job.Height} at {samples} paths a pixel in {seconds:0.0} s, {job.Slots.Length} meshes{skipped}{unmasked}{fewer}{undressed}, {names.Count} regions";
        if (job.Path is { } path)
        {
            var full = System.IO.Path.GetFullPath(path);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, Png(light, job.Width, job.Height));
            WritePfm(full + ".pfm", light, job.Width, job.Height);
            File.WriteAllBytes(full + ".regions", MemoryMarshal.AsBytes(shown.AsSpan()).ToArray());
            File.WriteAllLines(full + ".regions.txt", names.OrderBy(n => n.Key).Select(n => $"{n.Key}\t{n.Value}"));
            if (leaves is not null) File.WriteAllBytes(full + ".leaves", MemoryMarshal.AsBytes(leaves.AsSpan()).ToArray());
            said += leaves is null ? $", into {full} with its .pfm, .regions and .regions.txt" : $", leaf by leaf, into {full} with its .pfm, .regions, .regions.txt and .leaves";
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
    /// darker. Against a reference traced leaf by leaf it reads only the pixels whose paths met a
    /// leaf first at <see cref="WholeLeaf"/> or more.
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

        // A reference traced leaf by leaf holds its leaves' light alone, so only the pixels its paths
        // found all leaf are read against it.
        var leaves = File.Exists(full + ".leaves") ? MemoryMarshal.Cast<byte, float>(File.ReadAllBytes(full + ".leaves")).ToArray() : null;
        bool Counted(int i) => leaves is null || (i < leaves.Length && leaves[i] >= WholeLeaf);

        var each = new Dictionary<uint, (int Pixels, Vector3 Picture, Vector3 Reference)>();
        for (var i = 0; i < width * height; i++)
        {
            if (!Counted(i)) continue;
            var region = i < regions.Length ? regions[i] : 0u;
            var (pixels, seen, traced) = each.GetValueOrDefault(region);
            each[region] = (pixels + 1, seen + At(light, i), traced + At(reference, i));
        }

        var text = new StringBuilder();
        var (seenLight, tracedLight) = leaves is null ? (light, reference) : (Only(light, Counted), Only(reference, Counted));
        text.Append(CultureInfo.InvariantCulture, $"the picture lies {Distance(seenLight, tracedLight):0.000} of its light from the reference, {width} by {height}");
        if (leaves is not null) text.Append(CultureInfo.InvariantCulture, $", leaf by leaf over the pixels whose paths met a leaf first at {WholeLeaf:0.##} or more");
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

        // A light with every pixel the test leaves out made dark.
        static float[] Only(float[] light, Func<int, bool> counted)
        {
            var kept = (float[])light.Clone();
            for (var i = 0; i < kept.Length / 3; i++)
                if (!counted(i)) (kept[i * 3], kept[i * 3 + 1], kept[i * 3 + 2]) = (0f, 0f, 0f);
            return kept;
        }
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
