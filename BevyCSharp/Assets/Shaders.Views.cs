namespace Bevy;

using System.Runtime.InteropServices;
using Bevy.Interop;

public static unsafe partial class Shaders
{
    /// <summary>
    /// Replaces the full-screen passes a camera runs over what it drew, in the order given. Only
    /// valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pass is a program's pass stage run once for every pixel of the camera's picture, reading
    /// the picture so far and writing the next one: a color grade, an outline, a distortion, a
    /// scan line, anything a picture can be turned into. Passes before tonemapping see the linear
    /// picture, which can be brighter than white, and those after it see what the screen will show.
    /// </para>
    /// <para>
    /// <c>import bcs_pass;</c> gives a pass the picture, its depth and normals, the view and the
    /// time, which the bridge binds itself. Everything else the pass declares is its own, set by
    /// name on the <see cref="ShaderInstance"/>, and a value set later reaches the next frame.
    /// </para>
    /// <para>
    /// A pass whose program is still compiling is skipped, so the picture goes on as though it were
    /// not there until it arrives.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="passes">The passes, or none to take them all off.</param>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, an instance does not exist, or there is no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var scanlines = Shaders.CreateInstance(Shaders.CreateProgram(
    ///     new ShaderProgramSettings { Pass = "shaders/scanlines.slang" }));
    /// scanlines.Set("strength", 0.3f);
    /// Shaders.SetPasses(camera, new ShaderPass(scanlines, AfterTonemapping: true));
    /// </code>
    /// </example>
    public static void SetPasses(Entity camera, params ShaderPass[] passes)
    {
        ArgumentNullException.ThrowIfNull(passes);

        var ids = new int[passes.Length];
        var after = new int[passes.Length];

        for (var i = 0; i < passes.Length; i++)
        {
            if (!passes[i].Instance.IsValid)
            {
                throw new ArgumentException(
                    $"Pass {i} has no instance. Make one with Shaders.CreateInstance.",
                    nameof(passes));
            }

            ids[i] = passes[i].Instance.Id;
            after[i] = passes[i].Place;
        }

        fixed (int* idsAt = ids)
        fixed (int* afterAt = after)
        {
            Native.Check(
                Native.bcs_render_set_shader_passes(camera.Bits, idsAt, afterAt, ids.Length),
                "setting a camera's shader passes");
        }
    }

    /// <summary>
    /// Asks a camera to draw its depth, its normals, its motion vectors or any of them before the
    /// scene, for its passes and its compute shaders to read. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pass reads them with <c>bcs_pass::depth_at</c>, <c>normal_at</c> and <c>motion_at</c>, and
    /// a compute shader on the camera with <c>load_depth</c>, <c>load_normal</c> and
    /// <c>load_motion</c>. An outline, a fog or ambient occlusion is built from depth and normals,
    /// and anything reusing the previous frame needs motion to find where a surface was. A camera
    /// that draws none of them binds depth zero, which is the far plane, white normals and no
    /// motion, so a shader reading them runs either way.
    /// </para>
    /// <para>
    /// A prepass draws the scene a second time, so it is worth asking for only when something reads
    /// it. A multisampled camera draws it multisampled, which a pass cannot bind, so a camera read
    /// this way needs <see cref="PostSettings.Msaa"/> of one, and gets the stand-ins otherwise.
    /// </para>
    /// <para>
    /// Where Bevy's materials are drawn deferred, because some camera asked for the G-buffer or
    /// because ray-traced lighting is running, any prepass brings the G-buffer with it. Bevy draws
    /// a deferred material into the G-buffer of every camera with a prepass, and a camera with no
    /// G-buffer to draw into stops the app.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="depth">Whether to draw depth.</param>
    /// <param name="normals">Whether to draw normals.</param>
    /// <param name="motion">
    /// Whether to draw motion vectors, which temporal antialiasing and motion blur also ask for on
    /// their own.
    /// </param>
    /// <param name="deferred">
    /// Whether to draw Bevy's G-buffer, the base color, roughness, metallic and normal of every
    /// pixel, which a shader reads as <c>gbuffer</c> and unpacks with <c>bcs_pass::surface_of</c>.
    /// It turns on <see cref="Render.SetDeferredRendering"/>, which stays on after the camera stops
    /// asking, since other cameras may read it, and brings depth with it. Bevy's own materials are
    /// in it, and a Slang program's where it has a deferred stage
    /// (<see cref="ShaderProgramSettings.Deferred"/>) or draws into it at
    /// <see cref="FramePoint.InPrepass"/>; any other Slang material is drawn forward and leaves its
    /// pixels empty.
    /// </param>
    /// <param name="previous">
    /// Whether to keep the previous frame's depth and G-buffer as well, which a shader reads as
    /// <c>depth_previous</c> and <c>gbuffer_previous</c>. Comparing where a surface is now with
    /// what was at the same place last frame is how a temporal technique tells a pixel it can
    /// reuse from one that was hidden until now. Each costs a second texture of its kind.
    /// </param>
    /// <param name="pyramid">
    /// Whether to build Bevy's hierarchical depth, which a shader reads as <c>depth_pyramid</c>,
    /// every level at once. Each texel holds the farthest depth of those under it, with the first
    /// level the depth rounded down to a power of two, so a box whose nearest depth is farther
    /// than the texels it covers is hidden, which is the test a GPU culling instances or clusters
    /// makes. It turns Bevy's own occlusion culling on for the camera, which builds the pyramid,
    /// and brings depth. A screen-space trace needs the nearest depth instead, which a camera image
    /// with mip levels built a level at a time gives.
    /// </param>
    public static void SetPrepass(
        Entity camera,
        bool depth,
        bool normals = false,
        bool motion = false,
        bool deferred = false,
        bool previous = false,
        bool pyramid = false) =>
        Native.Check(
            Native.bcs_render_set_prepass(
                camera.Bits,
                (depth ? 1u : 0u) | (normals ? 2u : 0u) | (motion ? 4u : 0u) | (deferred ? 8u : 0u)
                    | (previous ? 16u : 0u) | (pyramid ? 32u : 0u)),
            "asking a camera for a prepass");

    /// <summary>
    /// Gives a camera images that its passes and compute shaders keep from frame to frame,
    /// replacing any it had. None takes them all away. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The engine makes each image at a fraction of the camera's picture, makes it again when the
    /// picture changes size (which starts it over from zeros), and binds it wherever a shader
    /// running on this camera declares its name, as a <c>Texture2D</c> to read or an
    /// <c>RWTexture2D</c> to write. A compute shader on the camera can write one that a pass later
    /// in the same frame reads, which is how ambient occlusion computed at half size is put onto the
    /// picture.
    /// </para>
    /// <para>
    /// One marked as history is two images that trade places every frame, so a shader reads last
    /// frame's under the name with <c>_previous</c> after it while writing this frame's under the
    /// name itself. One with more than one mip level is reachable a level at a time as
    /// <c>name_mip0</c>, <c>name_mip1</c> and so on, which is how a depth pyramid is built one level
    /// from the last, reading one level while writing the next. Every camera has its own, however many cameras there are.
    /// </para>
    /// <para>
    /// A shader must not read and write the same image in one dispatch or pass, which the GPU
    /// refuses, and history exists to avoid that.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// Shaders.SetViewImages(camera,
    ///     new ViewImage("occlusion", ShaderImageFormat.R16Float, Scale: 0.5f),
    ///     new ViewImage("accumulated", ShaderImageFormat.Rgba16Float, History: true));
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">A name is empty or repeated, or a scale is not positive.</exception>
    public static void SetViewImages(Entity camera, params ViewImage[] images)
    {
        ArgumentNullException.ThrowIfNull(images);

        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (var image in images)
        {
            ArgumentException.ThrowIfNullOrEmpty(image.Name, nameof(images));

            if (!names.Add(image.Name))
            {
                throw new ArgumentException($"A camera owns one image called {image.Name}.", nameof(images));
            }

            if (!(image.Scale > 0f) || image.Scale > 16f)
            {
                throw new ArgumentException(
                    $"{image.Name} is {image.Scale} of the picture, and a scale is above zero and at most sixteen.",
                    nameof(images));
            }
        }

        var native = new NativeViewImage[images.Length];
        var strings = new List<IntPtr>();

        try
        {
            for (var i = 0; i < images.Length; i++)
            {
                var name = Marshal.StringToCoTaskMemUTF8(images[i].Name);
                strings.Add(name);

                native[i] = new NativeViewImage
                {
                    Name = (byte*)name,
                    Format = (int)images[i].Format,
                    Scale = images[i].Scale,
                    History = images[i].History ? 1 : 0,
                    Mips = Math.Max(1, images[i].Mips),
                    Copy = images[i].CopyAt is { } point ? (int)point : -1,
                    Clear = images[i].ClearEachFrame ? 1 : 0,
                };
            }

            fixed (NativeViewImage* first = native)
            {
                Native.Check(
                    Native.bcs_render_set_view_images(camera.Bits, first, native.Length),
                    "giving a camera its images");
            }

            // Kept here for whatever lists them, which the render world cannot be asked.
            var listed = images.SelectMany(image =>
                    new[] { image.Name }
                        .Concat(image.History ? [image.Name + "_previous"] : [])
                        .Concat(image.Mips > 1 ? Enumerable.Range(0, image.Mips).Select(mip => $"{image.Name}_mip{mip}") : []))
                .ToArray();

            lock (_viewImages)
            {
                if (listed.Length == 0) _viewImages.Remove(camera.Bits);
                else _viewImages[camera.Bits] = listed;
            }
        }
        finally
        {
            foreach (var pointer in strings) Marshal.FreeCoTaskMem(pointer);
        }
    }

    /// <summary>The names every 3D camera's shaders can read besides the images the camera owns.</summary>
    /// <remarks>
    /// The prepass's depth, normals and motion, where the camera draws them, Bevy's ambient
    /// occlusion, where it is on, and Bevy's G-buffer, where the camera draws deferred. The G-buffer
    /// is packed bits, which a shader declares as <c>Texture2D&lt;uint4&gt; gbuffer</c> and unpacks
    /// with <c>bcs_pass::surface_of</c>. Last frame's depth and G-buffer are there as
    /// <c>depth_previous</c> and <c>gbuffer_previous</c> where the camera keeps them, and Bevy's
    /// hierarchical depth as <c>depth_pyramid</c> where the camera builds it. A watch reads the
    /// prepass's by these names too. Which of them a camera has is
    /// <see cref="DrawnViewImageNames"/>.
    /// </remarks>
    public static IReadOnlyList<string> EngineViewImageNames { get; } =
        ["depth", "normals", "motion", "ambient_occlusion", "gbuffer", "depth_previous", "gbuffer_previous", "depth_pyramid"];

    /// <summary>
    /// The names a <see cref="Watch"/> on the camera would find, as of the last frame it drew: its
    /// own images and those of <see cref="EngineViewImageNames"/> it has.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Whether a camera has an image depends on settings made in several places (a prepass, Bevy's
    /// ambient occlusion, deferred rendering, the images it was given), and on whether the frame
    /// drew them at all, since a prepass drawn several samples a pixel cannot be shown. So this is
    /// asked of the renderer rather than worked out from the settings, and a name listed here is
    /// one a watch shows.
    /// </para>
    /// <para>
    /// A frame behind, since the renderer answers once it has drawn, so a setting just changed
    /// shows here on the frame after next. Empty before the camera's first frame, and in a run
    /// with no renderer.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<string> DrawnViewImageNames(Entity camera)
    {
        if (!App.HasRenderer) return [];

        var text = Native.ReadText(
            (buffer, capacity) => Native.bcs_render_drawn_view_image_names(camera.Bits, buffer, capacity),
            "reading the images a camera drew");

        return text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// The names of the images a camera owns, as <see cref="SetViewImages"/> last gave them, with
    /// their <c>_previous</c> and <c>_mip</c> names. Empty for a camera that owns none.
    /// </summary>
    /// <remarks>What an inspector or a debug view offers to <see cref="Watch"/>.</remarks>
    public static IReadOnlyList<string> ViewImageNames(Entity camera)
    {
        lock (_viewImages)
        {
            return _viewImages.TryGetValue(camera.Bits, out var names) ? names : [];
        }
    }

    private static readonly Dictionary<ulong, string[]> _viewImages = [];

    /// <summary>
    /// Starts watching one of a camera's images. Every frame, once the camera's frame is done, it
    /// is drawn into an eight-bit image, each value times <paramref name="scale"/> plus
    /// <paramref name="offset"/>. Answers that image. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A screen-space technique is a chain of images nobody looks at, and when its result is wrong
    /// the question is which link broke. The images a camera owns live on the GPU in formats a
    /// picture cannot show, so a watch draws one into an image anything can show: the editor's
    /// Frame tab, a material, a UI node. One channel shows as gray, two as red and green, and more
    /// as color.
    /// </para>
    /// <para>
    /// Anything a shader on the camera reads by name can be watched, and the prepass's
    /// <c>depth</c>, <c>normals</c> and <c>motion</c>. Watching the same name again replaces the
    /// watch, with a new image. A watch costs a small draw a frame until <see cref="Unwatch"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Occlusion runs from zero to one, which is already the range an image shows.
    /// var picture = Shaders.Watch(camera, "occlusion", 320, 180);
    ///
    /// // Distances of a few hundred units, brought into range.
    /// var distances = Shaders.Watch(camera, "hit_distance", 320, 180, scale: 1f / 200f);
    /// </code>
    /// </example>
    public static AssetHandle Watch(
        Entity camera,
        string name,
        uint width = 320,
        uint height = 180,
        float scale = 1f,
        float offset = 0f)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);

        fixed (byte* named = ShaderValues.Utf8(name))
        {
            return new AssetHandle(Native.Check(
                Native.bcs_render_watch_view_image(camera.Bits, named, width, height, scale, offset),
                $"watching {name} on {camera}"));
        }
    }

    /// <summary>Stops watching one of a camera's images. Only valid inside a system.</summary>
    public static void Unwatch(Entity camera, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        fixed (byte* named = ShaderValues.Utf8(name))
        {
            Native.Check(Native.bcs_render_unwatch_view_image(camera.Bits, named), $"unwatching {name} on {camera}");
        }
    }

    /// <summary>
    /// Replaces the compute shaders a camera runs every frame, in order. None takes them all away.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A dispatch on a camera runs at a <see cref="FramePoint"/> of that camera's frame, with the
    /// camera's inputs (the picture, time, the view and the previous frame's, depth, normals and
    /// motion) through <c>import bcs_pass;</c>, and the camera's images under their names. An
    /// ambient occlusion, a screen-space GI or a temporal filter runs as a chain of dispatches and
    /// passes over one camera's frame, each reading what the last wrote.
    /// </para>
    /// <para>
    /// Each takes its instance's values as they are every frame, so a value set on the instance
    /// reaches the next frame. A dispatch whose program is still compiling is left out of the frame
    /// until it is ready.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var occlusion = Shaders.CreateInstance(Shaders.CreateProgram(
    ///     new ShaderProgramSettings { Compute = "shaders/occlusion.slang" }));
    ///
    /// Shaders.SetPrepass(camera, depth: true, normals: true);
    /// Shaders.SetViewImages(camera, new ViewImage("occlusion", ShaderImageFormat.R16Float, Scale: 0.5f));
    /// Shaders.SetViewDispatches(camera, ViewDispatch.PerPixel(occlusion, FramePoint.AfterPrepass, scale: 0.5f));
    /// </code>
    /// </example>
    /// <exception cref="ArgumentException">A dispatch has no instance.</exception>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, an instance or a buffer does not exist, or there is no renderer.
    /// </exception>
    public static void SetViewDispatches(Entity camera, params ViewDispatch[] dispatches)
    {
        ArgumentNullException.ThrowIfNull(dispatches);

        var native = new NativeViewDispatch[dispatches.Length];

        for (var i = 0; i < dispatches.Length; i++)
        {
            var dispatch = dispatches[i];

            if (!dispatch.Instance.IsValid)
            {
                throw new ArgumentException(
                    $"Dispatch {i} has no instance. Make one with Shaders.CreateInstance.",
                    nameof(dispatches));
            }

            ref var entry = ref native[i];
            entry.Instance = dispatch.Instance.Id;
            entry.Point = (int)dispatch.Point;
            entry.Mode = (int)dispatch.Mode;
            entry.Groups[0] = dispatch.X;
            entry.Groups[1] = dispatch.Y;
            entry.Groups[2] = dispatch.Z;
            entry.Scale = dispatch.Scale;
            entry.Buffer = dispatch.Buffer.Key;
            entry.Offset = dispatch.Offset;
        }

        fixed (NativeViewDispatch* first = native)
        {
            Native.Check(
                Native.bcs_render_set_view_dispatches(camera.Bits, first, native.Length),
                "setting the compute shaders a camera runs");
        }
    }

    /// <summary>
    /// Runs an instance's compute shader once, this frame, before any camera draws. Only valid
    /// inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A simulation dispatches every frame from a system, which leaves how often it steps to the
    /// game. Dispatches run in the order they were asked for, each seeing what the one before
    /// wrote, and all of them before the frame is drawn, so a material reading a buffer draws what
    /// the frame's dispatches left in it.
    /// </para>
    /// <para>
    /// The dispatch takes the instance's values as they are now, so the same instance dispatched
    /// twice in a frame with a value changed between runs twice with different values. A dispatch
    /// of a program still compiling does nothing that frame, which
    /// <see cref="ShaderProgram.State"/> says in advance.
    /// </para>
    /// </remarks>
    /// <param name="instance">An instance of a program with a compute stage.</param>
    /// <param name="x">
    /// Workgroups along the first axis. A workgroup is as many invocations as the shader's
    /// <c>numthreads</c> says, so a thousand elements at sixty-four a workgroup is sixteen, and the
    /// last workgroup checks that its elements exist.
    /// </param>
    /// <param name="y">Workgroups along the second axis.</param>
    /// <param name="z">Workgroups along the third axis.</param>
    /// <example>
    /// <code>
    /// // Once.
    /// var step = Shaders.CreateInstance(Shaders.CreateProgram(
    ///     new ShaderProgramSettings { Compute = "shaders/boids.slang" }));
    /// step.SetBuffer("boids", Shaders.CreateBuffer&lt;Boid&gt;(boids));
    ///
    /// // Every frame, 64 boids to a workgroup.
    /// step.Set("delta", (float)ctx.Time.DeltaSeconds);
    /// Shaders.Dispatch(step, (uint)(boids.Length + 63) / 64);
    /// </code>
    /// </example>
    public static void Dispatch(ShaderInstance instance, uint x, uint y = 1, uint z = 1)
    {
        if (!instance.IsValid)
        {
            throw new ArgumentException(
                "No instance was given. Make one with Shaders.CreateInstance.",
                nameof(instance));
        }

        Native.Check(
            Native.bcs_shader_dispatch(instance.Id, x, y, z),
            $"dispatching shader instance {instance.Id}");
    }

    /// <summary>
    /// Replaces the geometry a camera draws every frame out of buffers, in order. None takes it all
    /// away. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A draw on a camera is a program with a <see cref="ShaderProgramSettings.DrawVertex"/> and a
    /// <see cref="ShaderProgramSettings.DrawFragment"/> stage, drawn into the camera's picture and
    /// tested against its depth at a <see cref="FramePoint"/>. Its vertex shader is handed no
    /// vertices, only their numbers, and places what is drawn from the buffers it declares.
    /// Something whose shape lives on the GPU is drawn this way, such as particles a compute shader
    /// moves, clusters a culling pass chose, any number of instances whose count a buffer holds.
    /// </para>
    /// <para>
    /// Its count is fixed, or read from a buffer when it runs (<see cref="ViewDraw.Indirect"/>), so
    /// a compute shader earlier at the same point can decide it. Draws at a point run after that
    /// point's dispatches, and before the passes on the same side of tonemapping. The draw writes
    /// into the picture, so it is not readable while drawing, and its binding holds a stand-in.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var sparks = Shaders.CreateInstance(Shaders.CreateProgram(new ShaderProgramSettings
    /// {
    ///     DrawVertex = new ShaderStage("shaders/sparks.slang", "vertex"),
    ///     DrawFragment = new ShaderStage("shaders/sparks.slang", "fragment"),
    /// })).SetBuffer("sparks", buffer);
    ///
    /// Shaders.SetViewDraws(camera, ViewDraw.Fixed(sparks, FramePoint.AfterOpaque, 6, count, DrawBlend.Add));
    /// </code>
    /// </example>
    public static void SetViewDraws(Entity camera, params ViewDraw[] draws)
    {
        ArgumentNullException.ThrowIfNull(draws);

        var native = new NativeViewDraw[draws.Length];
        var strings = new List<IntPtr>();

        try
        {
            for (var i = 0; i < draws.Length; i++)
            {
                var draw = draws[i];

                if (!draw.Instance.IsValid)
                {
                    throw new ArgumentException(
                        $"Draw {i} has no instance. Make one with Shaders.CreateInstance.",
                        nameof(draws));
                }

                var target = IntPtr.Zero;

                if (draw.Into is not null && draw.Targets is not null)
                {
                    throw new ArgumentException(
                        $"Draw {i} names both Into and Targets. Into is one image, Targets several.",
                        nameof(draws));
                }

                // One name to a line, which is how the bridge reads several.
                var names = draw.Targets is { } several ? string.Join('\n', several) : draw.Into;

                if (names is not null)
                {
                    target = Marshal.StringToCoTaskMemUTF8(names);
                    strings.Add(target);
                }

                native[i] = new NativeViewDraw
                {
                    Instance = draw.Instance.Id,
                    Point = (int)draw.Point,
                    Mode = draw.FromBuffer ? 1 : draw.Groups != default ? 2 : 0,
                    Vertices = draw.Vertices,
                    Instances = draw.Instances,
                    Buffer = draw.Buffer.Key,
                    Offset = draw.Offset,
                    Blend = (int)draw.Blend,
                    DepthWrite = (draw.WritesDepth ? 1 : 0) | (draw.CastsShadows ? 2 : 0),
                    Target = (byte*)target,
                    GroupsX = draw.Groups.X,
                    GroupsY = draw.Groups.Y,
                    GroupsZ = draw.Groups.Z,
                };
            }

            fixed (NativeViewDraw* first = native)
            {
                Native.Check(
                    Native.bcs_render_set_view_draws(camera.Bits, first, native.Length),
                    "setting what a camera draws out of buffers");
            }
        }
        finally
        {
            foreach (var pointer in strings) Marshal.FreeCoTaskMem(pointer);
        }
    }

    /// <summary>
    /// Runs an instance's compute shader once, this frame, before any camera draws, with as many
    /// workgroups as the buffer says. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The three unsigned integers at <paramref name="offset"/> bytes into <paramref name="buffer"/>
    /// are the workgroup counts, read on the GPU when the dispatch runs. A dispatch before it can
    /// write them, so one compute shader decides how much work the next does (compacting the pixels
    /// or clusters that need it, say) without the count ever crossing back to the CPU.
    /// </remarks>
    /// <exception cref="ArgumentException">The offset is not a multiple of four.</exception>
    public static void DispatchIndirect(ShaderInstance instance, AssetHandle buffer, uint offset = 0)
    {
        if (!instance.IsValid)
        {
            throw new ArgumentException(
                "No instance was given. Make one with Shaders.CreateInstance.",
                nameof(instance));
        }

        if (offset % 4 != 0)
        {
            throw new ArgumentException("The counts start on a multiple of four bytes.", nameof(offset));
        }

        Native.Check(
            Native.bcs_shader_dispatch_indirect(instance.Id, buffer.Key, offset),
            $"dispatching shader instance {instance.Id} from a buffer");
    }
}
