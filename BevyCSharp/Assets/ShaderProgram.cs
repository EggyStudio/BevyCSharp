namespace Bevy;

using Bevy.Interop;

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
