namespace Bevy;

using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

/// <summary>
/// Materials, full-screen passes and compute shaders the game writes in Slang, laid out however the
/// shader declares, and reloaded while the game runs.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ShaderProgram"/> names the Slang files that make it. A material, a pass or a
/// dispatch runs one, and its values are set <b>by the names the shader declares</b>. A shader
/// declares what it needs as ordinary Slang globals, of any kind and at any size. The bridge asks
/// the compiler how they were laid out and builds the bind group from that, so nothing about a
/// shader's shape is fixed in advance and the only limits are the GPU's.
/// </para>
/// <code>
/// import bcs;
///
/// uniform float4 tint;
/// uniform float weights[1000];
/// Texture2D layers[64];
/// TextureCube skies[16];
/// SamplerState linear;
///
/// [shader("fragment")]
/// float4 fragment(bcs::VertexOutput input) : SV_Target
/// {
///     return tint * layers[7].Sample(linear, input.uv) * weights[999];
/// }
/// </code>
/// <code>
/// var material = Shaders.CreateMaterial(Shaders.CreateProgram("shaders/layered.slang"))
///     .Set("tint", new Vector4(1, 0.5f, 0.2f, 1))
///     .Set("weights", weights)
///     .SetTexture("layers", rock, 7);
/// Render.SetMaterial(ctx.Ecs, entity, material);
/// </code>
/// <para>
/// <b>What a name is.</b> A global's own name, <c>tint</c>. A field of a struct or a constant
/// buffer, <c>sun.color</c>. An element of an array of structs, <c>lights[3].color</c>. A texture,
/// buffer or sampler in an array is its name and an index given alongside. A value that does not
/// fit what the name declares is refused with an <see cref="ArgumentException"/> that lists what
/// the shader does declare. Before the program has compiled nothing can be checked, so a value set
/// then is kept and bound once the layout is known, and one that does not fit is left out with a
/// warning in the log.
/// </para>
/// <para>
/// <b>Reloading.</b> An edit to a shader file, or to any file it imported, reaches what is on
/// screen within a quarter of a second, with a layout of its own if the edit changed what the
/// shader declares. Values are kept by name, so a parameter the edit added starts at zero and the
/// rest keep what they held. A file that fails to compile leaves the last version that compiled in
/// use and says why in the log and in <see cref="ShaderProgram.Diagnostics"/>. One that has never
/// compiled draws magenta.
/// </para>
/// <para>
/// <b>The compiler.</b> <c>slangc</c> compiles each stage in the background. The build fetches a
/// pinned release into <c>build/tools/slang</c>, and every compile is cached under the asset root
/// in <c>.slang-cache</c> with its layout, so a game shipped with the cache needs no compiler. See
/// <see cref="SlangAvailable"/>.
/// </para>
/// </remarks>
public static unsafe class Shaders
{
    /// <summary>Whether a Slang shader can be compiled on this machine.</summary>
    /// <remarks>
    /// <para>
    /// <c>slangc</c> is found through the <c>BCS_SLANGC</c> environment variable, which names it
    /// outright, then on the <c>PATH</c>, then in a <c>build/tools/slang</c> above the app or the
    /// working directory, which is where the build fetches it. It is looked for once per process.
    /// </para>
    /// <para>
    /// False does not mean shaders fail. One compiled on a machine that had <c>slangc</c> is read
    /// back from the cache, provided neither it nor anything it imported has changed since, which
    /// is the situation of a shipped game. What false does mean is that an edit cannot be compiled.
    /// </para>
    /// </remarks>
    public static bool SlangAvailable => Native.bcs_shader_slang_available() != 0;

    /// <summary>
    /// Whether a validation error from the renderer is survived rather than closing the app.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A shader that compiles can still disagree with the pipeline it is put in, by reading an
    /// input the vertex shader never wrote, for instance. Bevy's answer to that is to close the
    /// app, which is right for a shipped game and wrong for one whose shaders are being edited while
    /// it runs.
    /// </para>
    /// <para>
    /// Set, the error is logged and <see cref="LastRenderError"/> keeps it, and frames drawn with
    /// the broken pipeline are dropped until a fixed shader reloads. Every other kind of error
    /// still closes the app. The editor sets it; a game has to ask. Process-wide, because the
    /// renderer asks from its own thread.
    /// </para>
    /// </remarks>
    public static bool KeepRenderingAfterErrors
    {
        get => _keepRendering;
        set
        {
            _keepRendering = value;
            if (App.HasRenderer) Native.bcs_shader_keep_rendering_after_errors(value ? 1 : 0);
        }
    }

    private static bool _keepRendering;

    /// <summary>The last error the renderer reported, or an empty string.</summary>
    public static string LastRenderError => App.HasRenderer
        ? Native.ReadText(
            (buffer, capacity) => Native.bcs_shader_last_render_error(buffer, capacity),
            "reading the last render error")
        : string.Empty;

    /// <summary>How many programs the running app has made. Only valid inside a system.</summary>
    public static int ProgramCount =>
        Native.Check(Native.bcs_shader_program_count(), "counting shader programs");

    /// <summary>Every program the running app has made. Only valid inside a system.</summary>
    public static IEnumerable<ShaderProgram> Programs =>
        Enumerable.Range(0, ProgramCount).Select(index => new ShaderProgram(index));

    /// <summary>Makes a program drawn by one fragment shader, leaving the rest to Bevy.</summary>
    /// <remarks>What most materials use. See <see cref="CreateProgram(ShaderProgramSettings)"/>.</remarks>
    /// <param name="fragment">A <c>.slang</c> file under the asset root.</param>
    public static ShaderProgram CreateProgram(ShaderStage fragment) =>
        CreateProgram(new ShaderProgramSettings { Fragment = fragment });

    /// <summary>
    /// Makes a program from the Slang named. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same settings answer the same program, so a system that makes its program every frame
    /// still makes one. Each stage is compiled in the background, and what runs it appears once the
    /// compile has finished, which <see cref="ShaderProgram.State"/> reports.
    /// </para>
    /// <para>
    /// Defines reach the shader as <c>-D</c>, so <c>#ifdef</c> and <c>#if</c> read them. Different
    /// defines make a different program, compiled separately.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// There is no fragment shader, pass or compute shader, or a file is not Slang.
    /// </exception>
    /// <exception cref="BevyNativeException">There is no renderer, or no world on loan.</exception>
    /// <example>
    /// <code>
    /// var water = Shaders.CreateProgram(new ShaderProgramSettings
    /// {
    ///     Vertex = "shaders/water.slang",
    ///     Fragment = "shaders/water.slang",
    ///     PrepassVertex = new ShaderStage("shaders/water.slang", "prepass_vertex"),
    ///     Defines = { ["WAVES"] = 4, ["FOAM"] = true },
    /// });
    /// </code>
    /// </example>
    public static ShaderProgram CreateProgram(ShaderProgramSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Fragment.IsSet && !settings.Compute.IsSet && !settings.Pass.IsSet
            && !settings.DrawFragment.IsSet)
        {
            throw new ArgumentException(
                "A shader program needs a fragment shader to draw with, a pass to run over a "
                + "camera's picture, a compute shader to dispatch, or a draw fragment shader to draw "
                + "on a camera with. Bevy's own fragment shader reads a material laid out "
                + "differently, so there is no default to fall back on.",
                nameof(settings));
        }

        if (settings.DrawFragment.IsSet != settings.DrawVertex.IsSet)
        {
            throw new ArgumentException(
                "Drawing on a camera takes both a draw vertex shader, which places what is drawn "
                + "out of buffers, and a draw fragment shader, which colors it.",
                nameof(settings));
        }

        foreach (var stage in settings.Stages().Where(stage => stage.Source is not { Length: > 0 }))
        {
            if (!stage.Path!.EndsWith(".slang", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"{stage.Path} is not Slang. A stage is a .slang file under the asset root, or "
                    + "Slang source handed over with ShaderStage.Slang.",
                    nameof(settings));
            }
        }

        // Every string goes into unmanaged memory freed on the way out, because the native side
        // copies what it reads before it returns.
        var strings = new List<IntPtr>();

        byte* Utf8(string? text)
        {
            if (text is null) return null;
            var pointer = Marshal.StringToCoTaskMemUTF8(text);
            strings.Add(pointer);
            return (byte*)pointer;
        }

        NativeShaderStage Stage(ShaderStage stage) => new()
        {
            Path = stage.Path is { Length: > 0 } ? Utf8(stage.Path) : null,
            Entry = stage.Entry is { Length: > 0 } ? Utf8(stage.Entry) : null,
            Source = stage.Source is { Length: > 0 } ? Utf8(stage.Source) : null,
        };

        try
        {
            var defines = new NativeShaderDefine[settings.Defines.Count];
            var index = 0;

            foreach (var (name, define) in settings.Defines)
            {
                defines[index++] = new NativeShaderDefine
                {
                    Name = Utf8(name),
                    Kind = (int)define.Kind,
                    Value = define.RawValue,
                };
            }

            fixed (NativeShaderDefine* first = defines)
            {
                var config = new NativeShaderProgramConfig
                {
                    Vertex = Stage(settings.Vertex),
                    Fragment = Stage(settings.Fragment),
                    PrepassVertex = Stage(settings.PrepassVertex),
                    PrepassFragment = Stage(settings.PrepassFragment),
                    Compute = Stage(settings.Compute),
                    Pass = Stage(settings.Pass),
                    Defines = defines.Length > 0 ? first : null,
                    DefineCount = defines.Length,
                    DrawVertex = Stage(settings.DrawVertex),
                    DrawFragment = Stage(settings.DrawFragment),
                };

                var id = Native.bcs_shader_program_create(&config);

                if (id == NativeStatus.NullArgument)
                {
                    throw new ArgumentException(
                        "A define has an empty name or one with whitespace in it, which the "
                        + "preprocessor does not accept.",
                        nameof(settings));
                }

                Native.Check(id, $"making a shader program from {settings.Main().Describe()}");
                return new ShaderProgram(id);
            }
        }
        finally
        {
            foreach (var pointer in strings) Marshal.FreeCoTaskMem(pointer);
        }
    }

    /// <summary>Makes a material drawn by a program. Only valid inside a system.</summary>
    /// <remarks>
    /// Its values start at zero, its textures at a stand-in of the right shape, and its samplers
    /// linear and repeating, so a shader draws something whatever has been set. Set them by name on
    /// what this returns.
    /// </remarks>
    /// <param name="program">The program that draws it.</param>
    /// <param name="alpha">
    /// What the renderer does where this material is not opaque. A shader writing anything but one
    /// in its alpha channel needs <see cref="AlphaMode.Blend"/> or another of the blending modes,
    /// since an opaque material's alpha is not read at all.
    /// </param>
    /// <returns>The material, which converts to the handle <see cref="Render.SetMaterial"/> takes.</returns>
    /// <exception cref="BevyNativeException">
    /// The program does not exist, or there is no renderer.
    /// </exception>
    public static ShaderMaterial CreateMaterial(ShaderProgram program, AlphaMode alpha = AlphaMode.Opaque) =>
        CreateMaterial(new ShaderMaterialSettings { Program = program, Alpha = alpha });

    /// <summary>Makes a material drawn by a program. Only valid inside a system.</summary>
    /// <remarks>See <see cref="CreateMaterial(ShaderProgram, AlphaMode)"/>.</remarks>
    /// <exception cref="ArgumentException">No program was given.</exception>
    /// <exception cref="BevyNativeException">
    /// The program does not exist, or there is no renderer.
    /// </exception>
    public static ShaderMaterial CreateMaterial(ShaderMaterialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        RequireProgram(settings.Program, nameof(settings));

        var key = Native.bcs_shader_material_create(
            settings.Program.Id,
            (int)settings.Alpha,
            settings.AlphaCutoff,
            (int)settings.Cull,
            settings.DepthBias);

        ThrowIfNoProgram(key, settings.Program);
        Native.Check(key, $"making a material drawn by shader program {settings.Program.Id}");
        return new ShaderMaterial(new AssetHandle(key));
    }

    /// <summary>
    /// The shader material an entity is drawn with, to read or set its values. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// By the entity rather than by the material's handle, because an inspector has the entity. The
    /// material is shared by every entity drawn with it, so a value set here changes all of them.
    /// </remarks>
    public static ShaderMaterial MaterialOn(Entity entity) => ShaderMaterial.OnEntity(entity);

    /// <summary>
    /// Which program draws an entity's material, or <see cref="ShaderProgram.None"/> where the
    /// entity is not drawn by one. Only valid inside a system.
    /// </summary>
    public static ShaderProgram ProgramOn(Entity entity)
    {
        var id = Native.bcs_shader_entity_program(entity.Bits);
        if (id == NativeStatus.NoComponent || id == NativeStatus.Unsupported) return ShaderProgram.None;

        return new ShaderProgram(Native.Check(id, $"asking what draws entity {entity}"));
    }

    /// <summary>
    /// Makes an instance of a program, which a pass over a camera's picture or a compute dispatch
    /// runs. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// An instance holds its values by name the way a material does, and keeps them from frame to
    /// frame, so a pass is set up once and a dispatch that runs every frame sets only what changed.
    /// Two instances of one program are two sets of values, which is how one blur runs twice with
    /// different radii.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The program does not exist, or there is no renderer.
    /// </exception>
    public static ShaderInstance CreateInstance(ShaderProgram program)
    {
        RequireProgram(program, nameof(program));

        var id = Native.bcs_shader_instance_create(program.Id);
        ThrowIfNoProgram(id, program);
        return new ShaderInstance(Native.Check(id, $"making an instance of shader program {program.Id}"));
    }

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
    /// asking, since other cameras may read it, and brings depth with it. Only Bevy's own materials
    /// are in it, since one drawn by a Slang program is drawn forward and leaves its pixels empty.
    /// </param>
    /// <param name="previous">
    /// Whether to keep the previous frame's depth and G-buffer as well, which a shader reads as
    /// <c>depth_previous</c> and <c>gbuffer_previous</c>. Comparing where a surface is now with
    /// what was at the same place last frame is how a temporal technique tells a pixel it can
    /// reuse from one that was hidden until now. Each costs a second texture of its kind.
    /// </param>
    public static void SetPrepass(
        Entity camera,
        bool depth,
        bool normals = false,
        bool motion = false,
        bool deferred = false,
        bool previous = false) =>
        Native.Check(
            Native.bcs_render_set_prepass(
                camera.Bits,
                (depth ? 1u : 0u) | (normals ? 2u : 0u) | (motion ? 4u : 0u) | (deferred ? 8u : 0u)
                    | (previous ? 16u : 0u)),
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
    /// <c>depth_previous</c> and <c>gbuffer_previous</c> where the camera keeps them. A watch reads
    /// the prepass's by these names too.
    /// </remarks>
    public static IReadOnlyList<string> EngineViewImageNames { get; } =
        ["depth", "normals", "motion", "ambient_occlusion", "gbuffer", "depth_previous", "gbuffer_previous"];

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
                    Mode = draw.FromBuffer ? 1 : 0,
                    Vertices = draw.Vertices,
                    Instances = draw.Instances,
                    Buffer = draw.Buffer.Key,
                    Offset = draw.Offset,
                    Blend = (int)draw.Blend,
                    DepthWrite = (draw.WritesDepth ? 1 : 0) | (draw.CastsShadows ? 2 : 0),
                    Target = (byte*)target,
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

    /// <summary>
    /// Makes a buffer of <paramref name="size"/> bytes that shaders read and write, holding zeros.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A buffer lives on the GPU. A compute shader writes it, the next frame's dispatch reads what
    /// the last one wrote, and a material or a pass bound to it draws from it, all without anything
    /// crossing back to the CPU. <see cref="BeginBufferRead"/> brings it back when something on the
    /// CPU needs to know. It is bound to any <c>StructuredBuffer</c>, <c>RWStructuredBuffer</c> or
    /// <c>ByteAddressBuffer</c> by name.
    /// </para>
    /// <para>
    /// Its size is fixed, rounded up to a whole number of four-byte words and never less than
    /// sixteen, because a buffer that grew would be a new GPU buffer and whatever was bound to the
    /// old one would go on reading it.
    /// </para>
    /// </remarks>
    public static AssetHandle CreateBuffer(int size) => CreateBuffer<byte>([], size);

    /// <summary>
    /// Makes a buffer holding <paramref name="items"/>, at least <paramref name="size"/> bytes long.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// A <c>StructuredBuffer&lt;T&gt;</c> is laid out with the storage rules, where a
    /// <c>float3</c> takes sixteen bytes, and a <c>ByteAddressBuffer</c> read with <c>Load&lt;T&gt;</c>
    /// packs the way a C# struct with sequential layout does. A struct shared with a structured
    /// buffer therefore spells its padding out, and one shared with a byte address buffer needs
    /// nothing.
    /// </remarks>
    public static AssetHandle CreateBuffer<T>(ReadOnlySpan<T> items, int size = 0) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        var bytes = MemoryMarshal.AsBytes(items);

        fixed (byte* at = bytes)
        {
            return new AssetHandle(Native.Check(
                Native.bcs_shader_buffer_create(at, bytes.Length, size),
                $"making a shader buffer of {Math.Max(size, bytes.Length)} bytes"));
        }
    }

    /// <summary>
    /// Replaces a buffer's contents with <paramref name="items"/>, padded with zeros to its size.
    /// Only valid inside a system.
    /// </summary>
    /// <exception cref="ArgumentException">The items are larger than the buffer.</exception>
    public static void WriteBuffer<T>(AssetHandle buffer, ReadOnlySpan<T> items) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(items);
        int status;

        fixed (byte* at = bytes)
        {
            status = Native.bcs_shader_buffer_write(buffer.Key, at, bytes.Length);
        }

        if (status == NativeStatus.BufferTooSmall)
        {
            throw new ArgumentException(
                $"{bytes.Length} bytes do not fit a buffer of {BufferSize(buffer)}, and a buffer's "
                + "size is fixed when it is made.",
                nameof(items));
        }

        Native.Check(status, "writing a shader buffer");
    }

    /// <summary>
    /// Makes a buffer at least <paramref name="size"/> bytes, keeping what it holds, and answers
    /// its size. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A GPU buffer cannot grow in place, so this is a new one with the old one's contents copied
    /// to its start on the GPU, and the rest zeros. What the GPU wrote since the last
    /// <see cref="WriteBuffer{T}"/> is kept, since the copy is made there rather than from here.
    /// </para>
    /// <para>
    /// Every material and every shader instance holding the buffer is built against the new one, so
    /// there is nothing to hand out again. A size no larger than the buffer's leaves it as it is,
    /// since a buffer never shrinks.
    /// </para>
    /// </remarks>
    public static int GrowBuffer(AssetHandle buffer, int size)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        return Native.Check(Native.bcs_shader_buffer_grow(buffer.Key, size), "growing a shader buffer");
    }

    /// <summary>How many bytes one slot of an instance buffer takes.</summary>
    /// <remarks>Two matrices of four columns: this frame's transform, then the previous frame's.</remarks>
    public const int InstanceSlotBytes = 128;

    /// <summary>
    /// Makes a buffer with <paramref name="capacity"/> slots that the engine fills every frame with
    /// the transforms of the entities put in them, this frame's and the previous frame's. Only
    /// valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Culling instances on the GPU, voxelizing a scene, drawing geometry a shader places and giving
    /// it motion: each needs every instance's transform in a buffer, with last frame's beside it.
    /// The engine writes both once transforms have been worked out each frame, and only when
    /// something moved.
    /// </para>
    /// <para>
    /// A shader reads it as a <c>StructuredBuffer&lt;bcs_scene::Instance&gt;</c> after
    /// <c>import bcs_scene;</c>, with <c>to_world</c>, <c>to_previous_world</c> and
    /// <c>position</c> to read a slot. It is bound by name like any other buffer.
    /// </para>
    /// </remarks>
    public static AssetHandle CreateInstanceBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        return new AssetHandle(Native.Check(
            Native.bcs_shader_instance_buffer_create(capacity),
            $"making an instance buffer of {capacity} slots"));
    }

    /// <summary>
    /// Makes an empty geometry pool: buffers holding the vertices and triangles of every mesh added
    /// to it, which any shader reaches by a mesh's number and a triangle's. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A ray traced in a compute shader, or a scene voxelized by one, meets triangles of whatever
    /// mesh is there, so every mesh it might meet has to be in buffers it can index rather than in
    /// the vertex buffers only a draw of that mesh binds. A shader reads
    /// <see cref="GeometryPool.Vertices"/> as a <c>StructuredBuffer&lt;bcs_scene::PoolVertex&gt;</c>,
    /// <see cref="GeometryPool.Indices"/> as a <c>StructuredBuffer&lt;uint&gt;</c> and
    /// <see cref="GeometryPool.Meshes"/> as a <c>StructuredBuffer&lt;bcs_scene::PoolMesh&gt;</c>, and
    /// <c>bcs_scene::pool_corner</c>, <c>intersect_triangle</c> and <c>intersect_box</c> do the rest.
    /// </para>
    /// <para>
    /// Put an entity's mesh in a pool, and the entity in the same slot of an instance buffer and a
    /// material buffer, and a shader has where its triangles are, how they have moved and what they
    /// are made of, which shading a ray's hit takes.
    /// </para>
    /// </remarks>
    public static GeometryPool CreateGeometryPool()
    {
        var keys = stackalloc int[3];
        Native.Check(Native.bcs_shader_geometry_pool_create(keys), "making a geometry pool");
        return new GeometryPool(new AssetHandle(keys[0]), new AssetHandle(keys[1]), new AssetHandle(keys[2]));
    }

    /// <summary>
    /// Adds a mesh to a geometry pool and answers its number there, counted from zero, which is the
    /// index of its <c>bcs_scene::PoolMesh</c>. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The mesh has to be triangles and has to have loaded; one still loading is refused rather than
    /// waited for, so a mesh from a file is added once <see cref="AssetServer"/> says it has loaded.
    /// Its normals and texture coordinates come along where it has them. The pool's buffers grow as
    /// meshes are added, and everything holding them is built against the grown ones.
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The mesh has not loaded, is not triangles, or the handle names no mesh.
    /// </exception>
    public static int AddToGeometryPool(GeometryPool pool, AssetHandle mesh) =>
        Native.Check(Native.bcs_shader_geometry_pool_add(pool.Meshes.Key, mesh.Key), "adding a mesh to a geometry pool");

    /// <summary>How many bytes one slot of a material buffer takes.</summary>
    /// <remarks>
    /// Base color and emissive color, four floats each, then roughness, metallic, reflectance and
    /// one for unlit.
    /// </remarks>
    public const int MaterialSlotBytes = 48;

    /// <summary>
    /// Makes a buffer with <paramref name="capacity"/> slots that the engine fills every frame with
    /// what the entities put in them are made of, from their standard materials. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A ray that hits something has to know its color to bounce light off it, and world-space GI
    /// and traced reflections shade their hits with it. The engine reads it from each entity's
    /// material rather than a package guessing it, and writes it only when something changed. Slots
    /// are set with <see cref="SetInstance"/>, and putting an entity in the same slot of an
    /// instance buffer and a material buffer gives a shader its transform and its material by one
    /// index.
    /// </para>
    /// <para>
    /// A shader reads it as a <c>StructuredBuffer&lt;bcs_scene::Material&gt;</c>. Textures are not
    /// in it, since the base color is the factor the material multiplies its texture by, and an
    /// entity drawn by a shader program, or with no material, holds zeros.
    /// </para>
    /// </remarks>
    public static AssetHandle CreateMaterialBuffer(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        return new AssetHandle(Native.Check(
            Native.bcs_shader_material_buffer_create(capacity),
            $"making a material buffer of {capacity} slots"));
    }

    /// <summary>
    /// Puts an entity in a slot of an instance or material buffer, or with
    /// <see cref="Entity.None"/> empties the slot. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// An entity put in a slot starts with no motion, so its previous transform is its current
    /// one, rather than wherever the slot's last entity was. An empty slot holds zeros.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The slot is past the buffer's capacity.</exception>
    public static void SetInstance(AssetHandle buffer, int slot, Entity entity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);

        var status = Native.bcs_shader_instance_buffer_set(buffer.Key, slot, entity == Entity.None ? 0 : entity.Bits);

        if (status == NativeStatus.NotPresent)
        {
            throw new ArgumentOutOfRangeException(nameof(slot), $"Slot {slot} is past the instance buffer's capacity.");
        }

        Native.Check(status, $"putting entity {entity} in slot {slot} of an instance buffer");
    }

    /// <summary>A buffer's size in bytes. Only valid inside a system.</summary>
    public static int BufferSize(AssetHandle buffer) =>
        Native.Check(Native.bcs_shader_buffer_size(buffer.Key), "reading a shader buffer's size");

    /// <summary>
    /// Starts copying a buffer back from the GPU. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The copy is of the buffer as it stands once this frame's dispatches have run, and it arrives
    /// a frame or two later, which <see cref="TryReadBuffer"/> is asked each frame until it says
    /// so. Any readback costs that latency, so work whose answer is needed every frame is better
    /// kept on the GPU.
    /// </remarks>
    public static BufferRead BeginBufferRead(AssetHandle buffer) =>
        new(Native.Check(Native.bcs_shader_buffer_read(buffer.Key), "reading a shader buffer back"));

    /// <summary>
    /// The bytes a read brought back, once they have arrived. Only valid inside a system.
    /// </summary>
    /// <returns>True once the bytes have arrived, after which the read is spent.</returns>
    public static bool TryReadBuffer(BufferRead read, out byte[]? bytes)
    {
        bytes = null;

        const int Probe = 4096;
        byte* probe = stackalloc byte[Probe];
        var length = Native.bcs_shader_buffer_take(read.Ticket, probe, Probe);

        if (length == NativeStatus.NotPresent) return false;
        Native.Check(length, "taking what a shader buffer read brought back");

        if (length <= Probe)
        {
            bytes = new ReadOnlySpan<byte>(probe, length).ToArray();
            return true;
        }

        var whole = new byte[length];

        fixed (byte* target = whole)
        {
            Native.Check(
                Native.bcs_shader_buffer_take(read.Ticket, target, whole.Length),
                "taking what a shader buffer read brought back");
        }

        bytes = whole;
        return true;
    }

    /// <summary>
    /// The elements a read brought back, once they have arrived. Only valid inside a system.
    /// </summary>
    public static bool TryReadBuffer<T>(BufferRead read, out T[]? items) where T : unmanaged
    {
        items = null;
        if (!TryReadBuffer(read, out var bytes) || bytes is null) return false;

        items = MemoryMarshal.Cast<byte, T>(bytes).ToArray();
        return true;
    }

    /// <summary>
    /// Makes an image a compute shader writes and anything samples. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A compute shader declares it as a <c>RWTexture2D</c> or <c>RWTexture3D</c> with a
    /// <c>[format(...)]</c> matching <paramref name="format"/>, and it is bound there by name. The
    /// same image put on a material or a pass as a texture draws what the dispatch wrote, which is
    /// how a compute shader paints a water surface, a noise field or a simulation into a picture.
    /// </para>
    /// <para>
    /// It starts transparent black, and its pixels live on the GPU, so a dispatch that writes it
    /// every frame costs nothing crossing the boundary.
    /// </para>
    /// <para>
    /// With <paramref name="mips"/> above one it has that many mip levels, as many as its size
    /// allows, and a level is bound on its own by the <c>mip</c> of <c>SetTexture</c>, so one
    /// dispatch reads a level while the next writes the level below, which is how a depth pyramid
    /// or a blur chain is built. Bound without a level, a texture reads every level and a storage
    /// image writes the first.
    /// </para>
    /// </remarks>
    /// <param name="width">Its width in pixels.</param>
    /// <param name="height">Its height in pixels.</param>
    /// <param name="format">What it holds per pixel.</param>
    /// <param name="depth">Above one, a 3D image this many slices deep.</param>
    /// <param name="mips">How many mip levels, one for just the image.</param>
    public static AssetHandle CreateImage(
        uint width,
        uint height,
        ShaderImageFormat format = ShaderImageFormat.Rgba8,
        uint depth = 1,
        uint mips = 1)
    {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        ArgumentOutOfRangeException.ThrowIfZero(depth);

        return new AssetHandle(Native.Check(
            Native.bcs_shader_image_create(width, height, depth, (int)format, Math.Max(1u, mips)),
            $"making a {width}x{height}x{depth} image for a compute shader"));
    }

    /// <summary>
    /// Makes an image as <see cref="CreateImage(uint, uint, ShaderImageFormat, uint, uint)"/> does,
    /// starting with <paramref name="texels"/> rather than zeros. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The texels are in the format's own layout, row after row and slice after slice, so a
    /// heightmap in <see cref="ShaderImageFormat.R32Float"/> is a float per texel and one in
    /// <see cref="ShaderImageFormat.Rgba16Float"/> is four halves. A picture whose numbers are not
    /// colors needs this, where eight bits a channel would lose them.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// The texels are not exactly the image's size in bytes.
    /// </exception>
    public static AssetHandle CreateImage<T>(
        uint width,
        uint height,
        ShaderImageFormat format,
        ReadOnlySpan<T> texels,
        uint depth = 1) where T : unmanaged
    {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        ArgumentOutOfRangeException.ThrowIfZero(depth);

        var bytes = MemoryMarshal.AsBytes(texels);
        // A compressed image is counted in four by four blocks, which is how its data is laid out.
        var wanted = format >= ShaderImageFormat.Bc1
            ? (long)(width / 4) * (height / 4) * depth * TexelBytes(format)
            : (long)width * height * depth * TexelBytes(format);

        if (bytes.Length != wanted)
        {
            throw new ArgumentException(
                $"A {width}x{height}x{depth} {format} image is {wanted} bytes, and {bytes.Length} were given.",
                nameof(texels));
        }

        fixed (byte* at = bytes)
        {
            return new AssetHandle(Native.Check(
                Native.bcs_shader_image_create_from(width, height, depth, (int)format, at, bytes.Length),
                $"making a {width}x{height}x{depth} {format} image"));
        }
    }

    /// <summary>
    /// Writes texels into a region of an image, <paramref name="width"/> by
    /// <paramref name="height"/> at <paramref name="x"/>, <paramref name="y"/>, on the GPU before
    /// this frame's work runs. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A texture streamer uploads a tile into its cache with this, and an image changed a piece at
    /// a time uses it rather than being made again. The texels are in the image format's own
    /// layout, row after row and slice after slice, as for <see cref="CreateImage{T}"/>, and
    /// <paramref name="depth"/> slices from <paramref name="z"/> reach into a 3D image or the
    /// layers of an array. <paramref name="mip"/> picks the level, whose size is the image's halved
    /// that many times.
    /// </para>
    /// <para>
    /// Only the GPU's copy changes, which is all a shader reads. Writes land in the order they are
    /// asked for, and one made before the image is on the GPU waits for it.
    /// </para>
    /// </remarks>
    /// <exception cref="BevyNativeException">
    /// The region is outside the level, or the texels are not exactly its size in bytes.
    /// </exception>
    public static void WriteImage<T>(
        AssetHandle image,
        ReadOnlySpan<T> texels,
        uint x,
        uint y,
        uint width,
        uint height,
        uint z = 0,
        uint depth = 1,
        uint mip = 0) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(texels);

        fixed (byte* at = bytes)
        {
            Native.Check(
                Native.bcs_shader_image_write(image.Key, x, y, z, width, height, depth, mip, at, bytes.Length),
                $"writing a {width}x{height}x{depth} region of an image");
        }
    }

    /// <summary>
    /// How many bytes one texel of <paramref name="format"/> takes, or for a block-compressed
    /// format one four by four block.
    /// </summary>
    public static int TexelBytes(ShaderImageFormat format) => format switch
    {
        ShaderImageFormat.Rgba8 or ShaderImageFormat.R32Float or ShaderImageFormat.R32UInt
            or ShaderImageFormat.R32Int or ShaderImageFormat.Rgba8UInt => 4,
        ShaderImageFormat.Rgba16Float or ShaderImageFormat.Rg32Float => 8,
        ShaderImageFormat.Rgba32Float or ShaderImageFormat.Rgba32UInt => 16,
        ShaderImageFormat.R16Float => 2,
        ShaderImageFormat.Bc1 or ShaderImageFormat.Bc4 => 8,
        ShaderImageFormat.Bc5 or ShaderImageFormat.Bc7 or ShaderImageFormat.Bc7Srgb or ShaderImageFormat.Bc6hFloat => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(format)),
    };

    private static void RequireProgram(ShaderProgram program, string parameter)
    {
        if (!program.IsValid)
        {
            throw new ArgumentException(
                "No shader program was given. Make one with Shaders.CreateProgram.",
                parameter);
        }
    }

    private static void ThrowIfNoProgram(int status, ShaderProgram program)
    {
        if (status == NativeStatus.InvalidState)
        {
            throw new BevyNativeException(
                NativeStatus.InvalidState,
                $"There is no shader program {program.Id} in the running app. A program belongs "
                + "to the app that made it.");
        }
    }
}

/// <summary>Something a shader's values are set on by name: a material or an instance.</summary>
/// <remarks>
/// Implemented only by <see cref="ShaderMaterial"/> and <see cref="ShaderInstance"/>, so the
/// setters in <see cref="ShaderValues"/> are written once for both.
/// </remarks>
public interface IShaderValues
{
    /// <summary>Which kind of target and which one, as the bridge numbers them.</summary>
    internal (int Kind, long Id) Target { get; }
}

/// <summary>Sets and reads a shader's values by the names it declares.</summary>
/// <remarks>
/// <para>
/// Every setter answers the target, so they chain. Each is only valid inside a system, and a value
/// reaches the shader on the next frame, costing a new bind group rather than a new pipeline.
/// Something that changes every frame for every material is cheaper computed from time in the
/// shader.
/// </para>
/// <para>
/// Numbers are checked for kind and shape, so a <c>float3</c> is set from a <see cref="Vector3"/>,
/// an <c>int</c> from an <see cref="int"/>, a <c>float4x4</c> from a <see cref="Matrix4x4"/>, and
/// an array from a span of its elements, which may be shorter than the array. A C# matrix is laid
/// out by rows, as a Slang <c>float4x4</c> is, so <c>mul(m, v)</c> in the shader is
/// <see cref="Vector4.Transform(Vector4, Matrix4x4)"/> with the matrix transposed.
/// </para>
/// </remarks>
public static unsafe class ShaderValues
{
    extension<T>(T target) where T : struct, IShaderValues
    {
        /// <summary>Sets a <c>float</c>.</summary>
        public T Set(string name, float value) => target.Numbers(name, ShaderScalar.Float, 1, &value, 1);

        /// <summary>Sets an <c>int</c>.</summary>
        public T Set(string name, int value) => target.Numbers(name, ShaderScalar.Int, 1, &value, 1);

        /// <summary>Sets a <c>uint</c>.</summary>
        public T Set(string name, uint value) => target.Numbers(name, ShaderScalar.UInt, 1, &value, 1);

        /// <summary>Sets a <c>bool</c>.</summary>
        public T Set(string name, bool value)
        {
            var word = value ? 1u : 0u;
            return target.Numbers(name, ShaderScalar.UInt, 1, &word, 1);
        }

        /// <summary>Sets a <c>float2</c>.</summary>
        public T Set(string name, Vector2 value) => target.Numbers(name, ShaderScalar.Float, 2, &value, 1);

        /// <summary>Sets a <c>float3</c>.</summary>
        public T Set(string name, Vector3 value) => target.Numbers(name, ShaderScalar.Float, 3, &value, 1);

        /// <summary>Sets a <c>float4</c>, which is also what a color is.</summary>
        public T Set(string name, Vector4 value) => target.Numbers(name, ShaderScalar.Float, 4, &value, 1);

        /// <summary>Sets a <c>float4</c> from a quaternion, as x, y, z and w.</summary>
        public T Set(string name, Quaternion value) => target.Numbers(name, ShaderScalar.Float, 4, &value, 1);

        /// <summary>Sets a <c>float4x4</c>.</summary>
        public T Set(string name, Matrix4x4 value) => target.Numbers(name, ShaderScalar.Float, 16, &value, 1);

        /// <summary>
        /// Sets an array of numbers, vectors or matrices from its first elements.
        /// </summary>
        /// <remarks>
        /// <typeparamref name="TItem"/> is <see cref="float"/>, <see cref="int"/>,
        /// <see cref="uint"/>, one of the <see cref="Vector2"/> to <see cref="Vector4"/>,
        /// <see cref="Quaternion"/> or <see cref="Matrix4x4"/>. A struct is set with
        /// <c>SetBytes</c> instead, because its layout in the shader is not
        /// necessarily its layout in C#.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <typeparamref name="TItem"/> is not a number, a vector or a matrix, or the shader declares
        /// something else under the name.
        /// </exception>
        public T Set<TItem>(string name, ReadOnlySpan<TItem> items) where TItem : unmanaged
        {
            var (scalar, components) = ShapeOf<TItem>()
                ?? throw new ArgumentException(
                    $"{typeof(TItem).Name} is not a number, a vector or a matrix. A struct is set "
                    + "with SetBytes, laid out the way the shader lays it out.",
                    nameof(items));

            fixed (TItem* at = items)
            {
                return target.Numbers(name, scalar, components, at, items.Length);
            }
        }

        /// <summary>Sets an array of numbers, vectors or matrices from its first elements.</summary>
        public T Set<TItem>(string name, TItem[] items) where TItem : unmanaged =>
            target.Set(name, (ReadOnlySpan<TItem>)items);

        /// <summary>
        /// Sets numbers <paramref name="components"/> to an element, for the shapes C# has no type
        /// for: an <c>int2</c>, a <c>uint3</c>, a <c>float3x3</c>, or an array of any of them.
        /// </summary>
        /// <remarks>
        /// <typeparamref name="TItem"/> is <see cref="float"/>, <see cref="int"/> or
        /// <see cref="uint"/>, and says which kind of number the shader holds. A matrix is given by
        /// rows, each padded to four, which is how a uniform holds it, so a <c>float3x3</c> is
        /// twelve numbers.
        /// </remarks>
        public T SetNumbers<TItem>(string name, int components, ReadOnlySpan<TItem> numbers) where TItem : unmanaged
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(components, 1);

            var scalar = ShapeOf<TItem>() is (var kind, 1)
                ? kind
                : throw new ArgumentException(
                    $"{typeof(TItem).Name} is not a float, an int or a uint.",
                    nameof(numbers));

            if (numbers.Length % components != 0)
            {
                throw new ArgumentException(
                    $"{numbers.Length} numbers are not a whole number of elements of {components}.",
                    nameof(numbers));
            }

            fixed (TItem* at = numbers)
            {
                return target.Numbers(name, scalar, components, at, numbers.Length / components);
            }
        }

        /// <summary>
        /// Copies bytes, as they are, to where a name is: a struct, an array of structs, or a whole
        /// <c>ConstantBuffer</c>.
        /// </summary>
        /// <remarks>
        /// A uniform is laid out with rules C# does not follow by itself. A <c>float3</c> starts on
        /// sixteen bytes, an array element takes a multiple of sixteen, and a struct is rounded up to
        /// sixteen. A C# struct matching it spells that padding out, and
        /// <see cref="ShaderMaterial.Program"/>'s <see cref="ShaderProgram.Layout"/> says where each
        /// field is.
        /// </remarks>
        public T SetBytes<TItem>(string name, ReadOnlySpan<TItem> items) where TItem : unmanaged
        {
            var bytes = MemoryMarshal.AsBytes(items);
            var (kind, id) = target.Target;

            fixed (byte* at = bytes)
            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_bytes(kind, id, named, at, bytes.Length),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>Sets a struct, as its bytes. See <c>SetBytes</c>.</summary>
        public T SetStruct<TItem>(string name, TItem value) where TItem : unmanaged =>
            target.SetBytes(name, new ReadOnlySpan<TItem>(&value, 1));

        /// <summary>
        /// Puts an image on a texture, at <paramref name="index"/> in an array of them, or takes it
        /// off again with <see cref="AssetHandle.None"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The image need not have finished loading, because the target holds a handle rather than
        /// pixels. Until it has, the texture reads a stand-in of its shape. The same goes for a
        /// texture nothing was put on, so a shader need not know which ones were given.
        /// </para>
        /// <para>
        /// An image of the wrong shape for the texture (a flat image on a <c>TextureCube</c>, say),
        /// or of a format the texture cannot read, is replaced by the stand-in with a warning in the
        /// log, because binding it would stop the renderer. A cubemap is made with
        /// <see cref="Render.MakeCubemap"/>, and an array or 3D texture with
        /// <see cref="Render.MakeTextureArray"/> and <see cref="Render.MakeVolume"/>. A
        /// <c>RWTexture</c> takes an image from <see cref="Shaders.CreateImage"/>.
        /// </para>
        /// <para>
        /// <paramref name="mip"/> binds one mip level of the image rather than all of them, so a
        /// shader building a pyramid can read the level above and write the next.
        /// </para>
        /// </remarks>
        public T SetTexture(string name, AssetHandle image, int index = 0, int mip = -1)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_image(kind, id, named, index, image.Key, mip),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>
        /// Puts a buffer from <see cref="Shaders.CreateBuffer(int)"/> on a storage buffer, or takes
        /// it off again with <see cref="AssetHandle.None"/>.
        /// </summary>
        /// <remarks>
        /// One nothing was put on reads sixteen bytes of zeros, so a shader that reads its length
        /// sees an empty buffer rather than failing.
        /// </remarks>
        public T SetBuffer(string name, AssetHandle buffer)
        {
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_buffer(kind, id, named, buffer.Key),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>Says how a sampler reads, at <paramref name="index"/> in an array of them.</summary>
        /// <remarks>A sampler nothing was said about reads linear and repeating.</remarks>
        public T SetSampler(string name, SamplerSettings settings, int index = 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            var config = new NativeSamplerConfig { Anisotropy = Math.Clamp(settings.Anisotropy, 1, 16) };
            config.Address[0] = AddressOf(settings.AddressU);
            config.Address[1] = AddressOf(settings.AddressV);
            config.Address[2] = AddressOf(settings.AddressW);
            config.Linear[0] = settings.Magnify == SamplerFilter.Linear ? 1 : 0;
            config.Linear[1] = settings.Minify == SamplerFilter.Linear ? 1 : 0;
            config.Linear[2] = settings.Mipmaps == SamplerFilter.Linear ? 1 : 0;

            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_sampler(kind, id, named, index, &config),
                    name,
                    target);
            }

            return target;
        }

        /// <summary>
        /// Takes the value set under a name off, so the shader reads zeros or a stand-in there
        /// again.
        /// </summary>
        public T Unset(string name)
        {
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                Native.Check(Native.bcs_shader_unset(kind, id, named), $"taking {name} off a shader");
            }

            return target;
        }

        /// <summary>
        /// The floats set under a name, or an empty array where nothing was.
        /// </summary>
        /// <remarks>
        /// What was set rather than what the shader holds, so a name set as a
        /// <see cref="Vector4"/> reads back as four floats, and one never set reads back empty
        /// although the shader reads zeros there.
        /// </remarks>
        public float[] GetFloats(string name) => ShaderValues.Read<float>(target.Target, name);

        /// <summary>The signed integers set under a name. See <c>GetFloats</c>.</summary>
        public int[] GetInts(string name) => ShaderValues.Read<int>(target.Target, name);

        /// <summary>The unsigned integers set under a name. See <c>GetFloats</c>.</summary>
        public uint[] GetUInts(string name) => ShaderValues.Read<uint>(target.Target, name);

        /// <summary>
        /// What the target's program declares, one entry per name, or none before it has compiled.
        /// </summary>
        /// <remarks>What an inspector draws a widget for each of.</remarks>
        public IReadOnlyList<ShaderParameter> Parameters
        {
            get
            {
                var (kind, id) = target.Target;
                var text = Native.ReadText(
                    (buffer, capacity) => Native.bcs_shader_target_names(kind, id, buffer, capacity),
                    "reading what a shader declares");

                return ShaderParameter.Parse(text);
            }
        }

        /// <summary>
        /// Sets numbers under a name from memory: <paramref name="count"/> elements of
        /// <paramref name="components"/> four-byte numbers each.
        /// </summary>
        internal T Numbers(string name, ShaderScalar scalar, int components, void* data, int count)
        {
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_numbers(kind, id, named, (int)scalar, components, (byte*)data, count),
                    name,
                    target);
            }

            return target;
        }
    }

    /// <summary>Reads back the four-byte numbers set under a name.</summary>
    internal static TItem[] Read<TItem>((int Kind, long Id) target, string name) where TItem : unmanaged
    {
        var named = Utf8(name);

        fixed (byte* at = named)
        {
            var length = Native.Check(
                Native.bcs_shader_get_numbers(target.Kind, target.Id, at, null, 0),
                $"reading {name} from a shader");

            var items = new TItem[length / sizeof(TItem)];

            fixed (TItem* into = items)
            {
                Native.Check(
                    Native.bcs_shader_get_numbers(target.Kind, target.Id, at, (byte*)into, items.Length * sizeof(TItem)),
                    $"reading {name} from a shader");
            }

            return items;
        }
    }

    /// <summary>The number of four-byte numbers in one <typeparamref name="TItem"/>, and their kind.</summary>
    internal static (ShaderScalar, int)? ShapeOf<TItem>() where TItem : unmanaged
    {
        if (typeof(TItem) == typeof(float)) return (ShaderScalar.Float, 1);
        if (typeof(TItem) == typeof(int)) return (ShaderScalar.Int, 1);
        if (typeof(TItem) == typeof(uint)) return (ShaderScalar.UInt, 1);
        if (typeof(TItem) == typeof(Vector2)) return (ShaderScalar.Float, 2);
        if (typeof(TItem) == typeof(Vector3)) return (ShaderScalar.Float, 3);
        if (typeof(TItem) == typeof(Vector4)) return (ShaderScalar.Float, 4);
        if (typeof(TItem) == typeof(Quaternion)) return (ShaderScalar.Float, 4);
        if (typeof(TItem) == typeof(Matrix4x4)) return (ShaderScalar.Float, 16);
        return null;
    }

    /// <summary>A name as NUL-terminated UTF-8.</summary>
    internal static byte[] Utf8(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var bytes = new byte[Encoding.UTF8.GetByteCount(name) + 1];
        Encoding.UTF8.GetBytes(name, bytes);
        return bytes;
    }

    /// <summary>
    /// Turns a refused value into an exception that says why, in the bridge's words, and any other
    /// failure into the usual one.
    /// </summary>
    internal static void Refuse<T>(int status, string name, T target) where T : IShaderValues
    {
        if (status == NativeStatus.InvalidState)
        {
            var reason = Native.ReadText(
                (buffer, capacity) => Native.bcs_shader_last_error(buffer, capacity),
                "reading why a shader value was refused");

            throw new ArgumentException(reason.Length > 0 ? reason : $"{name} was not set.", nameof(name));
        }

        if (status == NativeStatus.NoComponent)
        {
            throw new BevyNativeException(
                status,
                target is ShaderMaterial { IsEntity: true }
                    ? $"Setting {name} found no shader material on the entity."
                    : $"Setting {name} found nothing to set it on. A material or an instance belongs "
                    + "to the app that made it.");
        }

        Native.Check(status, $"setting {name} on a shader");
    }

    private static int AddressOf(SamplerAddress address) => address switch
    {
        SamplerAddress.Clamp => 0,
        SamplerAddress.Mirror => 2,
        _ => 1,
    };
}

/// <summary>
/// A material drawn by a <see cref="ShaderProgram"/>, made by
/// <see cref="Shaders.CreateMaterial(ShaderProgram, AlphaMode)"/>, whose values are set by name
/// through <see cref="ShaderValues"/>.
/// </summary>
/// <remarks>
/// A handle to an asset in the engine rather than the asset itself, so copies of it name the same
/// material, and it converts to the <see cref="AssetHandle"/> <see cref="Render.SetMaterial"/>
/// takes. One from <see cref="Shaders.MaterialOn"/> names whatever material an entity is drawn with
/// instead, and has no handle.
/// </remarks>
public readonly unsafe struct ShaderMaterial : IShaderValues, IEquatable<ShaderMaterial>
{
    private const int KindMaterial = 0;
    private const int KindEntity = 2;

    private readonly int _kind;
    private readonly long _id;

    /// <summary>A material by its asset handle.</summary>
    public ShaderMaterial(AssetHandle handle)
    {
        _kind = KindMaterial;
        _id = handle.Key;
    }

    private ShaderMaterial(Entity entity)
    {
        _kind = KindEntity;
        _id = unchecked((long)entity.Bits);
    }

    internal static ShaderMaterial OnEntity(Entity entity) => new(entity);

    (int Kind, long Id) IShaderValues.Target => (_kind, _id);

    /// <summary>Whether this names an entity's material rather than a material by its handle.</summary>
    internal bool IsEntity => _kind == KindEntity;

    /// <summary>
    /// The material's handle, or <see cref="AssetHandle.None"/> for one named by its entity.
    /// </summary>
    public AssetHandle Handle => _kind == KindMaterial ? new AssetHandle((int)_id) : AssetHandle.None;

    /// <summary>The material's handle.</summary>
    public static implicit operator AssetHandle(ShaderMaterial material) => material.Handle;

    /// <summary>
    /// Which program draws the material. Setting it has another program draw it, keeping every
    /// value by name. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// A value the new program does not declare is kept, unused, so switching back finds it again.
    /// </remarks>
    public ShaderProgram Program
    {
        get => new(Native.Check(
            Native.bcs_shader_target_program(_kind, _id),
            "reading which program draws a shader material"));
        set
        {
            if (!value.IsValid) throw new ArgumentException("No program was given.", nameof(value));

            Native.Check(
                Native.bcs_shader_material_configure(Material, value.Id, -1, 0, 0, 0),
                $"having shader program {value.Id} draw a material");
        }
    }

    /// <summary>
    /// Changes what the renderer does where the material is not opaque, which faces it leaves
    /// undrawn and how far its depth is pushed toward the camera. Only valid inside a system.
    /// </summary>
    public ShaderMaterial Configure(
        AlphaMode alpha,
        float cutoff = 0.5f,
        CullMode cull = CullMode.Back,
        float depthBias = 0)
    {
        Native.Check(
            Native.bcs_shader_material_configure(Material, -1, (int)alpha, cutoff, (int)cull, depthBias),
            "changing how a shader material is drawn");
        return this;
    }

    /// <summary>The asset key, for the calls that take a material by its handle alone.</summary>
    private int Material => _kind == KindMaterial
        ? (int)_id
        : throw new InvalidOperationException(
            "A material named by its entity is changed through its values. Its program and alpha "
            + "are changed on the material's own handle.");

    /// <inheritdoc />
    public bool Equals(ShaderMaterial other) => _kind == other._kind && _id == other._id;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ShaderMaterial other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_kind, _id);

    /// <inheritdoc />
    public override string ToString() =>
        _kind == KindMaterial ? $"ShaderMaterial({_id})" : $"ShaderMaterial(entity {_id})";

    /// <summary>Compares two materials.</summary>
    public static bool operator ==(ShaderMaterial left, ShaderMaterial right) => left.Equals(right);

    /// <summary>Compares two materials.</summary>
    public static bool operator !=(ShaderMaterial left, ShaderMaterial right) => !left.Equals(right);
}

/// <summary>
/// What a pass or a dispatch runs: a program and its values by name, made by
/// <see cref="Shaders.CreateInstance"/>.
/// </summary>
/// <remarks>
/// A number rather than an object, since it names something that lives in the engine. It belongs
/// to the app that made it, and means nothing to another.
/// </remarks>
public readonly struct ShaderInstance : IShaderValues, IEquatable<ShaderInstance>
{
    private const int KindInstance = 1;

    /// <summary>The instance's number plus one, so the default value names nothing.</summary>
    private readonly int _idPlusOne;

    internal ShaderInstance(int id) => _idPlusOne = id + 1;

    (int Kind, long Id) IShaderValues.Target => (KindInstance, Id);

    /// <summary>The number the engine knows this instance by.</summary>
    public int Id => _idPlusOne - 1;

    /// <summary>True when this names an instance rather than nothing.</summary>
    public bool IsValid => _idPlusOne > 0;

    /// <summary>
    /// Which program the instance runs. Setting it runs another, keeping every value by name. Only
    /// valid inside a system.
    /// </summary>
    public ShaderProgram Program
    {
        get => new(Native.Check(
            Native.bcs_shader_target_program(KindInstance, Id),
            $"reading which program shader instance {Id} runs"));
        set
        {
            if (!value.IsValid) throw new ArgumentException("No program was given.", nameof(value));

            Native.Check(
                Native.bcs_shader_instance_set_program(Id, value.Id),
                $"having shader instance {Id} run program {value.Id}");
        }
    }

    /// <inheritdoc />
    public bool Equals(ShaderInstance other) => _idPlusOne == other._idPlusOne;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ShaderInstance other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _idPlusOne;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"ShaderInstance({Id})" : "ShaderInstance(None)";

    /// <summary>Compares two instances.</summary>
    public static bool operator ==(ShaderInstance left, ShaderInstance right) => left.Equals(right);

    /// <summary>Compares two instances.</summary>
    public static bool operator !=(ShaderInstance left, ShaderInstance right) => !left.Equals(right);
}

/// <summary>One full-screen pass a camera runs over what it drew. See <see cref="Shaders.SetPasses"/>.</summary>
/// <param name="Instance">An instance of a program with a pass stage.</param>
/// <param name="AfterTonemapping">
/// Whether the pass runs on the picture as the screen will show it rather than on the linear one.
/// Before is right for anything about light, such as a glow or an exposure, because the numbers
/// are still proportional to it. After is right for anything about the picture as a picture, such
/// as scan lines, a palette or dithering.
/// </param>
/// <param name="At">
/// A point of the frame to run at instead, which is <see cref="FramePoint.AfterOpaque"/>: on the lit
/// opaque geometry, before transparent geometry is drawn over it, which is where something about
/// the lit surfaces goes (a screen-space reflection or GI composite, a fog glass should not be
/// under). It needs a camera drawn once a pixel (<see cref="PostSettings.Msaa"/> of one), since a
/// multisampled picture is not resolved until transparent geometry is drawn. Null runs the pass by
/// <paramref name="AfterTonemapping"/>, and the tonemapping points may be named here as well.
/// </param>
public readonly record struct ShaderPass(ShaderInstance Instance, bool AfterTonemapping = false, FramePoint? At = null)
{
    /// <summary>Where the bridge runs it, as the number it reads.</summary>
    internal int Place => At switch
    {
        null => AfterTonemapping ? 1 : 0,
        FramePoint.BeforeTonemapping => 0,
        FramePoint.AfterTonemapping => 1,
        FramePoint.AfterOpaque => 2,
        _ => throw new ArgumentException(
            "A pass runs on the picture, which does not exist yet after the prepass.", nameof(At)),
    };

    /// <summary>A pass that runs before tonemapping.</summary>
    public static implicit operator ShaderPass(ShaderInstance instance) => new(instance);
}

/// <summary>Which kind of number a shader value holds.</summary>
public enum ShaderScalar
{
    /// <summary>A 32-bit float.</summary>
    Float = 0,

    /// <summary>A 32-bit signed integer.</summary>
    Int = 1,

    /// <summary>A 32-bit unsigned integer.</summary>
    UInt = 2,

    /// <summary>A boolean, four bytes wide, set from any integer or a <see cref="bool"/>.</summary>
    Bool = 3,
}

/// <summary>What a name a shader declares is.</summary>
public enum ShaderParameterKind
{
    /// <summary>Numbers: a scalar, a vector, a matrix or an array of them.</summary>
    Number,

    /// <summary>A struct or an array of them, set field by field or as bytes.</summary>
    Struct,

    /// <summary>A texture the shader samples or loads from.</summary>
    Texture,

    /// <summary>An image the shader writes.</summary>
    Image,

    /// <summary>A storage buffer.</summary>
    Buffer,

    /// <summary>A sampler.</summary>
    Sampler,
}

/// <summary>One name a shader declares, as <see cref="ShaderValues"/> reports it.</summary>
/// <param name="Kind">What it is.</param>
/// <param name="Name">The name its value is set by.</param>
/// <param name="Scalar">For numbers, which kind.</param>
/// <param name="Components">
/// For numbers, how many make one element: one for a scalar, four for a <c>float4</c>, sixteen for
/// a <c>float4x4</c>.
/// </param>
/// <param name="Count">How many elements, which is one unless it is an array.</param>
public readonly record struct ShaderParameter(
    ShaderParameterKind Kind,
    string Name,
    ShaderScalar Scalar,
    int Components,
    int Count)
{
    /// <summary>Reads the bridge's listing, one tab-separated line per name.</summary>
    internal static IReadOnlyList<ShaderParameter> Parse(string text)
    {
        var parameters = new List<ShaderParameter>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('\t');

            static int Number(string text) =>
                int.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : 1;

            switch (parts)
            {
                case ["number", var name, var scalar, var components, var count]:
                    parameters.Add(new ShaderParameter(
                        ShaderParameterKind.Number,
                        name,
                        scalar switch
                        {
                            "int" => ShaderScalar.Int,
                            "uint" => ShaderScalar.UInt,
                            "bool" => ShaderScalar.Bool,
                            _ => ShaderScalar.Float,
                        },
                        Number(components),
                        Number(count)));
                    break;

                case [var kind, var name, var count]:
                    parameters.Add(new ShaderParameter(
                        kind switch
                        {
                            "texture" => ShaderParameterKind.Texture,
                            "image" => ShaderParameterKind.Image,
                            "buffer" => ShaderParameterKind.Buffer,
                            "sampler" => ShaderParameterKind.Sampler,
                            _ => ShaderParameterKind.Struct,
                        },
                        name,
                        ShaderScalar.Float,
                        0,
                        Number(count)));
                    break;
            }
        }

        return parameters;
    }
}

/// <summary>How a sampler reads a texture. The default is linear and repeating.</summary>
public readonly record struct SamplerSettings
{
    /// <summary>What reading past either side does across.</summary>
    public SamplerAddress AddressU { get; init; }

    /// <summary>What reading past either side does down.</summary>
    public SamplerAddress AddressV { get; init; }

    /// <summary>What reading past either side does in depth.</summary>
    public SamplerAddress AddressW { get; init; }

    /// <summary>How a texture drawn larger than it is reads between pixels.</summary>
    public SamplerFilter Magnify { get; init; }

    /// <summary>How a texture drawn smaller than it is reads between pixels.</summary>
    public SamplerFilter Minify { get; init; }

    /// <summary>How it reads between mip levels.</summary>
    public SamplerFilter Mipmaps { get; init; }

    /// <summary>
    /// How many samples a texture seen at a slant takes, from one to sixteen. Above one every
    /// filter has to be linear, and is made so.
    /// </summary>
    public int Anisotropy { get; init; }

    /// <summary>Linear and repeating.</summary>
    public static SamplerSettings Linear => default;

    /// <summary>Nearest pixel and repeating, for pixel art.</summary>
    public static SamplerSettings Nearest => new()
    {
        Magnify = SamplerFilter.Nearest,
        Minify = SamplerFilter.Nearest,
        Mipmaps = SamplerFilter.Nearest,
    };

    /// <summary>Linear and clamped to the edge, for anything not meant to tile.</summary>
    public static SamplerSettings Clamped => new()
    {
        AddressU = SamplerAddress.Clamp,
        AddressV = SamplerAddress.Clamp,
        AddressW = SamplerAddress.Clamp,
    };
}

/// <summary>What a sampler does reading past the edge of a texture.</summary>
public enum SamplerAddress
{
    /// <summary>Starts over from the other side.</summary>
    Repeat = 0,

    /// <summary>Reads the edge pixel.</summary>
    Clamp = 1,

    /// <summary>Reads back the way it came.</summary>
    Mirror = 2,
}

/// <summary>How a sampler reads between pixels.</summary>
public enum SamplerFilter
{
    /// <summary>Blends the nearest pixels.</summary>
    Linear = 0,

    /// <summary>Takes the nearest pixel.</summary>
    Nearest = 1,
}

/// <summary>A set of Slang shaders, made by <see cref="Shaders.CreateProgram(ShaderProgramSettings)"/>.</summary>
/// <remarks>
/// A number rather than an object, since it names something that lives in the engine. It belongs
/// to the app that made it, and means nothing to another.
/// </remarks>
public readonly unsafe struct ShaderProgram : IEquatable<ShaderProgram>
{
    /// <summary>The program's number plus one, so the default value names nothing.</summary>
    private readonly int _idPlusOne;

    internal ShaderProgram(int id) => _idPlusOne = id + 1;

    /// <summary>
    /// The number the engine knows this program by, which <c>shader.list</c> and
    /// <c>shader.errors</c> in the console show.
    /// </summary>
    public int Id => _idPlusOne - 1;

    /// <summary>A program that names nothing.</summary>
    public static ShaderProgram None => default;

    /// <summary>True when this names a program rather than nothing.</summary>
    public bool IsValid => _idPlusOne > 0;

    /// <summary>Whether the program can run yet. Only valid inside a system.</summary>
    /// <remarks>
    /// <see cref="ShaderProgramState.Failed"/> can still be drawing, with the last version that
    /// compiled or with a fallback. It is the answer regardless, because what is on disk is not
    /// what is on screen.
    /// </remarks>
    public ShaderProgramState State =>
        (ShaderProgramState)Native.Check(
            Native.bcs_shader_program_state(Id),
            $"asking about shader program {Id}");

    /// <summary>
    /// How many times this program's shaders have been replaced, counting the first time each
    /// compiled. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// Only ever grows. Something that edits a shader file and needs to see the result reads this
    /// first and waits for it to move, which says the edit reached the pipelines rather than
    /// guessing at a number of frames.
    /// </remarks>
    public int Generation =>
        Native.Check(
            Native.bcs_shader_program_generation(Id),
            $"asking about shader program {Id}");

    /// <summary>
    /// What the compiler said, one paragraph per stage, or an empty string. Only valid inside a
    /// system.
    /// </summary>
    public string Diagnostics
    {
        get
        {
            var id = Id;
            return Native.ReadText(
                (buffer, capacity) => Native.bcs_shader_program_diagnostics(id, buffer, capacity),
                $"reading what shader program {id}'s compiler said");
        }
    }

    /// <summary>Which files the program is made of. Only valid inside a system.</summary>
    public string Files
    {
        get
        {
            var id = Id;
            return Native.ReadText(
                (buffer, capacity) => Native.bcs_shader_program_describe(id, buffer, capacity),
                $"reading which files shader program {id} is made of");
        }
    }

    /// <summary>
    /// What the program's shaders declare, a line per binding and per field with its offset, or an
    /// empty string before they have compiled.
    /// </summary>
    /// <remarks>
    /// What a struct set with <see cref="ShaderValues"/>' <c>SetBytes</c> is laid out against, and
    /// what <c>shader.layout</c> in the console prints.
    /// </remarks>
    public string Layout
    {
        get
        {
            var id = Id;
            return Native.ReadText(
                (buffer, capacity) => Native.bcs_shader_program_layout(id, buffer, capacity),
                $"reading what shader program {id} declares");
        }
    }

    /// <summary>
    /// Compiles every stage again now, whether a file changed or not. Only valid inside a system.
    /// </summary>
    public void Reload() =>
        Native.Check(Native.bcs_shader_program_reload(Id), $"reloading shader program {Id}");

    /// <inheritdoc />
    public bool Equals(ShaderProgram other) => _idPlusOne == other._idPlusOne;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ShaderProgram other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _idPlusOne;

    /// <inheritdoc />
    public override string ToString() => IsValid ? $"ShaderProgram({Id})" : "ShaderProgram(None)";

    /// <summary>Compares two programs.</summary>
    public static bool operator ==(ShaderProgram left, ShaderProgram right) => left.Equals(right);

    /// <summary>Compares two programs.</summary>
    public static bool operator !=(ShaderProgram left, ShaderProgram right) => !left.Equals(right);
}

/// <summary>Whether a program can run.</summary>
public enum ShaderProgramState
{
    /// <summary>A stage is still compiling, and what it draws has not appeared yet.</summary>
    Compiling = 0,

    /// <summary>Every stage takes the stage its file declares.</summary>
    Ready = 1,

    /// <summary>A stage did not compile. See <see cref="ShaderProgram.Diagnostics"/>.</summary>
    Failed = 2,
}

/// <summary>The Slang that fills a stage, as a file or as text, and the entry point in it.</summary>
/// <param name="Path">A <c>.slang</c> file under the asset root.</param>
/// <param name="Entry">
/// The function, or null for the usual name: <c>vertex</c> for either vertex shader,
/// <c>fragment</c> for either fragment shader or a pass, and <c>main</c> for compute. Naming it
/// lets one file hold every stage of a program, the prepass's beside the main pass's.
/// </param>
public readonly record struct ShaderStage(string? Path, string? Entry = null)
{
    /// <summary>The Slang itself, where it was handed over as text rather than named by a path.</summary>
    public string? Source { get; init; }

    /// <summary>A stage whose entry point has the usual name.</summary>
    public static implicit operator ShaderStage(string path) => new(path);

    /// <summary>A stage made from Slang handed over as text.</summary>
    /// <remarks>
    /// <para>
    /// For a shader worked out at run time, such as one a node graph produced, one a player typed,
    /// or a variant built from pieces. It is compiled like a file, so it can <c>import bcs;</c> and
    /// any module under the asset root, and it needs <c>slangc</c> or a cache entry the same way.
    /// </para>
    /// <para>
    /// Different text is a different program, so there is nothing to reload. A change is a new
    /// program, and <see cref="ShaderMaterial.Program"/> puts it on a material that already exists.
    /// </para>
    /// </remarks>
    public static ShaderStage Slang(string source, string? entry = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        return new ShaderStage(null, entry) { Source = source };
    }

    /// <summary>Whether a file or a source was given.</summary>
    public bool IsSet => Path is { Length: > 0 } || Source is { Length: > 0 };

    /// <summary>What messages call it.</summary>
    internal string Describe() => Source is { Length: > 0 } ? "inline Slang" : Path ?? "nothing";
}

/// <summary>Which Slang files a <see cref="ShaderProgram"/> is made of.</summary>
/// <remarks>
/// <para>
/// A program draws materials with a fragment shader, runs over a camera's picture with a pass, and
/// is dispatched with a compute shader, and it needs at least one of the three. A vertex stage left
/// unset draws the mesh where it is.
/// </para>
/// <para>
/// The prepass draws depth for shadows, and normals and motion for the effects that read them. A
/// material that moves its own vertices needs a prepass vertex shader moving them the same way, or
/// it casts the shadow of the mesh it started from. One that discards pixels needs a prepass
/// fragment shader discarding the same ones, or its shadow has no holes in it. Both read the
/// material's values like the main stages do.
/// </para>
/// </remarks>
public sealed class ShaderProgramSettings
{
    /// <summary>The main pass's vertex shader. Unset, the mesh is drawn where it is.</summary>
    public ShaderStage Vertex { get; init; }

    /// <summary>The main pass's fragment shader, which draws a material.</summary>
    public ShaderStage Fragment { get; init; }

    /// <summary>The prepass's vertex shader. Unset, Bevy's.</summary>
    public ShaderStage PrepassVertex { get; init; }

    /// <summary>The prepass's fragment shader. Unset, Bevy's, which discards nothing.</summary>
    public ShaderStage PrepassFragment { get; init; }

    /// <summary>
    /// A compute shader, run by <see cref="Shaders.Dispatch"/>. Its entry point is called
    /// <c>main</c> unless it is named.
    /// </summary>
    /// <remarks>
    /// A program with a compute shader needs no fragment shader, and one with both can be
    /// dispatched and drawn with alike, which keeps a simulation and the shader drawing it in one
    /// file.
    /// </remarks>
    public ShaderStage Compute { get; init; }

    /// <summary>
    /// A full-screen pass over a camera's picture, run by <see cref="Shaders.SetPasses"/>. Its
    /// entry point is called <c>fragment</c> unless it is named.
    /// </summary>
    public ShaderStage Pass { get; init; }

    /// <summary>
    /// The vertex shader of geometry drawn on a camera by <see cref="Shaders.SetViewDraws"/>, out of
    /// buffers it reads rather than a mesh. Its entry point is called <c>vertex</c> unless it is
    /// named.
    /// </summary>
    /// <remarks>
    /// It is handed no vertices, only <c>SV_VertexID</c> and <c>SV_InstanceID</c>, and places what
    /// is drawn from whatever buffers it declares, as particles, a visibility buffer or clusters of
    /// a virtualized mesh are drawn. It reads the camera's inputs through <c>import bcs_pass;</c>,
    /// the view among them.
    /// </remarks>
    public ShaderStage DrawVertex { get; init; }

    /// <summary>The fragment shader of geometry drawn on a camera. Required with <see cref="DrawVertex"/>.</summary>
    public ShaderStage DrawFragment { get; init; }

    /// <summary>Names the shaders are compiled with defined.</summary>
    public Dictionary<string, ShaderDefine> Defines { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The stages that were set.</summary>
    internal IEnumerable<ShaderStage> Stages() =>
        new[] { Vertex, Fragment, PrepassVertex, PrepassFragment, Compute, Pass, DrawVertex, DrawFragment }
            .Where(stage => stage.IsSet);

    /// <summary>The stage a message names the program by.</summary>
    internal ShaderStage Main() =>
        Fragment.IsSet ? Fragment : Pass.IsSet ? Pass : Compute.IsSet ? Compute : DrawFragment;
}

/// <summary>The value a shader define has.</summary>
/// <remarks>
/// A boolean, a signed integer or an unsigned one. A false boolean is left undefined rather than
/// defined as zero, because <c>#ifdef</c> asks whether a name is present rather than what it holds,
/// so it reads as off.
/// </remarks>
public readonly record struct ShaderDefine
{
    internal ShaderDefineKind Kind { get; }

    internal int RawValue { get; }

    private ShaderDefine(ShaderDefineKind kind, int value)
    {
        Kind = kind;
        RawValue = value;
    }

    /// <summary>A boolean define.</summary>
    public static implicit operator ShaderDefine(bool value) =>
        new(ShaderDefineKind.Bool, value ? 1 : 0);

    /// <summary>A signed integer define.</summary>
    public static implicit operator ShaderDefine(int value) => new(ShaderDefineKind.Int, value);

    /// <summary>An unsigned integer define.</summary>
    public static implicit operator ShaderDefine(uint value) =>
        new(ShaderDefineKind.UInt, unchecked((int)value));

    /// <inheritdoc />
    public override string ToString() => Kind switch
    {
        ShaderDefineKind.Bool => RawValue != 0 ? "true" : "false",
        ShaderDefineKind.UInt => unchecked((uint)RawValue).ToString(System.Globalization.CultureInfo.InvariantCulture),
        _ => RawValue.ToString(System.Globalization.CultureInfo.InvariantCulture),
    };
}

/// <summary>Which kind of value a <see cref="ShaderDefine"/> holds.</summary>
internal enum ShaderDefineKind
{
    Bool = 0,
    Int = 1,
    UInt = 2,
}

/// <summary>Which faces a material leaves undrawn.</summary>
public enum CullMode
{
    /// <summary>The ones facing away, which a closed mesh never shows. The usual choice.</summary>
    Back = 0,

    /// <summary>The ones facing the camera, which draws the inside of a closed mesh.</summary>
    Front = 1,

    /// <summary>Neither, for anything modeled as a single sheet.</summary>
    None = 2,
}

/// <summary>How a shader material is drawn, apart from its values.</summary>
public sealed class ShaderMaterialSettings
{
    /// <summary>The program that draws it. Required.</summary>
    public ShaderProgram Program { get; set; }

    /// <summary>What the renderer does where the material is not opaque.</summary>
    public AlphaMode Alpha { get; set; } = AlphaMode.Opaque;

    /// <summary>Where <see cref="AlphaMode.Mask"/> stops drawing.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>Which faces are left undrawn.</summary>
    public CullMode Cull { get; set; } = CullMode.Back;

    /// <summary>How far toward the camera the depth is pushed, which stops a decal flickering.</summary>
    public float DepthBias { get; set; }
}

/// <summary>What an image a compute shader writes holds per pixel.</summary>
/// <remarks>
/// The shader's <c>RWTexture</c> declares the same format with <c>[format(...)]</c>, named in
/// brackets after each value here.
/// </remarks>
public enum ShaderImageFormat
{
    /// <summary>Eight bits a channel, from zero to one (<c>rgba8</c>).</summary>
    Rgba8 = 0,

    /// <summary>A half float a channel, which holds light brighter than white (<c>rgba16f</c>).</summary>
    Rgba16Float = 1,

    /// <summary>A float a channel (<c>rgba32f</c>).</summary>
    Rgba32Float = 2,

    /// <summary>One float (<c>r32f</c>).</summary>
    R32Float = 3,

    /// <summary>One unsigned integer (<c>r32ui</c>).</summary>
    R32UInt = 4,

    /// <summary>One signed integer (<c>r32i</c>).</summary>
    R32Int = 5,

    /// <summary>Two floats (<c>rg32f</c>).</summary>
    Rg32Float = 6,

    /// <summary>Four unsigned integers (<c>rgba32ui</c>).</summary>
    Rgba32UInt = 7,

    /// <summary>Four unsigned bytes (<c>rgba8ui</c>).</summary>
    Rgba8UInt = 8,

    /// <summary>One half float (<c>r16f</c>).</summary>
    R16Float = 9,

    /// <summary>
    /// Block-compressed color with one bit of alpha, eight bytes a four by four block. This and the
    /// compressed formats after it are only read, through a sampler. They are made empty or from
    /// blocks and filled a block at a time with <see cref="Shaders.WriteImage{T}"/>, and a streamed
    /// texture's cache is kept in them.
    /// </summary>
    Bc1 = 10,

    /// <summary>One block-compressed channel, eight bytes a block: a height, a mask, a roughness.</summary>
    Bc4 = 11,

    /// <summary>Two block-compressed channels, sixteen bytes a block, which is a normal map's form.</summary>
    Bc5 = 12,

    /// <summary>Block-compressed color and alpha at the best quality, sixteen bytes a block.</summary>
    Bc7 = 13,

    /// <summary>As <see cref="Bc7"/>, read as sRGB, the space a color texture is stored
    /// in.</summary>
    Bc7Srgb = 14,

    /// <summary>Block-compressed color brighter than white, sixteen bytes a block, for light and skies.</summary>
    Bc6hFloat = 15,
}

/// <summary>An image a camera owns. See <see cref="Shaders.SetViewImages"/>.</summary>
/// <param name="Name">The name a shader on the camera reads or writes it by.</param>
/// <param name="Format">What it holds per pixel.</param>
/// <param name="Scale">A fraction of the camera's picture, one for the same size.</param>
/// <param name="History">Whether last frame's is kept too, as <c>Name_previous</c>.</param>
/// <param name="Mips">How many mip levels, each reachable as <c>Name_mip0</c> and on.</param>
/// <param name="ClearEachFrame">
/// Whether it is cleared to zero at the start of every frame, before anything on the camera runs,
/// for an image that draws or atomics accumulate into.
/// </param>
/// <param name="CopyAt">
/// A point of the frame at which the camera's picture is copied into the image, scaled to it and
/// point sampled, or null for none. With <paramref name="History"/>, <c>Name_previous</c> is then
/// last frame's picture, which a technique reusing last frame's lighting reads. Copied at
/// <see cref="FramePoint.BeforeTonemapping"/> it is the lit picture in its own units, and at
/// <see cref="FramePoint.AfterOpaque"/> the same without transparent geometry, which needs a camera
/// drawn once a pixel. Only a float or eight-bit format can hold it, and
/// <see cref="FramePoint.AfterPrepass"/> is refused, since nothing is lit there yet.
/// </param>
public readonly record struct ViewImage(
    string Name,
    ShaderImageFormat Format,
    float Scale = 1f,
    bool History = false,
    int Mips = 1,
    FramePoint? CopyAt = null,
    bool ClearEachFrame = false);

/// <summary>Where in a camera's frame a compute shader on it runs.</summary>
public enum FramePoint
{
    /// <summary>Once depth, normals and motion are drawn, before anything is lit.</summary>
    AfterPrepass = 0,

    /// <summary>Once opaque geometry is drawn, before transparent geometry.</summary>
    AfterOpaque = 1,

    /// <summary>On the linear picture, before the passes that run before tonemapping.</summary>
    BeforeTonemapping = 2,

    /// <summary>On the picture as the screen will show it, before the passes that run after it.</summary>
    AfterTonemapping = 3,
}

/// <summary>How a <see cref="ViewDispatch"/> counts its workgroups.</summary>
public enum ViewDispatchMode
{
    /// <summary>Enough workgroups of a size in pixels to cover a fraction of the picture.</summary>
    PerPixel = 0,

    /// <summary>A fixed number of workgroups.</summary>
    Fixed = 1,

    /// <summary>As many as a buffer says, written on the GPU.</summary>
    Indirect = 2,
}

/// <summary>A compute shader a camera runs every frame. See <see cref="Shaders.SetViewDispatches"/>.</summary>
/// <remarks>Made with <see cref="PerPixel"/>, <see cref="Fixed"/> or <see cref="Indirect"/>.</remarks>
public readonly record struct ViewDispatch
{
    /// <summary>The instance whose program and values run.</summary>
    public ShaderInstance Instance { get; init; }

    /// <summary>Where in the camera's frame.</summary>
    public FramePoint Point { get; init; }

    /// <summary>How the workgroups are counted.</summary>
    public ViewDispatchMode Mode { get; init; }

    /// <summary>The workgroup's width in pixels, or the workgroups across.</summary>
    public uint X { get; init; }

    /// <summary>The workgroup's height in pixels, or the workgroups down.</summary>
    public uint Y { get; init; }

    /// <summary>The workgroups deep, for a fixed dispatch.</summary>
    public uint Z { get; init; }

    /// <summary>The fraction of the picture a per-pixel dispatch covers.</summary>
    public float Scale { get; init; }

    /// <summary>The buffer an indirect dispatch reads its counts from.</summary>
    public AssetHandle Buffer { get; init; }

    /// <summary>Where in the buffer the counts start, in bytes.</summary>
    public uint Offset { get; init; }

    /// <summary>
    /// Enough workgroups of <paramref name="groupX"/> by <paramref name="groupY"/> pixels to cover
    /// <paramref name="scale"/> of the picture, for a shader working a pixel at a time.
    /// </summary>
    /// <remarks>
    /// The workgroup size here matches the shader's <c>numthreads</c>, and the shader checks that
    /// its pixel is inside the picture, since the last workgroups across and down reach past it.
    /// </remarks>
    public static ViewDispatch PerPixel(
        ShaderInstance instance,
        FramePoint point,
        uint groupX = 8,
        uint groupY = 8,
        float scale = 1f) => new()
    {
        Instance = instance,
        Point = point,
        Mode = ViewDispatchMode.PerPixel,
        X = groupX,
        Y = groupY,
        Z = 1,
        Scale = scale,
    };

    /// <summary>Exactly this many workgroups.</summary>
    public static ViewDispatch Fixed(ShaderInstance instance, FramePoint point, uint x, uint y = 1, uint z = 1) => new()
    {
        Instance = instance,
        Point = point,
        Mode = ViewDispatchMode.Fixed,
        X = x,
        Y = y,
        Z = z,
        Scale = 1f,
    };

    /// <summary>As many workgroups as three unsigned integers in a buffer say, when it runs.</summary>
    public static ViewDispatch Indirect(ShaderInstance instance, FramePoint point, AssetHandle buffer, uint offset = 0) => new()
    {
        Instance = instance,
        Point = point,
        Mode = ViewDispatchMode.Indirect,
        Buffer = buffer,
        Offset = offset,
        Scale = 1f,
    };
}

/// <summary>How geometry drawn on a camera combines with the picture.</summary>
public enum DrawBlend
{
    /// <summary>Replaces what is there.</summary>
    Opaque = 0,

    /// <summary>Over what is there, by the fragment's alpha.</summary>
    Alpha = 1,

    /// <summary>Added to what is there, for anything glowing.</summary>
    Add = 2,
}

/// <summary>Geometry a camera draws every frame out of buffers. See <see cref="Shaders.SetViewDraws"/>.</summary>
/// <remarks>Made with <see cref="Fixed"/> or <see cref="Indirect"/>.</remarks>
public readonly record struct ViewDraw
{
    /// <summary>The instance whose program and values draw.</summary>
    public ShaderInstance Instance { get; init; }

    /// <summary>Where in the camera's frame.</summary>
    public FramePoint Point { get; init; }

    /// <summary>Whether the counts are read from <see cref="Buffer"/> when it runs.</summary>
    public bool FromBuffer { get; init; }

    /// <summary>How many vertices each instance has, for a fixed draw.</summary>
    public uint Vertices { get; init; }

    /// <summary>How many instances, for a fixed draw.</summary>
    public uint Instances { get; init; }

    /// <summary>The buffer an indirect draw reads its counts from.</summary>
    public AssetHandle Buffer { get; init; }

    /// <summary>Where in the buffer the counts start, in bytes.</summary>
    public uint Offset { get; init; }

    /// <summary>How it combines with the picture.</summary>
    public DrawBlend Blend { get; init; }

    /// <summary>
    /// Whether it writes depth, as opaque geometry does, or only tests against it, as anything
    /// see-through does.
    /// </summary>
    public bool WritesDepth { get; init; }

    /// <summary>
    /// Whether it is drawn into the shadow maps of the lights that cast shadows as well, so it
    /// shadows everything they light, Bevy's own geometry included.
    /// </summary>
    /// <remarks>
    /// Drawn again for each shadow view, depth alone, after Bevy has drawn its own casters there:
    /// each of the camera's directional cascades, each spot light, and each face of each point
    /// light's cube. Its vertex shader runs with that view in <c>bcs_pass::view</c>, so placing
    /// geometry from the view as it always does places it as the light sees it, and its fragment
    /// shader does not run. A spot or point light's map is shared by every camera, so every camera's
    /// casting draws are drawn into it.
    /// </remarks>
    public bool CastsShadows { get; init; }

    /// <summary>
    /// One of the camera's images (<see cref="Shaders.SetViewImages"/>) to draw into instead of the
    /// picture, or null for the picture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A visibility buffer is made this way, as geometry drawn into an
    /// <see cref="ShaderImageFormat.R32UInt"/> image, each pixel keeping which cluster and triangle
    /// is nearest, for a later pass to shade. The fragment shader returns what the image holds, an
    /// unsigned integer for an integer image.
    /// </para>
    /// <para>
    /// It is drawn once a pixel, and tested against the camera's depth, and writes it if
    /// <see cref="WritesDepth"/> says so, where the image is the picture's size and the camera
    /// draws once a pixel too; otherwise it draws without depth. An integer image cannot be
    /// blended, so a draw into one replaces what is there. Consecutive draws into the same target
    /// share one render pass.
    /// </para>
    /// </remarks>
    public string? Into { get; init; }

    /// <summary>
    /// Several of the camera's images to draw into at once, one for each of the fragment shader's
    /// outputs in order, instead of <see cref="Into"/>.
    /// </summary>
    /// <remarks>
    /// For a draw writing more than one thing a pixel, such as a visibility buffer's ids and the
    /// barycentrics beside them, or a G-buffer of its own. Every image is drawn once a pixel; they
    /// are tested against the camera's depth only where all of them are the picture's size, and
    /// none is blended where any is an integer image.
    /// </remarks>
    public IReadOnlyList<string>? Targets { get; init; }

    /// <summary><paramref name="vertices"/> vertices, <paramref name="instances"/> times.</summary>
    public static ViewDraw Fixed(
        ShaderInstance instance,
        FramePoint point,
        uint vertices,
        uint instances = 1,
        DrawBlend blend = DrawBlend.Opaque,
        bool writesDepth = true) => new()
    {
        Instance = instance,
        Point = point,
        Vertices = vertices,
        Instances = instances,
        Blend = blend,
        WritesDepth = writesDepth,
    };

    /// <summary>
    /// As many vertices and instances as four unsigned integers in a buffer say when it runs:
    /// vertices, instances, the first vertex and the first instance.
    /// </summary>
    public static ViewDraw Indirect(
        ShaderInstance instance,
        FramePoint point,
        AssetHandle buffer,
        uint offset = 0,
        DrawBlend blend = DrawBlend.Opaque,
        bool writesDepth = true) => new()
    {
        Instance = instance,
        Point = point,
        FromBuffer = true,
        Buffer = buffer,
        Offset = offset,
        Blend = blend,
        WritesDepth = writesDepth,
    };
}

/// <summary>The buffers of a geometry pool. See <see cref="Shaders.CreateGeometryPool"/>.</summary>
/// <param name="Vertices">Every vertex, as <c>bcs_scene::PoolVertex</c>.</param>
/// <param name="Indices">Every triangle's three vertex numbers, counted from its mesh's first vertex.</param>
/// <param name="Meshes">The meshes, as <c>bcs_scene::PoolMesh</c>, in the order they were added.</param>
public readonly record struct GeometryPool(AssetHandle Vertices, AssetHandle Indices, AssetHandle Meshes);

/// <summary>A buffer on its way back from the GPU, from <see cref="Shaders.BeginBufferRead"/>.</summary>
public readonly record struct BufferRead(int Ticket);
