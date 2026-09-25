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
/// declares what it needs as ordinary Slang globals: a thousand floats, sixty-four textures,
/// sixteen cubemaps, structs, constant buffers, storage buffers, storage images and samplers. The
/// bridge asks the compiler how they were laid out and builds the bind group from that, so nothing
/// about a shader's shape is fixed in advance.
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
    /// <remarks>What most materials want. See <see cref="CreateProgram(ShaderProgramSettings)"/>.</remarks>
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

        if (!settings.Fragment.IsSet && !settings.Compute.IsSet && !settings.Pass.IsSet)
        {
            throw new ArgumentException(
                "A shader program needs a fragment shader to draw with, a pass to run over a "
                + "camera's picture, or a compute shader to dispatch. Bevy's own fragment shader "
                + "reads a material laid out differently, so there is no default to fall back on.",
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
    /// in its alpha channel wants <see cref="AlphaMode.Blend"/> or another of the blending modes,
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
    /// Makes an instance of a program, which is what a pass over a camera's picture or a compute
    /// dispatch runs. Only valid inside a system.
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
            after[i] = passes[i].AfterTonemapping ? 1 : 0;
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
    /// Asks a camera to draw its depth, its normals or both before the scene, for its passes to
    /// read. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pass reads them with <c>bcs_pass::depth_at</c> and <c>normal_at</c>, which is what an
    /// outline, a fog or an edge detector is made of. A camera that draws neither binds depth zero,
    /// which is the far plane, and white normals, so a pass reading them runs either way.
    /// </para>
    /// <para>
    /// A prepass draws the scene a second time, so it is worth asking for only when something reads
    /// it. A multisampled camera draws it multisampled, which a pass cannot bind, so a camera read
    /// this way wants <see cref="PostSettings.Msaa"/> of one, and gets the stand-ins otherwise.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="depth">Whether to draw depth.</param>
    /// <param name="normals">Whether to draw normals.</param>
    public static void SetPrepass(Entity camera, bool depth, bool normals = false) =>
        Native.Check(
            Native.bcs_render_set_prepass(camera.Bits, (depth ? 1u : 0u) | (normals ? 2u : 0u)),
            "asking a camera for a prepass");

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

    /// <summary>A buffer's size in bytes. Only valid inside a system.</summary>
    public static int BufferSize(AssetHandle buffer) =>
        Native.Check(Native.bcs_shader_buffer_size(buffer.Key), "reading a shader buffer's size");

    /// <summary>
    /// Starts copying a buffer back from the GPU. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// The copy is of the buffer as it stands once this frame's dispatches have run, and it arrives
    /// a frame or two later, which <see cref="TryReadBuffer"/> is asked each frame until it says
    /// so. That latency is what any readback costs, so work whose answer is needed every frame is
    /// better kept on the GPU.
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
    /// </remarks>
    /// <param name="width">Its width in pixels.</param>
    /// <param name="height">Its height in pixels.</param>
    /// <param name="format">What it holds per pixel.</param>
    /// <param name="depth">Above one, a 3D image this many slices deep.</param>
    public static AssetHandle CreateImage(
        uint width,
        uint height,
        ShaderImageFormat format = ShaderImageFormat.Rgba8,
        uint depth = 1)
    {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        ArgumentOutOfRangeException.ThrowIfZero(depth);

        return new AssetHandle(Native.Check(
            Native.bcs_shader_image_create(width, height, depth, (int)format),
            $"making a {width}x{height}x{depth} image for a compute shader"));
    }

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
/// Implemented only by <see cref="ShaderMaterial"/> and <see cref="ShaderInstance"/>, which is
/// what lets the setters in <see cref="ShaderValues"/> be written once for both.
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
/// Numbers are checked for kind and shape: a <c>float3</c> is set from a <see cref="Vector3"/>, an
/// <c>int</c> from an <see cref="int"/>, a <c>float4x4</c> from a <see cref="Matrix4x4"/>, and an
/// array from a span of its elements, which may be shorter than the array. A C# matrix is laid out
/// by rows, which is what a Slang <c>float4x4</c> is, so <c>mul(m, v)</c> in the shader is
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
        /// A uniform is laid out with rules C# does not follow by itself: a <c>float3</c> starts on
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
        /// </remarks>
        public T SetTexture(string name, AssetHandle image, int index = 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            var (kind, id) = target.Target;

            fixed (byte* named = ShaderValues.Utf8(name))
            {
                ShaderValues.Refuse(
                    Native.bcs_shader_set_image(kind, id, named, index, image.Key),
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
public readonly record struct ShaderPass(ShaderInstance Instance, bool AfterTonemapping = false)
{
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
    /// The number the engine knows this program by, which is what <c>shader.list</c> and
    /// <c>shader.errors</c> in the console call it.
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
    /// Only ever grows. Something that edits a shader file and wants to see the result reads this
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

    /// <summary>Every stage is what its file says.</summary>
    Ready = 1,

    /// <summary>A stage did not compile. See <see cref="ShaderProgram.Diagnostics"/>.</summary>
    Failed = 2,
}

/// <summary>The Slang that fills a stage, as a file or as text, and the entry point in it.</summary>
/// <param name="Path">A <c>.slang</c> file under the asset root.</param>
/// <param name="Entry">
/// The function, or null for the usual name: <c>vertex</c> for either vertex shader,
/// <c>fragment</c> for either fragment shader or a pass, and <c>main</c> for compute. Naming it is
/// what lets one file hold every stage of a program, the prepass's beside the main pass's.
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
    /// What a shader worked out at run time wants: one a node graph produced, one a player typed,
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
/// The prepass is what draws depth for shadows, and normals and motion for the effects that read
/// them. A material that moves its own vertices wants a prepass vertex shader moving them the same
/// way, or it casts the shadow of the mesh it started from. One that discards pixels wants a
/// prepass fragment shader discarding the same ones, or its shadow has no holes in it. Both read
/// the material's values like the main stages do.
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
    /// dispatched and drawn with alike, which is what keeps a simulation and the shader drawing it
    /// in one file.
    /// </remarks>
    public ShaderStage Compute { get; init; }

    /// <summary>
    /// A full-screen pass over a camera's picture, run by <see cref="Shaders.SetPasses"/>. Its
    /// entry point is called <c>fragment</c> unless it is named.
    /// </summary>
    public ShaderStage Pass { get; init; }

    /// <summary>Names the shaders are compiled with defined.</summary>
    public Dictionary<string, ShaderDefine> Defines { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The stages that were set.</summary>
    internal IEnumerable<ShaderStage> Stages() =>
        new[] { Vertex, Fragment, PrepassVertex, PrepassFragment, Compute, Pass }.Where(stage => stage.IsSet);

    /// <summary>The stage a message names the program by.</summary>
    internal ShaderStage Main() => Fragment.IsSet ? Fragment : Pass.IsSet ? Pass : Compute;
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

    /// <summary>Neither, for anything modelled as a single sheet.</summary>
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
}

/// <summary>A buffer on its way back from the GPU, from <see cref="Shaders.BeginBufferRead"/>.</summary>
public readonly record struct BufferRead(int Ticket);
