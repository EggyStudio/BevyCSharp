using System.Numerics;
using System.Runtime.InteropServices;
using Bevy.Interop;

namespace Bevy;

/// <summary>
/// The scene's distance field, built on the GPU each frame from the meshes drawn that cast shadows,
/// around the camera the view is drawn by, as <see cref="SceneFieldPlan"/> works out, through the
/// bridge's compute shaders (<see cref="Render.SetSceneField"/>).
/// </summary>
/// <remarks>
/// <para>
/// Each frame, from the engine's tick in a run that draws, it reads the meshes drawn from the bridge,
/// works out the frame's builds and boxes, has the bridge pool the triangles of the meshes those
/// builds read, and asks for the dispatches, for each cascade built the splat over its meshes'
/// triangles, the paint over them and the resolve over its cells, then the stamp over the bricks
/// the boxes come within. The cascades lie one after another along z in one image of half floats,
/// the field the shaders read, beside one the still meshes alone make, which a brick goes back to
/// once a box has left it, with the color each cell's nearest surface paints it and the way that
/// surface's face looks laid out the same way, the still meshes' own beside them, and a buffer holds
/// where each cascade lies.
/// </para>
/// <para>
/// The meshes are those of last frame's transforms, since the engine's tick runs before the
/// program's systems and Bevy works the transforms out after them, so a box follows its mesh a frame
/// behind, which 3DEngine's field, built in its renderer, does not.
/// </para>
/// <para>
/// Nothing is built before the three compute programs have compiled, and the plan does not move on
/// meanwhile, so no cascade is taken as built that was not.
/// </para>
/// </remarks>
internal static unsafe partial class SceneField
{
    /// <summary>The distance a cell holds before its cascade is built, which reads as nothing near, in world units.</summary>
    internal const float Far = 10000f;

    /// <summary>How far toward a light a surface is traced for its contact shadows, in cells of the finest cascade, as 3DEngine traces it.</summary>
    internal const int ContactCells = 32;

    // What the program asked for, and the app it asked in, so a new app starts with none.
    private static App? _askedIn, _shadowsAskedIn;
    private static (int Cascades, float CellSize, int Budget, float FarCascade) _asked;
    private static FieldShadows _shadowsAsked;

    // What the running app's field is made of, made again for a new app or new settings.
    private static App? _app;
    private static Built? _built;
    private static ShaderInstance _splat, _resolve, _stamp, _shadows;
    // The camera the contact shadows are drawn on and what they were drawn with, so they are set on
    // a camera once and taken off it when they stop or the field turns to another.
    private static Entity _shadowCamera = Entity.None;
    private static (AssetHandle Field, FieldShadows Shadows, float Reach) _shadowsDrawn;
    // Whether the shadows gave their camera Bevy's high dynamic range, which they take off it again.
    private static bool _shadowsMadeHdr;
    private static NativeFieldDraw[] _draws = new NativeFieldDraw[256];
    private static readonly List<(SceneFieldPlan.Instance Instance, bool Skinned)> Drawn = [];
    private static readonly HashSet<SceneFieldPlan.Instance> Once = new(SceneFieldPlan.Comparer);
    // The frame's boxes and bricks as the stamp reads them, kept from frame to frame and made
    // larger as a frame needs, since a frame with a mesh moving writes both.
    private static float[] _shapes = [];
    private static uint[] _words = [];

    /// <summary>The field's images and buffers and its plan, for the settings they were made for.</summary>
    private sealed class Built((int Cascades, float CellSize, int Budget, float FarCell) made, SceneFieldPlan plan)
    {
        public readonly (int Cascades, float CellSize, int Budget, float FarCell) Made = made;
        public readonly SceneFieldPlan Plan = plan;
        public AssetHandle Field, Still, Albedo, StillPaint, Faces, StillFaces, Info, Cells, Behind, Paint, Instances, Shapes, Bricks, Pool = AssetHandle.None;
        public int Draws, BuiltThisFrame, BuildsDone;
    }

    /// <summary>Takes the program's settings for the running app.</summary>
    internal static void Ask(int cascades, float cellSize, int budget, float farCascade)
    {
        _askedIn = App.Running;
        _asked = (cascades, cellSize, budget, farCascade);
    }

    /// <summary>Takes where the program asks the field to darken a directional light, for the running app.</summary>
    internal static void AskShadows(FieldShadows shadows)
    {
        _shadowsAskedIn = App.Running;
        _shadowsAsked = shadows;
    }

    /// <summary>Where the running app asked the field to darken a directional light, past the shadow map by default.</summary>
    internal static FieldShadows Shadows =>
        App.Running is { } running && ReferenceEquals(_shadowsAskedIn, running) ? _shadowsAsked : FieldShadows.PastShadowMap;

    /// <summary>The settings asked for in the running app, none where it asked for none.</summary>
    internal static (int Cascades, float CellSize, int Budget, float FarCascade) Asked =>
        App.Running is { } running && ReferenceEquals(_askedIn, running) ? _asked : (0, 0.25f, 1, 0f);

    /// <summary>
    /// The field the running app's settings build, those it asked for, or where it asked for none and
    /// global illumination is on, four cascades of a quarter of a unit, which the probes are traced
    /// through, as 3DEngine turns its field on.
    /// </summary>
    private static (int Cascades, float CellSize, int Budget, float FarCascade) Wanted =>
        Asked.Cascades > 0 || WorldProbes.Asked == GlobalIllumination.Off ? Asked : (4, 0.25f, 1, 0f);

    /// <summary>The plan of the field being built, or null where none is.</summary>
    internal static SceneFieldPlan? Plan => ReferenceEquals(_app, App.Running) ? _built?.Plan : null;

    /// <summary>The image the shaders read, every cascade along z, or none where no field is built.</summary>
    internal static AssetHandle Image => ReferenceEquals(_app, App.Running) && _built is { } built ? built.Field : AssetHandle.None;

    /// <summary>The buffer holding one <c>FieldInfo</c>, where each cascade lies, or none where no field is built.</summary>
    internal static AssetHandle InfoBuffer => ReferenceEquals(_app, App.Running) && _built is { } built ? built.Info : AssetHandle.None;

    /// <summary>
    /// The color each cell's nearest surface paints it, eight bits a channel laid out as the field,
    /// alpha 1 where a surface painted the cell, or none where no field is built.
    /// </summary>
    internal static AssetHandle Albedo => ReferenceEquals(_app, App.Running) && _built is { } built ? built.Albedo : AssetHandle.None;

    /// <summary>
    /// The way each cell's nearest face looks, eight bits as <c>bcs_field</c>'s <c>faceWay</c> reads
    /// them laid out as the field, or none where no field is built.
    /// </summary>
    internal static AssetHandle Faces => ReferenceEquals(_app, App.Running) && _built is { } built ? built.Faces : AssetHandle.None;

    /// <summary>The camera the contact shadows are drawn on, or none.</summary>
    internal static Entity ShadowCamera => ReferenceEquals(_app, App.Running) ? _shadowCamera : Entity.None;

    /// <summary>How many cascades the last frame built.</summary>
    internal static int BuiltLastFrame => ReferenceEquals(_app, App.Running) ? _built?.BuiltThisFrame ?? 0 : 0;

    /// <summary>
    /// What the field is, said in one line, for the debug overlay and <c>gi.state</c>, or that it is
    /// off.
    /// </summary>
    internal static string Said()
    {
        if (Plan is not { } plan || _built is not { } built) return "Scene field off";

        var cascades = Enumerable.Range(0, plan.Cascades).Count(c => plan.BuiltOrigin(c) is not null);
        var reach = SceneFieldPlan.Resolution * plan.CellOf(plan.Cascades - 1);
        var far = plan.HasFar ? FormattableString.Invariant($" with a far one of {plan.FarCell:0.###} m cells") : "";
        return FormattableString.Invariant(
            $"Scene field {cascades} of {plan.Cascades} cascades built{far}, cells {plan.CellSize:0.###} m, {reach:0.#} m across, meshes {built.Draws}, still {plan.StillCount}, boxes {plan.Shapes.Count}");
    }

    /// <summary>
    /// The bytes of GPU memory the field holds, its two images of half floats, its two of colors and
    /// two of faces, four bytes a cell each, the words of a cascade's cells, its faces behind and its
    /// colors, and the pool of triangles.
    /// </summary>
    internal static long Bytes()
    {
        if (Plan is not { } plan) return 0;
        const long cells = SceneFieldPlan.Resolution * SceneFieldPlan.Resolution * SceneFieldPlan.Resolution;
        long pool = 0;
        Native.Check(Native.bcs_scene_field_pool_bytes(&pool), "reading the size of the scene's distance field's pool");
        return (2 * 2 + 4 * 4) * cells * plan.Cascades + 3 * cells * 4 + pool;
    }

    /// <summary>Builds the frame's cascades and stamps its boxes, once a frame from the engine's tick in a run that draws.</summary>
    internal static void Tick(World world)
    {
        // The field's images, buffers and programs are an app's, and the next app makes its own.
        if (!ReferenceEquals(App.Running, _app))
        {
            _app = App.Running;
            (_built, _splat, _resolve, _stamp, _shadows, _slice) = (null, default, default, default, default, default);
            (_shadowCamera, _shadowsDrawn, _shadowsMadeHdr, _sliceImage) = (Entity.None, default, false, AssetHandle.None);
        }

        var ecs = world.Resource<EcsWorld>();
        var asked = Wanted;
        if (asked.Cascades <= 0)
        {
            Release(ecs);
            return;
        }

        if (!_splat.IsValid)
        {
            _splat = Program("field_splat.slang");
            _resolve = Program("field_resolve.slang");
            _stamp = Program("field_stamp.slang");
            _shadows = Draw("field_shadows.slang");
            _slice = Program("field_slice.slang");
        }

        // Nothing is planned or built until the three have compiled.
        if (_splat.Program.State != ShaderProgramState.Ready || _resolve.Program.State != ShaderProgramState.Ready
            || _stamp.Program.State != ShaderProgramState.Ready)
            return;

        // The far cascade's cells from its reach, held to the camera's far plane, and kept as they were
        // in a frame with no camera to read the plane from.
        var eye = Gather(_built?.Plan.DrewTwice ?? false);
        var (counted, cell) = (Math.Clamp(asked.Cascades, 1, 8), Math.Max(1e-3f, asked.CellSize));
        var farCell = asked.FarCascade <= 0 ? 0
            : eye is { } at ? SceneFieldPlan.FarCellFor(MathF.Min(asked.FarCascade, at.Far), counted, cell)
            : _built?.Made.FarCell ?? 0;
        var wanted = (counted, cell, Math.Max(1, asked.Budget), farCell);
        if (_built is null || _built.Made != wanted)
        {
            Release(ecs);
            _built = Make(wanted);
        }

        var built = _built;
        built.Draws = Drawn.Count;
        built.BuiltThisFrame = 0;
        if (eye is not { } seen) return;

        var plan = built.Plan;
        plan.Update(seen.Position, Drawn, seen.Forward);
        WriteInfo(built, plan);

        if (plan.Builds.Count > 0) Build(built, plan);
        if (plan.Bricks.Count > 0) Stamp(built, plan);
        DrawShadows(ecs, built, seen.Camera);
    }

    /// <summary>
    /// Puts the contact shadows on the camera the field lies around, with the G-buffer they read
    /// and a picture in high dynamic range, or takes them off the camera they were on, where they
    /// stop or the field turns to another, changing the camera only when one of those changes.
    /// </summary>
    /// <remarks>
    /// The light taken back is added as less than nothing, which a picture of eight bits a channel
    /// clamps to nothing before it is added, so the camera draws in floats while the shadows are on
    /// it, Bevy's <c>Hdr</c> given it where it had none and taken away again after.
    /// </remarks>
    private static void DrawShadows(EcsWorld ecs, Built? built, Entity camera)
    {
        var shadows = built is null ? FieldShadows.Off : Shadows;
        if (_shadows.IsValid && _shadows.Program.State != ShaderProgramState.Ready) shadows = FieldShadows.Off;
        var on = shadows != FieldShadows.Off && !camera.IsNone;
        var wanted = on ? (built!.Field, shadows, ContactCells * built.Plan.CellSize) : default;
        if (on && camera == _shadowCamera && wanted == _shadowsDrawn) return;

        if (!_shadowCamera.IsNone && (!on || camera != _shadowCamera))
        {
            Shaders.SetEngineViewDraws(_shadowCamera);
            Shaders.SetEnginePrepass(_shadowCamera, 0);
            if (_shadowsMadeHdr && ecs.IsAlive(_shadowCamera)) ecs.RemoveReflected(_shadowCamera, Bevy.Reflected.HdrRef.TypePath);
            (_shadowCamera, _shadowsMadeHdr) = (Entity.None, false);
        }

        _shadowsDrawn = wanted;
        if (!on) return;

        _shadows.SetTexture("scene_field", built!.Field).SetBuffer("scene_field_info", built.Info)
            .SetSampler("scene_field_sampler", SamplerSettings.Clamped)
            .Set("shadows", new Vector4(shadows == FieldShadows.Everywhere ? 1f : 0f, wanted.Item3, 0f, 0f));

        // Depth and the G-buffer, which the light taken back is worked out from.
        Shaders.SetEnginePrepass(camera, 1u | 8u);
        if (camera != _shadowCamera && ecs.Get<Bevy.Reflected.HdrRef>(camera) is null)
        {
            ecs.Insert<Bevy.Reflected.HdrRef>(camera);
            _shadowsMadeHdr = true;
        }
        Shaders.SetEngineViewDraws(camera, ViewDraw.Fixed(_shadows, FramePoint.AfterOpaque, 3, 1, DrawBlend.Add, writesDepth: false));
        _shadowCamera = camera;
    }

    /// <summary>An instance of one of the field's compute programs, compiled from the library's own copy.</summary>
    private static ShaderInstance Program(string file) => Shaders.CreateInstance(KernelProgram(file));

    /// <summary>
    /// One of the library's compute programs, compiled from its own copy of the kernel, which the field
    /// and global illumination's probes are built and traced with.
    /// </summary>
    // Named by its file, so its GPU time reads as shader field_splat and the like.
    internal static ShaderProgram KernelProgram(string file) =>
        Shaders.CreateProgram(new ShaderProgramSettings { Compute = new ShaderStage(file) { Source = Kernel(file) } });

    /// <summary>An instance of one of the field's programs that draws on a camera, its vertex and fragment shaders in one file.</summary>
    private static ShaderInstance Draw(string file)
    {
        var source = Kernel(file);
        return Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
        {
            DrawVertex = new ShaderStage(file, "vertex") { Source = source },
            DrawFragment = new ShaderStage(file, "fragment") { Source = source },
        }));
    }

    /// <summary>One of the field's Slang files, as the library carries it.</summary>
    private static string Kernel(string file)
    {
        using var stream = typeof(SceneField).Assembly.GetManifestResourceStream("Bevy.Kernels." + file)
            ?? throw new InvalidOperationException($"The library carries no {file}, which the scene's distance field is built with.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>Makes the field's images and buffers for its settings, every cell far and unpainted and every word cleared.</summary>
    private static Built Make((int Cascades, float CellSize, int Budget, float FarCell) made)
    {
        const int size = SceneFieldPlan.Resolution;
        var built = new Built(made, new SceneFieldPlan(made.Cascades, made.CellSize, made.Budget, made.FarCell));
        var depth = (uint)(size * built.Plan.Cascades);

        var far = new Half[size * size * size * built.Plan.Cascades];
        Array.Fill(far, (Half)Far);
        built.Field = Shaders.CreateImage<Half>(size, size, ShaderImageFormat.R16Float, far, depth);
        built.Still = Shaders.CreateImage<Half>(size, size, ShaderImageFormat.R16Float, far, depth);
        built.Albedo = Shaders.CreateImage(size, size, ShaderImageFormat.Rgba8, depth);
        built.StillPaint = Shaders.CreateImage(size, size, ShaderImageFormat.Rgba8, depth);
        built.Faces = Shaders.CreateImage(size, size, ShaderImageFormat.R32UInt, depth);
        built.StillFaces = Shaders.CreateImage(size, size, ShaderImageFormat.R32UInt, depth);

        // A cell's word cleared to the band's distance in front, and its face behind to none, as the
        // resolve leaves them after each build.
        var cleared = new uint[size * size * size];
        Array.Fill(cleared, (uint)SceneFieldPlan.Band * 1024 << 10 | 1u << 8);
        built.Cells = Shaders.CreateBuffer<uint>(cleared);
        Array.Fill(cleared, uint.MaxValue);
        built.Behind = Shaders.CreateBuffer<uint>(cleared);
        Array.Clear(cleared);
        built.Paint = Shaders.CreateBuffer<uint>(cleared);

        built.Info = Shaders.CreateBuffer(InfoBytes);
        built.Instances = Shaders.CreateBuffer(InstanceBytes * 64);
        built.Shapes = Shaders.CreateBuffer(ShapeBytes * 16);
        built.Bricks = Shaders.CreateBuffer(16 * 64);

        _splat.SetBuffer("cells", built.Cells).SetBuffer("behind", built.Behind).SetBuffer("albedo", built.Paint).SetBuffer("instances", built.Instances);
        _resolve.SetBuffer("cells", built.Cells).SetBuffer("behind", built.Behind).SetBuffer("albedo", built.Paint)
            .SetTexture("still_field", built.Still).SetTexture("field", built.Field)
            .SetTexture("albedo_field", built.Albedo).SetTexture("still_paint", built.StillPaint)
            .SetTexture("face_field", built.Faces).SetTexture("still_faces", built.StillFaces);
        _stamp.SetBuffer("bricks", built.Bricks).SetBuffer("shapes", built.Shapes).SetBuffer("info", built.Info)
            .SetTexture("still_field", built.Still).SetTexture("field", built.Field)
            .SetTexture("still_paint", built.StillPaint).SetTexture("still_faces", built.StillFaces)
            .SetTexture("albedo_field", built.Albedo).SetTexture("face_field", built.Faces);
        return built;
    }

    /// <summary>Lets the field's images and buffers go, on a frame it is off or made again, and its shadows off their camera.</summary>
    private static void Release(EcsWorld ecs)
    {
        if (_built is not { } built) return;
        DrawShadows(ecs, null, Entity.None);
        foreach (var handle in (ReadOnlySpan<AssetHandle>)[built.Field, built.Still, built.Albedo, built.StillPaint, built.Faces, built.StillFaces, built.Info,
                     built.Cells, built.Behind, built.Paint, built.Instances, built.Shapes, built.Bricks])
            AssetServer.Release(handle);
        _built = null;
    }

    /// <summary>
    /// The meshes drawn this frame that cast shadows, into <see cref="Drawn"/>, each once where
    /// <paramref name="once"/> asks, and the camera the field lies around with how far its far plane
    /// lies, or null where there is none.
    /// </summary>
    private static (Vector3 Position, Vector3 Forward, Entity Camera, float Far)? Gather(bool once)
    {
        NativeFieldEye eye;
        int count;
        while (true)
        {
            fixed (NativeFieldDraw* draws = _draws)
                Native.Check(Native.bcs_scene_field_gather(draws, _draws.Length, &eye, &count), "reading the meshes the scene's distance field is built from");
            if (count <= _draws.Length) break;
            _draws = new NativeFieldDraw[Math.Max(count, _draws.Length * 2)];
        }

        Drawn.Clear();
        for (var i = 0; i < count; i++)
        {
            ref var draw = ref _draws[i];
            var world = new Matrix4x4(
                draw.Rows[0], draw.Rows[4], draw.Rows[8], 0,
                draw.Rows[1], draw.Rows[5], draw.Rows[9], 0,
                draw.Rows[2], draw.Rows[6], draw.Rows[10], 0,
                draw.Rows[3], draw.Rows[7], draw.Rows[11], 1);
            var bounds = new SceneFieldPlan.Box(new Vector3(draw.Low[0], draw.Low[1], draw.Low[2]), new Vector3(draw.High[0], draw.High[1], draw.High[2]));
            var instance = new SceneFieldPlan.Instance(draw.Mesh, draw.Generation, world, (draw.Flags & 1) != 0, bounds,
                new Vector3(draw.Color[0], draw.Color[1], draw.Color[2]), new Vector3(draw.Emission[0], draw.Emission[1], draw.Emission[2]));
            if (once && !Once.Add(instance)) continue;
            Drawn.Add((instance, (draw.Flags & 2) != 0));
        }

        Once.Clear();

        if (eye.Found == 0) return null;
        var forward = new Vector3(eye.ForwardX, eye.ForwardY, eye.ForwardZ);
        return (new Vector3(eye.X, eye.Y, eye.Z), forward.LengthSquared() > 1e-12f ? Vector3.Normalize(forward) : Vector3.Zero, new Entity(eye.Camera), eye.Far);
    }

    /// <summary>The most cascades a <c>FieldInfo</c> holds, eight counted and the far one.</summary>
    internal const int MostCascades = 9;

    /// <summary>The bytes of one <c>FieldInfo</c>, a float4, nine cascades' float4 and the eye's.</summary>
    internal const int InfoBytes = (1 + MostCascades + 1) * 16;

    // Bytes of an instance as field_splat.slang reads it, a shape as field_stamp.slang reads it.
    private const int InstanceBytes = 96, ShapeBytes = 96;

    /// <summary>Where each cascade lies as it was last built, as <c>FieldInfo</c>.</summary>
    internal static float[] InfoValues(SceneFieldPlan plan)
    {
        var values = new float[InfoBytes / 4];
        // w: the far cascade's place, the last, or 0 where there is none.
        (values[0], values[1], values[2], values[3]) = (plan.Cascades, SceneFieldPlan.Resolution, SceneFieldPlan.Band, plan.HasFar ? plan.Cascades - 1 : 0);
        for (var c = 0; c < plan.Cascades; c++)
            if (plan.BuiltOrigin(c) is { } origin)
                (values[4 + c * 4], values[5 + c * 4], values[6 + c * 4], values[7 + c * 4]) = (origin.X, origin.Y, origin.Z, plan.CellOf(c));
        const int eyeAt = 4 + MostCascades * 4;
        (values[eyeAt], values[eyeAt + 1], values[eyeAt + 2]) = (plan.Eye.X, plan.Eye.Y, plan.Eye.Z);
        return values;
    }

    private static void WriteInfo(Built built, SceneFieldPlan plan) => Shaders.WriteBuffer<float>(built.Info, InfoValues(plan));

    /// <summary>
    /// Pools the meshes the frame's builds read, writes every build's instances one build after
    /// another, and asks for each build's splat and resolve, the finest cascade first.
    /// </summary>
    private static void Build(Built built, SceneFieldPlan plan)
    {
        // Each mesh the builds read once, with where its triangles start in the pool.
        var meshes = new List<(uint Mesh, uint Generation)>();
        var index = new Dictionary<(uint, uint), int>();
        foreach (var build in plan.Builds)
            foreach (var instance in build.Instances)
                if (index.TryAdd((instance.Mesh, instance.Generation), meshes.Count)) meshes.Add((instance.Mesh, instance.Generation));

        var placed = new uint[Math.Max(1, meshes.Count) * 2];
        var asked = new uint[Math.Max(1, meshes.Count) * 2];
        for (var i = 0; i < meshes.Count; i++) (asked[i * 2], asked[i * 2 + 1]) = (meshes[i].Mesh, meshes[i].Generation);
        int pool;
        fixed (uint* ask = asked)
        fixed (uint* answer = placed)
            pool = Native.Check(Native.bcs_scene_field_pool(ask, meshes.Count, answer), "pooling the triangles the scene's distance field is built from");
        if (pool != built.Pool.Key)
        {
            built.Pool = new AssetHandle(pool);
            _splat.SetBuffer("corners", built.Pool);
        }

        // Every build's instances that have triangles, the build's first and how many, and its
        // triangles, a record each as field_splat.slang reads it.
        var records = new List<float>();
        var spans = new List<(int First, int Count, int Triangles)>();
        foreach (var build in plan.Builds)
        {
            var (first, count, triangles) = (records.Count / (InstanceBytes / 4), 0, 0);
            foreach (var instance in build.Instances)
            {
                var at = index[(instance.Mesh, instance.Generation)];
                var (start, many) = (placed[at * 2], placed[at * 2 + 1]);
                if (many == 0) continue;
                var w = instance.World;
                records.AddRange([w.M11, w.M21, w.M31, w.M41, w.M12, w.M22, w.M32, w.M42, w.M13, w.M23, w.M33, w.M43]);
                records.AddRange([BitConverter.UInt32BitsToSingle(start), BitConverter.UInt32BitsToSingle(many),
                    BitConverter.UInt32BitsToSingle(instance.DoubleSided ? 1u : 0u), BitConverter.UInt32BitsToSingle((uint)triangles)]);
                records.AddRange([instance.Color.X, instance.Color.Y, instance.Color.Z, 0, instance.Emission.X, instance.Emission.Y, instance.Emission.Z, 0]);
                triangles += (int)many;
                count++;
            }
            spans.Add((first, count, triangles));
        }

        Write<float>(built.Instances, CollectionsMarshal.AsSpan(records));

        const uint size = SceneFieldPlan.Resolution;
        for (var b = 0; b < plan.Builds.Count; b++)
        {
            var build = plan.Builds[b];
            var (first, count, triangles) = spans[b];
            if (triangles > 0)
            {
                // A workgroup a triangle, past 65,535 along x laid along y as well.
                var across = (uint)Math.Min(triangles, 65535);
                var down = (uint)((triangles + across - 1) / across);
                ReadOnlySpan<uint> counts = [size, (uint)count, (uint)triangles, SceneFieldPlan.Band];
                ReadOnlySpan<uint> groups = [across, (uint)first, 0, 0];
                _splat.Set("origin_and_cell", new Vector4(build.Origin, build.Cell)).SetNumbers("counts", 4, counts).SetNumbers("groups", 4, groups);
                Shaders.Dispatch(_splat, across, down);

                // Again over the same triangles once every cell holds its nearest, each cell painted
                // the color of the triangle whose code it kept.
                ReadOnlySpan<uint> painting = [across, (uint)first, 1, 0];
                _splat.SetNumbers("groups", 4, painting);
                Shaders.Dispatch(_splat, across, down);
            }

            _resolve.Set("cascade", new Vector4(build.Cascade, size, build.Cell, SceneFieldPlan.Band));
            Shaders.Dispatch(_resolve, size / 4, size / 4, size / 4);
        }

        built.BuiltThisFrame = plan.Builds.Count;
        built.BuildsDone += plan.Builds.Count;
    }

    /// <summary>Writes the frame's boxes and bricks and asks for the stamp over the bricks.</summary>
    private static void Stamp(Built built, SceneFieldPlan plan)
    {
        var floats = Math.Max(1, plan.Shapes.Count) * (ShapeBytes / 4);
        if (_shapes.Length < floats) _shapes = new float[floats * 2];
        var shapes = _shapes.AsSpan(0, floats);
        for (var s = 0; s < plan.Shapes.Count; s++)
        {
            var (m, center, extents, scale, color) = plan.Shapes[s];
            ReadOnlySpan<float> values = [m.M11, m.M21, m.M31, m.M41, m.M12, m.M22, m.M32, m.M42, m.M13, m.M23, m.M33, m.M43,
                center.X, center.Y, center.Z, scale, extents.X, extents.Y, extents.Z, 0, color.X, color.Y, color.Z, 0];
            values.CopyTo(shapes[(s * (ShapeBytes / 4))..]);
        }

        // Each brick, its cascade, its place a byte an axis, where its run of boxes begins and how
        // long it is, then the runs, made up to a whole element of four, since the shader reads four
        // places at a time, as 3DEngine's validation found it reading past the end.
        var (bricks, ranges, runs) = (plan.Bricks, plan.BrickRanges, plan.BrickShapes);
        var length = bricks.Count * 4 + (runs.Count + 3) / 4 * 4;
        if (_words.Length < length) _words = new uint[length * 2];
        var words = _words.AsSpan(0, length);
        words.Clear();
        for (var b = 0; b < bricks.Count; b++)
            (words[b * 4], words[b * 4 + 1], words[b * 4 + 2], words[b * 4 + 3]) =
                ((uint)bricks[b].Cascade, (uint)(bricks[b].X | bricks[b].Y << 8 | bricks[b].Z << 16), (uint)ranges[b].First, (uint)ranges[b].Count);
        for (var r = 0; r < runs.Count; r++) words[bricks.Count * 4 + r] = (uint)runs[r];

        Write<float>(built.Shapes, shapes);
        Write<uint>(built.Bricks, words);
        ReadOnlySpan<uint> counted = [(uint)bricks.Count, (uint)plan.Shapes.Count, 0, 0];
        _stamp.SetNumbers("stamp_counts", 4, counted);
        Shaders.Dispatch(_stamp, (uint)bricks.Count);
    }

    /// <summary>Writes a buffer, made larger first where the items do not fit it.</summary>
    private static void Write<T>(AssetHandle buffer, ReadOnlySpan<T> items) where T : unmanaged
    {
        var bytes = items.Length * sizeof(T);
        if (bytes > Shaders.BufferSize(buffer)) Shaders.GrowBuffer(buffer, Math.Max(bytes, Shaders.BufferSize(buffer) * 2));
        Shaders.WriteBuffer(buffer, items);
    }
}
