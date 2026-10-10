namespace Bevy;

using System.Numerics;
using System.Runtime.InteropServices;
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
public static unsafe partial class Shaders
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
            && !settings.DrawFragment.IsSet && !settings.Deferred.IsSet && !settings.Fragment2d.IsSet)
        {
            throw new ArgumentException(
                "A shader program needs a fragment shader to draw with, a pass to run over a "
                + "camera's picture, a compute shader to dispatch, a draw fragment shader to draw "
                + "on a camera with, or a 2D fragment shader to draw a 2D mesh with. Bevy's own "
                + "fragment shader reads a material laid out differently, so there is no default "
                + "to fall back on.",
                nameof(settings));
        }

        if (settings.Vertex2d.IsSet && !settings.Fragment2d.IsSet)
        {
            throw new ArgumentException(
                "A 2D vertex shader draws a 2D material with the 2D fragment shader beside it, so "
                + "it needs one.",
                nameof(settings));
        }

        if (settings.Deferred.IsSet && !settings.PrepassVertex.IsSet)
        {
            throw new ArgumentException(
                "A deferred stage needs a prepass vertex shader of the program's own as well, which "
                + "writes everything the deferred stage reads of the prepass (bcs::prepass_output), "
                + "whatever the mesh carries.",
                nameof(settings));
        }

        if (settings.DrawShadow.IsSet && !settings.DrawVertex.IsSet)
        {
            throw new ArgumentException(
                "A draw shadow stage is the fragment shader of geometry drawn on a camera, drawn "
                + "into shadow maps, so it needs the draw vertex and draw fragment shaders as well.",
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
                    Flags = settings.ComputeTarget == ShaderTarget.SpirV ? 1 : 0,
                    Deferred = Stage(settings.Deferred),
                    DrawShadow = Stage(settings.DrawShadow),
                    Vertex2d = Stage(settings.Vertex2d),
                    Fragment2d = Stage(settings.Fragment2d),
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
    /// Makes a material a program draws a 2D mesh with. Only valid inside a system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The program's <see cref="ShaderProgramSettings.Fragment2d"/> draws it, with its
    /// <see cref="ShaderProgramSettings.Vertex2d"/> where it has one. Its values are set by name
    /// on what this returns as a 3D material's are, starting at zero, its textures at a stand-in
    /// of the right shape. It goes on an entity with <see cref="Render2d.SetMaterial"/>, beside the
    /// 2D mesh <see cref="Render2d.SetMesh"/> gives it, and a 2D camera draws it as Bevy draws a
    /// <c>Material2d</c>.
    /// </para>
    /// </remarks>
    /// <param name="program">The program that draws it.</param>
    /// <param name="alpha">
    /// Which of Bevy's 2D passes draws it: the opaque one, the alpha-masked one, where the fragment
    /// shader discards what it leaves undrawn, as Bevy's own 2D materials do, or the blended one,
    /// which mixes it with what is behind by its alpha.
    /// </param>
    /// <param name="cutoff">
    /// The cutoff a masked material is made with, as Bevy's <c>AlphaMode2d::Mask</c> holds it.
    /// Bevy hands it to no shader, so a fragment shader that discards does it by a value of its
    /// own.
    /// </param>
    /// <exception cref="ArgumentException">No program was given.</exception>
    /// <exception cref="BevyNativeException">The program does not exist, or there is no renderer.</exception>
    public static ShaderMaterial CreateMaterial2d(ShaderProgram program, AlphaMode2d alpha = AlphaMode2d.Opaque, float cutoff = 0.5f)
    {
        RequireProgram(program, nameof(program));

        var key = Native.bcs_shader_material_2d_create(program.Id, (int)alpha, cutoff);

        ThrowIfNoProgram(key, program);
        Native.Check(key, $"making a 2D material drawn by shader program {program.Id}");
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
}
