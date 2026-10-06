namespace Bevy;

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
