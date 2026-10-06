namespace Bevy;

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

    /// <summary>
    /// What <see cref="Compute"/> is compiled to. <see cref="ShaderTarget.Wgsl"/> unless set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every other stage is WGSL, which Bevy reads on every backend and checks against the layout
    /// the bridge builds. A compute shader may need what WGSL cannot say, a ray query above all,
    /// since Slang writes ray queries only as SPIR-V. <see cref="ShaderTarget.SpirV"/> compiles it
    /// to SPIR-V and hands the binary to the driver untouched, which is how a shader importing
    /// <c>bcs_ray</c> traces rays against the scene Solari keeps.
    /// </para>
    /// <para>
    /// Nothing checks SPIR-V passed through this way before the GPU runs it. A shader reading
    /// past a buffer's end reads whatever is there rather than zero, and a declaration that does not
    /// match what the bridge binds is undefined behavior rather than an error. The layout is built
    /// from Slang's reflection, so what the shader declares is still set by name as it is for WGSL.
    /// The one difference is that a comparison sampler is bound as a plain one, since the reflection
    /// does not tell the two apart. On a backend other than Vulkan the SPIR-V is translated by naga
    /// instead, which works for ordinary compute and not for ray queries.
    /// </para>
    /// </remarks>
    public ShaderTarget ComputeTarget { get; init; }

    /// <summary>The stages that were set.</summary>
    internal IEnumerable<ShaderStage> Stages() =>
        new[] { Vertex, Fragment, PrepassVertex, PrepassFragment, Compute, Pass, DrawVertex, DrawFragment }
            .Where(stage => stage.IsSet);

    /// <summary>The stage a message names the program by.</summary>
    internal ShaderStage Main() =>
        Fragment.IsSet ? Fragment : Pass.IsSet ? Pass : Compute.IsSet ? Compute : DrawFragment;
}
