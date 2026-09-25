namespace Bevy;

using System.Runtime.InteropServices;
using System.Text;
using Bevy.Interop;

/// <summary>
/// Materials drawn by shaders the game wrote, in WGSL or in Slang, reloaded while the game runs.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ShaderProgram"/> names the files that draw a material, and a material says which
/// program draws it. Bevy asks a material's type rather than the material for its shaders, so the
/// bridge has one material type and rewrites each pipeline it builds with the shaders of the
/// program the material names. That leaves no limit on how many programs a game has, and lets a
/// material change program while it is drawn.
/// </para>
/// <para>
/// <b>What a material carries.</b> Sixty-four floats, whatever bytes the game gives it, eight
/// textures each with its sampler, and two each of cubemaps, array textures and 3D textures, all
/// in group three:
/// </para>
/// <list type="table">
/// <listheader><term>binding</term><description>holds</description></listheader>
/// <item><term>0</term><description>the floats, as <c>array&lt;vec4&lt;f32&gt;, 16&gt;</c> in a uniform</description></item>
/// <item><term>1, 2</term><description>texture zero and its sampler</description></item>
/// <item><term>3</term><description>the bytes, as a read-only storage buffer</description></item>
/// <item><term>4 to 17</term><description>textures one to seven, each followed by its sampler</description></item>
/// <item><term>18, 19</term><description>the cubemaps</description></item>
/// <item><term>20, 21</term><description>the array textures</description></item>
/// <item><term>22, 23</term><description>the 3D textures</description></item>
/// </list>
/// <para>
/// Everything is visible to both stages, so a vertex shader can displace a mesh by a heightmap the
/// fragment shader colors it with. A binding nobody set holds zeros or a fallback image, so a
/// shader need not know which ones were given.
/// </para>
/// <para>
/// <b>Slang.</b> A stage whose file ends in <c>.slang</c> is compiled to WGSL by <c>slangc</c>
/// in the background, and <c>import bcs;</c> gives it Bevy's view, globals and mesh transforms and
/// the material's bindings, with structs that line up with Bevy's own vertex shader. Every
/// successful compile is cached under the asset root in <c>.slang-cache</c>, and a machine without
/// <c>slangc</c> reads the cache instead, so a game shipped with it needs no compiler. See
/// <see cref="SlangAvailable"/>.
/// </para>
/// <para>
/// <b>Reloading.</b> An edit to a shader file, or to any file a Slang shader imported, reaches
/// what is on screen within a quarter of a second. A Slang file that fails to compile leaves the
/// last version that compiled in use and says why in the log and in
/// <see cref="ShaderProgram.Diagnostics"/>. One that has never compiled draws magenta.
/// </para>
/// </remarks>
public static unsafe class Shaders
{
    /// <summary>How many floats one material carries.</summary>
    /// <remarks>
    /// They reach the shader as sixteen <c>vec4</c> in the order they were given, because a
    /// uniform is laid out in sixteen-byte rows and a shader reading vectors reads exactly what was
    /// written. A shader declaring fewer rows reads the first ones, which is how a shader written
    /// for sixteen floats works unchanged.
    /// </remarks>
    public const int ParameterCount = 64;

    /// <summary>How many 2D textures one material carries, each with its own sampler.</summary>
    public const int TextureCount = NativeShaderMaterialConfig.TextureCount;

    /// <summary>How many cubemaps, array textures and 3D textures one material carries of each.</summary>
    /// <remarks>
    /// These have no samplers of their own, because a sampler is the scarcest binding on some
    /// backends. Any of the <see cref="TextureCount"/> samplers samples them.
    /// </remarks>
    public const int ExtraTextureCount = NativeShaderMaterialConfig.ExtraCount;

    /// <summary>Whether a Slang shader can be compiled on this machine.</summary>
    /// <remarks>
    /// <para>
    /// <c>slangc</c> is found through the <c>BCS_SLANGC</c> environment variable, which names it
    /// outright, or on the <c>PATH</c>. It is looked for once per process.
    /// </para>
    /// <para>
    /// False does not mean Slang shaders fail. One compiled on a machine that had
    /// <c>slangc</c> is read back from the cache, provided neither it nor anything it imported has
    /// changed since, which is the situation of a shipped game. What false does mean is that an
    /// edit cannot be compiled.
    /// </para>
    /// </remarks>
    public static bool SlangAvailable => Native.bcs_shader_slang_available() != 0;

    /// <summary>
    /// Whether a validation error from the renderer is survived rather than closing the app.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A shader that compiles can still disagree with the pipeline it is put in, by declaring a
    /// binding as a different type or reading an input the vertex shader never wrote. Bevy's
    /// answer to that is to close the app, which is right for a shipped game and wrong for one
    /// whose shaders are being edited while it runs.
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
    /// <param name="fragment">A <c>.wgsl</c> or <c>.slang</c> file under the asset root.</param>
    public static ShaderProgram CreateProgram(ShaderStage fragment) =>
        CreateProgram(new ShaderProgramSettings { Fragment = fragment });

    /// <summary>
    /// Makes a program from the shaders named. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same settings answer the same program, so a system that makes its program every frame
    /// still makes one. A Slang stage is compiled in the background, and a material drawn by it
    /// appears once the compile has finished, which <see cref="ShaderProgram.State"/> reports.
    /// </para>
    /// <para>
    /// Defines reach a WGSL shader through naga_oil's preprocessor, so <c>#ifdef</c> and
    /// <c>#{NAME}</c> read them, and a Slang shader through its own, as <c>-D</c>. Different
    /// defines make a different program, compiled separately.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// There is no fragment shader, or a file is neither WGSL nor Slang.
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

        if (!settings.Fragment.IsSet && !settings.Compute.IsSet)
        {
            throw new ArgumentException(
                "A shader program needs a fragment shader to draw with, or a compute shader to "
                + "dispatch. Bevy's own fragment shader reads a material laid out differently "
                + "from the one these shaders are handed, so there is no default to fall back on.",
                nameof(settings));
        }

        foreach (var stage in settings.Stages().Where(stage => stage.Source is not { Length: > 0 }))
        {
            if (!stage.Path!.EndsWith(".wgsl", StringComparison.OrdinalIgnoreCase)
                && !stage.Path.EndsWith(".slang", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    $"{stage.Path} is not a shader this bridge compiles. A stage is a .wgsl or a "
                    + ".slang file under the asset root.",
                    nameof(settings));
            }
        }

        // Every string goes into one block of unmanaged memory, freed on the way out, because the
        // native side copies what it reads before it returns.
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
            Language = (int)stage.Language,
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
                    Defines = defines.Length > 0 ? first : null,
                    DefineCount = defines.Length,
                    Compute = Stage(settings.Compute),
                };

                var id = Native.bcs_shader_program_create(&config);

                if (id == NativeStatus.NullArgument)
                {
                    throw new ArgumentException(
                        "A define has an empty name or one with whitespace in it, which neither "
                        + "preprocessor accepts.",
                        nameof(settings));
                }

                Native.Check(
                    id,
                    $"making a shader program from {(settings.Fragment.IsSet ? settings.Fragment : settings.Compute).Describe()}");
                return new ShaderProgram(id);
            }
        }
        finally
        {
            foreach (var pointer in strings) Marshal.FreeCoTaskMem(pointer);
        }
    }

    /// <summary>
    /// Makes a material drawn by a program, with up to <see cref="ParameterCount"/> floats and one
    /// picture.
    /// </summary>
    /// <remarks>
    /// The short form, for a material that is a handful of numbers and at most a texture. What
    /// needs more takes <see cref="CreateMaterial(ShaderMaterialSettings)"/>.
    /// </remarks>
    /// <param name="program">The program that draws it.</param>
    /// <param name="parameters">Up to <see cref="ParameterCount"/> floats. The rest are zero.</param>
    /// <param name="texture">
    /// The picture at binding one, or <see cref="AssetHandle.None"/> to leave it unbound, which a
    /// shader that does not sample one does not notice.
    /// </param>
    /// <param name="alpha">
    /// What the renderer does where this material is not opaque. A shader writing anything but one
    /// in its alpha channel wants <see cref="AlphaMode.Blend"/> or another of the blending modes,
    /// since an opaque material's alpha is not read at all.
    /// </param>
    /// <returns>A handle to give <see cref="Render.SetMaterial"/>.</returns>
    /// <example>
    /// <code>
    /// var ripple = Shaders.CreateProgram("shaders/ripple.wgsl");
    /// var water = Shaders.CreateMaterial(ripple, [0.1f, 0.4f, 0.8f, 1f, speed]);
    /// Render.SetMaterial(ctx.Ecs, pond, water);
    /// </code>
    /// </example>
    public static AssetHandle CreateMaterial(
        ShaderProgram program,
        ReadOnlySpan<float> parameters = default,
        AssetHandle texture = default,
        AlphaMode alpha = AlphaMode.Opaque)
    {
        var settings = new ShaderMaterialSettings
        {
            Program = program,
            Parameters = parameters.ToArray(),
            Alpha = alpha,
        };

        settings.Textures[0] = texture;
        return CreateMaterial(settings);
    }

    /// <summary>Makes a material drawn by a program. Only valid inside a system.</summary>
    /// <remarks>
    /// A texture need not have finished loading, because the material holds a handle rather than
    /// pixels. It draws once every texture it names has arrived. An image of the wrong shape for its
    /// slot, or of a format that cannot be filtered, is replaced by the fallback with a warning in
    /// the log, because binding it would stop the renderer.
    /// </remarks>
    /// <returns>A handle to give <see cref="Render.SetMaterial"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Too many parameters or textures were given, or no program.
    /// </exception>
    /// <exception cref="BevyNativeException">
    /// The program does not exist, a texture names no image, or there is no renderer.
    /// </exception>
    public static AssetHandle CreateMaterial(ShaderMaterialSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Program.IsValid)
        {
            throw new ArgumentException(
                "A shader material needs a program to draw it. Make one with "
                + "Shaders.CreateProgram.",
                nameof(settings));
        }

        var parameters = settings.Parameters ?? [];

        CheckCount(parameters.Length, ParameterCount, "floats", nameof(settings));
        CheckCount(settings.Textures.Length, TextureCount, "textures", nameof(settings));
        CheckCount(settings.Cubemaps.Length, ExtraTextureCount, "cubemaps", nameof(settings));
        CheckCount(settings.TextureArrays.Length, ExtraTextureCount, "array textures", nameof(settings));
        CheckCount(settings.Volumes.Length, ExtraTextureCount, "3D textures", nameof(settings));

        var config = new NativeShaderMaterialConfig
        {
            Program = settings.Program.Id,
            ParameterCount = parameters.Length,
            DataLength = settings.Data?.Length ?? 0,
            Alpha = (int)settings.Alpha,
            AlphaCutoff = settings.AlphaCutoff,
            Cull = (int)settings.Cull,
            DepthBias = settings.DepthBias,
            Buffer = settings.Buffer.Key,
        };

        for (var i = 0; i < settings.Textures.Length; i++) config.Textures[i] = settings.Textures[i].Key;
        for (var i = 0; i < settings.Cubemaps.Length; i++) config.Cubes[i] = settings.Cubemaps[i].Key;
        for (var i = 0; i < settings.TextureArrays.Length; i++) config.Arrays[i] = settings.TextureArrays[i].Key;
        for (var i = 0; i < settings.Volumes.Length; i++) config.Volumes[i] = settings.Volumes[i].Key;

        fixed (float* floats = parameters)
        fixed (byte* bytes = settings.Data)
        {
            config.Parameters = floats;
            config.Data = bytes;

            var key = Native.bcs_shader_material_create(&config);

            if (key == NativeStatus.InvalidState)
            {
                throw new BevyNativeException(
                    NativeStatus.InvalidState,
                    $"There is no shader program {settings.Program.Id} in the running app. A "
                    + "program belongs to the app that made it.");
            }

            Native.Check(key, $"making a material drawn by shader program {settings.Program.Id}");
            return new AssetHandle(key);
        }
    }

    /// <summary>
    /// Overwrites some of a material's floats, starting at <paramref name="offset"/>, and leaves the
    /// rest. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// What animates a material from the game side. The change reaches the shader on the next
    /// frame, and costs a new bind group rather than a new pipeline. Something that changes every
    /// frame for every material is cheaper read from time in the shader, which needs no call at all.
    /// </remarks>
    public static void SetParameters(AssetHandle material, ReadOnlySpan<float> values, int offset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        if (offset + values.Length > ParameterCount)
        {
            throw new ArgumentException(
                $"A shader material carries {ParameterCount} floats, and {values.Length} from "
                + $"{offset} runs past the end.",
                nameof(values));
        }

        fixed (float* at = values)
        {
            Native.Check(
                Native.bcs_shader_material_set_parameters(material.Key, offset, at, values.Length),
                "setting a shader material's parameters");
        }
    }

    /// <summary>Sets one of a material's floats. Only valid inside a system.</summary>
    public static void SetParameter(AssetHandle material, int index, float value) =>
        SetParameters(material, [value], index);

    /// <summary>Reads all of a material's floats. Only valid inside a system.</summary>
    public static float[] GetParameters(AssetHandle material)
    {
        var values = new float[ParameterCount];

        fixed (float* at = values)
        {
            Native.Check(
                Native.bcs_shader_material_get_parameters(material.Key, at, values.Length),
                "reading a shader material's parameters");
        }

        return values;
    }

    /// <summary>
    /// Replaces a material's data with <paramref name="items"/>, as bytes. Only valid inside a
    /// system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Any number of them: this is what a material carries past its sixty-four floats, whether that
    /// is a palette, a list of points or a whole grid.
    /// </para>
    /// <para>
    /// A Slang shader reads them with <c>bcs::element&lt;T&gt;(index)</c>, and Slang reads a raw
    /// buffer with the same packing a C# struct with sequential layout has, so a struct of floats,
    /// integers and vectors declared the same way on both sides agrees on every offset. A WGSL
    /// shader declares <c>@group(3) @binding(3) var&lt;storage, read&gt;</c>, where a
    /// <c>vec3</c> is aligned to sixteen bytes, so a struct shared with WGSL spells its padding out.
    /// </para>
    /// </remarks>
    public static void SetData<T>(AssetHandle material, ReadOnlySpan<T> items) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(items);

        fixed (byte* at = bytes)
        {
            Native.Check(
                Native.bcs_shader_material_set_data(material.Key, at, bytes.Length),
                "setting a shader material's data");
        }
    }

    /// <summary>Puts an image in one of a material's texture slots. Only valid inside a system.</summary>
    /// <param name="material">The material.</param>
    /// <param name="kind">Which kind of slot.</param>
    /// <param name="index">
    /// Which one, below <see cref="TextureCount"/> for 2D textures and <see cref="ExtraTextureCount"/>
    /// for the others.
    /// </param>
    /// <param name="image">The image, or <see cref="AssetHandle.None"/> to empty the slot.</param>
    public static void SetTexture(
        AssetHandle material,
        ShaderTextureKind kind,
        int index,
        AssetHandle image)
    {
        var limit = kind == ShaderTextureKind.Texture2D ? TextureCount : ExtraTextureCount;
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, limit);

        Native.Check(
            Native.bcs_shader_material_set_texture(material.Key, (int)kind, index, image.Key),
            $"setting {kind} {index} of a shader material");
    }

    /// <summary>Puts an image in one of a material's 2D texture slots.</summary>
    public static void SetTexture(AssetHandle material, int index, AssetHandle image) =>
        SetTexture(material, ShaderTextureKind.Texture2D, index, image);

    /// <summary>Has a different program draw a material. Only valid inside a system.</summary>
    /// <remarks>
    /// Everything else about the material stays, so a program swapped in reads the same numbers,
    /// data and textures the old one did.
    /// </remarks>
    public static void SetProgram(AssetHandle material, ShaderProgram program)
    {
        if (!program.IsValid) throw new ArgumentException("No program was given.", nameof(program));

        Native.Check(
            Native.bcs_shader_material_set_program(material.Key, program.Id),
            $"having shader program {program.Id} draw a material");
    }

    /// <summary>Which program draws a material. Only valid inside a system.</summary>
    public static ShaderProgram ProgramOf(AssetHandle material) =>
        new(Native.Check(
            Native.bcs_shader_material_program(material.Key),
            "reading which program draws a shader material"));

    /// <summary>
    /// Changes what the renderer does where a material is not opaque. Only valid inside a system.
    /// </summary>
    /// <param name="material">The material.</param>
    /// <param name="alpha">The mode.</param>
    /// <param name="cutoff">Where <see cref="AlphaMode.Mask"/> stops drawing.</param>
    public static void SetAlpha(AssetHandle material, AlphaMode alpha, float cutoff = 0.5f) =>
        Native.Check(
            Native.bcs_shader_material_set_alpha(material.Key, (int)alpha, cutoff),
            "changing a shader material's alpha mode");

    /// <summary>How many textures a shader pass carries besides the picture.</summary>
    public const int PassTextureCount = NativeShaderPassConfig.TextureCount;

    /// <summary>
    /// Replaces the full-screen passes a camera runs over what it drew, in the order given. Only
    /// valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pass is a program's fragment shader run once for every pixel of the camera's picture,
    /// reading the picture so far and writing the next one: a color grade, an outline, a
    /// distortion, a scan line, anything a picture can be turned into. Passes before tonemapping
    /// see the linear picture, which can be brighter than white, and those after it see what the
    /// screen will show. Each one is compiled and reloaded the way a material's shaders are.
    /// </para>
    /// <para>
    /// A pass reads group zero: the picture and a linear sampler at bindings zero and one,
    /// sixty-four floats at two, whatever bytes it was given at three, Bevy's globals at four, the
    /// view at five, and four textures with their samplers from six to thirteen. Its input is the
    /// position and a <c>uv</c> at location zero, which is what Bevy's full-screen triangle writes.
    /// A Slang pass gets all of it with <c>import bcs_pass;</c>.
    /// </para>
    /// <para>
    /// A pass whose program is still compiling is skipped, so the picture goes on as though it
    /// were not there until it arrives.
    /// </para>
    /// </remarks>
    /// <param name="camera">The camera.</param>
    /// <param name="passes">The passes, or none to take them all off.</param>
    /// <exception cref="ArgumentException">A pass has no program, or too many floats or textures.</exception>
    /// <exception cref="BevyNativeException">
    /// The entity is not a camera, a program does not exist, or there is no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// var scanlines = Shaders.CreateProgram("shaders/scanlines.slang");
    /// Shaders.SetPasses(camera, new ShaderPassSettings { Program = scanlines, Parameters = [0.3f] });
    /// </code>
    /// </example>
    public static void SetPasses(Entity camera, params ShaderPassSettings[] passes)
    {
        ArgumentNullException.ThrowIfNull(passes);

        var configs = new NativeShaderPassConfig[passes.Length];
        var pins = new List<System.Runtime.InteropServices.GCHandle>();

        try
        {
            for (var i = 0; i < passes.Length; i++)
            {
                var pass = passes[i] ?? throw new ArgumentNullException(nameof(passes));

                if (!pass.Program.IsValid)
                {
                    throw new ArgumentException(
                        $"Pass {i} has no program. Make one with Shaders.CreateProgram.",
                        nameof(passes));
                }

                var parameters = pass.Parameters ?? [];
                CheckCount(parameters.Length, ParameterCount, "floats", nameof(passes));
                CheckCount(pass.Textures.Length, PassTextureCount, "textures", nameof(passes));

                ref var config = ref configs[i];
                config.Program = pass.Program.Id;
                config.ParameterCount = parameters.Length;
                config.DataLength = pass.Data?.Length ?? 0;
                config.AfterTonemapping = pass.AfterTonemapping ? 1 : 0;
                config.Buffer = pass.Buffer.Key;

                for (var t = 0; t < pass.Textures.Length; t++) config.Textures[t] = pass.Textures[t].Key;

                // Pinned rather than fixed, because there is one array of each per pass and a
                // fixed statement cannot be written for a count known only at run time.
                if (parameters.Length > 0)
                {
                    var pin = System.Runtime.InteropServices.GCHandle.Alloc(
                        parameters, System.Runtime.InteropServices.GCHandleType.Pinned);
                    pins.Add(pin);
                    config.Parameters = (float*)pin.AddrOfPinnedObject();
                }

                if (pass.Data is { Length: > 0 } data)
                {
                    var pin = System.Runtime.InteropServices.GCHandle.Alloc(
                        data, System.Runtime.InteropServices.GCHandleType.Pinned);
                    pins.Add(pin);
                    config.Data = (byte*)pin.AddrOfPinnedObject();
                }
            }

            fixed (NativeShaderPassConfig* first = configs)
            {
                var status = Native.bcs_render_set_shader_passes(camera.Bits, first, configs.Length);

                if (status == NativeStatus.InvalidState)
                {
                    throw new BevyNativeException(
                        NativeStatus.InvalidState,
                        "A pass names a shader program that is not in the running app. A program "
                        + "belongs to the app that made it.");
                }

                Native.Check(status, "setting a camera's shader passes");
            }
        }
        finally
        {
            foreach (var pin in pins) pin.Free();
        }
    }

    /// <summary>
    /// Overwrites some of a pass's floats, starting at <paramref name="offset"/>. Only valid inside
    /// a system.
    /// </summary>
    /// <param name="camera">The camera the pass is on.</param>
    /// <param name="pass">Which pass, counting from zero in the order they were set.</param>
    /// <param name="values">The floats.</param>
    /// <param name="offset">Where the first one goes.</param>
    public static void SetPassParameters(
        Entity camera,
        int pass,
        ReadOnlySpan<float> values,
        int offset = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        if (offset + values.Length > ParameterCount)
        {
            throw new ArgumentException(
                $"A shader pass carries {ParameterCount} floats, and {values.Length} from "
                + $"{offset} runs past the end.",
                nameof(values));
        }

        fixed (float* at = values)
        {
            Native.Check(
                Native.bcs_render_set_shader_pass_parameters(camera.Bits, pass, offset, at, values.Length),
                $"setting the parameters of shader pass {pass}");
        }
    }

    /// <summary>Replaces a pass's data with <paramref name="items"/>, as bytes. Only valid inside a system.</summary>
    /// <remarks>Read the way a material's data is. See <see cref="SetData{T}"/>.</remarks>
    public static void SetPassData<T>(Entity camera, int pass, ReadOnlySpan<T> items) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(items);

        fixed (byte* at = bytes)
        {
            Native.Check(
                Native.bcs_render_set_shader_pass_data(camera.Bits, pass, at, bytes.Length),
                $"setting the data of shader pass {pass}");
        }
    }

    /// <summary>How many buffers a dispatch is handed.</summary>
    public const int DispatchBufferCount = NativeShaderDispatchConfig.BufferCount;

    /// <summary>
    /// Makes a buffer of <paramref name="size"/> bytes that shaders read and write, holding zeros.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A buffer lives on the GPU. A compute shader writes it, the next frame's dispatch reads what
    /// the last one wrote, and a material or a pass bound to it draws from it, all without anything
    /// crossing back to the CPU. <see cref="BeginBufferRead"/> brings it back when something on the
    /// CPU needs to know.
    /// </para>
    /// <para>
    /// Its size is fixed, rounded up to a whole number of four-byte words and never less than
    /// sixteen, because a buffer that grew would be a new GPU buffer and whatever was bound to the
    /// old one would go on reading it.
    /// </para>
    /// </remarks>
    /// <returns>A handle to hand a dispatch, a material or a pass.</returns>
    public static AssetHandle CreateBuffer(int size) => CreateBuffer<byte>([], size);

    /// <summary>
    /// Makes a buffer holding <paramref name="items"/>, at least <paramref name="size"/> bytes long.
    /// Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// A Slang shader reads and writes it with <c>bcs_compute::element</c> and <c>store</c>, which
    /// pack a struct the way a C# struct with sequential layout is packed, so the same struct
    /// declared on both sides agrees on every offset.
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
    /// Runs a compute shader once, this frame, before any camera draws. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A simulation dispatches every frame from a system, which leaves how often it steps to the
    /// game. Dispatches run in the order they were asked for, each seeing what the one before
    /// wrote, and all of them before the frame is drawn, so a material reading a buffer draws what
    /// the frame's dispatches left in it.
    /// </para>
    /// <para>
    /// A compute shader reads group zero: the floats at binding zero, the four buffers read and
    /// written at bindings one to four, and Bevy's globals at five. A Slang one gets all of it with
    /// <c>import bcs_compute;</c>. A dispatch of a program still compiling does nothing that frame,
    /// which <see cref="ShaderProgram.State"/> says in advance.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">There is no program, or too many floats or buffers.</exception>
    /// <exception cref="BevyNativeException">
    /// The program does not exist, a buffer handle names no buffer, or there is no renderer.
    /// </exception>
    /// <example>
    /// <code>
    /// // Once.
    /// var step = Shaders.CreateProgram(new ShaderProgramSettings { Compute = "shaders/boids.slang" });
    /// var flock = Shaders.CreateBuffer&lt;Boid&gt;(boids);
    ///
    /// // Every frame, 64 boids to a workgroup.
    /// Shaders.Dispatch(step, (uint)(boids.Length + 63) / 64, flock);
    /// </code>
    /// </example>
    public static void Dispatch(DispatchSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.Program.IsValid)
        {
            throw new ArgumentException(
                "A dispatch needs a program with a compute shader. Make one with "
                + "Shaders.CreateProgram.",
                nameof(settings));
        }

        var parameters = settings.Parameters ?? [];
        CheckCount(parameters.Length, ParameterCount, "floats", nameof(settings));
        CheckCount(settings.Buffers.Length, DispatchBufferCount, "buffers", nameof(settings));
        CheckCount(settings.Images.Length, 2, "images written", nameof(settings));
        CheckCount(settings.Textures.Length, 2, "images read", nameof(settings));

        var config = new NativeShaderDispatchConfig
        {
            Program = settings.Program.Id,
            ParameterCount = parameters.Length,
            X = settings.X,
            Y = settings.Y,
            Z = settings.Z,
        };

        for (var i = 0; i < settings.Buffers.Length; i++) config.Buffers[i] = settings.Buffers[i].Key;
        for (var i = 0; i < settings.Images.Length; i++) config.Images[i] = settings.Images[i].Key;
        for (var i = 0; i < settings.Textures.Length; i++) config.Textures[i] = settings.Textures[i].Key;

        fixed (float* floats = parameters)
        {
            config.Parameters = floats;
            var status = Native.bcs_shader_dispatch(&config);

            if (status == NativeStatus.InvalidState)
            {
                throw new BevyNativeException(
                    NativeStatus.InvalidState,
                    $"There is no shader program {settings.Program.Id} in the running app. A "
                    + "program belongs to the app that made it.");
            }

            Native.Check(status, $"dispatching shader program {settings.Program.Id}");
        }
    }

    /// <summary>
    /// Runs a compute shader over <paramref name="workgroups"/> workgroups along one axis, handing
    /// it <paramref name="buffers"/> in order. Only valid inside a system.
    /// </summary>
    public static void Dispatch(ShaderProgram program, uint workgroups, params AssetHandle[] buffers)
    {
        var settings = new DispatchSettings { Program = program, X = workgroups };
        CheckCount(buffers.Length, DispatchBufferCount, "buffers", nameof(buffers));
        buffers.CopyTo(settings.Buffers, 0);
        Dispatch(settings);
    }

    /// <summary>
    /// Makes an image a compute shader writes and a material or a pass samples. Only valid inside
    /// a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A dispatch writes <see cref="ShaderImageFormat.Rgba8"/> images at binding six and
    /// <see cref="ShaderImageFormat.Rgba16Float"/> images at binding seven, because a storage
    /// texture's format is part of its binding. The same image handed to a material as a texture
    /// draws what the dispatch wrote, which is how a compute shader paints a water surface, a
    /// noise field or a simulation into a picture.
    /// </para>
    /// <para>
    /// It starts transparent black, and its pixels live on the GPU, so a dispatch that writes it
    /// every frame costs nothing crossing the boundary.
    /// </para>
    /// </remarks>
    public static AssetHandle CreateImage(uint width, uint height, ShaderImageFormat format = ShaderImageFormat.Rgba8)
    {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);

        return new AssetHandle(Native.Check(
            Native.bcs_shader_image_create(width, height, (int)format),
            $"making a {width}x{height} image for a compute shader"));
    }

    /// <summary>
    /// Binds a buffer at a material's data binding in place of its own bytes, or takes it off with
    /// <see cref="AssetHandle.None"/>. Only valid inside a system.
    /// </summary>
    public static void SetBuffer(AssetHandle material, AssetHandle buffer) =>
        Native.Check(
            Native.bcs_shader_material_set_buffer(material.Key, buffer.Key),
            "binding a buffer to a shader material");

    private static void CheckCount(int given, int limit, string what, string parameter)
    {
        if (given > limit)
        {
            throw new ArgumentException(
                $"A shader material carries {limit} {what} and {given} were given.",
                parameter);
        }
    }
}

/// <summary>A set of shaders that draws materials, made by <see cref="Shaders.CreateProgram(ShaderProgramSettings)"/>.</summary>
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

    /// <summary>Whether the program can draw yet. Only valid inside a system.</summary>
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
    /// loaded. Only valid inside a system.
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
    /// What the compilers said, one paragraph per stage, or an empty string. Only valid inside a
    /// system.
    /// </summary>
    public string Diagnostics
    {
        get
        {
            var id = Id;
            return Native.ReadText(
                (buffer, capacity) => Native.bcs_shader_program_diagnostics(id, buffer, capacity),
                $"reading what shader program {id}'s compilers said");
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
    /// Compiles or reloads every stage now, whether a file changed or not. Only valid inside a
    /// system.
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

/// <summary>Whether a program can draw.</summary>
public enum ShaderProgramState
{
    /// <summary>A stage is still compiling or loading, and what it draws has not appeared yet.</summary>
    Compiling = 0,

    /// <summary>Every stage is what its file says.</summary>
    Ready = 1,

    /// <summary>A stage did not compile. See <see cref="ShaderProgram.Diagnostics"/>.</summary>
    Failed = 2,
}

/// <summary>The code that fills a stage, as a file or as text, and the entry point in it.</summary>
/// <param name="Path">A <c>.wgsl</c> or <c>.slang</c> file under the asset root.</param>
/// <param name="Entry">
/// The function, or null for the name Bevy's own shaders use for the stage, which is
/// <c>vertex</c>, <c>fragment</c> or, for compute, <c>main</c>. Naming it is what lets one file
/// hold every stage of a program.
/// </param>
public readonly record struct ShaderStage(string? Path, string? Entry = null)
{
    /// <summary>The code itself, where it was handed over as text rather than named by a path.</summary>
    public string? Source { get; init; }

    /// <summary>What <see cref="Source"/> is written in.</summary>
    public ShaderLanguage Language { get; init; }

    /// <summary>A stage whose entry point has the usual name.</summary>
    public static implicit operator ShaderStage(string path) => new(path);

    /// <summary>A stage made from WGSL handed over as text.</summary>
    /// <remarks>
    /// <para>
    /// What a shader worked out at run time wants: one a node graph produced, one a player typed,
    /// or a variant built from pieces. It may <c>#import</c> Bevy's modules like a file can.
    /// </para>
    /// <para>
    /// Different text is a different program, so there is nothing to reload. A change is a new
    /// program, and <see cref="Shaders.SetProgram"/> puts it on a material that already exists.
    /// </para>
    /// </remarks>
    public static ShaderStage Wgsl(string source, string? entry = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        return new ShaderStage(null, entry) { Source = source, Language = ShaderLanguage.Wgsl };
    }

    /// <summary>A stage made from Slang handed over as text.</summary>
    /// <remarks>
    /// Compiled like a file, so it can <c>import bcs;</c> and any module under the asset root, and
    /// it needs <c>slangc</c> or a cache entry the same way. See <see cref="Wgsl"/>.
    /// </remarks>
    public static ShaderStage Slang(string source, string? entry = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(source);
        return new ShaderStage(null, entry) { Source = source, Language = ShaderLanguage.Slang };
    }

    /// <summary>Whether a file or a source was given.</summary>
    public bool IsSet => Path is { Length: > 0 } || Source is { Length: > 0 };

    /// <summary>What messages call it.</summary>
    internal string Describe() => Source is { Length: > 0 } ? $"inline {Language}" : Path ?? "nothing";
}

/// <summary>What a <see cref="ShaderStage"/> handed over as text is written in.</summary>
public enum ShaderLanguage
{
    /// <summary>The stage names a file, whose extension says.</summary>
    File = 0,

    /// <summary>WGSL.</summary>
    Wgsl = 1,

    /// <summary>Slang.</summary>
    Slang = 2,
}

/// <summary>Which files a <see cref="ShaderProgram"/> is made of.</summary>
/// <remarks>
/// <para>
/// A stage left unset is Bevy's own. A fragment shader is required to draw with the program, and a
/// compute shader to dispatch it, so one of the two is.
/// </para>
/// <para>
/// The prepass is what draws depth for shadows, and normals and motion for the effects that read
/// them. A material that moves its own vertices wants a prepass vertex shader moving them the same
/// way, or it casts the shadow of the mesh it started from. One that discards pixels wants a
/// prepass fragment shader discarding the same ones, or its shadow has no holes in it.
/// </para>
/// </remarks>
public sealed class ShaderProgramSettings
{
    /// <summary>The main pass's vertex shader. Unset, the mesh is drawn where it is.</summary>
    public ShaderStage Vertex { get; init; }

    /// <summary>The main pass's fragment shader. Required.</summary>
    public ShaderStage Fragment { get; init; }

    /// <summary>The prepass's vertex shader. Unset, Bevy's.</summary>
    public ShaderStage PrepassVertex { get; init; }

    /// <summary>The prepass's fragment shader. Unset, Bevy's, which discards nothing.</summary>
    public ShaderStage PrepassFragment { get; init; }

    /// <summary>
    /// A compute shader, run by <see cref="Shaders.Dispatch(DispatchSettings)"/>. Its entry point
    /// is called <c>main</c> unless it is named.
    /// </summary>
    /// <remarks>
    /// A program with a compute shader needs no fragment shader, and one with both can be
    /// dispatched and drawn with alike, which is what keeps a simulation and the shader drawing
    /// it in one file.
    /// </remarks>
    public ShaderStage Compute { get; init; }

    /// <summary>Names the shaders are compiled with defined.</summary>
    public Dictionary<string, ShaderDefine> Defines { get; init; } = new(StringComparer.Ordinal);

    /// <summary>The stages that were set.</summary>
    internal IEnumerable<ShaderStage> Stages() =>
        new[] { Vertex, Fragment, PrepassVertex, PrepassFragment, Compute }.Where(stage => stage.IsSet);
}

/// <summary>The value a shader define has.</summary>
/// <remarks>
/// A boolean, a signed integer or an unsigned one, because those are the kinds naga_oil's
/// preprocessor knows, and a define has to mean the same to a WGSL shader and a Slang one. A false
/// boolean is left undefined rather than defined as false, because <c>#ifdef</c> asks whether a
/// name is present rather than what it holds, so in both languages it reads as off.
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

/// <summary>Which kind of texture slot of a shader material.</summary>
public enum ShaderTextureKind
{
    /// <summary>One of the eight 2D textures, each with its sampler.</summary>
    Texture2D = 0,

    /// <summary>One of the two cubemaps.</summary>
    Cube = 1,

    /// <summary>One of the two 2D array textures.</summary>
    Array = 2,

    /// <summary>One of the two 3D textures.</summary>
    Volume = 3,
}

/// <summary>Everything a shader material is made of.</summary>
/// <remarks>
/// A texture need not have loaded when the material is made. The material draws once every image
/// it names has arrived, which is what keeps it from drawing a frame with a white square where a
/// picture belongs.
/// </remarks>
public sealed class ShaderMaterialSettings
{
    /// <summary>The program that draws it. Required.</summary>
    public ShaderProgram Program { get; set; }

    /// <summary>Up to <see cref="Shaders.ParameterCount"/> floats, at binding zero.</summary>
    public float[]? Parameters { get; set; }

    /// <summary>Any number of bytes, at binding three. See <see cref="Shaders.SetData{T}"/>.</summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// A buffer from <see cref="Shaders.CreateBuffer(int)"/> bound at binding three in place of
    /// <see cref="Data"/>.
    /// </summary>
    /// <remarks>
    /// What draws the result of a compute shader: the buffer stays on the GPU, the dispatch writes
    /// it and the material reads it, and nothing crosses back to the CPU on the way.
    /// </remarks>
    public AssetHandle Buffer { get; set; }

    /// <summary>The 2D textures, at bindings one and four to sixteen, each with its sampler.</summary>
    public AssetHandle[] Textures { get; } = new AssetHandle[Shaders.TextureCount];

    /// <summary>The cubemaps, at bindings eighteen and nineteen.</summary>
    /// <remarks>
    /// An image has to have been made a cube first, with <see cref="Render.MakeCubemap"/>. A flat
    /// image here is replaced by the fallback rather than bound.
    /// </remarks>
    public AssetHandle[] Cubemaps { get; } = new AssetHandle[Shaders.ExtraTextureCount];

    /// <summary>The 2D array textures, at bindings twenty and twenty-one.</summary>
    /// <remarks>See <see cref="Render.MakeTextureArray"/>.</remarks>
    public AssetHandle[] TextureArrays { get; } = new AssetHandle[Shaders.ExtraTextureCount];

    /// <summary>The 3D textures, at bindings twenty-two and twenty-three.</summary>
    public AssetHandle[] Volumes { get; } = new AssetHandle[Shaders.ExtraTextureCount];

    /// <summary>What the renderer does where the material is not opaque.</summary>
    public AlphaMode Alpha { get; set; } = AlphaMode.Opaque;

    /// <summary>Where <see cref="AlphaMode.Mask"/> stops drawing.</summary>
    public float AlphaCutoff { get; set; } = 0.5f;

    /// <summary>Which faces are left undrawn.</summary>
    public CullMode Cull { get; set; } = CullMode.Back;

    /// <summary>How far toward the camera the depth is pushed, which stops a decal flickering.</summary>
    public float DepthBias { get; set; }
}

/// <summary>One full-screen pass a camera runs over what it drew.</summary>
/// <remarks>See <see cref="Shaders.SetPasses"/>.</remarks>
public sealed class ShaderPassSettings
{
    /// <summary>The program whose fragment shader is run over the picture. Required.</summary>
    public ShaderProgram Program { get; set; }

    /// <summary>Up to <see cref="Shaders.ParameterCount"/> floats, at binding two.</summary>
    public float[]? Parameters { get; set; }

    /// <summary>Any number of bytes, at binding three.</summary>
    public byte[]? Data { get; set; }

    /// <summary>A buffer bound at binding three in place of <see cref="Data"/>.</summary>
    public AssetHandle Buffer { get; set; }

    /// <summary>Pictures besides the camera's own, at bindings six to thirteen with their samplers.</summary>
    public AssetHandle[] Textures { get; } = new AssetHandle[Shaders.PassTextureCount];

    /// <summary>
    /// Whether the pass runs after tonemapping, on the picture as the screen will show it, rather
    /// than before, on the linear one.
    /// </summary>
    /// <remarks>
    /// Before is right for anything about light, such as a glow or an exposure, because the numbers
    /// are still proportional to it. After is right for anything about the picture as a picture,
    /// such as scan lines, a palette or dithering.
    /// </remarks>
    public bool AfterTonemapping { get; set; }
}

/// <summary>One run of a compute shader. See <see cref="Shaders.Dispatch(DispatchSettings)"/>.</summary>
public sealed class DispatchSettings
{
    /// <summary>A program with a compute stage. Required.</summary>
    public ShaderProgram Program { get; set; }

    /// <summary>How many workgroups along the first axis.</summary>
    /// <remarks>
    /// A workgroup is as many invocations as the shader's <c>numthreads</c> or
    /// <c>@workgroup_size</c> says, so a thousand elements at sixty-four a workgroup is sixteen,
    /// and the last workgroup checks that its elements exist.
    /// </remarks>
    public uint X { get; set; } = 1;

    /// <summary>How many workgroups along the second axis.</summary>
    public uint Y { get; set; } = 1;

    /// <summary>How many workgroups along the third axis.</summary>
    public uint Z { get; set; } = 1;

    /// <summary>Up to <see cref="Shaders.ParameterCount"/> floats, at binding zero.</summary>
    public float[]? Parameters { get; set; }

    /// <summary>Buffers from <see cref="Shaders.CreateBuffer(int)"/>, at bindings one to four.</summary>
    public AssetHandle[] Buffers { get; } = new AssetHandle[Shaders.DispatchBufferCount];

    /// <summary>
    /// Images from <see cref="Shaders.CreateImage"/> the shader writes: an
    /// <see cref="ShaderImageFormat.Rgba8"/> one at binding six and an
    /// <see cref="ShaderImageFormat.Rgba16Float"/> one at seven.
    /// </summary>
    public AssetHandle[] Images { get; } = new AssetHandle[2];

    /// <summary>Any images the shader reads, at bindings eight and nine, with a linear sampler at ten.</summary>
    public AssetHandle[] Textures { get; } = new AssetHandle[2];
}

/// <summary>What an image a compute shader writes holds per pixel.</summary>
public enum ShaderImageFormat
{
    /// <summary>Eight bits a channel, from zero to one. Written at binding six.</summary>
    Rgba8 = 0,

    /// <summary>A half float a channel, which can hold light brighter than white. Written at binding seven.</summary>
    Rgba16Float = 1,
}

/// <summary>A buffer on its way back from the GPU, from <see cref="Shaders.BeginBufferRead"/>.</summary>
public readonly record struct BufferRead(int Ticket);
