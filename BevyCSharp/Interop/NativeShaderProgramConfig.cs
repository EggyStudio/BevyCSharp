namespace Bevy.Interop;

/// <summary>Which Slang files a program is made of. Mirrors <c>BcsShaderProgramConfig</c>.</summary>
public unsafe struct NativeShaderProgramConfig
{
    /// <summary>The main pass's vertex shader.</summary>
    public NativeShaderStage Vertex;

    /// <summary>The main pass's fragment shader.</summary>
    public NativeShaderStage Fragment;

    /// <summary>The prepass's vertex shader.</summary>
    public NativeShaderStage PrepassVertex;

    /// <summary>The prepass's fragment shader.</summary>
    public NativeShaderStage PrepassFragment;

    /// <summary>A compute shader.</summary>
    public NativeShaderStage Compute;

    /// <summary>A full-screen pass over a camera's picture.</summary>
    public NativeShaderStage Pass;

    /// <summary>The defines, or null.</summary>
    public NativeShaderDefine* Defines;

    /// <summary>How many defines there are.</summary>
    public int DefineCount;

    /// <summary>The vertex shader of geometry drawn on a camera out of buffers.</summary>
    public NativeShaderStage DrawVertex;

    /// <summary>The fragment shader of the same.</summary>
    public NativeShaderStage DrawFragment;

    /// <summary>Bit zero compiles the compute stage to SPIR-V rather than WGSL.</summary>
    public int Flags;

    /// <summary>The fragment shader a material draws into Bevy's deferred buffers with.</summary>
    public NativeShaderStage Deferred;

    /// <summary>The fragment shader geometry drawn on a camera is drawn into shadow maps with.</summary>
    public NativeShaderStage DrawShadow;

    /// <summary>The vertex shader of a material drawn on a 2D mesh.</summary>
    public NativeShaderStage Vertex2d;

    /// <summary>The fragment shader of the same.</summary>
    public NativeShaderStage Fragment2d;

    /// <summary>The task shader of geometry drawn on a camera with mesh shaders.</summary>
    public NativeShaderStage DrawTask;

    /// <summary>The mesh shader of the same.</summary>
    public NativeShaderStage DrawMesh;
}
