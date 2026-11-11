using System.Reflection;

namespace Bevy;

// The library's own programs, compiled from the Slang files it carries, which the scene's distance
// field, global illumination, the sky and the look are built with. Each area carries its own files
// in its assembly and names the assembly it reads them from.
public static partial class Shaders
{
    /// <summary>
    /// One of the library's compute programs, compiled from its own copy of the kernel, which the field
    /// and global illumination's probes are built and traced with.
    /// </summary>
    // Named by its file, so its GPU time reads as shader field_splat and the like, and by its entry
    // point beside the file's name where it is not main, as shader clouds_shadows.
    internal static ShaderProgram KernelProgram(Assembly carrier, string file, ShaderTarget target = ShaderTarget.Wgsl, string entry = "main", IReadOnlyDictionary<string, ShaderDefine>? defines = null)
    {
        var named = entry == "main" ? file : $"{Path.GetFileNameWithoutExtension(file)}_{entry}.slang";
        return CreateProgram(new ShaderProgramSettings
        {
            Compute = new ShaderStage(named, entry) { Source = Kernel(carrier, file) },
            ComputeTarget = target,
            Defines = defines is null ? new(StringComparer.Ordinal) : new(defines, StringComparer.Ordinal),
        });
    }

    /// <summary>An instance of one of the library's programs that draws on a camera, its vertex and fragment shaders in one file.</summary>
    internal static ShaderInstance KernelDraw(Assembly carrier, string file)
    {
        var source = Kernel(carrier, file);
        return CreateInstance(CreateProgram(new ShaderProgramSettings
        {
            DrawVertex = new ShaderStage(file, "vertex") { Source = source },
            DrawFragment = new ShaderStage(file, "fragment") { Source = source },
        }));
    }

    /// <summary>An instance of one of the library's programs that runs over a camera's picture as a full-screen pass.</summary>
    internal static ShaderInstance KernelPass(Assembly carrier, string file) =>
        CreateInstance(CreateProgram(new ShaderProgramSettings { Pass = new ShaderStage(file) { Source = Kernel(carrier, file) } }));

    /// <summary>One of the library's Slang files, as the area's assembly carries it.</summary>
    private static string Kernel(Assembly carrier, string file)
    {
        using var stream = carrier.GetManifestResourceStream("Bevy.Kernels." + file)
            ?? throw new InvalidOperationException($"The library carries no {file}, which one of its programs is built with.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
