namespace Bevy;

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
